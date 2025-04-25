using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Astro.Reference;

namespace Astro
{
    public class FocusState : SharedStateComponent
    {
        [NonSerialized] public RingBuffer<UIFocus> ActiveFocii = new RingBuffer<UIFocus>(64, RingBufferMode.Expand);
        [NonSerialized] public UIFocusPackedData[] ActiveFociiPacked = new UIFocusPackedData[128];
        [NonSerialized] public BitSet256 ActiveFociiVisibleBits;

        [NonSerialized] public UIFocus CurrentFocus = null;
        [NonSerialized] public bool FocusUpdated = false;
        [NonSerialized] public bool MonitorInputActive = false;

        public SpriteRenderer FocusOutline;

        private Action setMonitorInputActive;
        private Action setMonitorInputInactive;

        protected override void OnEnable() {
            setMonitorInputActive = () => { MonitorInputActive = true; };
            setMonitorInputInactive = () => { MonitorInputActive = false; };

            base.OnEnable();
            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, FocusableUtility.ClickEmptySpace);

            Game.Events.Register(GameEvents.StartNeutrinoNavigation, setMonitorInputInactive);
            Game.Events.Register(GameEvents.StopNeutrinoNavigation, setMonitorInputActive);

            Game.Events.Register(GameEvents.StartPuzzleNavigation, setMonitorInputInactive);
            Game.Events.Register(GameEvents.StopPuzzleNavigation, setMonitorInputActive);

            Game.Events.Register(GameEvents.LockMonitorFocus, setMonitorInputInactive);
            Game.Events.Register(GameEvents.UnlockMonitorFocus, setMonitorInputActive);
        }

        protected override void OnDisable() {
            base.OnDisable();

            if (Game.IsShuttingDown) {
                return;
            }

            Game.Events.Deregister(GameEvents.MonitorEmptySpaceClicked, FocusableUtility.ClickEmptySpace);
            Game.Events.Deregister(GameEvents.StartNeutrinoNavigation, setMonitorInputInactive);
            Game.Events.Deregister(GameEvents.StopNeutrinoNavigation, setMonitorInputActive);
            Game.Events.Deregister(GameEvents.StartPuzzleNavigation, setMonitorInputInactive);
            Game.Events.Deregister(GameEvents.StopPuzzleNavigation, setMonitorInputActive);
            Game.Events.Deregister(GameEvents.LockMonitorFocus, setMonitorInputInactive);
            Game.Events.Deregister(GameEvents.UnlockMonitorFocus, setMonitorInputActive);

        }
    }

    public static partial class FocusableUtility {
        public static void SetCurrentFocus(FocusState state, UIFocus focus) {
            if (focus == null && state.CurrentFocus == null) {
                // already focused on nothing
                return;
            } else if (!(focus == null || state.CurrentFocus == null) && state.CurrentFocus.TargetData.DisplayName.Equals(focus.TargetData.DisplayName)) {
                // already focused on this object
                return;
            } else if (ReviewUtility.ReviewInProgress()) {
                return;
            } else if (!state.MonitorInputActive) {
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

            // Scripting
            if(state.CurrentFocus == null) return;

            if (IsCurrentFocusInNeutrinoEvent()) {
                ScriptUtility.Trigger(ScriptEvents.OnNeutrinoStarSelected);
            } else { 
                using (var table = TempVarTable.Alloc()) {
                    table.Set("starName", focus.TargetData.DisplayName);
                    ScriptUtility.Trigger(ScriptEvents.OnStarSelected, table);
                }
            }
        }

        public static bool IsCurrentFocusInNeutrinoEvent() {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();
            DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[state.DayIndex]);

            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            return Array.IndexOf(day.NeutrinoEvent.RelevantObjectIds, focus.TargetData.AssetId) >= 0;
        }

        public static void ClickEmptySpace()
        {
            FocusState state = Find.State<FocusState>();
            SetCurrentFocus(state, null);
        }
    }

}