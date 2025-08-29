
using System;
using FieldDay;
using FieldDay.Components;
using BeauUtil.UI;
using UnityEngine;
using BeauRoutine;

namespace Astro {
    public class UIFocus : BatchedComponent, IRegistrationCallbacks {
        [NonSerialized] public Transform Target;
        [NonSerialized] public CelestialAsset TargetData;
        [NonSerialized] public bool IsVisibleInCurrentFilter;

        [NonSerialized] public bool HasHighlight;
        [NonSerialized] public SpriteRenderer Highlight;

        public Transform Root;
        public SphereCollider Clickable;
        public PointerListener Button;

        [Header("Apperance")]
        public SpriteRenderer Represent2D;
        public SpriteRenderer TrackerSprite;
        public SpriteRenderer PipSprite;

        public StarLogData StarLog = default;

        public void OnDestroy()
        {
            if (Game.IsShuttingDown) { return; }
            Game.Events.DeregisterAll(GameEvents.StartPuzzleNavigation);
            Game.Events.DeregisterAll(GameEvents.PuzzleNavigationComplete);
            Game.Events.DeregisterAll(GameEvents.StopPuzzleNavigation);
        }

        public void OnDeregister()
        {

        }

        public void OnRegister()
        {
            Game.Events.Register(GameEvents.StartPuzzleNavigation, () => { FocusableUtility.SetCursorHintEnabled(this, false); });
            Game.Events.Register(GameEvents.PuzzleNavigationComplete, () => { FocusableUtility.SetCursorHintEnabled(this, true); });
            Game.Events.Register(GameEvents.StopPuzzleNavigation, () => { FocusableUtility.SetCursorHintEnabled(this, true); });
        }

        private void Awake() {
            Button.onClick.Register(OnClicked);
        }

        private void OnClicked() {
            FocusableUtility.SetCurrentFocus(Find.State<FocusState>(), this);

            StarLog.IsHighlighted = HasHighlight;
            AstroGame.Events.Dispatch(GameEvents.StarClicked, EvtArgs.Box(StarLog));
        }
    }

    public struct UIFocusPackedData {
        public Vector3 TargetPos;
        public Vector3 TargetVector;
    }

    public static partial class FocusableUtility { 
        public static void InitFocusable(FocusState state, UIFocus focus, Transform target, CelestialAsset asset, Sprite represent2D, Sprite trackerSprite = null) {
#if UNITY_EDITOR
            focus.gameObject.name = asset.DisplayName;
            focus.Button.name = asset.DisplayName + " (Button)";
#endif // UNITY_EDITOR

            focus.Target = target;
            if (represent2D == null) {
                focus.Represent2D.enabled = false;
            }
            focus.Represent2D.sprite = represent2D;
            focus.Represent2D.sprite = trackerSprite;

            focus.TargetData = asset;

            focus.StarLog.AssetID = focus.TargetData.AssetId;
            focus.StarLog.Name = focus.TargetData.DisplayName;
            focus.StarLog.Constellation = focus.TargetData.ConstellationName;
            focus.StarLog.Coordinates = focus.TargetData.Coords;
            focus.StarLog.Distance = focus.TargetData.Distance;
            if (!focus.TargetData.ColorId.IsEmpty)
            {
                focus.StarLog.ColorIndex = focus.TargetData.ApparentBlueMagnitude - focus.TargetData.ApparentMagnitude;
            }
            focus.StarLog.Temperature = focus.TargetData.Temperature;
            focus.StarLog.VisMagnitude = focus.TargetData.ApparentMagnitude;
            focus.StarLog.BlueMagnitude = focus.TargetData.ApparentBlueMagnitude;
            focus.StarLog.InfraredMagnitude = focus.TargetData.ApparentIRMagnitude;
            focus.StarLog.AbsoluteMagnitude = focus.TargetData.AbsoluteMagnitude;
            focus.StarLog.Elements = focus.TargetData.Spectrograph;

            float scaleFactor = Mathf.Clamp(Mathf.Pow(state.BaseScale, asset.ApparentMagnitude) - 0.45f, state.MinScale, state.MaxScale);
            focus.Root.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            Vector3 scaleDefault = Find.State<FocusState>().DefaultTrackerPipScale;
            focus.TrackerSprite.GetComponent<Transform>().localScale = new Vector3(scaleDefault.x / scaleFactor, scaleDefault.y / scaleFactor, scaleDefault.z);

            float clickableRadius = state.BaseScale * Math.Min(1, 1 / scaleFactor);

            focus.Clickable.radius = clickableRadius / scaleFactor;
        }

