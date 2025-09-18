using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public class NavProjectionState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public bool Initialized = false;
        // public Sprite StarOutlineSprite;

        public Color ConstellationEdgeColor = new Color(0.9490196f, 1, 0.9411765f, 1);
        public Color NavigationCompleteColor;
        
        public Canvas NavigationCanvas;
        public float EdgeInset = 38;
        [SerializeField] private float m_EdgeWidth = 10;
        public float EdgeWidth => m_EdgeWidth;
        public float DefaultEdgeAlpha = 1.0f;
        public RectTransform NavigationArrow;

        public RectTransform PuzzleReticleGroup;

        public RectTransform OutlineGroup;
        public float NonCriticalPuzzleEdgeAlpha = 0.005f;

        public RectTransform PuzzleOutlineGroup;
        public float CriticalPuzzleEdgeAlpha = 0.2f;

        public CanvasGroup BoarderGroup;

        [Header("Sprites")]
        public Sprite StarReticle;
        public Sprite NeutrinoReticleElbow;
        public Sprite ConstellationReticleElbow;

        public void OnRegister() {
            Game.Scenes.QueueOnLoad(() => {
                SpaceCameraState spaceCamState = Find.State<SpaceCameraState>();
                NavProjectionState navProjState = Find.State<NavProjectionState>();

                navProjState.NavigationCanvas.worldCamera = spaceCamState.Camera.Camera; 
                navProjState.NavigationCanvas.planeDistance = 0.5f; 

                navProjState.NavigationCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            });

            Game.Events.Register(GameEvents.StartPuzzleNavigation, NavigationCanvasUtil.SetupConstellationNavUI);
            Game.Events.Register(GameEvents.StopPuzzleMode, NavigationCanvasUtil.DisableConstellationNavUI);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.StartPuzzleNavigation, NavigationCanvasUtil.SetupConstellationNavUI);
            Game.Events.Deregister(GameEvents.StopPuzzleMode, NavigationCanvasUtil.DisableConstellationNavUI);
        }
    }

    static public partial class NavigationCanvasUtil {
        public static void SetupConstellationNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.NavigationArrow.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(true);
            state.OutlineGroup.gameObject.SetActive(true);

            // Set approrpiate elbow sprites
            foreach (Image image in state.BoarderGroup.transform.GetComponentsInChildren<Image>()) {
                image.sprite = state.ConstellationReticleElbow;
            }
            state.BoarderGroup.gameObject.SetActive(true);
            state.Initialized = false;

            InitNavProjectionSystem(state);
        }

        public static void DisableConstellationNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            state.OutlineGroup.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(false);
        }

        public static void SetupNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.OutlineGroup.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(true);
            // state.NavigationArrow.gameObject.SetActive(false);

            // Set approrpiate elbow sprites
            foreach (Image image in state.BoarderGroup.transform.GetComponentsInChildren<Image>()) {
                image.sprite = state.NeutrinoReticleElbow;
            }
            state.BoarderGroup.gameObject.SetActive(true);
        }

        public static void DisableActiveNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(true);
        }

        public static void DisableNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            state.OutlineGroup.gameObject.SetActive(false);
            state.BoarderGroup.gameObject.SetActive(false);
            state.NavigationArrow.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(false);
        }

        public static void UpdatedNeutrinoNavigationArrow(SpaceCameraState spaceCameraState) {
            NavProjectionState navProjectionState = Find.State<NavProjectionState>();
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            if (!config) return;

            SkyDome dome = Find.State<SkyDome>();

            EqCoords target = config.NeutrinoEvent.NeutrinoCoordinates;

            Vector3 targetPos = CelestialPositionerUtility.GetObjectPosition(dome.Position, target.RightAscension, target.Declination);

            Transform navCanvas = navProjectionState.NavigationCanvas.transform;
            RectTransform navArrow = navProjectionState.NavigationArrow;
            
            Vector3 viewPoint = spaceCameraState.Camera.Camera.WorldToViewportPoint(targetPos);

            bool isTargetVisible = viewPoint.z > 0;
            Vector2 direction = new Vector2(viewPoint.x - 0.5f, viewPoint.y - 0.5f);

            if (!isTargetVisible) direction = - direction;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Set the arrow's rotation
            navArrow.localEulerAngles = new Vector3(0, 0, angle - 90); // -90 because the arrow points up by default

            if (!IsTargetOnScreen(viewPoint)) {
                direction.Normalize();
                navArrow.gameObject.SetActive(true);
                
                if (!(Find.State<NavigationState>().CurrentNavigationMode != NavigationMode.Inactive)){
                    ScriptUtility.Trigger(ScriptEvents.OnLeaveNeutrinoRegion);
                }                  

                // Position the arrow within the screen bounds
                Vector2 canvasSize = navCanvas.GetComponent<RectTransform>().sizeDelta / 2;

                // Calculate the offset from center
                Vector2 offset = direction * canvasSize;

                offset.x = Mathf.Clamp(offset.x, -canvasSize.x + navArrow.rect.width, canvasSize.x - navArrow.rect.width);
                offset.y = Mathf.Clamp(offset.y, -canvasSize.y + navArrow.rect.height, canvasSize.y - navArrow.rect.height);
                
                // Set the arrow's position
                navArrow.anchoredPosition = offset;
            } else {
                navArrow.gameObject.SetActive(false);
            }
        }

        private static bool IsTargetOnScreen(Vector3 viewportPosition) {
            return viewportPosition.x > 0 
            && viewportPosition.x < 1 
            && viewportPosition.y > 0 
            && viewportPosition.y < 1 
            && viewportPosition.z > 0;
        }

        public static void AddPuzzleReticles() {
            NavProjectionState navProjectionState = Find.State<NavProjectionState>();
            PuzzleState puzzleState = Find.State<PuzzleState>();

            //Make a target reticle for each star
            for (int i = 0; i < 4; i++) {
                StringHash32 assetId = puzzleState.ActivePuzzle.Rows[i].Object;
                CelestialAsset currAsset = Find.NamedAsset<CelestialAsset>(assetId);
                UIFocus currFocus = FocusableUtility.GetFocusByData(assetId);

                Type[] components = { typeof(RectTransform), typeof(Image) };
                var reticle = new GameObject("PuzzleReticle", components);

                // Set-up the target reticle transform
                RectTransform reticleRectTransform = reticle.GetComponent<RectTransform>();
                reticleRectTransform.anchorMin = reticleRectTransform.anchorMax = new Vector2(0, 0);
                reticleRectTransform.SetRotation(Vector3.zero);
                reticleRectTransform.SetPosition(Vector3.zero);
                reticleRectTransform.SetScale(Vector3.one);
                reticleRectTransform.SetParent(navProjectionState.PuzzleReticleGroup.transform, false);

                FocusState focusState = Find.State<FocusState>();
                float scaleFactor = Mathf.Clamp(Mathf.Pow(focusState.BaseScale, currAsset.ApparentMagnitude) - focusState.ScaleOffset, focusState.MinScale, focusState.MaxScale);

                Vector3 center = Find.State<SkyDome>().Position;
                Vector2 canvasSize = navProjectionState.OutlineGroup.rect.size;
                Vector3 assetPostion = CelestialPositionerUtility.GetObjectPosition(center, currAsset.Coords.RightAscension, currAsset.Coords.Declination);

                reticleRectTransform.SetAnchorPos(Find.State<SpaceCameraState>().Camera.Camera.WorldToViewportPoint(assetPostion) * canvasSize);
                reticleRectTransform.sizeDelta = new Vector2(125f, 125f) * scaleFactor;

                // Assign the target reticle sprite
                reticle.GetComponent<Image>().sprite = navProjectionState.StarReticle;
            }

        }

        public static unsafe void InitNavProjectionSystem(NavProjectionState navState) {
            SkyDome dome = Find.State<SkyDome>();
            FocusPools focusPools = Find.State<FocusPools>();

            PuzzleState puzzleState = Find.State<PuzzleState>();
            SpaceCameraState spaceCam = Find.State<SpaceCameraState>();

            if (!puzzleState.ActivePuzzle) return;

            EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;
            Vector3 spaceCameraOriginalRot = spaceCam.Camera.RootTransform.localEulerAngles;

            // populate sky with celestial objects
            Vector3 center = dome.Position;

            Camera spaceCamera = spaceCam.Camera.Camera;

            // Hack: just point our camera at our target position to draw our navigation overlay
            float stashedFOV = spaceCamera.fieldOfView;
            WorldPositionUtility.LookAt(spaceCam, target);
            spaceCamera.fieldOfView = spaceCam.Camera.OriginalFOV / puzzleState.ActivePuzzle.PuzzleCameraZoom;

            // Remove any old projections
            for (int i = 0; i < navState.OutlineGroup.childCount; i++) {
                GameObject.Destroy(navState.OutlineGroup.GetChild(i).gameObject);
            }
            for (int i = 0; i < navState.PuzzleOutlineGroup.childCount; i++) {
                GameObject.Destroy(navState.PuzzleOutlineGroup.GetChild(i).gameObject);
            }

            Vector2 canvasSize = navState.OutlineGroup.rect.size;
            int starCount = puzzleState.ActivePuzzle.ConstellationStars.Length;
            StringHash32* starAssetIds = stackalloc StringHash32[starCount];
            Vector2* starAnchors = stackalloc Vector2[starCount];

            for (int i = 0; i < starCount; i++) {
                StringHash32 assetId = puzzleState.ActivePuzzle.ConstellationStars[i];
                starAssetIds[i] = assetId;
                CelestialAsset currAsset = Find.NamedAsset<CelestialAsset>(assetId);

                Vector3 assetPostion = CelestialPositionerUtility.GetObjectPosition(center, currAsset.Coords.RightAscension, currAsset.Coords.Declination);

                starAnchors[i] = spaceCamera.WorldToViewportPoint(assetPostion) * canvasSize;
            }

            // Create connection for constellations
            for (int i = 0; i < puzzleState.ActivePuzzle.Edges.Length; i++) {
                PuzzleAsset.Edge e = puzzleState.ActivePuzzle.Edges[i];

                int focusA = FindIndex(starAssetIds, starCount, e.Object1);
                int focusB = FindIndex(starAssetIds, starCount, e.Object2);

                Assert.True(focusA >= 0 && focusB >= 0);

                string displayName =
#if UNITY_EDITOR
                    string.Concat(e.Object1.ToDebugString(), "_to_", e.Object2.ToDebugString());
#else
                    "connection";
#endif // UNITY_EDITOR


                StringHash32[] puzzleStars = new StringHash32[puzzleState.ActivePuzzle.Rows.Length];
                for (int j = 0; j < puzzleState.ActivePuzzle.Rows.Length; j++) {
                    puzzleStars[j] = puzzleState.ActivePuzzle.Rows[j].Object;
                }

                var connection = new GameObject(displayName, typeof(RectTransform));

                RectTransform edgeParentObject;
                if (Array.IndexOf(puzzleStars, e.Object1) != -1 && Array.IndexOf(puzzleStars, e.Object2) != -1) {
                    // This is one of the stars in our puzzle
                    edgeParentObject = navState.PuzzleOutlineGroup;
                } else {
                    // This is a star in the constellation but not the puzzle
                    edgeParentObject = navState.OutlineGroup;
                }
                connection.transform.SetParent(edgeParentObject, false);

                connection.AddComponent<RoundedRectGraphic>().color = navState.ConstellationEdgeColor;
                RectTransform connectionRect = connection.GetComponent<RectTransform>();
                connectionRect.anchorMin = connectionRect.anchorMax = new Vector2(0, 0);

                Vector2 anchorA = starAnchors[focusA];
                Vector2 anchorB = starAnchors[focusB];

                connectionRect.anchoredPosition = (anchorA + anchorB) / 2;
                connectionRect.sizeDelta = new Vector2(navState.EdgeWidth, Vector2.Distance(anchorA, anchorB) - navState.EdgeInset);

                Vector2 vector = anchorA - anchorB;
                float angle = Vector2.SignedAngle(Vector2.up, vector);
                connectionRect.localEulerAngles = new Vector3(0, 0, angle);
            }

            navState.OutlineGroup.GetComponent<CanvasGroup>().alpha = navState.DefaultEdgeAlpha;

            // Okay now put that camera back where it came from, or so help me.
            WorldPositionUtility.ForceLocalRotation(spaceCam, spaceCameraOriginalRot);
            spaceCamera.fieldOfView = stashedFOV;
            navState.Initialized = true;
        }

        static private unsafe int FindIndex(StringHash32* buffer, int bufferSize, StringHash32 find) {
            for(int i = 0; i < bufferSize; i++) {
                if (buffer[i] == find) {
                    return i;
                }
            }

            return -1;
        }
    }
}