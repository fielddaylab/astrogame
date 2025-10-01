using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleAmbienceConfigurations : SharedStateComponent, IScenePreload, IRegistrationCallbacks {
        [AudioEvent] public StringHash32 BaseLayer;
        public float TransitionDuration = 1;

        [Header("New Game")]
        [AudioEvent] public StringHash32 CityLayer;

        [Header("Continue Game")]
        [AudioEvent] public StringHash32[] ContinueRandom;

        [NonSerialized] public AudioHandle BaseHandle;
        [NonSerialized] public AudioHandle CityHandle;
        [NonSerialized] public Routine ContinueGameRoutine;

        void IRegistrationCallbacks.OnRegister() {
            
        }

        void IRegistrationCallbacks.OnDeregister() {
            Sfx.Stop(BaseHandle);
            Sfx.Stop(CityHandle);
            Game.Events.DeregisterAllForContext(this);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            AstroGame.Events.Register<ViewNode>(ViewNavUtility.Events.NodeLoaded, OnNodeLoading);
            Game.Scenes.QueueOnLoad(OnSceneEnabled);
            return null;
        }

        private void OnSceneEnabled() {
            BaseHandle = Sfx.Play(BaseLayer);
        }

        private IEnumerator ContinueGameRandomOneshots() {
            yield return RNG.Instance.NextFloat(2, 4);
            while(true) {
                Sfx.Play(RNG.Instance.Choose(ContinueRandom));
                yield return RNG.Instance.NextFloat(8, 20);
            }
        }

        private void OnNodeLoading(ViewNode node) {
            StringHash32 nodeId = node.Id;

            if (nodeId == "Title") {
                Sfx.StopAllWithTag("ContinueGameGroup");
                Sfx.StopAllWithTag("NewGameGroup");
                Sfx.Stop(CityHandle, TransitionDuration);
                ContinueGameRoutine.Stop();
                Sfx.SetVolume(BaseHandle, 1, TransitionDuration);
                CityHandle = default;
            } else if (nodeId == "New") {
                CityHandle = Sfx.Play(CityLayer, new SfxPlayArgs() {
                    Pitch = 1,
                    Volume = 0,
                    Delay = 0,
                    Pan = 0
                });
                Sfx.SetVolume(CityHandle, 1, TransitionDuration);
            } else if (nodeId == "Continue") {
                Sfx.SetVolume(BaseHandle, 0.6f, TransitionDuration);
                ContinueGameRoutine.Replace(this, ContinueGameRandomOneshots());
            } else if (nodeId == "FreePlay") {
                Sfx.SetVolume(BaseHandle, 0.2f, TransitionDuration);
            }
        }
    }
}