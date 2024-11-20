using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Components;
using UnityEngine.UI;

namespace Astro {
    public class FocusableObject : BatchedComponent
    {
        [HideInInspector] public bool BecameVisible;
        [HideInInspector] public bool BecameInvisible;

        #region Unity Callbacks

        private void OnBecameVisible()
        {
            BecameVisible = true;
        }

        private void OnBecameInvisible()
        {
            BecameInvisible = true;
        }

        #endregion // Unity Callbacks
    }
}
