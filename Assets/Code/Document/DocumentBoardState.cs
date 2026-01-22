
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BeauUtil;
using BeauRoutine;
using FieldDay;
using FieldDay.HID;
using Leaf.Runtime;
using FieldDay.Assets;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Audio;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using EasyAssetStreaming;
using FieldDay.Scenes;
using FieldDay.UI;

namespace Astro {
    public sealed class DocumentBoardState : SharedStateComponent, IRegistrationCallbacks, ISceneLoadDependency {
        public bool EnableDocumentInteraction;
        [NonSerialized] public DocumentInteractable SelectedDocument;
        [NonSerialized] public Vector3 LastMousePos;
        [NonSerialized] public bool InteractedThisFrame;
        [NonSerialized] public Vector3 StoredDocPos;
        [NonSerialized] public bool OverrideStoredDoc;
        [NonSerialized] public Vector3 OverrideStoredDocPos;
        [NonSerialized] public DocumentInteractable DocZoomed;
        [NonSerialized] public Routine SpawnDocumentToCamera;
        [NonSerialized] public RingBuffer<Routine> DocumentLoadQueue = new RingBuffer<Routine>();

        [NonSerialized] public Routine DocumentReturnToBoard;
        [NonSerialized] public Routine DocumentBringToCam;

        public Transform DocumentParent;
        public Rect DraggableBounds;

        public bool DraggablePlacedThisFrame = false;
        public DocumentInteractable DraggablePlaced = null;

        public AssetPack DocumentAssets;

        [Header("Streaming Materials")]
        public Material AlphaStreamingMaterial;
        public Material OpaqueStreamingMaterial;
        public Material[] DebuggingMaterials;

        [NonSerialized] public List<DocumentRenderer> SpawnedDocuments = new List<DocumentRenderer>();
        [NonSerialized] public Dictionary<StringHash32, bool> DocumentCloseEnabledState = new Dictionary<StringHash32, bool>();

        [Header("Interact Settings")]
        [Range(0f, 1f)] public float FollowSpeed;
        public Vector3 DocHoverOffset;
        public Vector3 DocZoomOffset;

        public static readonly DocPartFunction[] ZoomActiveFunctions = new []{ DocPartFunction.Close, DocPartFunction.Flip };
        public static readonly DocPartFunction[] BoardActiveFunctions = new []{ DocPartFunction.Move, DocPartFunction.Zoom };

