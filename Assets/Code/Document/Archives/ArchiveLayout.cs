using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [Serializable]
    public class ArchiveLayout
    {
        public List<DocumentAsset> Documents;
        // [NonSerialized] public Dictionary<StringHash32, Vector3> AssetPositions = new Dictionary<StringHash32, Vector3>();
    }
}