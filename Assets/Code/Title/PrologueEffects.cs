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
    public sealed class PrologueEffects : SharedStateComponent, IScenePreload, IRegistrationCallbacks {
        void IRegistrationCallbacks.OnDeregister() {
            ScriptUtility.DeregisterAllSignalsForContext(this);
        }

        void IRegistrationCallbacks.OnRegister() {

        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ScriptUtility.RegisterForSignal("FadeOutCityAmb", OnFadeOutCitySignal);
            ScriptUtility.RegisterForSignal("WalkForward", OnWalkForwardSignal);
            return null;
        }

        private void OnFadeOutCitySignal() {
            TitleAmbienceConfigurations titleAmb = Find.State<TitleAmbienceConfigurations>();
            Sfx.Stop(titleAmb.CityHandle, 4);
            Sfx.SetVolume(titleAmb.BaseHandle, 0.5f, 4);
        }

        private void OnWalkForwardSignal() {
            ViewNavUtility.MoveToNode(Find.State<ViewState>(), ViewNavUtility.GetNodeById("NewForward"), new TweenSettings(3, Curve.Smooth));
        }
    }
}