using System;
using System.Collections;

using TMPro;
using UnityEngine;

using BeauUtil;
using BeauRoutine;

using FieldDay;
using FieldDay.SharedState;
using System.Text;
using BeauPools;
using BeauUtil.Debugger;
using UnityEngine.UI;

namespace Astro {
    public class CelestialDataDisplay : SharedStateComponent, IRegistrationCallbacks {
        [HideInInspector] public bool DataPanelActive;
        public Routine AnimRoutine;

        [Header("Data Display Panel")]
        public CanvasGroup DataDisplayPanel;
        public RectTransform RowGroupTransform;
        [HideInInspector] public int NumRevealedRows;

        [Header("Clearance Points Panel")]
        public CanvasGroup ClearancePointsPanel;
        public RectTransform PointsRowTransform;

        [Header("Data Requirement Hint")]
        public GameObject DataRequirmentHint;
        public TextMeshProUGUI HintDisplayText;

        private void Awake() {
            HideCelestialDataDisplay();
            HideClearancePointDisplay();
        }
        
        protected override void OnEnable() {
            Game.Events.Register(GameEvents.ValidOpenIdSubmission, CelestialDataDisplayUtil.PlayClearancePointAnimation);
            Game.Events.Register(GameEvents.ValidKnowledgeSubmission, CelestialDataDisplayUtil.PlayUnacceptedFeedback);
            Game.Events.Register(GameEvents.UnacceptedOpenIdSubmission, CelestialDataDisplayUtil.PlayUnacceptedFeedback);
            Game.Events.Register(GameEvents.DuplicateOpenIdSubmission, CelestialDataDisplayUtil.PlayDuplicateFeedback);
            Game.Events.Register(GameEvents.IncorrectOpenIdSubmission, CelestialDataDisplayUtil.PlayIncorrectFeedback);
            Game.Events.Register(GameEvents.InvalidOpenIdSubmission, CelestialDataDisplayUtil.PlayIncorrectFeedback);
            Game.Scenes.QueueOnLoad(() => {
                Find.State<FocusState>().OnFocusUpdated.Register(CelestialDataDisplayUtil.OnFocusUpdated);

                // Set the number of pips in our clearance level display
                DayConfigAsset config = DayConfigUtil.GetConfigForState();

                int i = 0;
                foreach (RectTransform transform in PointsRowTransform) {
                    if (transform == PointsRowTransform) continue;

                    if (i >= config.NumNeutrinoPoints) {
                        transform.gameObject.SetActive(false);
                    } else {
                        transform.gameObject.SetActive(true);
                    }
                    i++;
                }
            });

            NumRevealedRows = 0;
            base.OnEnable();
        }

        public IEnumerator RevealCelestialDataDisplay() {
            DataDisplayPanel.alpha = 0;
            yield return Tween.Value(0f, 1f, (f) => { DataDisplayPanel.alpha = f; }, Mathf.Lerp, 0.2f).ForceOnCancel();
            DataPanelActive = true;
        }

        public IEnumerator FadeOutCelestialDataDisplay() {
            DataDisplayPanel.alpha = 1;
            yield return Tween.Value(1f, 0f, (f) => { DataDisplayPanel.alpha = f; }, Mathf.Lerp, 0.1f).ForceOnCancel();
            DataPanelActive = false;
        }

        public void HideCelestialDataDisplay() {
            DataDisplayPanel.alpha = 0;
            DataPanelActive = false;
        }

        public IEnumerator RevealClearancePointDisplay() {
            RectTransform PanelRoot = ClearancePointsPanel.GetComponent<RectTransform>();

            float startPosY = 75f;
            float endPosY = -30f;

            PanelRoot.anchoredPosition = new Vector2(PanelRoot.anchoredPosition.x, startPosY);
            ClearancePointsPanel.alpha = 0;

            yield return Routine.Combine(
                Tween.Value(0f, 1f, (f) => { ClearancePointsPanel.alpha = f; }, Mathf.Lerp, 0.2f),
                Tween.Value(startPosY, endPosY, (f) => { PanelRoot.anchoredPosition = new Vector2(PanelRoot.anchoredPosition.x, f); }, Mathf.Lerp, 0.3f).Ease(Curve.QuadInOut)
            );
        }

        public IEnumerator RemoveClearancePointDisplay() {
            RectTransform PanelRoot = ClearancePointsPanel.GetComponent<RectTransform>();

            float startPosY = -30f;
            float endPosY = 75;

            PanelRoot.anchoredPosition = new Vector2(PanelRoot.anchoredPosition.x, startPosY);
            ClearancePointsPanel.alpha = 1f;

            yield return Routine.Combine(
                Tween.Value(1f, 0f, (f) => { ClearancePointsPanel.alpha = f; }, Mathf.Lerp, 0.2f),
                Tween.Value(startPosY, endPosY, (f) => { PanelRoot.anchoredPosition = new Vector2(PanelRoot.anchoredPosition.x, f); }, Mathf.Lerp, 0.3f).Ease(Curve.QuadInOut)
            );
        }

