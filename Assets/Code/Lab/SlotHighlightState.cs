using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class SlotHighlightState : SharedStateComponent {
        [Header("Consts")]
        public Material UnselectedCellMat;
        public Material AvailableCellMat;
        public Material SelectedCellMat;
        public Material SelectedInstrumentMat;
        public Material DimmedInstrumentMat;
    }
}