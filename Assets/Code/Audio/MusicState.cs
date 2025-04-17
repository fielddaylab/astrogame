using System;
using System.Collections;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUWT;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Audio;
using FieldDay.SharedState;
using Leaf.Runtime;
using UnityEngine;

namespace Astro.Audio {
    public sealed class MusicState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public AudioHandle MusicTrack;
        [NonSerialized] public string MusicTag = "music";
        [NonSerialized] public RingBuffer<StringHash32> TrackQueue = new RingBuffer<StringHash32>(2, RingBufferMode.Expand);
        [NonSerialized] public StringHash32 CurrentTrackId;

        public void OnDeregister() {
            Sfx.Stop(MusicTrack);
            Sfx.StopAllWithTag(MusicTag);
            CurrentTrackId = default;
         }

        public void OnRegister() { }
    }


    static public class MusicUtility {

        [LeafMember("QueueMusic")]
        static private void LeafQueueMusic(StringHash32 track) {
            MusicState state = Find.State<MusicState>();
            state.TrackQueue.PushBack(track);
        }

        [LeafMember("PlayMusic")]
        static public IEnumerator LeafPlayMusic(StringHash32 track, float fadeInTime = 0) {
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
        static private void StopMusic(float fadeOutTime = 0) {
            MusicState state = Find.State<MusicState>();
            Sfx.StopAllWithTag(state.MusicTag, fadeOutTime);

            state.MusicTrack = default;
            state.CurrentTrackId = default;
        }
    } 
}