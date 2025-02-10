using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.SharedState;
using System;

namespace Astro
{
    public class LabInteractableState : SharedStateComponent
    {
        [NonSerialized] public LabInteractable CurrInteractable;
        [NonSerialized] public Vector2 StartMousePos;
        [NonSerialized] public Vector2 CurrMousePos;
    }
}