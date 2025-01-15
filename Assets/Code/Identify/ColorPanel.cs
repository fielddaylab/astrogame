using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Components;
using BeauUtil;

namespace Astro
{
    public class ColorPanel : BatchedComponent
    {
        public MeshRenderer PanelMesh;
        public MeshRenderer IndicatorMesh;
        [ColorId] public StringHash32 ColorId;
    }
}
