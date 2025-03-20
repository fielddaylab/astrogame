using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class ArchiveLayout
    {
        [NonSerialized] public Dictionary<StringHash32, Vector3> AssetPositions;
    }
}