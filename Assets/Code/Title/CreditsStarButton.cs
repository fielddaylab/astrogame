
using UnityEngine;
using System.Collections.Generic;

using FieldDay;
using BeauUtil;
using BeauRoutine;
using Leaf.Runtime;
using FieldDay.HID;
using FieldDay.Scenes;
using FieldDay.Scripting;
using BeauUtil.Debugger;

namespace Astro.Title {
    [RequireComponent(typeof(TitleBillboard))]
    public sealed class CreditsStarButton : ScriptActorComponent, IScenePreload {
        public Collider Clickable;
        public CursorHint Cursor;
        public ColorGroup FadeGroup;
        public ColorGroup GlowGroup;
        public ViewLink Link;

        [HideInInspector] public TitleBillboard Billboarder;

        private Routine m_FadeRoutine;
        private Routine m_GlowRoutine;

        [LeafMember("SetClickable")]
        public void SetClickable(bool clickable) {
            Clickable.enabled = clickable;
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Billboarder = GetComponent<TitleBillboard>();

            Cursor.onClick.Register(() => {
                using (TempVarTable vars = TempVarTable.Alloc()) {
                    vars.Set("actorId", ScriptUtility.ActorId(this));
                    ScriptUtility.Trigger("StarClicked", vars);
                }
                RegisterViewLink();

                LoadCredits();
            });

            Cursor.OnHover.Register(OnHover);

            GlowGroup.SetAlpha(0);
            if (Link) {
                RegisterViewLink();
            }

            return null;
        }

        private void LoadCredits(StringHash32 transitionType = default) {
            SceneReference sceneRef = SceneUtils.GetSceneByName("Credits");
            Game.Scenes.LoadMainScene(sceneRef, true, new MainSceneTransitionArgs() {
                TransitionType = transitionType
            });
        }

        private void OnHover(CursorHint hint, bool hovering) {
            m_GlowRoutine.Replace(this, Tween.Float(GlowGroup.GetAlpha(), hovering ? 1 : 0, GlowGroup.SetAlpha, 0.2f));
        }

        public void RegisterViewLink() {
            Link.OnActiveStateChanged.Register(OnActiveStateChanged);
        }

        public void OnActiveStateChanged() {
            Billboarder.enabled = Link.LastKnownActiveState;
            if (Game.Scenes.IsMainLoading()) {
                FadeGroup.SetAlpha(Link.LastKnownActiveState ? 1 : 0);
            } else {
                m_FadeRoutine.Replace(this, Tween.Float(FadeGroup.GetAlpha(), Link.LastKnownActiveState ? 1 : 0, FadeGroup.SetAlpha, 0.5f));
            }
        }
    }
}