        public void HideClearancePointDisplay() {
            RectTransform PanelRoot = ClearancePointsPanel.GetComponent<RectTransform>();
            PanelRoot.anchoredPosition = new Vector2(PanelRoot.anchoredPosition.x, 75);
            ClearancePointsPanel.alpha = 0f;
        }

        public IEnumerator AddPointToClearancePointDisplay(int pointIndex) {
            // Point pips are set up such that they have 2 images
            // Child 0 is the full point pip and Child 1 is the outline
            var PointPip = PointsRowTransform.GetChild(pointIndex);

            Image FullPip = PointPip.GetChild(0).GetComponent<Image>();
            yield return Tween.Value(0f, 1f, (f) => { FullPip.SetAlpha(f); }, Mathf.Lerp, 0.3f);
        }

        private Action clearDataDisplay;

        public void OnRegister() { 
            clearDataDisplay = () => {
                if (Find.State<FocusState>().MonitorInputActive) {
                    CelestialDataDisplayUtil.OnFocusUpdated(null);
                }
            };

            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, clearDataDisplay);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.MonitorEmptySpaceClicked, clearDataDisplay);
        }

    }

    public static class CelestialDataDisplayUtil {
        private static void RevealDataHint(CelestialDataDisplay display, CelestialAsset asset) {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Photometer) && !HasIdentifiedDataType(ClassificationTypeMask.Photometer, asset)) {
                    psb.Builder.Append("BRIGHTNESS, ");
                }
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.ColorMeter) && !HasIdentifiedDataType(ClassificationTypeMask.ColorMeter, asset)) {
                    psb.Builder.Append("SPECTRAL-TYPE, ");
                }
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Spectrometer) && !HasIdentifiedDataType(ClassificationTypeMask.Spectrometer, asset)) {
                    psb.Builder.Append("ELEMENTS, ");
                }
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Luminosity) && !HasIdentifiedDataType(ClassificationTypeMask.Luminosity, asset)) {
                    psb.Builder.Append("LUMINOSITY, ");
                }
                Assert.True(psb.Builder.Length >= 2);

                psb.Builder.Length -= 2;

                display.HintDisplayText.SetText(psb);
            }
            display.DataRequirmentHint.SetActive(true);
        }

        static public void UpdateStarRepresentation(UIFocus focus, bool hasIdentifiedNeutrinoType) {
            bool inNeutrinoEvent = NeutrinoEventUtil.IsAssetInNeutrinoEvent(focus.TargetData);

            Sprite update;
            if (inNeutrinoEvent && hasIdentifiedNeutrinoType) {
                update = FocusState.DataSubmittedNeutrinoStarSprite;
            } else if (HasIdDataToDisplay(focus.TargetData)) {
                update = FocusState.DataSubmittedStarSprite;
            } else {
                update = null;
            }

            FocusableUtility.UpdateFocusPipSprite(focus, update);
        }

        public static void OnFocusUpdated(UIFocus focus) {
            CelestialDataDisplay display = Find.State<CelestialDataDisplay>();
    
            if (focus != null) { // Update the display

                if (!display.DataPanelActive) { // Is our display already active for another star?
                    // Nothing identified for this star yet
                    if (HasIdDataToDisplay(focus.TargetData)) {
                        // Animate in the display for this star
                        UpdateDataDisplay(display, focus.TargetData);
                        display.AnimRoutine = Routine.Start( display.RevealCelestialDataDisplay() );
                    }

                    // Check if we need to submit data for neutrino event
                    DayConfigAsset config = DayConfigUtil.GetConfigForState();

                    bool hasIdentifiedNeutrinoType = HasIdentifiedDataType(config.AcceptedIDSubmissions, focus.TargetData);
                    bool targetInNeutrinoEvent = NeutrinoEventUtil.IsAssetInNeutrinoEvent(focus.TargetData);
                    if (!hasIdentifiedNeutrinoType && targetInNeutrinoEvent) {
                        RevealDataHint(display, focus.TargetData);
                    } else { 
                        display.DataRequirmentHint.SetActive(false);
                    }
                } else {
                    if (!HasIdDataToDisplay(focus.TargetData)) {
                        if (display.AnimRoutine.Exists()) { // Hide Display
                            display.AnimRoutine.OnComplete(() => display.FadeOutCelestialDataDisplay());
                        } else {
                            display.AnimRoutine = Routine.Start(display.FadeOutCelestialDataDisplay());
                        }
                    }

                    UpdateDataDisplay(display, focus.TargetData);

                    // Check if we need to submit data for neutrino event
                    DayConfigAsset config = DayConfigUtil.GetConfigForState();

                    bool hasIdentifiedNeutrinoType = HasIdentifiedDataType(config.AcceptedIDSubmissions, focus.TargetData);
                    bool targetInNeutrinoEvent = NeutrinoEventUtil.IsAssetInNeutrinoEvent(focus.TargetData);
                    if (!hasIdentifiedNeutrinoType && targetInNeutrinoEvent) {
                        RevealDataHint(display, focus.TargetData);
                    } else { 
                        display.DataRequirmentHint.SetActive(false);
                    }
                }
            } else { // Disable the display
                display.DataRequirmentHint.SetActive(false);
                if (!display.DataPanelActive) return; // Display already disabled

                if (display.AnimRoutine.Exists()) { // Hide Display
                    display.AnimRoutine.OnComplete(() => display.FadeOutCelestialDataDisplay());
                } else {
                    display.AnimRoutine = Routine.Start(display.FadeOutCelestialDataDisplay());
                }
                
            }
        }

        public static void PlayClearancePointAnimation() {
            CelestialDataDisplay display = Find.State<CelestialDataDisplay>();
            PlayerPointsState points = Find.State<PlayerPointsState>();
            Find.FirstComponent<ConsoleTypedText>().Play("IdCorrect");
            if (display.DataPanelActive) {
                display.HideCelestialDataDisplay();
            }

            Game.Events.Dispatch(GameEvents.LockMonitorFocus);
            display.AnimRoutine = Routine.Start(display,
                Sequence.Create(display.RevealClearancePointDisplay())
                .Wait(0.2f)
                .Then(display.AddPointToClearancePointDisplay(points.SciencePoints - 1))
                .Wait(1.5f)
                .Then(display.RemoveClearancePointDisplay())
                .Wait(0.3f)
                .Then(() => UpdateCurrentDataDisplay())
                .Wait(1f)
                .Then(() => OnFocusUpdated(Find.State<FocusState>().CurrentFocus))
                .Wait(0.1f)
            );
            display.AnimRoutine.OnComplete(() => Game.Events.Dispatch(GameEvents.UnlockMonitorFocus));
        }

        public static void UpdateCurrentDataDisplay() {
            CelestialDataDisplay display = Find.State<CelestialDataDisplay>();
            var focus = Find.State<FocusState>().CurrentFocus;
            if (focus == null) return;

            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            bool hasIdentifiedNeutrinoType = HasIdentifiedDataType(config.AcceptedIDSubmissions, focus.TargetData);

            UpdateStarRepresentation(focus, hasIdentifiedNeutrinoType);

            UpdateDataDisplay(display, focus.TargetData);


            if (!display.DataPanelActive) {
                display.AnimRoutine = Routine.Start(display.RevealCelestialDataDisplay());
            }

            // Check if we need to submit data for neutrino event
            bool targetInNeutrinoEvent = NeutrinoEventUtil.IsAssetInNeutrinoEvent(focus.TargetData);
            if (!hasIdentifiedNeutrinoType && targetInNeutrinoEvent) {
                RevealDataHint(display, focus.TargetData);
            } else {
                display.DataRequirmentHint.SetActive(false);
            }
        }

        public static void PlayUnacceptedFeedback() {
            Find.FirstComponent<ConsoleTypedText>().Play("IdIrrelevant");
            UpdateCurrentDataDisplay();
        }

        public static void PlayDuplicateFeedback() {
            Find.FirstComponent<ConsoleTypedText>().Play("IdDuplicate");
        }

        public static void PlayIncorrectFeedback() {
            Find.FirstComponent<ConsoleTypedText>().Play("IdIncorrect");
        }

        public static void UpdateDataDisplay(CelestialDataDisplay display, CelestialAsset asset) {
            PlayerProgressState progress = Find.State<PlayerProgressState>();

            // Clear Rows
            for (int i = 0; i < display.RowGroupTransform.childCount; i++) {
                Transform child = display.RowGroupTransform.GetChild(i);
                child.gameObject.SetActive(false);
            }
            display.NumRevealedRows = 0;

            // Populate Identified Data
            progress.Knowledge.TryGetValue(asset.AssetId, out var knowledge);

            BitSet32 identified = knowledge.Classifications;

            // Start with classifications
            for (int classificationIdx = 0; classificationIdx < asset.ClassIds.Length; classificationIdx++) {
                if (classificationIdx < 0) continue;

                StringHash32 id = asset.ClassIds[classificationIdx];
                ReferenceClassification refClass = Find.NamedAsset<ReferenceClassification>(id);

                if (identified[classificationIdx]) {
                    Transform Row = display.RowGroupTransform.GetChild( display.NumRevealedRows );
                    Row.gameObject.SetActive(true);
                    display.NumRevealedRows++;
                    StringBuilder sb = new StringBuilder();
                    sb.Append(MapTypeToLabel(refClass.Type));
                    sb.Append(":");
                    Row.Find("Label").GetComponent<TextMeshProUGUI>().SetText( sb.ToString() );
                    Row.Find("Value").GetComponent<TextMeshProUGUI>().SetText( refClass.Label );
                } 
            }

            // Special case for spectrometer reading
            if ((knowledge.Flags & PlayerCelestialAssetKnowledgeFlags.IdentifiedResources) != 0) {
                Transform Row = display.RowGroupTransform.GetChild( display.NumRevealedRows );
                Row.gameObject.SetActive(true);
                display.NumRevealedRows++;
                Row.Find("Label").GetComponent<TextMeshProUGUI>().SetText("Elements:");
                Row.Find("Value").GetComponent<TextMeshProUGUI>().SetText( SpectrographUtility.ToSymbolsString(asset.Spectrograph) );
            }

            return; 
        }

        public static bool HasIdDataToDisplay(CelestialAsset asset) {
            PlayerProgressState progress = Find.State<PlayerProgressState>();

            progress.Knowledge.TryGetValue(asset.AssetId, out var knowledge);

            BitSet32 identified = knowledge.Classifications;

            for (int classificationIdx = 0; classificationIdx < asset.ClassIds.Length; classificationIdx++) {
                if (classificationIdx < 0) continue;
                
                if (identified[classificationIdx]) {
                    return true;
                }
            }

            if ((knowledge.Flags & PlayerCelestialAssetKnowledgeFlags.IdentifiedResources) != 0) return true;

            return false;
        }

        public static bool HasIdentifiedDataType(ClassificationTypeMask type, CelestialAsset asset) {
            PlayerProgressState progress = Find.State<PlayerProgressState>();

            progress.Knowledge.TryGetValue(asset.AssetId, out var knowledge);

            BitSet32 identified = knowledge.Classifications;

            for (int classificationIdx = 0; classificationIdx < asset.ClassIds.Length; classificationIdx++) {
                if (classificationIdx < 0) continue;
                ReferenceClassification refClass = Find.NamedAsset<ReferenceClassification>(asset.ClassIds[classificationIdx]);
                if (!refClass.Type.HasFlag(type)) continue;

                if (identified[classificationIdx]) {
                    return true;
                }
            }

            if (type.HasFlag(ClassificationTypeMask.Spectrometer)) { // special case for spectrometer
                if ((knowledge.Flags & PlayerCelestialAssetKnowledgeFlags.IdentifiedResources) != 0) return true;
            }

            return false;
        }

        private static string MapTypeToLabel(ClassificationTypeMask type) {
            if (type.HasFlag(ClassificationTypeMask.Photometer)) {
                return "BRIGHTNESS";
            } else if (type.HasFlag(ClassificationTypeMask.ColorMeter)) {
                return "SPECTRAL-TYPE";
            } else if (type.HasFlag(ClassificationTypeMask.Spectrometer)) {
                return "SPECTRAL-TYPE";
            } else if (type.HasFlag(ClassificationTypeMask.Luminosity)) {
                return "LUMINOSITY";
            // } else if (type.HasFlag(ClassificationTypeMask.Infrared)) {
            //     return "SPECTRAL-TYPE";
            } else {
                return "UNKOWN";
            }
        }

        //private static string BuildMaterialsLabel(SpectrographMaterialMask materials) {
        //    StringBuilder sb = new StringBuilder();
        //    if (materials != 0) {
        //        if ((materials & SpectrographMaterialMask.Hydrogen) != 0) {
        //            sb.Append("H, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Helium) != 0) {
        //            sb.Append("He, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Carbon) != 0) {
        //            sb.Append("C, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Oxygen) != 0) {
        //            sb.Append("O, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Sodium) != 0) {
        //            sb.Append("Na, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Magnesium) != 0) {
        //            sb.Append("Mg, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Calcium) != 0) {
        //            sb.Append("Ca, ");
        //        }
        //        if ((materials & SpectrographMaterialMask.Iron) != 0) {
        //            sb.Append("Fe, ");
        //        }
        //        sb.Length -= 2; // trim last delim
        //    }
        //    return sb.ToString();
        //}

    }
}