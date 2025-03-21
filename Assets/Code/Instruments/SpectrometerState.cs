using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public class SpectrometerState : SharedStateComponent {
        // gradient background
        // black lines at the wavelengths of each element
        [Header("Inspector")]
        public GameObject LinePrefab;
        public Material SpectrumBackground;
        public Material BlankBackground;

        public readonly int MIN_WAVELENGTH = 380;
        public readonly int MAX_WAVELENGTH = 750;

        public readonly Dictionary<SpectrographMaterialMask, int[]> Wavelengths = new Dictionary<SpectrographMaterialMask, int[]>() {
            [SpectrographMaterialMask.Hydrogen] = new int[] { 656, 486, 434, 410 },
            [SpectrographMaterialMask.Helium] = new int[] { 588 },
            [SpectrographMaterialMask.Carbon] = new int[] { },
            [SpectrographMaterialMask.Iron] = new int[] { 517, 496, 467, 438, 431, 382, 358, 302 },
            [SpectrographMaterialMask.Calcium] = new int[] { 397, 393 },
            [SpectrographMaterialMask.Sodium] = new int[] { 590, 589 },
            [SpectrographMaterialMask.Magnesium] = new int[] { 517, 516 },
            [SpectrographMaterialMask.Oxygen] = new int[] { 687, 628 },
            [SpectrographMaterialMask.Titanium] = new int[] { 336 }
        };
    }


    public static partial class SpectrographUtility {
        public static List<int> GetWavelengths(SpectrographMaterialMask mask, SpectrometerState state) {
            List<int> result = new List<int>();
            // TODO: iterate through flags instead..?
            if (mask.HasFlag(SpectrographMaterialMask.Hydrogen)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Hydrogen]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Helium)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Helium]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Carbon)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Carbon]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Iron)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Iron]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Calcium)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Calcium]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Sodium)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Sodium]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Magnesium)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Magnesium]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Oxygen)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Oxygen]);
            }
            if (mask.HasFlag(SpectrographMaterialMask.Titanium)) {
                result.AddRange(state.Wavelengths[SpectrographMaterialMask.Titanium]);
            }
            return result;
        }

        public static List<float> GetNormalizedWavelengths(SpectrographMaterialMask mask, SpectrometerState state) {
            List<float> result = new List<float>();
            GetWavelengths(mask, state).ForEach(wavelength => {
                result.Add(Mathf.InverseLerp(state.MIN_WAVELENGTH, state.MAX_WAVELENGTH, wavelength));
            });
            return result;
        }
       
    }
}