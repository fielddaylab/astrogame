using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Astro {
    public sealed class RenderAtlasDebug : BatchedComponent, IRegistrationCallbacks {
        [Header("Texture Destination")]
        [Required] public RenderAtlasUpdateState Group;
        [Required] public RawImage OutputPrimary;

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            // TODO: deregister
        }

        void IRegistrationCallbacks.OnRegister() {
            OutputPrimary.texture = Group.Atlas.Texture;
        }

        #endregion // IRegistrationCallbacks
    }
}