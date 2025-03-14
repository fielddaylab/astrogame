using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astro
{
    public class FocusState : SharedStateComponent
    {
        [NonSerialized] public RingBuffer<UIFocus> AllFocii = new RingBuffer<UIFocus>(8, RingBufferMode.Expand);
        [NonSerialized] public RingBuffer<UIFocus> ActiveFocii = new RingBuffer<UIFocus>(8, RingBufferMode.Expand);
        [NonSerialized] public UIFocus CurrentFocus = null;
        [NonSerialized] public bool FocusUpdated = false;
        public Graphic FocusOutline;

        protected override void OnEnable()
        {
            base.OnEnable();
            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, FocusableUtility.ClickEmptySpace);
        }
    }

    public static partial class FocusableUtility
    {
        public static void SetCurrentFocus(FocusState state, UIFocus focus)
        {
            if (focus == null && state.CurrentFocus == null)
            {
                // already focused on nothing
                return;
            }
            else if (!(focus == null || state.CurrentFocus == null) && state.CurrentFocus.TargetData.DisplayName.Equals(focus.TargetData.DisplayName)) {
                // already focused on this object
                return;
            } else if (PointsUtility.ReviewInProgress()) {
                return;
            }

            // TODO: anything that needs to happen to previous focus

            // Set new focus
            state.CurrentFocus = focus;
            state.FocusUpdated = true;
            var dataState = Find.State<DataPacketDistributionState>();
            var data = focus == null ? null : focus.TargetData;
            DataDistributionUtility.QueueConversion(dataState, data);

            ReferenceUtility.TryEnableIDSubmit(focus != null);
        }

        public static void ClickEmptySpace()
        {
            FocusState state = Find.State<FocusState>();
            SetCurrentFocus(state, null);
        }
    }

}