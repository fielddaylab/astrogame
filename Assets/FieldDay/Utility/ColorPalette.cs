using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay {
    #region Palettes

    /// <summary>
    /// Color32 2-color palette.
    /// </summary>
    [Serializable]
    public struct ColorPalette2 : IEquatable<ColorPalette2> {
        public Color32 Content;
        public Color32 Background;

        public ColorPalette2(Color32 content, Color32 background) {
            Content = content;
            Background = background;
        }

        #region Interfaces

        public bool Equals(ColorPalette2 other) {
            return Colors.Equals32(Content, other.Content)
                && Colors.Equals32(Background, other.Background);
        }

        #endregion // Interfaces

        #region Operators

        static public implicit operator ColorPalette2F(ColorPalette2 palette) {
            return new ColorPalette2F(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette4(ColorPalette2 palette) {
            return new ColorPalette4(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette4F(ColorPalette2 palette) {
            return new ColorPalette4F(palette.Content, palette.Background);
        }

        #endregion // Operators
    }

    /// <summary>
    /// Color 2-color palette.
    /// </summary>
    [Serializable]
    public struct ColorPalette2F : IEquatable<ColorPalette2F> {
        public Color Background;
        public Color Content;

        public ColorPalette2F(Color content, Color background) {
            Content = content;
            Background = background;
        }

        #region Interfaces

        public bool Equals(ColorPalette2F other) {
            return Content == other.Content
                && Background == other.Background;
        }

        #endregion // Interfaces

        #region Operators

        static public implicit operator ColorPalette2(ColorPalette2F palette) {
            return new ColorPalette2(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette4(ColorPalette2F palette) {
            return new ColorPalette4(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette4F(ColorPalette2F palette) {
            return new ColorPalette4F(palette.Content, palette.Background);
        }

        #endregion // Operators
    }

    /// <summary>
    /// Color32 4-color palette.
    /// </summary>
    [Serializable]
    public struct ColorPalette4 : IEquatable<ColorPalette4> {
        public Color32 Content;
        public Color32 Background;
        public Color32 Highlight;
        public Color32 Shadow;

        public ColorPalette4(Color32 content, Color32 background) {
            Content = content;
            Background = background;
            Highlight = Color32.LerpUnclamped(content, Color.white, 0.25f);
            Shadow = Color32.LerpUnclamped(content, Color.black, 0.25f);
        }

        public ColorPalette4(Color32 content, Color32 background, Color32 highlight, Color32 shadow) {
            Content = content;
            Background = background;
            Highlight = highlight;
            Shadow = shadow;
        }

        #region Interfaces

        public bool Equals(ColorPalette4 other) {
            return Colors.Equals32(Content, other.Content)
                && Colors.Equals32(Background, other.Background)
                && Colors.Equals32(Highlight, other.Highlight)
                && Colors.Equals32(Shadow, other.Shadow);
        }

        #endregion // Interfaces

        #region Operators

        static public implicit operator ColorPalette2(ColorPalette4 palette) {
            return new ColorPalette2(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette2F(ColorPalette4 palette) {
            return new ColorPalette2F(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette4F(ColorPalette4 palette) {
            return new ColorPalette4F(palette.Content, palette.Background, palette.Highlight, palette.Shadow);
        }

        #endregion // Operators
    }

    /// <summary>
    /// Color 4-color palette.
    /// </summary>
    [Serializable]
    public struct ColorPalette4F : IEquatable<ColorPalette4F> {
        public Color Content;
        public Color Background;
        public Color Highlight;
        public Color Shadow;

        public ColorPalette4F(Color content, Color background) {
            Content = content;
            Background = background;
            Highlight = Color.LerpUnclamped(content, Color.white, 0.25f);
            Shadow = Color.LerpUnclamped(content, Color.black, 0.25f);
        }

        public ColorPalette4F(Color content, Color background, Color highlight, Color shadow) {
            Content = content;
            Background = background;
            Highlight = highlight;
            Shadow = shadow;
        }

        #region Interfaces

        public bool Equals(ColorPalette4F other) {
            return Content == other.Content
                && Background == other.Background
                && Highlight == other.Highlight
                && Shadow == other.Shadow;
        }

        #endregion // Interfaces

        #region Operators

        static public implicit operator ColorPalette2(ColorPalette4F palette) {
            return new ColorPalette2(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette2F(ColorPalette4F palette) {
            return new ColorPalette2F(palette.Content, palette.Background);
        }

        static public implicit operator ColorPalette4(ColorPalette4F palette) {
            return new ColorPalette4(palette.Content, palette.Background, palette.Highlight, palette.Shadow);
        }

        #endregion // Operators
    }

    #endregion // Palettes

    /// <summary>
    /// Color palette utilities.
    /// </summary>
    static public class ColorPalette {

        #region Lerp

        static public ColorPalette2 Lerp(ColorPalette2 a, ColorPalette2 b, float t) {
            return new ColorPalette2() {
                Content = Color32.LerpUnclamped(a.Content, b.Content, t),
                Background = Color32.LerpUnclamped(a.Background, b.Background, t)
            };
        }

        static public ColorPalette2F Lerp(ColorPalette2F a, ColorPalette2F b, float t) {
            return new ColorPalette2() {
                Content = Color.LerpUnclamped(a.Content, b.Content, t),
                Background = Color.LerpUnclamped(a.Background, b.Background, t)
            };
        }

        static public ColorPalette4 Lerp(ColorPalette4 a, ColorPalette4 b, float t) {
            return new ColorPalette4() {
                Content = Color32.LerpUnclamped(a.Content, b.Content, t),
                Background = Color32.LerpUnclamped(a.Background, b.Background, t),
                Highlight = Color32.LerpUnclamped(a.Highlight, b.Highlight, t),
                Shadow = Color32.LerpUnclamped(a.Shadow, b.Shadow, t)
            };
        }

        static public ColorPalette4F Lerp(ColorPalette4F a, ColorPalette4F b, float t) {
            return new ColorPalette4F() {
                Content = Color.LerpUnclamped(a.Content, b.Content, t),
                Background = Color.LerpUnclamped(a.Background, b.Background, t),
                Highlight = Color.LerpUnclamped(a.Highlight, b.Highlight, t),
                Shadow = Color.LerpUnclamped(a.Shadow, b.Shadow, t)
            };
        }

        #endregion // Lerp

        #region Apply

        static public void Apply(ColorPalette2 palette, ColorPaletteTargetSet2 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
        }

        static public void Apply(ColorPalette2 palette, ColorPaletteTarget2 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
        }

        static public void Apply(ColorPalette2 palette, ColorChannelTarget content, ColorChannelTarget background) {
            content.Apply(palette.Content);
            background.Apply(palette.Background);
        }

        static public void Apply(ColorPalette2F palette, ColorPaletteTargetSet2 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
        }

        static public void Apply(ColorPalette2F palette, ColorPaletteTarget2 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
        }

        static public void Apply(ColorPalette2F palette, ColorChannelTarget content, ColorChannelTarget background) {
            content.Apply(palette.Content);
            background.Apply(palette.Background);
        }

        static public void Apply(ColorPalette4 palette, ColorPaletteTargetSet4 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
            target.Highlight.Apply(palette.Highlight);
            target.Shadow.Apply(palette.Shadow);
        }

        static public void Apply(ColorPalette4 palette, ColorPaletteTarget4 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
            target.Highlight.Apply(palette.Highlight);
            target.Shadow.Apply(palette.Shadow);
        }

        static public void Apply(ColorPalette4F palette, ColorPaletteTargetSet4 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
            target.Highlight.Apply(palette.Highlight);
            target.Shadow.Apply(palette.Shadow);
        }

        static public void Apply(ColorPalette4F palette, ColorPaletteTarget4 target) {
            target.Content.Apply(palette.Content);
            target.Background.Apply(palette.Background);
            target.Highlight.Apply(palette.Highlight);
            target.Shadow.Apply(palette.Shadow);
        }

        #endregion // Apply
    }

    #region Targets

    [Serializable]
    public struct ColorChannelTarget {
        public Graphic Graphic;
        public SpriteRenderer Sprite;
        public ColorGroup Group;

        [Il2CppSetOption(Option.NullChecks, false)]
        public void Apply(Color color) {
            if (Graphic) {
                Graphic.color = color;
            } else if (Sprite) {
                Sprite.color = color;
            } else if (Group) {
                Group.Color = color;
            }
        }

        /// <summary>
        /// Attempts to create a color channel target by analyzing the components on the given GameObject.
        /// </summary>
        static public bool TryCreate(GameObject obj, out ColorChannelTarget target) {
            if (obj.TryGetComponent(out Graphic graphic)) {
                target = graphic;
                return true;
            } else if (obj.TryGetComponent(out SpriteRenderer sprite)) {
                target = sprite;
                return true;
            } else if (obj.TryGetComponent(out ColorGroup group)) {
                target = group;
                return true;
            } else {
                target = default;
                return false;
            }
        }

        /// <summary>
        /// Create a color channel target by analyzing the components on the given GameObject.
        /// </summary>
        static public ColorChannelTarget Create(GameObject obj) {
            if (obj.TryGetComponent(out Graphic graphic)) {
                return graphic;
            } else if (obj.TryGetComponent(out SpriteRenderer sprite)) {
                return sprite;
            } else if (obj.TryGetComponent(out ColorGroup group)) {
                return group;
            } else {
                return default;
            }
        }

        #region Operators

        static public implicit operator ColorChannelTarget(Graphic graphic) {
            return new ColorChannelTarget() {
                Graphic = graphic
            };
        }

        static public implicit operator ColorChannelTarget(SpriteRenderer sprite) {
            return new ColorChannelTarget() {
                Sprite = sprite
            };
        }

        static public implicit operator ColorChannelTarget(ColorGroup group) {
            return new ColorChannelTarget() {
                Group = group
            };
        }

        #endregion // Operators
    }

    [Serializable]
    public struct ColorPaletteTarget2 {
        public ColorChannelTarget Content;
        public ColorChannelTarget Background;
    }

    [Serializable]
    public struct ColorPaletteTarget4 {
        public ColorChannelTarget Content;
        public ColorChannelTarget Background;
        public ColorChannelTarget Highlight;
        public ColorChannelTarget Shadow;
    }

    #endregion // Targets

    #region Target Sets

    [Serializable]
    public struct ColorChannelTargetSet {
        public Graphic[] Graphics;
        public SpriteRenderer[] Sprites;
        public ColorGroup[] Groups;

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Apply(Color color) {
            for (int i = 0, len = Graphics.Length; i < len; i++) {
                Graphics[i].color = color;
            }
            for (int i = 0, len = Sprites.Length; i < len; i++) {
                Sprites[i].color = color;
            }
            for (int i = 0, len = Groups.Length; i < len; i++) {
                Groups[i].Color = color;
            }
        }

        public bool IsEmpty {
            get {
                return IsNullOrEmpty(Graphics) && IsNullOrEmpty(Sprites) && IsNullOrEmpty(Groups);
            }
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsNullOrEmpty<T>(T[] array) {
            return array == null || array.Length <= 0;
        }
    }


    [Serializable]
    public struct ColorPaletteTargetSet2 {
        public ColorChannelTargetSet Content;
        public ColorChannelTargetSet Background;

        public bool IsEmpty {
            get {
                return Content.IsEmpty && Background.IsEmpty;
            }
        }
    }

    [Serializable]
    public struct ColorPaletteTargetSet4 {
        public ColorChannelTargetSet Content;
        public ColorChannelTargetSet Background;
        public ColorChannelTargetSet Highlight;
        public ColorChannelTargetSet Shadow;

        public bool IsEmpty {
            get {
                return Content.IsEmpty && Background.IsEmpty && Highlight.IsEmpty && Shadow.IsEmpty;
            }
        }
    }

    #endregion // Target Sets
}