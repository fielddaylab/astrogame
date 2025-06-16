using Astro;
using FieldDay.Components;
using System;
using UnityEngine;

public class ParallaxAnimation : BatchedComponent {
    #region Inspector
    public Transform SunSprite;
    public Transform EarthSprite;
    public float OrbitRadius;
    public Timer OrbitTimer;

    [NonSerialized] public float ScaleFactor;

    #endregion // Inspector



}