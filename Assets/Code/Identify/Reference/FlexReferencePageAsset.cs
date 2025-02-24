using FieldDay.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AstroGame/Reference/FlexPage")]
public class FlexReferencePageAsset : NamedAsset {
    public PageLayout Type;
    public TableData TableData;
    public TextData TextData;
    public Sprite Background;
}

public enum PageLayout {
    None = 0,
    ImageOnly,
    MovieOnly,
    TitleBody,
    Table,
}

public struct TextData {
    public string TitleText;
    public string BodyText;
}


[Serializable]
public struct TableData {
    public string[] Headers;
    private bool[] ColumnHasImage;
    public RefGuideRow[] Rows;
}

[Serializable]
public struct RefGuideRow {
    public RefGuideCell[] Cells;
}

[Serializable]
public struct RefGuideCell {
    public string Text;
    public Sprite Image;
}
