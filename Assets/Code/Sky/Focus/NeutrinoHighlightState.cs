using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.SharedState;
using Astro;
using System;
using BeauUtil;

public class NeutrinoHighlightState : SharedStateComponent, IRegistrationCallbacks
{
    [NonSerialized] public bool OpenModeStarted;
    [NonSerialized] public bool OpenModeEnded;

    // TODO: pools
    [NonSerialized] public RingBuffer<RectTransform> ActiveHighlights = new RingBuffer<RectTransform>(8, RingBufferMode.Expand);

    #region Registration

    public void OnDeregister()
    {
    }

    public void OnRegister()
    {
        Game.Events.Register(GameEvents.StartOpenMode, () => {
            OpenModeStarted = true;
        });
        Game.Events.Register(GameEvents.StopOpenMode, () => {
            OpenModeEnded = true;
        });
    }

    #endregion // Registration
}
