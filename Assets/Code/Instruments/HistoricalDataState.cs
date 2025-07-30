using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astro {
    public class HistoricalDataState : SharedStateComponent, IRegistrationCallbacks {
        public DataDisplay DistanceDisplay;

        [Header("Mode Toggle")]
        public Transform ModeSwitch;
        public Quaternion ModeRotDefault;
        public Quaternion ModeRotOn;

        public MeshRenderer InstrumentMesh;

        [Header("External")]
        public Photometer ConnectedPhotometer;

        [NonSerialized] public bool SendingAbsMag;
        [NonSerialized] public Routine KnobRoutine;


        public void OnRegister() {
            ParallaxDataUtility.SetInstrumentMode(false, this, true, false);
        }

        public void OnDeregister() { }
    }
}