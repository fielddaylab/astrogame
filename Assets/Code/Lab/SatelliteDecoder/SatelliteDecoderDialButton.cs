using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Astro
{
    public class SatelliteDecoderDialButton : BatchedComponent
    {
        public SatelliteDecoderDial Target;
        public int Vector; // how much to change the dial by and in which direction (up or down)
    }
}