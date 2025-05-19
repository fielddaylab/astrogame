using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 10000, AstroGame.InteractUpdateMask)]
    public sealed class ViewNavSystem : SharedStateSystemBehaviour<ViewState, InputState> {
        public override void ProcessWork(float deltaTime) {
            if (m_StateA.ActiveTransitionRoutine || !m_StateA.ActiveNode) {
                return;
            }

            ViewLink backLink = m_StateA.ActiveNode.BackLink;
            if (backLink) {
                if (Game.Input.IsMousePressed(MouseButton.Right)) {
                    ViewNavUtility.MoveByLink(m_StateA, backLink);
                }
            }
        }
    }
}