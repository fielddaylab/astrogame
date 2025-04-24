using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 10000)]
    public sealed class ViewFreeLookSystem : ComponentSystemBehaviour<ViewFreeLook, ViewNode> {
        public override void ProcessWork(float deltaTime) {
            ViewState state = Find.State<ViewState>();
            InputState input = Find.State<InputState>();

            if (state.ActiveTransitionRoutine || !state.ActiveNode) {
                return;
            }

            if (!state.ActiveNode.TryGetComponent(out ViewFreeLook freeLook)) {
                return;
            }

            float scale = 1;

            if (ScriptUtility.CurrentCutscene.IsRunning() || input.ClickableLayerMask == 0 || Game.Input.AreRaycastsPaused()) {
                scale = freeLook.CutsceneScale;
            }

            Quaternion baseLook = freeLook.transform.rotation;
            Quaternion desiredRot = baseLook;
            Quaternion currentRot = state.Camera.RootTransform.rotation;
            Quaternion nextRot = currentRot;

            if (Input.mousePresent || Input.touchCount > 0) {
                Vector2 normalizedMouseViewportOffset = Input.mousePosition;
                normalizedMouseViewportOffset.x /= Screen.width;
                normalizedMouseViewportOffset.y /= Screen.height;
                Geom.Remap(normalizedMouseViewportOffset, DefaultViewport, Game.Rendering.VirtualViewport);

                normalizedMouseViewportOffset.x = (0.5f - normalizedMouseViewportOffset.x) * 2;
                normalizedMouseViewportOffset.y = (0.5f - normalizedMouseViewportOffset.y) * 2;

                float signX = Math.Sign(normalizedMouseViewportOffset.x);
                float signY = Math.Sign(normalizedMouseViewportOffset.y);

                float invDeadZone = 1f / (1 - freeLook.DeadZone);
                float invEdge = 1f / freeLook.Edge;

                float absX = Math.Max(0, Math.Abs(normalizedMouseViewportOffset.x) - freeLook.DeadZone) * invDeadZone * invEdge * scale;
                float absY = Math.Max(0, Math.Abs(normalizedMouseViewportOffset.y) - freeLook.DeadZone) * invDeadZone * invEdge * scale;

                float horRot = -freeLook.HorizontalRange * signX * Mathf.Clamp01(absX);
                float vertRot = freeLook.VerticalRange * signY * Mathf.Clamp01(absY);

                desiredRot *= Quaternion.Euler(vertRot, horRot, 0);
            }

            float slerpAmt = TweenUtil.Lerp(freeLook.LerpStrength, 1, deltaTime);
            nextRot = Quaternion.Slerp(currentRot, desiredRot, slerpAmt);
            Vector3 euler = nextRot.eulerAngles;
            euler.z = baseLook.eulerAngles.z;
            nextRot = Quaternion.Euler(euler);

            if (currentRot != desiredRot) {
                state.Camera.RootTransform.rotation = nextRot;
            }

        }

        static private readonly Rect DefaultViewport = new Rect(0, 0, 1, 1);
    }
}