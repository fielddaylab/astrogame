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
        [NonSerialized] public AudioHandle MusicTrack;
        [NonSerialized] public string MusicTag = "music";
        [NonSerialized] public RingBuffer<QueuedTrack> TrackQueue = new RingBuffer<QueuedTrack>(2, RingBufferMode.Expand);
        [NonSerialized] public StringHash32 CurrentTrackId;

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
        }

        public struct QueuedTrack {
            public StringHash32 TrackId;
            public float Delay;
        }
    }


    static public class MusicUtility {

        [LeafMember("QueueMusic")]
        static public void QueueMusic(StringHash32 track, float delay = 0) {
            MusicState state = Find.State<MusicState>();
            state.TrackQueue.PushBack(new MusicState.QueuedTrack() {
                TrackId = track,
                Delay = delay
            });
        }

        [LeafMember("PlayMusic")]
        static public IEnumerator PlayMusic(StringHash32 track, float fadeInTime = 0) {
            MusicState state = Find.State<MusicState>();
            if (state.CurrentTrackId == track) {
                yield break;
            }

            if (state.MusicTrack.IsValid) {
                StopMusic(fadeInTime);
                yield return fadeInTime;
            }

            state.MusicTrack = Sfx.Play(track);
            state.CurrentTrackId = track;
            Sfx.OverrideTag(state.MusicTrack, state.MusicTag);
            if (fadeInTime > 0) {
                Sfx.SetVolume(state.MusicTrack, 0);
                Sfx.SetVolume(state.MusicTrack, 1, fadeInTime);
            }
        }

        [LeafMember("StopMusic")]
        static public void StopMusic(float fadeOutTime = 0) {
            MusicState state = Find.State<MusicState>();
            Sfx.StopAllWithTag(state.MusicTag, fadeOutTime);

            state.MusicTrack = default;
            state.CurrentTrackId = default;
        }
    } 
}