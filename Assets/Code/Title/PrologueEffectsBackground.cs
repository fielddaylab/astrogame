using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class PrologueEffectsBackground : MonoBehaviour, IScenePreload {
        public SpriteRenderer Horizon;
        public float Delay;
        public float Duration;

        private void OnDestroy() {
            ScriptUtility.DeregisterAllSignalsForContext(this);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ScriptUtility.RegisterForSignal("FadeOutHorizon", OnFadeOutHorizon);
            return null;
        }

        private void OnFadeOutHorizon() {
            Routine.Start(this, Horizon.FadeTo(0, Duration).DelayBy(Delay));
        }
    }
}