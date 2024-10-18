using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Data;
using UnityEngine;

namespace FieldDay.Vox {
    [DisallowMultipleComponent]
    public class VoxEmitter : BatchedComponent, IEditorOnlyData, IRegistrationCallbacks {
        public SerializedHash32 CharacterId;
        [AudioEventRef] public StringHash32 DefaultPlaybackEvent;
        [Required] public AudioSource Player;

        [NonSerialized] internal VoxPriority PlayingPriority;
        [NonSerialized] internal AudioHandle AudioHandle;
        [NonSerialized] internal VoxRequestHandle RequestHandle;

        public CastableEvent<VoxRequestHandle, StringHash32> OnStartSpeaking = new CastableEvent<VoxRequestHandle, StringHash32>();
        public CastableEvent<VoxRequestHandle, StringHash32> OnFinishSpeaking = new CastableEvent<VoxRequestHandle, StringHash32>();

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            if (!CharacterId.IsEmpty) {
                if (VoxUtility.DB.EmitterMap.TryGetValue(CharacterId, out VoxEmitter existing)) {
                    if (existing != this) {
                        Log.Warn("[VoxEmitter] Multiple emitters with id '{0}'", CharacterId);
                    }
                } else {
                    VoxUtility.DB.EmitterMap.Add(CharacterId, this);
                }
            }
        }

        void IRegistrationCallbacks.OnDeregister() {
            if (Game.IsShuttingDown) {
                return;
            }

            VoxUtility.Stop(this);

            if (!CharacterId.IsEmpty) {
                if (VoxUtility.DB.EmitterMap.TryGetValue(CharacterId, out VoxEmitter existing) && existing == this) {
                    VoxUtility.DB.EmitterMap.Remove(CharacterId);
                }
            }
        }

        #endregion // IRegistrationCallbacks

#if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorData(bool isDevelopmentBuild) {
            EditorOnlyData.Strip(ref CharacterId);
        }

#endif // UNITY_EDITOR
    }

    static public partial class VoxUtility {
        /// <summary>
        /// Stops emitting from the given VoxEmitter.
        /// </summary>
        static public void Stop(VoxEmitter emitter) {
            Assert.NotNull(emitter);
            KillVoxRequest(ref emitter.RequestHandle);
        }
    }
}