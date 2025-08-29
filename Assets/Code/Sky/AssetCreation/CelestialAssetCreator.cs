using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astro
{
    /// <summary>
    /// Creates CelestialAssets as defined in an external source.
    /// Intended to be used in the Editor, creating the assets before build time.
    /// </summary>
    static public class CelestialAssetCreator
    {
        private const string ASSET_DELIM = "::";
        private const string FEATURE_DELIM = "@";
        private const string SUBGROUP_OPEN = "(";
        private const string SUBGROUP_CLOSE = ")";
        private const string SUBGROUP_SEP = ",";
        private const string DEF_DIR = "/Code/Sky/AssetCreation/Definitions/";

        // First item is source file. Second item is destination sub-directory to look for existing asset
        private static string[][] DEF_FILE_PAIRS = {
            new string[2] { "Constellations.txt", "Constellations" },
            new string[2] { "Planets.txt", "Planets" },
            new string[2] { "Stars.txt", "Stars" },
            new string[2] { "Comets.txt", "Comets" },
            new string[2] { "Satellites.txt", "Satellites" },
            new string[2] { "Galaxies.txt", "Galaxies" },
        };

        private const string NEW_ASSET_DIR = "_Assets/Data/Sky/";
        private const string CONSTELLATION_DIR = "_Assets/Data/Sky/Constellations/";
        private const string ENTRY_DIR = "_Assets/Data/Reference/Pages/";
        private const string COLOR_DIR = "_Assets/Data/Reference/Colors/";

        private const string NAME_ID = "name";
        private const string COORD_ID = "coords";
        private const string CATEGORY_ID = "category";
        private const string REF_ENTRY_ID = "entry";
        private const string CONSTELLATION_ID = "constellation";
        private const string TEMPERATURE_ID = "temperature"; // Kelvin
        private const string COLOR_ID = "color";
        private const string APP_MAG_ID = "appmag";
        private const string ABS_MAG_ID = "absmag";
        private const string SPECTROGRAPH_ID = "materials";
        private const string DISTANCE_ID = "distance"; // parsecs

#if UNITY_EDITOR
        [MenuItem("Astro/Load Celestial Assets")]
        public static void LoadCelestialAssets()
        {
            Debug.Log("[CelestialAssetCreator] Loading Celestial Assets...");

            foreach (string[] fileDirPair in DEF_FILE_PAIRS)
            {
                string path = Application.dataPath + DEF_DIR + fileDirPair[0];

                try
                {
                    StreamReader reader = new StreamReader(path);

                    string contents = reader.ReadToEnd();
                    string[] unparsedAssets = contents.Split(ASSET_DELIM);

                    for (int a = 1; a < unparsedAssets.Length; a++)
                    {
                        string[] features = unparsedAssets[a].Split(FEATURE_DELIM);

                        string assetName = features[0].Trim();
                        string assetPath = "Assets/" + NEW_ASSET_DIR + fileDirPair[1] + "/" + assetName + ".asset";

                        CelestialAsset currAsset = AssetDatabase.LoadAssetAtPath<CelestialAsset>(assetPath);
                        bool currAssetExists = true;
                        if (currAsset == null)
                        {
                            // Create new asset if none exists
                            currAsset = ScriptableObject.CreateInstance<CelestialAsset>();
                            currAssetExists = false;
                            Debug.Log("[CelestialAssetCreator] Creating new asset " + assetName + ".");
                        }
                        else
                        {
                            // Modify existing asset
                            Debug.Log("[CelestialAssetCreator] Modifying existing asset " + assetName + ".");
                        }

                        currAsset.DisplayName = string.Empty;

                        for (int f = 1; f < features.Length; f++)
                        {
                            string feature = features[f];
                            int spaceIndex = feature.IndexOf(" ");
                            if (spaceIndex == -1) { continue; }
                            string id = feature.Substring(0, spaceIndex).ToLower();
                            string data = feature.Substring(spaceIndex).Trim();
                            string workingData = data;

                            if (id.Contains(NAME_ID))
                            {
                                ReadName(ref currAsset, workingData);
                            }
                            else if (id.Contains(COORD_ID))
                            {
                                ReadCoords(ref currAsset, workingData);
                            }
                            else if (id.Contains(CATEGORY_ID))
                            {
                                ReadCategory(ref currAsset, workingData);
                            }
                            else if (id.Contains(REF_ENTRY_ID))
                            {
                                ReadReferenceEntry(ref currAsset, workingData);
                            }
                            else if (id.Contains(CONSTELLATION_ID))
                            {
                                ReadConstellation(ref currAsset, workingData);
                            }
                            else if (id.Contains(TEMPERATURE_ID))
                            {
                                ReadTemperature(ref currAsset, workingData);
                            }
                            else if (id.Contains(COLOR_ID))
                            {
                                ReadColor(ref currAsset, workingData);
                            }
                            else if (id.Contains(APP_MAG_ID))
                            {
                                ReadAppMagnitude(ref currAsset, workingData);
                            }
                            else if (id.Contains(ABS_MAG_ID))
                            {
                                ReadAbsMagnitude(ref currAsset, workingData);
                            }
                            else if (id.Contains(SPECTROGRAPH_ID))
                            {
                                ReadSpectrograph(ref currAsset, workingData);
                            }
                            else if (id.Contains(DISTANCE_ID))
                            {
                                ReadDistance(ref currAsset, workingData);
                            }
                        }

                        // if asset does not currently exist, create a new one
                        if (!currAssetExists)
                        {
                            AssetDatabase.CreateAsset(currAsset, assetPath);
                        }
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    reader.Close();
                }
                catch (FileNotFoundException ex)
                {
                    continue;
                }
            }

            Debug.Log("[CelestialAssetCreator] Loading Completed!");
        }

        #region Helpers

        private static void ReadName(ref CelestialAsset currAsset, string workingData)
        {
            currAsset.DisplayName = workingData;
        }

        private static void ReadCoords(ref CelestialAsset currAsset, string workingData)
        {
            currAsset.Coords = new EqCoords();
            int hrs;
            int mins;
            float secs;

            // RA
            int openIdx = workingData.IndexOf(SUBGROUP_OPEN) + 1;
            int closeIdx = workingData.IndexOf(SUBGROUP_CLOSE);
            string ra = workingData.Substring(openIdx, closeIdx - openIdx);
            string[] vals = ra.Split(SUBGROUP_SEP, System.StringSplitOptions.RemoveEmptyEntries);
            hrs = int.Parse(vals[0].Trim());
            mins = int.Parse(vals[1].Trim());
            secs = float.Parse(vals[2].Trim());

            currAsset.Coords.RightAscension = new HmsCoords(hrs, mins, secs);
            workingData = workingData.Substring(closeIdx + 1).Trim();

            // Declination
            openIdx = workingData.IndexOf(SUBGROUP_OPEN) + 1;
            closeIdx = workingData.IndexOf(SUBGROUP_CLOSE);
            string decl = workingData.Substring(openIdx, closeIdx - openIdx);
            vals = decl.Split(SUBGROUP_SEP, System.StringSplitOptions.RemoveEmptyEntries);
            hrs = int.Parse(vals[0].Trim());
            mins = int.Parse(vals[1].Trim());
            secs = float.Parse(vals[2].Trim());
            currAsset.Coords.Declination = new DmsCoords(hrs, mins, secs);
        }

        private static void ReadCategory(ref CelestialAsset currAsset, string workingData)
        {
            if (Enum.TryParse<CelestialObjectCategory>(workingData, true, out CelestialObjectCategory category)) {
                currAsset.Category = category;
            }
            else {
                Debug.LogWarning("[CelestialAssetCreator] Unable to read category for asset " + currAsset.DisplayName);
            }
        }

        private static void ReadReferenceEntry(ref CelestialAsset currAsset, string workingData)
        {
            string assetName = workingData;
            string entryAssetPath = "Assets/" + ENTRY_DIR + assetName + ".asset";
            ReferenceEntry entryAsset = AssetDatabase.LoadAssetAtPath<ReferenceEntry>(entryAssetPath);
            if (entryAsset != null) {
                currAsset.ReferenceId = entryAsset.AssetId;
            }
        }

        private static void ReadConstellation(ref CelestialAsset currAsset, string workingData)
        {
            string assetName = workingData;
            string constellationAssetPath = "Assets/" + CONSTELLATION_DIR + assetName + ".asset";
            CelestialAsset constellationAsset = AssetDatabase.LoadAssetAtPath<CelestialAsset>(constellationAssetPath);
            if (constellationAsset != null) {
                currAsset.ConstellationId = constellationAsset.AssetId;
                currAsset.ConstellationName = workingData;
            }
        }

        private static void ReadTemperature(ref CelestialAsset currAsset, string workingData)
        {
            if (uint.TryParse(workingData, out uint temperature)) {
                currAsset.Temperature = temperature;
            }
        }

        private static void ReadColor(ref CelestialAsset currAsset, string workingData)
        {
            string assetName = workingData;
            string colorAssetPath = "Assets/" + COLOR_DIR + assetName + ".asset";
            ReferenceColor colorAsset = AssetDatabase.LoadAssetAtPath<ReferenceColor>(colorAssetPath);
            if (colorAsset != null)
            {
                currAsset.ColorId = colorAsset.AssetId;
            }
        }

        private static void ReadAppMagnitude(ref CelestialAsset currAsset, string workingData)
        {
            if (float.TryParse(workingData, out float magnitude)) {
                currAsset.ApparentMagnitude = magnitude;
            }
        }

        private static void ReadAbsMagnitude(ref CelestialAsset currAsset, string workingData)
        {
            if (float.TryParse(workingData, out float magnitude)) {
                currAsset.AbsoluteMagnitude = magnitude;
            }
        }

        private static void ReadSpectrograph(ref CelestialAsset currAsset, string workingData)
        {
            SpectrographMaterialMask finalMask = 0;

            string[] vals = workingData.Split(SUBGROUP_SEP, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string val in vals) {
                if (Enum.TryParse<SpectrographMaterialMask>(val, true, out SpectrographMaterialMask valMask)) {
                    finalMask |= valMask;
                }
                else {
                    Debug.LogWarning("[CelestialAssetCreator] Unable to read a spectrograph material for asset " + currAsset.DisplayName + ": " + val);
                }
            }

            currAsset.Spectrograph = finalMask;
        }

        private static void ReadDistance(ref CelestialAsset currAsset, string workingData)
        {
            if (float.TryParse(workingData, out float distance)) {
                currAsset.Distance = distance;
            }
        }

        #endregion // Helpers

#endif // UNITY_EDITOR

    }
}