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
        [AudioEventRef] public StringHash32 BaseLayer;
        public float TransitionDuration = 1;

        [Header("New Game")]
        [AudioEventRef] public StringHash32 CityLayer;

        [Header("Continue Game")]
        [AudioEventRef] public StringHash32[] ContinueRandom;

        [NonSerialized] public AudioHandle BaseHandle;
        [NonSerialized] public AudioHandle CityHandle;
        [NonSerialized] public Routine ContinueGameRoutine;

        void IRegistrationCallbacks.OnDeregister() {
            Game.Events.DeregisterAllForContext(this);
            ScriptUtility.DeregisterAllSignalsForContext(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            AstroGame.Events.Register<ViewNode>(ViewNavUtility.Events.NodeLoaded, OnNodeLoading);
            Game.Scenes.QueueOnLoad(OnSceneEnabled);
            ScriptUtility.RegisterForSignal("FadeOutCityAmb", OnFadeOutCitySignal);
            return null;
        }

        private void OnSceneEnabled() {
            BaseHandle = Sfx.Play(BaseLayer);
        }

        private void OnFadeOutCitySignal() {
            Sfx.Stop(CityHandle, 4);
            Sfx.SetVolume(BaseHandle, 0.5f, 4);
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
                ContinueGameRoutine.Replace(this, ContinueGameRandomOneshots());
            }
        }
    }
}