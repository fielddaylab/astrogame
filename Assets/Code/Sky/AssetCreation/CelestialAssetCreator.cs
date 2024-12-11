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
    public class CelestialAssetCreator : MonoBehaviour
    {
        private const string ASSET_DELIM = "::";
        private const string FEATURE_DELIM = "@";
        private const string DEF_FILENAME = "CelestialAssetDefinitions";
        private const string NEW_ASSET_DIR = "_Assets/Data/Sky/Stars/";

        private const string NAME_ID = "name";
        private const string COORD_ID = "coords";
        private const string CATEGORY_ID = "category";
        private const string COLOR_ID = "color";

#if UNITY_EDITOR
        [MenuItem("Astro/Load Celestial Assets")]
        public static void LoadCelestialAssets()
        {
            Debug.Log("[CelestialAssetCreator] Loading Celestial Assets...");

            string path = Application.dataPath + "/Code/Sky/AssetCreation/" + DEF_FILENAME + ".txt";

            StreamReader reader = new StreamReader(path);

            string contents = reader.ReadToEnd();
            string[] unparsedAssets = contents.Split(ASSET_DELIM);

            for (int a = 1; a < unparsedAssets.Length; a++)
            {
                string[] features = unparsedAssets[a].Split(FEATURE_DELIM);

                string assetName = features[0].Trim();
                string assetPath = "Assets/" + NEW_ASSET_DIR + assetName + ".asset";

                CelestialAsset currAsset = AssetDatabase.LoadAssetAtPath<CelestialAsset>(assetPath);
                bool currAssetExists = true;
                if (currAsset == null) {
                    // Create new asset if none exists
                    currAsset = ScriptableObject.CreateInstance<CelestialAsset>();
                    currAssetExists = false;
                    Debug.Log("[CelestialAssetCreator] Creating new asset " + assetName + ".");
                }
                else {
                    // Modify existing asset
                    Debug.Log("[CelestialAssetCreator] Modifying existing asset " + assetName + ".");
                }

                currAsset.DisplayName = "TEST";

                for (int f = 1; f < features.Length; f++) {
                    string feature = features[f];
                    int spaceIndex = feature.IndexOf(" ");
                    if (spaceIndex == -1) { continue; }
                    string id = feature.Substring(0, spaceIndex).ToLower();
                    string data = feature.Substring(spaceIndex).Trim();

                    if (id.Contains(NAME_ID)) {
                        currAsset.DisplayName = data;
                    }
                    else if (id.Contains(COORD_ID))
                    {
                        // TODO
                        // newAsset.Coords = new EqCoords();
                    }
                    else if (id.Contains(CATEGORY_ID))
                    {
                        // TODO
                        // newAsset.Coords = new EqCoords();
                    }
                    else if (id.Contains(COLOR_ID)) {
                        // TODO
                        // newAsset.ColorId = data;
                    }
                }


                // if asset does not currently exist, create a new one
                if (!currAssetExists) {
                    AssetDatabase.CreateAsset(currAsset, assetPath);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            reader.Close();

            Debug.Log("[CelestialAssetCreator] Loading Completed!");
        }
#endif // UNITY_EDITOR
    }
}