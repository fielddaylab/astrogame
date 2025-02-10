

using Astro;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Astro {

    public class Spectrograph : BatchedComponent {
        public MeshRenderer Background;
        [NonSerialized] public SpectrographMaterialMask CurrentElements;
        // TODO: use pools for lines?
        public List<GameObject> Lines;
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
    }

    public static partial class SpectrographUtility {

        public static void SetMaterials(Spectrograph graph, SpectrographMaterialMask elements) {
            graph.CurrentElements = elements;
            DisplaySpectrum(graph);
        }
        public static void ClearMaterials(Spectrograph graph) {
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
                graph.Lines[i].transform.position.Set(linePos[i] - 0.5f, 0f, -0.01f);
            }
        }

        private static void EqualizeLineNums(Spectrograph graph, int numLines, SpectrometerState state) {
            int lineNumDiff = graph.Lines.Count - numLines;
            if (lineNumDiff > 0) {
                for (int i = 0; i < lineNumDiff; i++) {
                    graph.Lines.Add(GameObject.Instantiate(state.LinePrefab));
                }
            } else if (lineNumDiff < 0) {
                for (int i = 0; i < -lineNumDiff; i++) {
                    graph.Lines[graph.Lines.Count - i - 1].SetActive(false);
                }
            }
        }

        private static void UpdateBackground(Spectrograph graph, SpectrometerState state) {
            graph.Background.material = graph.CurrentElements == 0 ? state.BlankBackground : state.SpectrumBackground;
        }
    }
}
