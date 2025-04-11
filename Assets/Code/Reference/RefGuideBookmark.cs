using System;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro.Reference {
    public sealed class RefGuideBookmark : BatchedComponent {
        public Transform Group;
        public ActiveGroup Contents;
        public Collider Clickable;
        [AssetName(typeof(ReferencePageAsset))] public StringHash32 Page;

        [NonSerialized] public int CachedAbsolutePageNum;
    }
}