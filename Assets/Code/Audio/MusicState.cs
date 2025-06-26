using BeauUtil;
using BeauUtil.Debugger;
using BeauUWT;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using UnityEngine;

namespace Astro.Audio {
    public sealed class MusicState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public StringHash32 CurrentTrackId;
        [NonSerialized] public AudioHandle MusicTrack;
        [NonSerialized] public string MusicTag = "music";

        [NonSerialized] public QueuedTrack Queued;
        [NonSerialized] public State CurrentState;

        public void OnDeregister() {
            Sfx.Stop(MusicTrack);
            Sfx.StopAllWithTag(MusicTag);
            CurrentTrackId = default;

            SceneMgr.DeregisterDebugLoadCallback(OnSceneDebugLoad);
        }

        public void OnRegister() {
            SceneMgr.RegisterDebugLoadCallback(OnSceneDebugLoad);
        }

        private void OnSceneDebugLoad() {
            Sfx.Stop(MusicTrack);
            Sfx.StopAllWithTag(MusicTag);
            CurrentTrackId = default;
            Queued = default;
            CurrentState = State.Stopped;
        }

        public struct QueuedTrack {
            public StringHash32 TrackId;
            public float FadeIn;
        }

        public enum State {
            Stopped,
            Playing,
            FadeOut
        }
    }


    static public class MusicUtility {
        [LeafMember("PlayMusic")]
        static public void PlayMusic(StringHash32 track, float fadeInTime = 0) {
            MusicState state = Find.State<MusicState>();
            state.Queued = new MusicState.QueuedTrack() {
                TrackId = track,
                FadeIn = fadeInTime
            };
        }

        [LeafMember("StopMusic")]
        static public void StopMusic(float fadeOutTime = 0) {
            MusicState state = Find.State<MusicState>();
            if (state.MusicTrack.IsValid) {
                Sfx.Stop(state.MusicTrack, fadeOutTime);
                state.CurrentState = MusicState.State.FadeOut;
            }
        }
    } 
}