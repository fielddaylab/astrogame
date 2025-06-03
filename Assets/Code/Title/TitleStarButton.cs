using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.Scenes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleStarButton : BatchedComponent, IScenePreload {
        public Collider Clickable;
        public CursorHint Cursor;
        public ViewLink Link;
        public ColorGroup FadeGroup;
        public ColorGroup GlowGroup;
        public TitleBillboard Billboarder;

        private Routine m_FadeRoutine;
        private Routine m_GlowRoutine;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Cursor.onClick.Register(() => {
                ViewNavUtility.MoveByLink(Find.State<ViewState>(), Link);
            });

            Cursor.OnHover.Register(OnHover);

            GlowGroup.SetAlpha(0);
            Link.OnActiveStateChanged.Register(OnActiveStateChanged);
            return null;
        }

        private void OnHover(CursorHint hint, bool hovering) {
            m_GlowRoutine.Replace(this, Tween.Float(GlowGroup.GetAlpha(), hovering ? 1 : 0, GlowGroup.SetAlpha, 0.2f));
        }

        private void OnActiveStateChanged() {
            Billboarder.enabled = Link.LastKnownActiveState;
            m_FadeRoutine.Replace(this, Tween.Float(FadeGroup.GetAlpha(), Link.LastKnownActiveState ? 1 : 0, FadeGroup.SetAlpha, 0.5f));
        }
    }
}