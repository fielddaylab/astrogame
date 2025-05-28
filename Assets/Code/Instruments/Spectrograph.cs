

using Astro;
using FieldDay;
using FieldDay.Components;
using ScriptableBake;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Astro {

    public class Spectrograph : BatchedComponent, IBaked, IRegistrationCallbacks {
        public MeshRenderer Background;
        [NonSerialized] public float BackgroundWidth;
        [NonSerialized] public SpectrographMaterialMask CurrentElements;
        // TODO: use pools for lines?
        public List<GameObject> Lines;

        public void OnRegister() {
            if (BackgroundWidth == default) {
                BackgroundWidth = Background.gameObject.transform.localScale.x;
            }
        }

        public void OnDeregister() {
            
        }


#if UNITY_EDITOR
        int IBaked.Order => 1000;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Background.sharedMaterial = Find.Any<SpectrometerState>().BlankBackground;
            return true;
        }

#endif // UNITY_EDITOR
    }

    [Flags]
    public enum SpectrographMaterialMask {
        Hydrogen = 0x001,
        Helium = 0x002,
        Carbon = 0x004,
        Iron = 0x008,
        Calcium = 0x010,
        Sodium = 0x020,
        Magnesium = 0x040,
        Oxygen = 0x080,
        Titanium = 0x100,
        Lithium = 0x200,

        OType = Helium,
        BType = Hydrogen | Helium,
        AType = Hydrogen | Helium | Iron,
        FType = Hydrogen          | Iron | Sodium | Magnesium,
        GType = Hydrogen          | Iron | Sodium | Magnesium | Calcium,
        KType = Iron | Sodium | Magnesium | Calcium,
        MType = Iron | Sodium | Magnesium | Calcium | Titanium

    }

    public static partial class SpectrographUtility {

        public static void SetElements(Spectrograph graph, SpectrographMaterialMask elements) {
            graph.CurrentElements = elements;
            DisplaySpectrum(graph);
        }
        public static void ClearElements(Spectrograph graph) {
            graph.CurrentElements = 0;
            DisplaySpectrum(graph);
        }

        public static void DisplaySpectrum(Spectrograph graph) {
            SpectrometerState state = Find.State<SpectrometerState>();
            List<float> linePos = GetNormalizedWavelengths(graph.CurrentElements, state);
            UpdateBackground(graph, state);
            EqualizeLineNums(graph, linePos.Count, state);
            for (int i = 0; i < linePos.Count; i++) {
                graph.Lines[i].SetActive(true);
                graph.Lines[i].transform.localPosition = new Vector3((graph.BackgroundWidth*linePos[i] - graph.BackgroundWidth/2), 0f, -0.01f);
            }
        }

        private static void EqualizeLineNums(Spectrograph graph, int numLines, SpectrometerState state) {
            int lineNumDiff = numLines - graph.Lines.Count;
            if (lineNumDiff > 0) {
                for (int i = 0; i < lineNumDiff; i++) {
                    graph.Lines.Add(GameObject.Instantiate(state.LinePrefab, graph.transform));
                }
            } else if (lineNumDiff < 0) {
                for (int i = 0; i < -lineNumDiff; i++) {
                    graph.Lines[graph.Lines.Count - i - 1].SetActive(false);
                }
            }
        }

        private static void UpdateBackground(Spectrograph graph, SpectrometerState state) {
            if (graph.CurrentElements == 0) {
                graph.Background.sharedMaterial = state.BlankBackground;
            } else {
                graph.Background.sharedMaterial = state.SpectrumBackground;
            }
        }
    }
}
