using System;
using System.Collections;
using System.IO;
using BeauData;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro.Save {
    public class SaveLoadState : SharedStateComponent {
        public string ServerURL;
        public Routine Operation;

        [NonSerialized] public int TicksToCommit;

        private void Awake() {
            OGD.Core.Configure(ServerURL, "ASTRO");
        }
    }

    static public class SaveUtility {
        [ConfigVar("Ticks Before Committing Save", 1, 50)] static public int TicksBeforeCommittingSave = 20;

        static public void Save(SaveSlot slot) {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, SaveRoutine(slot));
        }

        static public void Commit() {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, CommitSaveRoutine());
        }

        static public void Reload() {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return;
            }

            save.Operation = Routine.Start(save, ReloadRoutine(true));
        }

        static public Future LoadFromServer(string inUserId) {
            var save = Game.SharedState.Get<SaveLoadState>();
            if (save.Operation) {
                Log.Error("[SaveUtility] Save/load operation is ongoing");
                return Future.Failed();
            }

            Future future = new Future();
            save.Operation = Routine.Start(save, LoadFromServerRoutine(inUserId, future));
            return future;
        }

        static private IEnumerator SaveRoutine(SaveSlot slot) {
            yield return null;

            AstroGame.SaveBuffer.Write(slot);
            if (slot == SaveSlot.Main) {
                AstroGame.SaveBuffer.EncodeToBase64();

#if UNITY_EDITOR
                WriteToFileSystem();
#endif // UNITY_EDITOR

                if (!string.IsNullOrEmpty(AstroGame.SaveBuffer.SaveCode)) {
                    yield return WriteToRemoteSave();
                }
            } else {
                Find.State<SaveLoadState>().TicksToCommit = TicksBeforeCommittingSave;
            }
        }

        static private IEnumerator CommitSaveRoutine() {
            yield return null;

            if (AstroGame.SaveBuffer.TryCommitSave()) {
                AstroGame.SaveBuffer.EncodeToBase64();

#if UNITY_EDITOR
                WriteToFileSystem();
#endif // UNITY_EDITOR

                if (!string.IsNullOrEmpty(AstroGame.SaveBuffer.SaveCode)) {
                    yield return WriteToRemoteSave();
                }
            }
        }

#if UNITY_EDITOR
        static unsafe private void WriteToFileSystem() {
            var chars = AstroGame.SaveBuffer.GetCurrentBase64();
            Directory.CreateDirectory("Saves");
            string fileName = "Saves/" + DateTime.Now.ToFileTime().ToString() + ".bin";
            using (var str = File.Open(fileName, FileMode.Create)) {
                using (var stream = new StreamWriter(str)) {
                    var charsAsSys = new ReadOnlySpan<char>(chars.Ptr, chars.Length);
                    stream.Write(charsAsSys);
                    Log.Msg("[SaveUtility] Wrote save to local file '{0}'", fileName);
                }
            }
        }
#endif // UNITY_EDITOR

        static private IEnumerator WriteToRemoteSave() {
            // get save data
            var saveData = AstroGame.SaveBuffer.GetCurrentBase64AsString();

            // try to send save data to server - just copied from aqualab

            string profileName = AstroGame.SaveBuffer.SaveCode;
            int attempts = (int)(8 + 1);
            int retryCount = 0;
            while (attempts > 0) {
                using (var future = Future.Create())
                using (var saveRequest = OGD.GameState.PushState(profileName, saveData, future.Complete, (r) => future.Fail(r), retryCount)) {
                    yield return future;

                    if (future.IsComplete()) {
                         Log.Msg("[SaveUtility] Saved to server!");
                        break;
                    } else {
                        attempts--;
                        Log.Warn("[SaveUtility] Failed to save to server: {0}", future.GetFailure().Object);
                        if (attempts > 0) {
                            Log.Warn("[SaveUtility] Retrying server save...", attempts);
                            yield return 1;
                            ++retryCount;
                        } else {
                            Log.Error("[SaveUtility] Server save failed after {0} attempts", 8 + 1);
                        }
                    }
                }
            }
        }

        static private IEnumerator ReloadRoutine(bool waitForCutsceneClose) {
            if (waitForCutsceneClose) {
                /*
                var scriptRuntime = ScriptUtility.Runtime;
                while(scriptRuntime.Cutscene.IsRunning()) {
                    yield return null;
                }
                */
            }
            yield return null;
            if (AstroGame.SaveBuffer.HasSave) {
                AstroGame.SaveBuffer.Read();
            }
            Game.Scenes.ReloadMainScene();
        }

        static private IEnumerator LoadFromServerRoutine(string inUserCode, Future response) {

            using (var future = Future.Create<string>())
            using (var request = OGD.GameState.RequestLatestState(inUserCode, future.Complete, (r) => future.Fail(r), 0)) {
                yield return future;

                if (future.IsComplete()) {
                    bool bSuccess;
                    using (Profiling.Time("reading save data from server")) {
                        bSuccess = AstroGame.SaveBuffer.DecodeFromBase64(future.Get());
                        if (bSuccess) {
                            bSuccess = AstroGame.SaveBuffer.Read();
                        }
                    }

                    if (!bSuccess) {
                        UnityEngine.Debug.LogErrorFormat("[SaveUtility] Server profile '{0}' could not be read...", inUserCode);
                        response.Fail();
                    } else {
                        response.Complete();
                    }
                } else {
                    UnityEngine.Debug.LogErrorFormat("[SaveUtility] Failed to find profile on server: {0}", future.GetFailure());
                    response.Fail(future.GetFailure());
                }
            }
        }

        [DebugMenuFactory]
        static private DMInfo SaveDebugMenu() {
            DMInfo info = new DMInfo("Save", 4);
            info.AddButton("Write Current to Memory", () => SaveUtility.Save(SaveSlot.Main));
            info.AddButton("Read Current from Memory", () => {
                SaveUtility.Reload();
            }, () => AstroGame.SaveBuffer.HasSave);
            return info;
        }
    }
}