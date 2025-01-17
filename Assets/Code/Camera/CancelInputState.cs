using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    /// <summary>
    /// Tracks when nothing in particular is clicked so that various systems may behave accordingly,
    /// such as by cancelling focus.
    /// </summary>
    public class CancelInputState : SharedStateComponent
    {
        [HideInInspector] public bool ClickedThisFrame;
        [HideInInspector] public bool SlotClicked;
    }
}