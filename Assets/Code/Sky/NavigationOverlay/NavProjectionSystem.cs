using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.UI;
using static Astro.PuzzleAsset;

namespace Astro {
    public class NavProjectionSystem : SharedStateSystemBehaviour<NavProjectionState, PuzzleNavigationState> {
        public override bool HasWork() {
            bool hasWork = base.HasWork();
            if (m_StateA) {
                hasWork = hasWork && !m_StateA.Initialized && m_StateB.NavigationModeActive;
            }
            else { 
                return false; 
            }

            return hasWork;
        }

        public override void ProcessWork(float deltaTime) {
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

            // Remove any old projections
            for(int i = 0; i < m_StateA.OutlineGroup.childCount; i++) {
                Destroy( m_StateA.OutlineGroup.GetChild(i).gameObject );
            }

            for (int i = 0; i < puzzleState.ActivePuzzle.Rows.Length; i++) {
                CelestialAsset currAsset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[i].Object); 
                Vector3 assetPostion = CelestialPositionerUtility.GetObjectPosition(center, currAsset.Coords.RightAscension, currAsset.Coords.Declination);

                // Use UIFocus pool
                var navFocus = focusPools.Focii.Alloc(m_StateA.OutlineGroup);
                navFocus.name = currAsset.DisplayName + " (Navigation Outline)";
                InitNavRepresntation(navFocus, currAsset, DetermineSprite(currAsset.Category));
                
                outlineState.ActiveOutlines.PushBack(navFocus);

                Vector2 viewPoint = spaceCamera.WorldToViewportPoint(assetPostion);
                navFocus.Rect.anchorMin = navFocus.Rect.anchorMax = viewPoint;
            }

            // Create connection for constellations
            for(int i = 0; i < puzzleState.ActivePuzzle.Edges.Length; i++) {
                Edge e = puzzleState.ActivePuzzle.Edges[i];
                CelestialAsset ca1 = Find.NamedAsset<CelestialAsset>(e.Object1);
                CelestialAsset ca2 = Find.NamedAsset<CelestialAsset>(e.Object2);

                UIFocus focusA = outlineState.ActiveOutlines.Find(x => x.TargetData == ca1);
                UIFocus focusB = outlineState.ActiveOutlines.Find(x => x.TargetData == ca2);

                var connection = Instantiate(new GameObject(ca1.DisplayName + "_to_" + ca2.DisplayName, typeof(RectTransform)), m_StateA.OutlineGroup);
                connection.AddComponent<Image>();
                RectTransform connectionRect = connection.GetComponent<RectTransform>();

                Vector2 canvasSize = m_StateA.NavigationCanvas.GetComponent<RectTransform>().sizeDelta;
                float focusARadius = focusA.Rect.sizeDelta.x / 2;
                float focusBRadius = focusB.Rect.sizeDelta.x / 2;
                connectionRect.sizeDelta = new Vector2(5, Vector2.Distance(focusA.Rect.anchorMin * canvasSize, focusB.Rect.anchorMin * canvasSize) - focusARadius - focusBRadius);
                connectionRect.sizeDelta = new Vector2(5, Vector2.Distance(focusA.Rect.anchorMin * canvasSize, focusB.Rect.anchorMin * canvasSize) - 75);

                connectionRect.anchorMin = connectionRect.anchorMax = (focusA.Rect.anchorMax + focusB.Rect.anchorMax) / 2; 
                Vector2 vector = (focusA.Rect.anchorMax * canvasSize) - (focusB.Rect.anchorMin * canvasSize);
                float angle = Vector2.Angle(Vector2.up, vector);
                connectionRect.localEulerAngles = new Vector3(0, 0, angle);
            }

            // Okay now put the camera back
            WorldPositionUtility.TryLook(dome.transform, spaceCam.Camera.RootTransform, stashedPos);
            spaceCamera.fieldOfView = stashedFOV;
            m_StateA.Initialized = true;
        }

        private Sprite DetermineSprite(CelestialObjectCategory category) {
            switch(category)
            {
                case CelestialObjectCategory.Star:
                    return m_StateA.StarOutlineSprite;
                case CelestialObjectCategory.Planet:
                    return m_StateA.PlanetOutlineSprite;
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
            // float scaleFactor = 1.5f * Mathf.Pow(0.63f, asset.ApparentMagnitude);
            // focus.Rect.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            focus.TargetData = asset;
        }
    }
}