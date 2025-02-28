using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public class NavProjectionSystem : SharedStateSystemBehaviour<NavProjectionState> {
        public override bool HasWork() {
            bool hasWork = base.HasWork();
            if (m_State) {
                hasWork = hasWork && !m_State.Initialized;
            }
            else { 
                return false; 
            }

            return hasWork;
        }

        public override void ProcessWork(float deltaTime) {
            var layout = Find.GlobalAsset<SkyLayoutAsset>();
            var dome = Find.State<SkyDome>();
            var focusPools = Find.State<FocusPools>();
            var outlineState = Find.State<OutlineState>();

            var puzzleState = Find.State<PuzzleState>();
            var spaceCam = Find.State<SpaceCameraState>();

            if (!puzzleState.ActivePuzzle) return;

            EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;
            Quaternion targetQuat = Quaternion.Euler(
                360 - (float)CoordinateUtility.DeclinationToDecimalDegrees(target.Declination),
                360 - (float)CoordinateUtility.RAToDegrees(target.RightAscension),
                0
            );
            Vector3 targetFoward = Geom.Forward(targetQuat);

            Quaternion spaceCameraQuat = spaceCam.Camera.RootTransform.rotation;
            // Vector3 spaceCamForward = Geom.Forward(spaceCameraQuat);

            // populate sky with celestial objects
            var center = dome.Position;

            Camera spaceCamera = spaceCam.Camera.Camera;

            // Hack: just point our camera at our target position to draw our navigation overlay
            EqCoords stashedPos = new EqCoords(CoordinateUtility.DegreesToRA(360 - spaceCameraQuat.eulerAngles.y), CoordinateUtility.DecimalDegreesToDeclination(360 - spaceCameraQuat.eulerAngles.x));
            float stashedFOV = spaceCamera.fieldOfView;
            WorldPositionUtility.TryLook(dome.transform, spaceCam.Camera.RootTransform, target);
            spaceCamera.fieldOfView = spaceCam.Camera.OriginalFOV / puzzleState.ActivePuzzle.PuzzleCameraZoom;
            // Matrix4x4 P = spaceCamera.projectionMatrix;
            // Matrix4x4 V = spaceCamera.transform.worldToLocalMatrix;
            // Matrix4x4 VP = P * V;

            for (int i = 0; i < puzzleState.ActivePuzzle.Rows.Length; i++) {
                CelestialAsset currAsset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[i].Object); 
                Vector3 assetPostion = CelestialPositionerUtility.GetObjectPosition(center, currAsset.Coords.RightAscension, currAsset.Coords.Declination);

                // Use UIFocus pool
                var navFocus = focusPools.Focii.Alloc(m_State.NavigationCanvas.transform);
                navFocus.name = currAsset.DisplayName + " (Navigation Outline)";
                InitNavRepresntation(navFocus, currAsset, DetermineSprite(currAsset.Category));
                
                outlineState.ActiveOutlines.PushBack(navFocus);

                Vector2 viewPoint = spaceCamera.WorldToViewportPoint(assetPostion);
                navFocus.Rect.anchorMin = navFocus.Rect.anchorMax = viewPoint;
            }

            // Okay now put the camera back
            WorldPositionUtility.TryLook(dome.transform, spaceCam.Camera.RootTransform, stashedPos);
            spaceCamera.fieldOfView = stashedFOV;
            m_State.Initialized = true;
        }

        private Sprite DetermineSprite(CelestialObjectCategory category) {
            switch(category)
            {
                case CelestialObjectCategory.Star:
                    return m_State.StarOutlineSprite;
                case CelestialObjectCategory.Planet:
                    return m_State.PlanetOutlineSprite;
                case CelestialObjectCategory.Satellite:
                    return null;
                case CelestialObjectCategory.Constellation:
                    return null;
                case CelestialObjectCategory.Galaxy:
                    return null;
                case CelestialObjectCategory.Comet:
                    return null;
                default:
                    return null;
            }
        }

        public static void InitNavRepresntation(UIFocus focus, CelestialAsset asset, Sprite represent2D) {
            focus.Represent2D.sprite = represent2D;
            if (represent2D == null) {
                focus.Represent2D.enabled = false;
            }
            focus.TargetData = asset;
        }
    }
}