using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using UnityEngine;

namespace Astro
{
    public class FocusPools : SharedStateComponent, IRegistrationCallbacks
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
    }
}