using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using UnityEngine;
using FieldDay.Scenes;
using System.Collections.Generic;
using BeauUtil;

namespace Astro
{
    public class FocusPools : SharedStateComponent, IRegistrationCallbacks, IScenePreload
    {
        #region Types

        [Serializable] public class FocusPool : SerializablePool<UIFocus> { }
        [Serializable] public class NeutrinoHighlightPool : SerializablePool<Transform> { }

        #endregion // Types

        [Header("Focus")]
        public FocusPool Focii;
        public NeutrinoHighlightPool NeutrinoHighlights;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister()
        {
            Focii.TryInitialize(PoolRoot);
            NeutrinoHighlights.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister()
        {

        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Focii.TryInitialize(PoolRoot, null, 0);
            for(int i = 1; i <= 72; i++) {
                Focii.Prewarm(i);
                yield return null;
            }

            NeutrinoHighlights.TryInitialize(PoolRoot, null, 0);
            for (int i = 1; i <= 10; i++) {
                NeutrinoHighlights.Prewarm(i);
                yield return null;
            }
        }
    }
}