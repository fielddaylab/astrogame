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

    [NonSerialized] public RingBuffer<Transform> ActiveHighlights = new RingBuffer<Transform>(8, RingBufferMode.Expand);

    #region Registration

    private Action setOpenModeStarted;
    private Action setOpenModeEnded;


    public void OnRegister() {
        setOpenModeStarted = () => { OpenModeStarted = true; };
        setOpenModeEnded = () => { OpenModeEnded = true; };

        Game.Events.Register(GameEvents.StartOpenMode, setOpenModeStarted);
        Game.Events.Register(GameEvents.StopOpenMode, setOpenModeEnded);
    }

    public void OnDeregister() {
        Game.Events.Deregister(GameEvents.StartOpenMode, setOpenModeStarted);
        Game.Events.Deregister(GameEvents.StopOpenMode, setOpenModeEnded);
    }

    #endregion // Registration
}
