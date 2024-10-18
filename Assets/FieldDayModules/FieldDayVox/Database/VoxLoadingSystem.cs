#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay.Assets;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay.Vox {
    [SysUpdate(GameLoopPhaseMask.PreUpdate | GameLoopPhaseMask.UnscaledUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 1000, AllowExecutionDuringLoad = true)]
    internal class VoxLoadingSystem : ISystem {
        public bool HasWork() {
            return VoxUtility.DB != null;
        }

        public void ProcessWork(float deltaTime) {
            VoxDatabase db = VoxUtility.DB;
            bool didWork = HandleLoading(db);
            if (!didWork) {
                HandleUnloading(db);
            }
        }

        static private bool HandleUnloading(VoxDatabase db) {
            if (db.LineCodeUnloadQueue.TryPopFront(out StringHash32 lineCode)) {
                if (!db.FileEntryMap.TryGetValue(lineCode, out var entry)) {
                    return false;
                }

                if (!db.LoadedFileDataMap.TryGetValue(entry.PathHash, out var data)) {
                    if (db.CurrentLoadFileId == entry.PathHash) {
                        Log.Warn("[VoxLoadingSystem] Clip '{0}' load interrupted due to unload request", entry.Path);
                        db.CurrentLoad.Abort();
                        db.CurrentLoad.Dispose();
                        db.CurrentLoad = null;
                        db.CurrentLoadFileId = default;
                        return true;
                    }
                    return false;
                }

                AssetUtility.ManualUnload(data.Clip);
                db.LoadedFileDataMap.Remove(entry.PathHash);
                Log.Msg("[VoxLoadingSystem] Clip '{0}' unloaded", entry.Path);
                return true;
            }

            while(db.FileUnloadQueue.TryPopFront(out VoxFileData directUnload)) {
                if (directUnload.Clip != null) {
                    string clipName = directUnload.Clip.name;
                    AssetUtility.ManualUnload(directUnload.Clip);
                    Log.Msg("[VoxLoadingSystem] Clip '{0}' unloaded directly", clipName);
                    return true;
                }
            }

            return false;
        }

        static private bool HandleLoading(VoxDatabase db) {
            bool isRunningLoad = db.CurrentLoad != null;
            bool didWork = false;
            if (isRunningLoad) {
                if (db.CurrentLoad.isDone) {

                    if (db.CurrentLoad.result == UnityWebRequest.Result.Success) {
                        AudioClip clip = ((DownloadHandlerAudioClip) db.CurrentLoad.downloadHandler).audioClip;
#if DEVELOPMENT
                        clip.name = db.CurrentLoadLineCode.ToDebugString();
#endif // DEVELOPMENT
                        db.LoadedFileDataMap.Add(db.CurrentLoadFileId, new VoxFileData() {
                            Clip = clip
                        });
                        Log.Msg("[VoxLoadingSystem] Loaded clip '{0}'", clip.name);
                    } else {
                        Log.Error("[VoxLoadingSystem] Could not load clip '{0}' due to error {1} - '{2}'", db.CurrentLoad.url, db.CurrentLoad.result, db.CurrentLoad.error);
                        db.LoadedFileDataMap.Add(db.CurrentLoadFileId, new VoxFileData() {
                            Clip = null
                        });
                    }

                    db.CurrentLoad.Dispose();
                    db.CurrentLoad = null;
                    db.CurrentLoadFileId = default;
                    db.CurrentLoadLineCode = default;

                    isRunningLoad = false;
                    didWork = true;
                }
            }

            while(!isRunningLoad && db.LineCodeLoadQueue.TryPopFront(out StringHash32 lineCode)) {
                if (!VoxUtility.TryResolveEntryForLineCode(db, lineCode, out VoxFileEntry entry)) {
                    continue;
                }

                if (db.LoadedFileDataMap.ContainsKey(entry.PathHash)) {
                    continue;
                }

                Uri uri = new Uri(Streaming.ResolveAddressToURL(entry.Path));
                Log.Msg("[VoxLoadingSystem] Beginning load of clip '{0}' at path '{1}'", lineCode.ToDebugString(), uri.ToString());

                db.CurrentLoadLineCode = lineCode;
                db.CurrentLoadFileId = entry.PathHash;
                db.CurrentLoad = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET, new DownloadHandlerAudioClip(uri, AudioType.UNKNOWN), null);
                db.CurrentLoad.SendWebRequest();
                didWork = true;
                isRunningLoad = true;
            }

            return didWork;
        }

        public void Initialize() {
        }

        public void Shutdown() {
        }
    }
}