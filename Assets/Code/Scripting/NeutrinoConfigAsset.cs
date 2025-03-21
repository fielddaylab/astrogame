using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using Leaf;
using System;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Neutrino Config")] 
    public sealed class NeutrinoConfigAsset : NamedAsset, IRegistrationCallbacks {
        [Header("Neutrino Origin")]
        public EqCoords NeutrinoCoordinates;

        [Header("Open ID Objects")]
        public CelestialAsset[] RelevantObjects;
        [NonSerialized] public StringHash32[] RelevantObjectIds;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            RelevantObjectIds = new StringHash32[RelevantObjects.Length];
            int index = 0;
            foreach (var obj in RelevantObjects) {
                RelevantObjectIds[index] = RelevantObjects[index].AssetId;
                index++;
            }
        }
    }
}