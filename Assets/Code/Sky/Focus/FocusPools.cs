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

        #endregion // Types

        [Header("Focus")]
        public FocusPool Focii;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister()
        {
            Focii.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister()
        {

        }
    }
}