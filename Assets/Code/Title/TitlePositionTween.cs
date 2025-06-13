using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitlePositionTween : ScriptActorComponent {
        public Transform Position;
        public Transform Target;
        public Space Space = Space.Self;
        public TweenSettings MoveTween = new TweenSettings(1);
        public float MoveDelay;

        [NonSerialized] public Routine MoveRoutine;

        [LeafMember("PlayTween")]
        public void PlayTween() {
            MoveRoutine.Replace(this, Position.MoveTo(Target, MoveTween, Axis.XYZ, Space).DelayBy(MoveDelay));
        }
    }
}