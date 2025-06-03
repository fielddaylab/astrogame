using System;
using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;

namespace Astro.Reference {
    [SysUpdate(GameLoopPhase.LateUpdate, -100)]
    public sealed class RefGuideControlAnimSystem : ComponentSystemBehaviour<RefGuideControlAnim, RefGuideControlPulseAnim> {
        public override void ProcessWork(float deltaTime) {
            float currTime = Time.time;
            foreach(var tuple in m_Components) {
                float offsetTime = currTime - tuple.Primary.Offset;
                float totalDuration = tuple.Primary.Duration + tuple.Primary.Pause;
                float pauseRatio = tuple.Primary.Pause / totalDuration;
                float cycle = (Mathf.Sin(Mathf.PI * 2 * (offsetTime % totalDuration) / totalDuration) + 1) / 2;
                cycle = Math.Max(0, cycle - pauseRatio) / (1 - pauseRatio);
                Color c = Color.LerpUnclamped(tuple.Secondary.MinColor, tuple.Secondary.MaxColor, cycle);
                tuple.Secondary.Renderer.color = c;
            }
        }
    }
}