using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class ArchiveInteractable : BatchedComponent
    {
        [NonSerialized] public int ArchiveIndex;
        [NonSerialized] public Mesh Mesh;
    }
}