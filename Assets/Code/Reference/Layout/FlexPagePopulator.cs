

using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(FlexPage))]
    public class FlexPagePopulator : MonoBehaviour {
        public FlexPage Page;
        public FlexReferencePageAsset RefAsset;

        [Header("Prefabs")]
        public FlexCell GraphicCell;
        public FlexCell TextCell;
        public FlexCell HeaderCell;
        public GameObject RowBreak;
    }
}