        public static void UpdateFocusAppearance(UIFocus focus, Sprite represent2D, Vector3 scale = default){
            focus.Represent2D.sprite = represent2D;
            if (scale == default) return;

            focus.Root.localScale = scale;
            Vector3 currTrackerScale = focus.TrackerSprite.GetComponent<Transform>().localScale;
            Transform tracker = focus.TrackerSprite.transform;
            Transform pip = focus.PipSprite.transform;
            tracker.localScale = pip.localScale = new Vector3(currTrackerScale.x / scale.x, currTrackerScale.y / scale.y, 1f);
        }

        public static void UpdateFocusFilterAppearance(UIFocus focus, CelestialObjectVisMask visMask = CelestialObjectVisMask.Visible){
            FocusState state = Find.State<FocusState>();
            CelestialAsset asset = focus.TargetData;

            bool prevVis = focus.IsVisibleInCurrentFilter;
            focus.IsVisibleInCurrentFilter = (focus.TargetData.Visibility & visMask) != 0;

            // Update our neutrino highlight if this is a neutrino star 
            if (focus.HasHighlight && focus.IsVisibleInCurrentFilter != prevVis) {
                focus.Highlight.sprite = focus.IsVisibleInCurrentFilter ? NeutrinoHighlightState.HighlightVisibleSprite : NeutrinoHighlightState.HighlightNotVisibleSprite;
                focus.Highlight.SetAlpha(focus.IsVisibleInCurrentFilter ? 1 : NeutrinoHighlightState.HighlightNotVisibleAlpha);

                if (focus.IsVisibleInCurrentFilter) {
                    AstroGame.Events.Dispatch(GameEvents.StarHighlighted, focus.TargetData.DisplayName);
                }
                else {
                    AstroGame.Events.Dispatch(GameEvents.StarUnhighlighted, focus.TargetData.DisplayName);
                }
            }

            // Are we visible in the new filter?
            if (!focus.IsVisibleInCurrentFilter) {
                // Update our apperance based on the current filter
                focus.Represent2D.sprite = null;
                focus.Represent2D.enabled = false;
                
                // Update our scale based on the base highlight size
                float highlightScaleFactor = state.BaseScale - 0.45f;
                Vector3 highlightScaleDefault = Find.State<FocusState>().DefaultTrackerPipScale;

                focus.Root.localScale = new Vector3(highlightScaleFactor, highlightScaleFactor, highlightScaleFactor);

                Transform tracker = focus.TrackerSprite.transform;
                Transform pip = focus.PipSprite.transform;
                tracker.localScale = pip.localScale = new Vector3(highlightScaleDefault.x / highlightScaleFactor, highlightScaleDefault.y / highlightScaleFactor, 1f);
                return;
            }
            
            // We are visible in the new filter and need to update accordingly
            Sprite represent2D = FocusState.DefaultStarSprite;
            float visibleLight = asset.ApparentMagnitude;

            switch (visMask) {
                case CelestialObjectVisMask.Blue:
                    represent2D = FocusState.BlueTintStarSprite;
                    visibleLight = asset.ApparentBlueMagnitude;
                    break;
                case CelestialObjectVisMask.Infrared:
                    represent2D = FocusState.IRTintStarSprite;
                    visibleLight = asset.ApparentIRMagnitude;
                    break;
                default:
                    break;
            }

            // Update our apperance based on the current filter
            focus.Represent2D.sprite = represent2D;
            focus.Represent2D.size = new Vector2(0.32f, 0.32f);

            // Update our scale based on the visible magnitude for our current filter
            float scaleFactor = Mathf.Clamp(Mathf.Pow(state.BaseScale, visibleLight) - 0.45f, state.MinScale, state.MaxScale);
            focus.Root.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            Vector3 scaleDefault = Find.State<FocusState>().DefaultTrackerPipScale;
            var trackerTransform = focus.TrackerSprite.transform;
            var pipTransform = focus.PipSprite.transform;
            pipTransform.localScale = trackerTransform.localScale = new Vector3(scaleDefault.x / scaleFactor, scaleDefault.y / scaleFactor, scaleDefault.z);

            // Update clicable region of our asset to match the new scale
            float clickableRadius = state.BaseScale * Math.Min(1, 1 / scaleFactor);
            focus.Clickable.radius = clickableRadius / scaleFactor;
        }

        public static void UpdateFocusTrackerSprite(UIFocus focus, Sprite trackerSprite) {
            focus.TrackerSprite.sprite = trackerSprite; 
        }

        public static void UpdateFocusPipSprite(UIFocus focus, Sprite pipSprite) {
            focus.PipSprite.sprite = pipSprite; 
        }

        public static void SetCursorHintEnabled(UIFocus focus, bool enabled) {
            focus.Button.enabled = enabled;
        }
    }
}
