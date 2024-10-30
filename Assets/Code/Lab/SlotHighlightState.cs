using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SlotHighlightState : SharedStateComponent
    {
        [Header("Consts")]
        public Material UnselectedCellMat;
        public Material SelectedCellMat;
    }
}