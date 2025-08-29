using FieldDay.HID;
using FieldDay.Scripting;
using Leaf.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class PreludeMeteorActivation : ScriptActorComponent
    {
        public CursorHint CursorHint;

        private void Awake()
        {
            CursorHint.onPointerEnter.AddListener(OnEnter);
            CursorHint.onPointerExit.AddListener(OnExit);
        }

        private void OnEnter()
        {
            AstroGame.Events.Dispatch(GameEvents.MeteorAreaHighlighted);
        }

        private void OnExit()
        {
            AstroGame.Events.Dispatch(GameEvents.MeteorAreaUnhighlighted);
        }

        [LeafMember("ActivateMeteors")]
        public void LeafActiveMeteors()
        {
            this.gameObject.SetActive(true);
            AstroGame.Events.Dispatch(GameEvents.MeteorAreaAssigned);
        }
    }
}