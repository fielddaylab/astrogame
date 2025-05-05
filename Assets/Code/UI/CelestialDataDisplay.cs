using System;
using System.Collections;

using TMPro;
using UnityEngine;

using BeauUtil;
using BeauRoutine;

using FieldDay;
using FieldDay.SharedState;
using System.Text;

namespace Astro {
    public class CelestialDataDisplay : SharedStateComponent {
        [HideInInspector] public bool Active;
        public Routine AnimRoutine;

        public CanvasGroup DataDisplayPanel;
        public RectTransform RowGroupTransform;
        [HideInInspector] public int NumRevealedRows;

        protected override void OnEnable() {
            Game.Events.Register(GameEvents.ValidOpenIdSubmission, CelestialDataDisplayUtil.UpdateCurrentDataDisplay);
            Game.Scenes.QueueOnLoad(() => {
                Find.State<FocusState>().OnFocusUpdated.Register(CelestialDataDisplayUtil.OnFocusUpdated);
            }); 

            NumRevealedRows = 0;
            base.OnEnable();
        }

        public IEnumerator RevealCelestialDataDisplay() {
            DataDisplayPanel.alpha = 0;
            yield return Tween.Value(0f, 1f, (f) => { DataDisplayPanel.alpha = f; }, Mathf.Lerp, 0.2f);
            Active = true;       
        }

        public IEnumerator FadeOutCelestialDataDisplay() {
            DataDisplayPanel.alpha = 1;
            yield return Tween.Value(1f, 0f, (f) => { DataDisplayPanel.alpha = f; }, Mathf.Lerp, 0.1f);
            Active = false;
        }

        public void HideCelestialDataDisplay() {
            DataDisplayPanel.alpha = 0;
            Active = false;
        }
    }

    public static class CelestialDataDisplayUtil {
        public static void OnFocusUpdated(UIFocus focus) {
            CelestialDataDisplay display = Find.State<CelestialDataDisplay>();
    
            if (focus != null) { // Update the display
                if (!display.Active) {
                    // Nothing identified for this star yet
                    if (!IdentifiedDataToDisplay(focus.TargetData)) return;

                    // Animate in the display for this star
                    UpdateDataDisplay(display, focus.TargetData);
                    display.AnimRoutine = Routine.Start( display.RevealCelestialDataDisplay() );
                } else {
                    if (!IdentifiedDataToDisplay(focus.TargetData)) {
                        if (display.AnimRoutine.Exists()) { // Hide Display
                            display.AnimRoutine.OnComplete(() => display.FadeOutCelestialDataDisplay());
                        } else {
                            display.AnimRoutine = Routine.Start(display.FadeOutCelestialDataDisplay());
                        }
                    }

                    UpdateDataDisplay(display, focus.TargetData);
                }
            } else { // Disable the display
                if (!display.Active) return; // Display already disabled

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
            if (!display.Active) {
                display.AnimRoutine = Routine.Start( display.RevealCelestialDataDisplay() );
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
                    Row.Find("Label").GetComponent<TextMeshProUGUI>().SetText( MapTypeToLabel(refClass.Type) );
                    Row.Find("Value").GetComponent<TextMeshProUGUI>().SetText( refClass.Label );
                } 
            }

            // Special case for spectrometer reading
            if ((knowledge.Flags & PlayerCelestialAssetKnowledgeFlags.IdentifiedResources) != 0) {
                Transform Row = display.RowGroupTransform.GetChild( display.NumRevealedRows );
                Row.gameObject.SetActive(true);
                display.NumRevealedRows++;
                Row.Find("Label").GetComponent<TextMeshProUGUI>().SetText("Elements");
                Row.Find("Value").GetComponent<TextMeshProUGUI>().SetText( BuildMaterialsLabel(asset.Spectrograph) );
            }

            return; 
        }

        public static bool IdentifiedDataToDisplay(CelestialAsset asset) {
            PlayerProgressState progress = Find.State<PlayerProgressState>();

            progress.Knowledge.TryGetValue(asset.AssetId, out var knowledge);

            BitSet32 identified = knowledge.Classifications;

            for (int classificationIdx = 0; classificationIdx < asset.ClassIds.Length; classificationIdx++) {
                if (classificationIdx < 0) continue;
                
                if (identified[classificationIdx]) {
                    return true;
                }
            }

            return false;
        }

        private static string MapTypeToLabel(ClassificationTypeMask type) {
            if (type.HasFlag(ClassificationTypeMask.Photometer)) {
                return "BRIGHTNESS:";
            } else if (type.HasFlag(ClassificationTypeMask.ColorMeter)) {
                return "SPECTRAL-TYPE:";
            } else if (type.HasFlag(ClassificationTypeMask.Spectrometer)) {
                return "SPECTRAL-TYPE:";
            } else if (type.HasFlag(ClassificationTypeMask.Historical)) {
                return "LUMOSITY:";
            } else {
                return "UNKOWN:";
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