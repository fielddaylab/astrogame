using FieldDay.Assets;
using System;
using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/FlexPage")]
    public class FlexReferencePageAsset : NamedAsset {
        public PageLayout Layout;
        public TextData TextData;
        public TableData TableData;
        public Sprite Background;
    }

    public enum PageLayout {
        None = 0,
        ImageOnly,
        MovieOnly,
        TitleBody,
        Table,
    }

    [Serializable]
    public struct TextData {
        public string TitleText;
        public string BodyText;
    }


    [Serializable]
    public struct TableData {
        public string[] Headers;
        public bool[] ColumnHasImage;
        public RefGuideCell[] Cells;
    }


    [Serializable]
    public struct RefGuideCell {
        public string Text;
        public Sprite Image;
    }
}