        void OnDrawGizmosSelected() { 
            var oldMatrix = Gizmos.matrix;
            Gizmos.color = Color.red;
            var position = DocumentParent.transform.position;
            var rotation = DocumentParent.transform.rotation;
            var scale = (Vector3) DraggableBounds.size;
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, scale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = oldMatrix;
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes.DeregisterLoadDependency(this);
        }

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            return DocumentLoadQueue.Count == 0;
        }
    }

    public static partial class DocumentUtility {
        #region Spawning

        public static DocumentRenderer SpawnDocument(DocumentAsset asset, StringHash32 id, out Vector3 pinnedPos, DocumentBoardState state = null, bool toBoard = false) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            DocumentRenderer spawned = GameObject.Instantiate(asset.Prefab, state.DocumentParent);
            spawned.Interactable.AssetName = id;
            spawned.TriggersPrompter = asset.TriggersPrompter;
            spawned.name = id.ToDebugString();

            state.SpawnedDocuments.Add(spawned);
            state.DocumentCloseEnabledState.TryAdd(spawned.Interactable.AssetName, true);

            if (toBoard && !asset.Prefab.AlwaysHighRes) {
                DisplayLowResDocument(spawned, asset);
            } else {
                DisplayFullDocument(spawned, asset);
            }
            AstroGame.Events.Dispatch(GameEvents.ActiveDocChanged, asset.name);
            if (asset.IsStickyNote) {
                AstroGame.Events.Dispatch(GameEvents.NewPostitReceived);
            }
            else {
                AstroGame.Events.Dispatch(GameEvents.NewDocReceived);
            }

            Material streamingMaterial = asset.UseCutoutMaterial ? state.AlphaStreamingMaterial : state.OpaqueStreamingMaterial;
            foreach(var quadTexture in spawned.StreamingTextures) {
                quadTexture.SharedMaterial = streamingMaterial;
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();

            var localPos = asset.DefaultPinnedPos;

            spawned.transform.localPosition = localPos;
            var pinnedPosCopy = pinnedPos = spawned.transform.position;

            // initially somewhere offscreen until visuals are loaded
            spawned.transform.position = new Vector3(-500, -500, 500);
            
            UpdateEnabledDocParts(spawned.Interactable, DocumentBoardState.BoardActiveFunctions);

            Action restoreDocPos = () => { 
                if (toBoard) { spawned.transform.localPosition = localPos; }
            };

            // start the load routine for the DocumentLoadSystem to listen to
            Routine loadRoutine = Routine.Null;
            loadRoutine = Routine.Start(AwaitDocLoadComplete(spawned)).OnComplete(restoreDocPos).OnStop(restoreDocPos);

            state.DocumentLoadQueue.PushBack(loadRoutine);

            return spawned;
        }

        [LeafMember("SpawnDocument")]
        public static void LeafSpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id), id, out Vector3 pinnedPos, Find.State<DocumentBoardState>(), true);
        }

        [LeafMember("SpawnDocumentToCamera")]
        public static void LeafSpawnDocumentToCamera(StringHash32 id) {
            DocumentBoardState state = Find.State<DocumentBoardState>();

            if (state.SpawnDocumentToCamera.Exists()) {
                // wait for previous document routine to complete
                state.SpawnDocumentToCamera.OnComplete(() => { state.SpawnDocumentToCamera.Replace(SpawnDocumentToCamera(state, id)); });
            } else {
                state.SpawnDocumentToCamera.Replace(SpawnDocumentToCamera(state, id));
            }
        }

        public static IEnumerator SpawnDocumentToCamera(DocumentBoardState state, StringHash32 id) {
            var asset = Find.NamedAsset<DocumentAsset>(id);
            DocumentRenderer spawned = SpawnDocument(asset, id, out Vector3 pinnedPos, state, false);

            // Init pinned position
            spawned.transform.SetParent(state.DocumentParent, false);
            if (spawned.transform.localPosition == Vector3.zero) {
                spawned.transform.localPosition = FindAvailablePos(state, spawned);
            }
            state.OverrideStoredDocPos = pinnedPos;
            state.OverrideStoredDoc = true;
            // Spawn below player view
            spawned.transform.SetParent(Game.Rendering.PrimaryCamera.transform, true);
            spawned.transform.localPosition = Vector3.zero;
            // Move to zoomed view
            ToggleZoomDoc(spawned.Interactable, false, state);
            SetDocumentInteractionEnabled(true);

            yield return null;
        }

        public static void SpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id), id, out Vector3 pinnedPos);
        }

        public static Vector3 FindAvailablePos(DocumentBoardState state, DocumentRenderer doc) {

            Vector2 finalPos = Vector3.zero;
            int maxTries = 10;
            for (int i = 0; i < maxTries; i++) {
                // random point on board
                float xExtents = state.DraggableBounds.x * 0.6f;
                float yExtents = state.DraggableBounds.y * 0.6f;
                var offset = state.DocumentParent.transform.position;
                Vector3 pos = offset + new Vector3(UnityEngine.Random.Range(-xExtents, xExtents), UnityEngine.Random.Range(-yExtents, yExtents), 0);
                Vector3 boxPos = pos - new Vector3(0, doc.Size.height / 2, 0);

                Vector3 docExtents = new Vector3(doc.Size.width, doc.Size.height, 5);
                if (!Physics.CheckBox(boxPos, docExtents, state.DocumentParent.transform.rotation, LayerMasks.DocumentInteract_Mask) || (i == maxTries - 1)) {
                    finalPos = pos - offset;
                    break;
                }
            }

            return finalPos;
        }

        public static bool OverlapBoxAtPos(DocumentBoardState state, DocumentRenderer doc, out DocumentRenderer hit) {
            hit = null;

            Vector3 docExtents = new Vector3(doc.Size.width / 2, doc.Size.height / 2, 1);
            var position = doc.transform.position + new Vector3(0f, doc.Size.y, 0f);

            var hits = Physics.OverlapBoxNonAlloc(position, docExtents, s_BoxOverlapWorkArray, doc.transform.rotation, LayerMasks.DocumentInteract_Mask);
            for (int i = 0; i < hits; i++) {
                var currPart = s_BoxOverlapWorkArray[i].GetComponent<DocumentPart>();
                var currRenderer = currPart ? currPart.Document.Renderer : null;
                if (currRenderer && !currRenderer.Interactable.AssetName.Equals(doc.Interactable.AssetName)) {
                    hit = currRenderer;
                    Array.Clear(s_BoxOverlapWorkArray, 0, hits);
                    return true;
                }
            }

            Array.Clear(s_BoxOverlapWorkArray, 0, hits);
            return false;
        }

        static private Collider[] s_BoxOverlapWorkArray = new Collider[16];

        #endregion // Spawning

        #region Enable/Disable
        public static void SetDocumentInteractionEnabled(bool enable, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            state.EnableDocumentInteraction = enable;
        }
        public static void EnableDocumentInteraction() {
            SetDocumentInteractionEnabled(true);
        }
        public static void DisableDocumentInteraction() {
            SetDocumentInteractionEnabled(false);
        }

        #endregion // Enable/Disable

        #region Interaction

        public static void ProcessDocPartInteraction(DocumentPart docPart, DocumentBoardState state) {
            if (state.SpawnDocumentToCamera.Exists()) {
                return;
            }
            switch (docPart.PartType) {
                case DocPartFunction.Move: {
                        StartMoveDoc(docPart.Document, docPart.Cursor, state);
                        break;
                    }
                case DocPartFunction.Zoom: {
                        ToggleZoomDoc(docPart.Document, true, state);
                        break;
                    }
                case DocPartFunction.Close: {
                        state.DocumentReturnToBoard.Replace(ReturnDocToBoard(docPart.Document, state));
                        break;
                    }
                case DocPartFunction.Flip: {
                        FlipDoc(docPart.Document, state);
                        break;
                    }
                default: {
                        break;
                    }
            }
        }

        public static void StartMoveDoc(DocumentInteractable newDoc, CursorHint partHint, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (state.SpawnDocumentToCamera.Exists() || state.DocZoomed) {
                return;
            }
            if (newDoc != null && state.SelectedDocument != newDoc) {
                state.SelectedDocument = newDoc;
                state.SpawnDocumentToCamera.Replace(ToggleDocHover(state.SelectedDocument.transform, state.DocHoverOffset));
                state.SelectedDocument.IsDragging = true;
                CursorHint.TryLock(partHint);
            } else {
                CursorHint.Unlock();
                state.SpawnDocumentToCamera.Replace(ToggleDocHover(state.SelectedDocument.transform, -state.DocHoverOffset));
                state.SelectedDocument.IsDragging = false;
                state.DraggablePlacedThisFrame = true;
                state.DraggablePlaced = state.SelectedDocument;
                state.SelectedDocument = null;
            }
            state.InteractedThisFrame = true;

            //check if we are currently over a puzzle doc
            DocumentRenderer hoverDoc = Find.State<DocumentPuzzleState>().CurrHoverDoc;
            if (hoverDoc != null && hoverDoc.TriggersPrompter) SetDocumentHighlight(hoverDoc, DocumentPuzzleState.DocHighlightColor);

            if (newDoc == null) {
                // AstroGame.Events.Dispatch(GameEvents.LatestMovedDocChanged, string.Empty);
            }
            else {
                var asset = Find.NamedAsset<DocumentAsset>(state.SelectedDocument.AssetName);
                AstroGame.Events.Dispatch(GameEvents.LatestMovedDocChanged, asset.name);
                if (asset.IsStickyNote) {
                    AstroGame.Events.Dispatch(GameEvents.GrabPostit);
                }
            }
        }

        public static void DeselectDocument(DocumentBoardState state) {
            StartMoveDoc(null, null, state);
        }

        public static void ToggleZoomDoc(DocumentInteractable doc, bool userInitiated, DocumentBoardState state = null) {
            if (doc == null) return;
            if (state == null) state = Find.State<DocumentBoardState>();

            if (state.SelectedDocument) {
                DeselectDocument(state);
            }

            if (state.DocZoomed) {
                // Return doc to board
                state.DocumentReturnToBoard.Replace(ReturnDocToBoard(doc, state));
            } else {
                // Bring doc to camera
                state.DocumentBringToCam.Replace(BringDocToCam(doc, state, userInitiated));
            }

            state.InteractedThisFrame = true;
        }

        public static void UpdateEnabledDocParts(DocumentInteractable doc, DocPartFunction[] mode) {
            DocumentBoardState state = Find.State<DocumentBoardState>();

            foreach (DocumentPart part in doc.Parts) {
                if (Array.IndexOf(mode, part.PartType) != -1) {
                    if (part.PartType == DocPartFunction.Close) {
                        if (state.DocumentCloseEnabledState[doc.AssetName]) {
                            part.gameObject.SetActive(true);
                        } else {
                            part.gameObject.SetActive(false);
                        }
                    } else {
                        part.gameObject.SetActive(true);
                    }
                } else {
                    part.gameObject.SetActive(false);
                }
            }
        }

        [LeafMember("ReturnDocToBoard")]
        private static void ReturnDocToBoardLeaf(StringHash32 docId) {
            DocumentBoardState state = Find.State<DocumentBoardState>();
            DocumentRenderer doc = state.SpawnedDocuments.Find(doc => doc.Interactable.AssetName == docId);

            if (doc == null) Debug.LogWarningFormat("[DocumentBoardState > ReturnDocToBoard] failed to find document {0}", docId.ToDebugString());

            state.DocumentReturnToBoard.Replace(ReturnDocToBoard(doc.Interactable, state));
        }

        private static IEnumerator ReturnDocToBoard(DocumentInteractable doc, DocumentBoardState state) {
            // Reveal the Pin object if we have one
            Transform pin = doc.transform.Find("Pin");
            if (pin != null) pin.gameObject.SetActive(true);

            // Always show the starting face of the documents
            if (doc.Flipped) FlipDoc(doc, false, state);

            SetInteractionLayer(state.DocZoomed, LayerMasks.DocumentInteract_Index);

            UpdateEnabledDocParts(doc, DocumentBoardState.BoardActiveFunctions);

            state.SpawnDocumentToCamera.Replace(MoveDocToPos(doc.transform, state.StoredDocPos))
                .OnComplete(() => {
                    DocumentRenderer renderer = doc.Renderer;
                    DocumentAsset asset = Find.NamedAsset<DocumentAsset>(doc.AssetName);

                    DisplayLowResDocument(renderer, asset);
                });                    

            state.StoredDocPos = Vector3.zero;
            state.DocZoomed = null;
            InputUtility.SetClickableMaskDefault(Find.State<InputState>());
            doc.transform.SetParent(state.DocumentParent, true);
            using (var table = TempVarTable.Alloc()) {
                table.Set("documentId", doc.AssetName);
                ScriptUtility.Trigger(ScriptEvents.DocumentInspectEnd, table);
            }

            DocumentAsset asset = Find.NamedAsset<DocumentAsset>(doc.AssetName);
            if (asset.IsStickyNote) {
                AstroGame.Events.Dispatch(GameEvents.PostitDismissed);
            } else {
                AstroGame.Events.Dispatch(GameEvents.DocDismissed);
            }

            AstroGame.Events.Dispatch(GameEvents.ActiveDocChanged, string.Empty);

            yield return null;
        }

        [LeafMember("BringDocToCam")]
        private static void BringDocToCamLeaf(StringHash32 docId) {
            DocumentBoardState state = Find.State<DocumentBoardState>();
            DocumentRenderer doc = state.SpawnedDocuments.Find(doc => doc.Interactable.AssetName == docId);

            if (doc == null) Debug.LogWarningFormat("[DocumentBoardState > BringDocToCam] failed to find document {0}", docId.ToDebugString());

            state.DocumentBringToCam.Replace(BringDocToCam(doc.Interactable, state, false)); 
        }

        private static IEnumerator BringDocToCam(DocumentInteractable doc, DocumentBoardState state, bool userInitiated) {
            // only allow one document at the camera at once
            if (state.DocZoomed != null) {
                state.DocumentReturnToBoard.Replace(ReturnDocToBoard(state.DocZoomed, state));
                // wait for other document to return to board
                while (state.SpawnDocumentToCamera.Exists() || state.DocumentReturnToBoard.Exists()) {
                    yield return null;
                }
            }

            // Hide the Pin object if we have one
            Transform pin = doc.transform.Find("Pin");
            if (pin != null) pin.gameObject.SetActive(false);
            
            if (state.OverrideStoredDoc) {
                state.StoredDocPos = state.OverrideStoredDocPos;
                state.OverrideStoredDoc = false;
            } else {
                state.StoredDocPos = doc.transform.position;
            }
            Vector3 zoomOffset = doc.Renderer.ZoomOffsetOverride == default ? state.DocZoomOffset : doc.Renderer.ZoomOffsetOverride;
            var viewState = Find.State<ViewState>();

            DocumentRenderer renderer = doc.Renderer;
            DocumentAsset asset = Find.NamedAsset<DocumentAsset>(doc.AssetName);

            DisplayFullDocument(renderer, asset);

            UpdateEnabledDocParts(doc, DocumentBoardState.ZoomActiveFunctions);

            state.SpawnDocumentToCamera.Replace(MoveDocToCam(viewState, doc.transform, Game.Rendering.PrimaryCamera.transform, zoomOffset))
                .OnComplete(() => {
                    using (var table = TempVarTable.Alloc()) {
                        table.Set("documentId", doc.AssetName);
                        ScriptUtility.Trigger(ScriptEvents.DocumentInspectStart, table);
                    }
                });

            state.DocZoomed = doc;
            doc.transform.SetParent(Game.Rendering.PrimaryCamera.transform, true);
            SetInteractionLayer(state.DocZoomed, LayerMasks.TopLayer_Index);
            // disallow selecting other documents while this loads
            InputUtility.SetClickableMaskTopLayer(Find.State<InputState>());

            AstroGame.Events.Dispatch(GameEvents.ActiveDocChanged, asset.name);
            if (userInitiated) {
                AstroGame.Events.Dispatch(GameEvents.DocViewed);
            }

            yield return null;
        }

        public static void OverrideStoredDocPos(DocumentInteractable doc, Vector3 newPos, DocumentBoardState state = null) {
            if (doc == null) {
                return;
            }
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }

            state.StoredDocPos = newPos;
        }

        public static void CancelZoom(DocumentBoardState state) {
            state.SpawnDocumentToCamera.OnComplete(() => ToggleZoomDoc(state.DocZoomed, true, state));
        }

        [LeafMember("FlipDoc")]
        private static void FlipDocLeaf(StringHash32 docId) {
            DocumentBoardState state = Find.State<DocumentBoardState>();
            DocumentRenderer doc = state.SpawnedDocuments.Find(doc => doc.Interactable.AssetName == docId);

            if (doc == null) Debug.LogWarningFormat("[DocumentBoardState > BringDocToCam] failed to find document {0}", docId.ToDebugString());

            FlipDoc(doc.Interactable, state);
        }

        public static void FlipDoc(DocumentInteractable doc, bool userInitiated, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (doc == null) {
                return;
            }
            doc.Flipped = !doc.Flipped;
            float angle = doc.Flipped ? 180 : 0;
            float lift = state.DocZoomed ? 0.5f : -0.5f;

            bool toFront = !doc.Flipped;
            if (userInitiated) {
                AstroGame.Events.Dispatch(GameEvents.DocFlipped, toFront);
            }

            state.SpawnDocumentToCamera.Replace(DocRotateY(doc, lift, angle));
            state.SpawnDocumentToCamera.OnStop(() => {
                Quaternion docRot = doc.BodyRoot.localRotation;
                doc.BodyRoot.Rotate(Vector3.up, 180f);
            });
            state.InteractedThisFrame = true;

            using (var table = TempVarTable.Alloc()) {
                table.Set("documentId", doc.AssetName);
                ScriptUtility.Trigger(ScriptEvents.DocumentInspectFlip, table);
            }
        }

        #endregion // Interaction

        #region Routines
        /// <summary>
        /// Used for moving question docs to a specific position relative to another document
        /// </summary>
        /// <param name="doc">the document the player has in hand</param>
        /// <param name="relativeTo">the document player is currently hovering over</param>
        public static IEnumerator MoveAboveRelativeToDoc(DocumentInteractable doc, DocumentRenderer relativeTo) {
            // align to bottom-left with out-sticking margin
            var margin = 0.2f;
            var localOffset = new Vector3(-relativeTo.Size.width / 2 + doc.Renderer.Size.width / 2 - margin, -relativeTo.Size.height + doc.Renderer.Size.height / 2, -0.25f);
            var globalOffset = relativeTo.transform.TransformVector(localOffset);
            Vector3 targetPos = relativeTo.transform.position + globalOffset;
            yield return doc.transform.MoveTo(targetPos, 0.2f).Ease(Curve.CubeIn);
        }

        private static IEnumerator ToggleDocHover(Transform doc, Vector3 hoverOffset) {
            yield return doc.MoveTo(doc.localPosition + hoverOffset, 0.2f, Axis.XYZ, Space.Self).Ease(Curve.CubeIn);
            yield return null;
        }

        private static IEnumerator AwaitDocLoadComplete(DocumentRenderer doc) {
            while (!IsFullyLoaded(doc)) yield return null;
        }

        private static IEnumerator MoveDocToCam(ViewState viewState, Transform doc, Transform cam, Vector3 offset) {
            // don't move doc until camera has finished moving
            while (viewState.ActiveTransitionRoutine.Exists()) {
                yield return null;
            }
            var newOffset = cam.TransformVector(offset);
            PlayLiftSound(doc.GetComponent<DocumentRenderer>());
            yield return Routine.Combine(
                doc.MoveTo(cam.position + newOffset, 0.5f).Ease(Curve.QuartInOut),
                doc.RotateTo(cam, 0.3f));
            yield return null;
        }
        
        private static IEnumerator MoveDocToPos(Transform doc, Vector3 pos) {
            PlayDropSound(doc.GetComponent<DocumentRenderer>());
            yield return Routine.Combine(
                doc.MoveTo(pos, 0.5f).Ease(Curve.QuartInOut),
                doc.RotateTo(0f, 0.3f, Axis.XYZ, Space.Self));
            yield return null;
        }

        private static IEnumerator DocRotateY(DocumentInteractable doc, float lift, float angle) {
            // Pick document up
            yield return Routine.Combine(
                doc.transform.MoveTo(doc.transform.localPosition.z + lift, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeIn).ForceOnCancel(),
                doc.BodyRoot.MoveTo(doc.BodyRoot.localPosition.y - 0.1f, 0.2f, Axis.Y, Space.Self).Ease(Curve.CubeIn).ForceOnCancel()
            );

            // Flip it
            PlayFlipSound(doc.GetComponent<DocumentRenderer>());
            yield return doc.BodyRoot.RotateTo(doc.BodyRoot.localRotation.y + angle, 0.3f, Axis.Y, Space.Self, AngleMode.Absolute).Ease(Curve.SineInOut).ForceOnCancel();
            
            // Put document back
            yield return Routine.Combine(
                doc.BodyRoot.MoveTo(doc.BodyRoot.localPosition.y + 0.1f, 0.2f, Axis.Y, Space.Self).Ease(Curve.CubeIn).ForceOnCancel(),
                doc.transform.MoveTo(doc.transform.localPosition.z - lift, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeIn).ForceOnCancel()
            );
            yield return null;
        }

        #endregion // routines

        #region Sfx

        static public void PlayLiftSound(DocumentRenderer doc) {
            if (doc.Thickness == DocumentThickness.Thick) {
                Sfx.Play("Oneshot.Document.Thick.Lift", doc.transform);
            } else {
                Sfx.Play("Oneshot.Document.Thin.Lift", doc.transform);
            }
        }

        static public void PlayFlipSound(DocumentRenderer doc) {
            if (doc.Thickness == DocumentThickness.Thick) {
                Sfx.Play("Oneshot.Document.Thick.Flip", doc.transform);
            } else {
                Sfx.Play("Oneshot.Document.Thin.Flip", doc.transform);
            }
        }

        static public void PlayDropSound(DocumentRenderer doc) {
            if (doc.Thickness == DocumentThickness.Thick) {
                Sfx.Play("Oneshot.Document.Thick.Drop", doc.transform);
            } else {
                Sfx.Play("Oneshot.Document.Thin.Drop", doc.transform);
            }
        }

        #endregion // Sfx

        #region Debugging

        [DebugMenuFactory]
        static private DMInfo GenerateDebugMenu() {
            DMInfo documents = new DMInfo("Documents");

            documents.AddButton("Force Cutout Material", () => {
                DEBUG_ChangeAllDocumentStreamingRenderers(Find.State<DocumentBoardState>().DebuggingMaterials[0]);
            }, () => SceneUtils.ActiveSceneIndex() == 4);
            documents.AddButton("Force Cutout Material (Transparent)", () => {
                DEBUG_ChangeAllDocumentStreamingRenderers(Find.State<DocumentBoardState>().DebuggingMaterials[1]);
            }, () => SceneUtils.ActiveSceneIndex() == 4);
            documents.AddButton("Force Cutout Material (No Vert Color)", () => {
                DEBUG_ChangeAllDocumentStreamingRenderers(Find.State<DocumentBoardState>().DebuggingMaterials[2]);
            }, () => SceneUtils.ActiveSceneIndex() == 4);
            documents.AddButton("Force Opaque Material", () => {
                DEBUG_ChangeAllDocumentStreamingRenderers(Find.State<DocumentBoardState>().DebuggingMaterials[3]);
            }, () => SceneUtils.ActiveSceneIndex() == 4);
            documents.AddButton("Force Opaque Material (No Vert Color)", () => {
                DEBUG_ChangeAllDocumentStreamingRenderers(Find.State<DocumentBoardState>().DebuggingMaterials[4]);
            }, () => SceneUtils.ActiveSceneIndex() == 4);

            return documents;
        }

        static private void DEBUG_ChangeAllDocumentStreamingRenderers(Material material) {
            foreach(var documentRenderer in UnityEngine.Object.FindObjectsOfType<DocumentRenderer>(true)) {
                foreach (var streaming in documentRenderer.GetComponentsInChildren<StreamingQuadTexture>(true)) {
                    streaming.SharedMaterial = material;
                }
            }
        }

        #endregion // Debugging
    }
}