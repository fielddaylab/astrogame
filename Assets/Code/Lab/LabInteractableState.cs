using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.SharedState;

namespace Astro
{
    public class LabInteractableState : SharedStateComponent
    {
        public LabInteractable CurrInteractable;
        public Vector2 StartMousePos;
        public Vector2 CurrMousePos;
    }
}