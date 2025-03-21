using System;
using System.Collections;
using System.Linq;
using BeauUtil;
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

        public void OnDeregister() {
            Sfx.Stop(MusicTrack);
            Sfx.StopAllWithTag(MusicTag);
         }

        public void OnRegister() { }
    }


    static public class MusicUtility {
        [LeafMember("PlayMusic")]
        static private IEnumerator LeafPlayMusic(StringHash32 track, float fadeInTime = 0) {
            MusicState state = Find.State<MusicState>();
            if (state.MusicTrack.IsValid) {
                StopMusic(fadeInTime);
                yield return fadeInTime;
            }

            state.MusicTrack = Sfx.Play(track); 
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
        }
    } 
}