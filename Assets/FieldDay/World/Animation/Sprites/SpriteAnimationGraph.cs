using BeauUtil;
using FieldDay.Assets;
using System;
using UnityEngine;

namespace FieldDay.Animation.Sprites {
    [CreateAssetMenu(menuName = "Field Day/Sprite Animation Graph", order = 201)]
    public sealed class SpriteAnimationGraph : NamedAsset {
        [AssetName(typeof(SpriteAnimationClip))] public StringHash32[] ClipIds;
    }
}