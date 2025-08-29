using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using UnityEngine;
using Astro.Reference;
using BeauUtil.Debugger;

namespace Astro {
    public class FocusState : SharedStateComponent {
        [NonSerialized] public RingBuffer<UIFocus> ActiveFocii = new RingBuffer<UIFocus>(64, RingBufferMode.Expand);
        [NonSerialized] public UIFocusPackedData[] ActiveFociiPacked = new UIFocusPackedData[128];
        [NonSerialized] public BitSet256 ActiveFociiVisibleBits;

        [NonSerialized] public UIFocus CurrentFocus = null;
        [NonSerialized] public bool FocusUpdated = false;
        [NonSerialized] public bool MonitorInputActive = false;

        [Header("Focus Sprites")]
        // Could have realistically made this a global asset but yolo
        [SerializeField] private Sprite m_DefaultStarSprite;
        [SerializeField] private Sprite m_BlueTintStarSprite;
        [SerializeField] private Sprite m_IRTintStarSprite;
        [SerializeField] private Sprite m_DataSubmittedStarSprite;
        [SerializeField] private Sprite m_DataSubmittedNeutrinoStarSprite;
        [SerializeField] private Sprite[] m_GuessTrackerSubmittedSprites = new Sprite[4];
        [SerializeField] private Sprite[] m_GuessTrackerSprites = new Sprite[4];

        [NonSerialized] static public Sprite DefaultStarSprite;
        [NonSerialized] static public Sprite BlueTintStarSprite;
        [NonSerialized] static public Sprite IRTintStarSprite;
        [NonSerialized] static public Sprite DataSubmittedStarSprite;
        [NonSerialized] static public Sprite DataSubmittedNeutrinoStarSprite;
        [NonSerialized] static public Sprite[] GuessTrackerSprites = new Sprite[4];
        [NonSerialized] static public Sprite[] GuessTrackerSubmittedSprites = new Sprite[4];

        public SpriteRenderer FocusOutline;

        [Header("Focus Scale")]
        public Vector3 DefaultTrackerPipScale = new Vector3(0.88f, 0.88f, 1f);
        public float BaseScale;
        public float MinScale;
        public float MaxScale;
        public float ScaleOffset;

        private Action setMonitorInputActive;
        private Action setMonitorInputInactive;

        public CastableEvent<UIFocus> OnFocusUpdated = new CastableEvent<UIFocus>();

        protected override void OnEnable() {
            setMonitorInputActive = () => {
                MonitorInputActive = true; };
            setMonitorInputInactive = () => {
                MonitorInputActive = false; };

            DefaultStarSprite = m_DefaultStarSprite;
            DataSubmittedStarSprite = m_DataSubmittedStarSprite;
            DataSubmittedNeutrinoStarSprite = m_DataSubmittedNeutrinoStarSprite;
            GuessTrackerSprites = m_GuessTrackerSprites;
            GuessTrackerSubmittedSprites = m_GuessTrackerSubmittedSprites;
            BlueTintStarSprite = m_BlueTintStarSprite;
            IRTintStarSprite = m_IRTintStarSprite;

            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, FocusableUtility.ClickEmptySpace);

            Game.Events.Register(GameEvents.StartNeutrinoNavigation, setMonitorInputInactive);
            Game.Events.Register(GameEvents.StopNeutrinoNavigation, setMonitorInputActive);

            Game.Events.Register(GameEvents.StartPuzzleNavigation, setMonitorInputInactive);
            Game.Events.Register(GameEvents.StopPuzzleNavigation, setMonitorInputActive);

            Game.Events.Register(GameEvents.LockMonitorFocus, setMonitorInputInactive);
            Game.Events.Register(GameEvents.UnlockMonitorFocus, setMonitorInputActive);

            base.OnEnable();
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (Game.IsShuttingDown) return;

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
        public static void SetCurrentFocus(CelestialAsset asset, FocusState state = null) {
            if (state == null) state = Find.State<FocusState>();

            UIFocus assetFocus = state.ActiveFocii.Find(f => f.TargetData == asset);
            if (assetFocus != null) {
                SetCurrentFocus(state, assetFocus);
            } else {
                Log.Warn("[FocusableUtility > SetCurrentFocus] could not find CelestialAsset: {0}", asset != null ? asset.AssetId : "null");
            }
        }
        
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

            state.OnFocusUpdated.Invoke(focus);

            // Set new focus
            state.CurrentFocus = focus;
            state.FocusUpdated = true;
            var dataState = Find.State<DataPacketDistributionState>();
            var data = focus == null ? null : focus.TargetData;
            DataDistributionUtility.QueueConversion(dataState, data);

            ReferenceUtility.TryEnableIDSubmit(focus != null);

            AstroGame.Events.Dispatch(GameEvents.OnStarSelected, EvtArgs.Ref(state.CurrentFocus));

            // clear reference guide selections
            var rgs = Find.State<RefGuideState>();
            ReferenceUtility.ClearControls(rgs);

            // Scripting
            if (state.CurrentFocus == null) return;

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
            return NeutrinoEventUtil.IsIdInNeutrinoEvent(focus.TargetData.AssetId);
        }

        public static void ClickEmptySpace() {
            FocusState state = Find.State<FocusState>();
            SetCurrentFocus(state, null);
        }

        public static UIFocus GetFocusByData(StringHash32 celestialAssetId, FocusState focusState = null) {
            if (focusState == null) focusState = Find.State<FocusState>();

            UIFocus returnFocus = null;
            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(celestialAssetId);

            foreach (UIFocus focus in focusState.ActiveFocii) {
                if (focus.TargetData == asset) {
                    returnFocus = focus;
                    break;
                }
            }

            return returnFocus;
        }
    }

}