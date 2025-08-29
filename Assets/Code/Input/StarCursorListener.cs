using FieldDay.HID;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class StarCursorListener : MonoBehaviour
    {
        [SerializeField] private CursorHint CursorHint;
        [SerializeField] private UIFocus Focus;

        private void Awake()
        {
            CursorHint.onPointerEnter.AddListener(OnEnter);
            CursorHint.onPointerExit.AddListener(OnExit);
        }

        private void OnEnter()
        {
            AstroGame.Events.Dispatch(GameEvents.HoverStar, Focus.TargetData.DisplayName);
        }

        private void OnExit()
        {

        }
    }
}