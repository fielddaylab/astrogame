using System;
using System.Collections;

using TMPro;
using UnityEngine;

using BeauUtil;
using BeauRoutine;

using FieldDay;
using FieldDay.SharedState;

namespace Astro {
    public class CelestialDataDisplay : SharedStateComponent {
        [HideInInspector] public bool Active;
        public Routine AnimRoutine;

        public CanvasGroup DataDisplayPanel;
        public RectTransform RowGroupTransform;
        public RectTransform DataRowPrefab;

        protected override void OnEnable() {
            Game.Events.Register(GameEvents.ValidOpenIdSubmission, CelestialDataDisplayUtil.UpdateCurrentDataDisplay);
            Game.Scenes.QueueOnLoad(() => {
                Find.State<FocusState>().OnFocusUpdated.Register(CelestialDataDisplayUtil.OnFocusUpdated);
            }); 

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
                GameObject.Destroy(child.gameObject);
            }

            // Populate Identified Data
            progress.Classifications.TryGetValue(asset.AssetId, out BitSet32 identified);

            foreach (var id in asset.ClassIds) {
                ReferenceClassification refClass = Find.NamedAsset<ReferenceClassification>(id);

                int classificationIdx = Array.IndexOf(asset.ClassIds, id);
                if (classificationIdx < 0) continue;

                if (identified[classificationIdx]) {
                    RectTransform Row = GameObject.Instantiate(display.DataRowPrefab, display.RowGroupTransform);
                    Row.Find("Label").GetComponent<TextMeshProUGUI>().SetText( MapTypeToLabel(refClass.Type) );
                    Row.Find("Value").GetComponent<TextMeshProUGUI>().SetText( refClass.Label );
                } 
            }

            return; 
        }

        public static bool IdentifiedDataToDisplay(CelestialAsset asset) {
            PlayerProgressState progress = Find.State<PlayerProgressState>();

            progress.Classifications.TryGetValue(asset.AssetId, out BitSet32 identified);

            foreach (var id in asset.ClassIds) {
                int classificationIdx = Array.IndexOf(asset.ClassIds, id);
                if (classificationIdx < 0) continue;

                if (identified[classificationIdx]) {
                    return true;
                }
            }

            return false;
        }

        private static string MapTypeToLabel(ClassificationTypeMask type) {
            switch (type) {
                case ClassificationTypeMask.Photometer: {
                    return "BRIGHTNESS:";
                }
                case ClassificationTypeMask.ColorMeter: {
                    return "SPECTRAL-TYPE:";
                }
                case ClassificationTypeMask.Spectrometer: {
                    return "ELEMENTS:";
                }
                case ClassificationTypeMask.Historical: {
                    return "LUMOSITY:";
                }
                default : {
                    return "UNKOWN:";
                }
            }
        }

    }
}