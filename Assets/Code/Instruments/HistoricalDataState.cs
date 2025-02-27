using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astro {
    public class HistoricalDataState : SharedStateComponent, IRegistrationCallbacks {
        public HistoricalDataGraph InstrumentGraph;
        public DataDisplay DistanceDisplay;
        public Transform ModeKnob;
        public List<PatternMaterialPair> PatternMaterials;
        public Photometer ConnectedPhotometer;
        [NonSerialized] public bool ShowingParallax;
        [NonSerialized] public Routine KnobRoutine;

        public void OnDeregister() {
        }

        public void OnRegister() {
            HistoricalDataUtility.SetParallaxShowing(false, this);
        }
    }

    [Serializable]
    public struct PatternMaterialPair {
        public HistoricalPatternType Pattern;
        public Material Material;
    }
}