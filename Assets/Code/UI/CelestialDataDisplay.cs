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

namespace Astro {
    public class CelestialDataDisplay : SharedStateComponent, IRegistrationCallbacks {
        [HideInInspector] public bool DataPanelActive;
        public Routine AnimRoutine;

        [Header("Data Display Panel")]
        public CanvasGroup DataDisplayPanel;
        public RectTransform RowGroupTransform;
        [HideInInspector] public int NumRevealedRows;

        [Header("Data Requirement Hint")]
        public GameObject DataRequirmentHint;
        public TextMeshProUGUI HintDisplayText;

        protected override void OnEnable() {
            Game.Events.Register(GameEvents.ValidOpenIdSubmission, CelestialDataDisplayUtil.UpdateCurrentDataDisplay);
            Game.Events.Register(GameEvents.ValidKnowledgeSubmission, CelestialDataDisplayUtil.UpdateCurrentDataDisplay);
            Game.Events.Register(GameEvents.UnacceptedOpenIdSubmission, CelestialDataDisplayUtil.UpdateCurrentDataDisplay);
            Game.Scenes.QueueOnLoad(() => {
                Find.State<FocusState>().OnFocusUpdated.Register(CelestialDataDisplayUtil.OnFocusUpdated);
            }); 

            NumRevealedRows = 0;
            base.OnEnable();
        }

        public IEnumerator RevealCelestialDataDisplay() {
            DataDisplayPanel.alpha = 0;
            yield return Tween.Value(0f, 1f, (f) => { DataDisplayPanel.alpha = f; }, Mathf.Lerp, 0.2f);
            DataPanelActive = true;       
        }

        public IEnumerator FadeOutCelestialDataDisplay() {
            DataDisplayPanel.alpha = 1;
            yield return Tween.Value(1f, 0f, (f) => { DataDisplayPanel.alpha = f; }, Mathf.Lerp, 0.1f);
            DataPanelActive = false;
        }

        public void HideCelestialDataDisplay() {
            DataDisplayPanel.alpha = 0;
            DataPanelActive = false;
        }

        public void OnRegister() {
            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, () => {
                CelestialDataDisplayUtil.OnFocusUpdated(null);
            });
        }

        public void OnDeregister() {
        }

    }

    public static class CelestialDataDisplayUtil {
        private static void RevealDataHint(CelestialDataDisplay display, CelestialAsset asset) {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            using (PooledStringBuilder psb = PooledStringBuilder.Create())
            {

                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Photometer) && !HasIdentifiedDataType(ClassificationTypeMask.Photometer, asset))
                {
                    psb.Builder.Append("BRIGHTNESS, ");
                }
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.ColorMeter) && !HasIdentifiedDataType(ClassificationTypeMask.ColorMeter, asset))
                {
                    psb.Builder.Append("SPECTRAL-TYPE, ");
                }
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Spectrometer) && !HasIdentifiedDataType(ClassificationTypeMask.Spectrometer, asset))
                {
                    psb.Builder.Append("ELEMENTS, ");
                }
                if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Luminosity) && !HasIdentifiedDataType(ClassificationTypeMask.Luminosity, asset))
                {
                    psb.Builder.Append("LUMINOSITY, ");
                }
                // if (config.AcceptedIDSubmissions.HasFlag(ClassificationTypeMask.Infrared) && !HasIdentifiedDataType(ClassificationTypeMask.Infrared, asset))
                // {
                //     psb.Builder.Append("SPECTRAL-TYPE, ");
                // }
                Assert.True(psb.Builder.Length >= 2);

                psb.Builder.Length -= 2;

                display.HintDisplayText.SetText(psb);
            }
            display.DataRequirmentHint.SetActive(true);
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

        public static void UpdateCurrentDataDisplay() {
            CelestialDataDisplay display = Find.State<CelestialDataDisplay>();
            var focus = Find.State<FocusState>().CurrentFocus; 

            if (focus == null) return;

            UpdateDataDisplay(display, focus.TargetData);
            if (!display.DataPanelActive) {
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
                Row.Find("Value").GetComponent<TextMeshProUGUI>().SetText( BuildMaterialsLabel(asset.Spectrograph) );
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

        private static string BuildMaterialsLabel(SpectrographMaterialMask materials) {
            StringBuilder sb = new StringBuilder();
            if (materials != 0) {
                if ((materials & SpectrographMaterialMask.Hydrogen) != 0) {
                    sb.Append("H, ");
                }
                if ((materials & SpectrographMaterialMask.Helium) != 0) {
                    sb.Append("He, ");
                }
                if ((materials & SpectrographMaterialMask.Carbon) != 0) {
                    sb.Append("C, ");
                }
                if ((materials & SpectrographMaterialMask.Oxygen) != 0) {
                    sb.Append("O, ");
                }
                if ((materials & SpectrographMaterialMask.Sodium) != 0) {
                    sb.Append("Na, ");
                }
                if ((materials & SpectrographMaterialMask.Magnesium) != 0) {
                    sb.Append("Mg, ");
                }
                if ((materials & SpectrographMaterialMask.Calcium) != 0) {
                    sb.Append("Ca, ");
                }
                if ((materials & SpectrographMaterialMask.Iron) != 0) {
                    sb.Append("Fe, ");
                }
                sb.Length -= 2; // trim last delim
            }
            return sb.ToString();
        }

    }
}