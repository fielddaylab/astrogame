using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InteractRowSelector : BatchedComponent
    {
        public int Col = -1;
        public int SelectDir = 1; // 1 cycles forward, -1 cycles backward
    }
}