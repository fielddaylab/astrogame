using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleStarButton : ScriptActorComponent, IScenePreload {
        public Collider Clickable;
        public CursorHint Cursor;
        public ViewLink Link;
        public ColorGroup FadeGroup;
        public ColorGroup GlowGroup;
        public TitleBillboard Billboarder;

        private Routine m_FadeRoutine;
        private Routine m_GlowRoutine;

        [LeafMember("SetClickable")]
        public void SetClickable(bool clickable) {
            Clickable.enabled = clickable;
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Cursor.onClick.Register(() => {
                using (TempVarTable vars = TempVarTable.Alloc()) {
                    vars.Set("actorId", ScriptUtility.ActorId(this));
                    ScriptUtility.Trigger("StarClicked", vars);
                }

                if (Link) {
                    ViewNavUtility.MoveByLink(Find.State<ViewState>(), Link);
                }
            });

            Cursor.OnHover.Register(OnHover);

            GlowGroup.SetAlpha(0);
            if (Link) {
                Link.OnActiveStateChanged.Register(OnActiveStateChanged);
            }

            return null;
        }

        private void OnHover(CursorHint hint, bool hovering) {
            m_GlowRoutine.Replace(this, Tween.Float(GlowGroup.GetAlpha(), hovering ? 1 : 0, GlowGroup.SetAlpha, 0.2f));
        }

        private void OnActiveStateChanged() {
            Billboarder.enabled = Link.LastKnownActiveState;
            if (Game.Scenes.IsMainLoading()) {
                FadeGroup.SetAlpha(Link.LastKnownActiveState ? 1 : 0);
            } else {
                m_FadeRoutine.Replace(this, Tween.Float(FadeGroup.GetAlpha(), Link.LastKnownActiveState ? 1 : 0, FadeGroup.SetAlpha, 0.5f));
            }
        }
    }
}