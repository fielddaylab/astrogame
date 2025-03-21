using BeauUtil;
using FieldDay;
using FieldDay.Jobs;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astro
{
    [PreloadOrder(-200)]
    public class SkyDomeLoader : MonoBehaviour, IScenePreload
    {
        public CelestialObject CelestialObjPrefab;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            SkyDome dome = Find.State<SkyDome>();
            SkyLayoutAsset layout = Find.GlobalAsset<SkyLayoutAsset>();

            Scene myScene = gameObject.scene;
            Vector3 center = dome.Position;

            CelestialObject[] allObjects = new CelestialObject[layout.AllCelestialObjs.Length];

            for(int i = 0; i < layout.AllCelestialObjs.Length; i++) {
                CelestialAsset resource = layout.AllCelestialObjs[i];

                CelestialObject obj = Instantiate(CelestialObjPrefab, dome.StarRoot);
                CelestialPositionerUtility.PositionObject(center, obj.transform, resource.Coords.RightAscension, resource.Coords.Declination);
                obj.Resource = resource;
                obj.name = resource.DisplayName;

                allObjects[i] = obj;

                yield return null;
            }

            Array.Sort(allObjects, (a, b) => {
                if (a.Resource.Visibility == b.Resource.Visibility) {
                    return a.Resource.AssetId.CompareTo(b.Resource.AssetId);
                }
                return a.Resource.Visibility - b.Resource.Visibility;
            });

            yield return null;

            dome.AllObjects = allObjects;
        }
    }
}