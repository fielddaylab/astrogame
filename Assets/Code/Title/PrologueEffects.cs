using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class PrologueEffects : MonoBehaviour, IScenePreload {
        public ParticleSystem[] Pops;

        [NonSerialized] public int PopIndex;

        private void OnDestroy() {
            ScriptUtility.DeregisterAllSignalsForContext(this);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ScriptUtility.RegisterForSignal("FadeOutCityAmb", OnFadeOutCitySignal);
            ScriptUtility.RegisterForSignal("WalkForward", OnWalkForwardSignal);
            ScriptUtility.RegisterForSignal("TransformerPop", OnTransformerPop);
            ScriptUtility.RegisterForSignal("DisableFreeLook", OnDisableFreeLook);
            ScriptUtility.RegisterForSignal("DisconnectView", OnDisconnectView);
            return null;
        }

        private void OnFadeOutCitySignal() {
            TitleAmbienceConfigurations titleAmb = Find.State<TitleAmbienceConfigurations>();
            Sfx.Stop(titleAmb.CityHandle, 4);
            Sfx.SetVolume(titleAmb.BaseHandle, 0.5f, 4);
        }

        private void OnWalkForwardSignal() {
            ViewState viewState = Find.State<ViewState>();
            ViewNavUtility.MoveToNode(viewState, ViewNavUtility.GetNodeById("NewForward"), new TweenSettings(3, Curve.Smooth));
        }

        private void OnDisableFreeLook() {
            ViewFreeLook freeLook = ViewNavUtility.GetNodeById("NewForward").GetComponent<ViewFreeLook>();
            freeLook.CutsceneScale = freeLook.DefaultScale = 0;
        }

        private void OnDisconnectView() {
            ViewState viewState = Find.State<ViewState>();
            CameraDrift cameraDrift = viewState.Camera.Camera.GetComponent<CameraDrift>();
            Routine.Start(this, Tween.OneToZero((f) => cameraDrift.Scale = f, 0.5f));
        }

        private void OnTransformerPop() {
            Pops[PopIndex++].Play();
            Sfx.Play("Prelude.TransformerPop");
        }
    }
}