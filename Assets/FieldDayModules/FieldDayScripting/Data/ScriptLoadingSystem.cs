using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Systems;
using Leaf;

namespace FieldDay.Scripting {
    [SysUpdate(GameLoopPhaseMask.PreUpdate | GameLoopPhaseMask.LateUpdate, AllowExecutionDuringLoad = true)]
    internal class ScriptLoadingSystem : ISystem {
        public void Initialize() { }
        public void Shutdown() { }

        public bool HasWork() {
            return ScriptUtility.DB != null;
        }

        public void ProcessWork(float dt) {
            if (HandleCurrentLoad(ScriptUtility.DB)) {
                return;
            }

            if (StartLoadingOne(ScriptUtility.DB)) {
                return;
            }

            UnloadScripts(ScriptUtility.DB);
        }

        static private bool HandleCurrentLoad(ScriptDatabase db) {
            ref var req = ref db.CurrentLoadRequest;
            if (req.Asset == null) {
                return false;
            }

            if (req.ParseHandle.IsRunning()) {
                return true;
            }

            ScriptDBUtility.RegisterPackage(db, req.Package, req.Asset);
            Log.Msg("[ScriptLoadingSystem] Finished loading script '{0}'", req.Asset.name);

            req = default;
            return true;
        }

        static private bool StartLoadingOne(ScriptDatabase db) {
            if (db.CurrentLoadRequest.Handle.Id != 0) {
                return false;
            }

            if (db.LoadQueue.TryPopFront(out db.CurrentLoadRequest)) {
                ref var req = ref db.CurrentLoadRequest;
                Log.Msg("[ScriptLoadingSystem] Loading script '{0}'...", req.Asset.name);
                req.Package = LeafAsset.CompileAsync(req.Asset, ScriptNodePackage.Parser.Instance, out IEnumerator loader);
                req.ParseHandle = Async.Schedule(loader, AsyncFlags.HighPriority);
                req.Package.SetActive(false);
                req.Package.m_LoadId = req.Handle;
                db.LoadedHandleMap[req.Handle.Index] = req.Package;
                return true;
            }

            return false;
        }

        static private void UnloadScripts(ScriptDatabase db) {
            for (int i = db.UnloadQueue.Count - 1; i >= 0; i--) {
                UniqueId16 loadId = db.UnloadQueue[i];
                if (loadId.Id == 0 || !db.HandleGenerator.IsValid(loadId)) {
                    db.UnloadQueue.FastRemoveAt(i);
                    continue;
                }

                ScriptNodePackage package = db.LoadedHandleMap[loadId.Index];
                if (package.IsReferenced()) {
                    continue;
                }

                if (ScriptDBUtility.CancelCurrentLoad(db, loadId)) {
                    return;
                }

                ScriptDBUtility.DeregisterPackage(db, package);
                package.Clear();
                db.UnloadQueue.FastRemoveAt(i);
                db.HandleGenerator.Free(loadId);
                db.LoadedHandleMap[loadId.Index] = null;
                return;
            }
        }
    }
}