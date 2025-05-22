using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.HID;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public sealed class DocumentBoardState : SharedStateComponent {
        public bool EnableDocumentInteraction;
        [NonSerialized] public DocumentInteractable SelectedDocument;
        [NonSerialized] public Vector3 LastMousePos;
        [NonSerialized] public bool InteractedThisFrame;
        [NonSerialized] public Vector3 StoredDocPos;
        [NonSerialized] public bool OverrideStoredDoc;
        [NonSerialized] public Vector3 OverrideStoredDocPos;
        [NonSerialized] public DocumentInteractable DocZoomed;
        [NonSerialized] public Routine DocumentRoutine;
        [NonSerialized] public Routine DocumentLoadRoutine;

        public Transform DocumentParent;
        public Rect DraggableBounds;

        public bool DraggablePlacedThisFrame = false;
        public DocumentInteractable DraggablePlaced = null;

        public AssetPack DocumentAssets;

        [NonSerialized] public List<DocumentRenderer> SpawnedDocuments = new List<DocumentRenderer>();
        [NonSerialized] public Dictionary<StringHash32, bool> DocumentCloseEnabledState = new Dictionary<StringHash32, bool>();

        [Header("Interact Settings")]
        [Range(0f, 1f)] public float FollowSpeed;
        public Vector3 DocHoverOffset;
        public Vector3 DocZoomOffset;

        public static readonly DocPartFunction[] ZoomActiveFunctions = new []{ DocPartFunction.Close, DocPartFunction.Flip };
        public static readonly DocPartFunction[] BoardActiveFunctions = new []{ DocPartFunction.Move, DocPartFunction.Zoom, DocPartFunction.Flip };

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
    }

    public static partial class DocumentUtility {
        #region Spawning

        public static DocumentRenderer SpawnDocument(DocumentAsset asset, StringHash32 id, out Vector3 pinnedPos, DocumentBoardState state = null, bool addToArchive = true, bool toBoard = false) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            DocumentRenderer spawned = GameObject.Instantiate(asset.Prefab, state.DocumentParent);
            spawned.Interactable.AssetName = id;
            spawned.PreserveInArchive = asset.PreserveInArchive;
            spawned.name = id.ToDebugString();

            state.SpawnedDocuments.Add(spawned);
            state.DocumentCloseEnabledState.TryAdd(spawned.Interactable.AssetName, true);

            if (toBoard && !asset.Prefab.AlwaysHighRes) {
                DocumentUtility.DisplayLowResDocument(spawned, asset);
            }
            else {
                DocumentUtility.DisplayFullDocument(spawned, asset);
            }

            spawned.transform.localPosition = asset.DefaultPinnedPos;
            var pinnedPosCopy = pinnedPos = spawned.transform.position;

            // place somewhere offscreen
            spawned.transform.position = new Vector3(-500, -500, 500);

            // load assets
            state.DocumentLoadRoutine.Replace(AwaitDocLoadComplete(spawned))
                .OnComplete(() => {
                    // restore doc position
                    if (toBoard) { spawned.transform.localPosition = asset.DefaultPinnedPos; }

                    // add asset to ArchiveState (usually if not being spawned from an Archive)
                    if (addToArchive && asset.PreserveInArchive)
                    {
                        var archiveState = Find.State<ArchiveState>();
                        ArchiveUtility.AddAssetToArchive(archiveState, id, pinnedPosCopy);
                    }
                });

            return spawned;
        }

        [LeafMember("SpawnDocument")]
        public static void LeafSpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id), id, out Vector3 pinnedPos, Find.State<DocumentBoardState>(), true, true);
        }

        [LeafMember("SpawnDocumentToCamera")]
        public static void LeafSpawnDocumentToCamera(StringHash32 id) {
            DocumentBoardState state = Find.State<DocumentBoardState>();
            ArchiveState archiveState = Find.State<ArchiveState>();

            if (state.DocumentRoutine.Exists()) {
                // wait for previous document routine to complete
                state.DocumentRoutine.OnComplete(() => { state.DocumentRoutine.Replace(SpawnDocumentToCamera(state, archiveState, id)); });
            }
            else {
                state.DocumentRoutine.Replace(SpawnDocumentToCamera(state, archiveState, id));
            }
        }

        public static IEnumerator SpawnDocumentToCamera(DocumentBoardState state, ArchiveState archiveState, StringHash32 id) {
            var asset = Find.NamedAsset<DocumentAsset>(id);
            var spawned = SpawnDocument(asset, id, out Vector3 pinnedPos, state, true, false);

            // wait for assets to load
            while (state.DocumentLoadRoutine.Exists()) {
                yield return null;
            }

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
            ToggleZoomDoc(spawned.Interactable, state);
            SetDocumentInteractionEnabled(true);
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

            var hits = Physics.OverlapBox(position, docExtents, doc.transform.rotation, LayerMasks.DocumentInteract_Mask);
            for (int i = 0; i < hits.Length; i++) {
                var currPart = hits[i].GetComponent<DocumentPart>();
                var currRenderer = currPart ? currPart.Document.Renderer : null;
                if (currRenderer && !currRenderer.Interactable.AssetName.Equals(doc.Interactable.AssetName)) {
                    hit = currRenderer;
                    return true;
                }
            }

            return false;
        }

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
            if (state.DocumentRoutine.Exists()) {
                return;
            }
            switch (docPart.PartType) {
                case DocPartFunction.Move: {
                        StartMoveDoc(docPart.Document, docPart.Cursor, state);
                        break;
                    }
                case DocPartFunction.Zoom: {
                        ToggleZoomDoc(docPart.Document, state);
                        break;
                    }
                case DocPartFunction.Close: {
                        ReturnDocToBoard(docPart.Document, state);
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
            if (state.DocumentRoutine.Exists() || state.DocZoomed) {
                return;
            }
            if (newDoc != null && state.SelectedDocument != newDoc) {
                state.SelectedDocument = newDoc;
                state.DocumentRoutine.Replace(ToggleDocHover(state.SelectedDocument.transform, state.DocHoverOffset)); // 
                state.SelectedDocument.IsDragging = true;
                CursorHint.TryLock(partHint);
            } else {
                CursorHint.Unlock();
                state.DocumentRoutine.Replace(ToggleDocHover(state.SelectedDocument.transform, -state.DocHoverOffset));
                state.SelectedDocument.IsDragging = false;
                state.DraggablePlacedThisFrame = true;
                state.DraggablePlaced = state.SelectedDocument;
                state.SelectedDocument = null;
            }
            state.InteractedThisFrame = true;

            //check if we are currently over a puzzle doc
            DocumentRenderer hoverDoc = Find.State<DocumentPuzzleState>().CurrHoverDoc;
            if (hoverDoc != null) SetDocumentHighlight(hoverDoc, DocumentPuzzleState.DocHighlightColor);
        }

        public static void DeselectDocument(DocumentBoardState state) {
            StartMoveDoc(null, null, state);
        }

        public static void ToggleZoomDoc(DocumentInteractable doc, DocumentBoardState state = null) {
            if (doc == null) return;
            if (state == null) state = Find.State<DocumentBoardState>();

            if (state.SelectedDocument) {
                DeselectDocument(state);
            }

            if (state.DocZoomed) {
                // Return doc to board
                ReturnDocToBoard(doc, state);
            } else {
                // Bring doc to camera
                BringDocToCam(doc, state);
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

        private static void ReturnDocToBoard(DocumentInteractable doc, DocumentBoardState state) {
            SetInteractionLayer(state.DocZoomed, LayerMasks.DocumentInteract_Index);

            UpdateEnabledDocParts(doc, DocumentBoardState.BoardActiveFunctions);

            state.DocumentRoutine.Replace(MoveDocToPos(doc.transform, state.StoredDocPos))
                .OnComplete(() => {
                    DocumentRenderer renderer = doc.Renderer;
                    DocumentAsset asset = Find.NamedAsset<DocumentAsset>(doc.AssetName);

                    DocumentUtility.DisplayLowResDocument(renderer, asset);

                });

            state.StoredDocPos = Vector3.zero;
            state.DocZoomed = null;
            InputUtility.SetClickableMaskDefault(Find.State<InputState>());
            doc.transform.SetParent(state.DocumentParent, true);
            using (var table = TempVarTable.Alloc()) {
                table.Set("documentId", doc.AssetName);
                ScriptUtility.Trigger(ScriptEvents.DocumentInspectEnd, table);
            }
        }

        private static void BringDocToCam(DocumentInteractable doc, DocumentBoardState state) {
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

            DocumentUtility.DisplayFullDocument(renderer, asset);
            renderer.transform.localPosition = asset.DefaultPinnedPos;

            UpdateEnabledDocParts(doc, DocumentBoardState.ZoomActiveFunctions);

            state.DocumentRoutine.Replace(MoveDocToCam(viewState, doc.transform, Game.Rendering.PrimaryCamera.transform, zoomOffset))
                .OnComplete(() => {
                    using (var table = TempVarTable.Alloc()) {
                        table.Set("documentId", doc.AssetName);
                        ScriptUtility.Trigger(ScriptEvents.DocumentInspectStart, table);
                    }
                });

            state.DocZoomed = doc;
            doc.transform.SetParent(Game.Rendering.PrimaryCamera.transform, true);
            SetInteractionLayer(state.DocZoomed, LayerMasks.TopLayer_Index);
            InputUtility.SetClickableMaskTopLayer(Find.State<InputState>());
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
            state.DocumentRoutine.OnComplete(() => ToggleZoomDoc(state.DocZoomed, state));
        }

        public static void FlipDoc(DocumentInteractable doc, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (doc == null) {
                return;
            }
            doc.Flipped = !doc.Flipped;
            float angle = doc.Flipped ? 180 : 0;
            float lift = state.DocZoomed ? 0.5f : -0.5f;
            state.DocumentRoutine.Replace(DocRotateY(doc, lift, angle));
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

        private static IEnumerator AwaitDocLoadComplete(DocumentRenderer doc)
        {
            while (!DocumentUtility.IsFullyLoaded(doc))
            {
                yield return null;
            }
        }

        private static IEnumerator MoveDocToCam(ViewState viewState, Transform doc, Transform cam, Vector3 offset) {
            // don't move doc until camera has finished moving
            while (viewState.ActiveTransitionRoutine.Exists()) {
                yield return null;
            }
            var newOffset = cam.TransformVector(offset);
            yield return Routine.Combine(
                doc.MoveTo(cam.position + newOffset, 0.5f).Ease(Curve.QuartInOut),
                doc.RotateTo(cam, 0.3f));
            yield return null;
        }
        
        private static IEnumerator MoveDocToPos(Transform doc, Vector3 pos) {
            yield return Routine.Combine(
                doc.MoveTo(pos, 0.5f).Ease(Curve.QuartInOut),
                doc.RotateTo(0f, 0.3f, Axis.XYZ, Space.Self));
            yield return null;
        }

        private static IEnumerator DocRotateY(DocumentInteractable doc, float lift, float angle) {
            yield return Routine.Combine(
                doc.transform.MoveTo(doc.transform.localPosition.z + lift, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeIn),
                doc.BodyRoot.MoveTo(doc.BodyRoot.localPosition.y - 0.1f, 0.2f, Axis.Y, Space.Self).Ease(Curve.CubeIn)
            );
            yield return doc.BodyRoot.RotateTo(doc.BodyRoot.localRotation.y + angle, 0.3f, Axis.Y, Space.Self, AngleMode.Absolute).Ease(Curve.SineInOut);
            
            yield return Routine.Combine(
                doc.BodyRoot.MoveTo(doc.BodyRoot.localPosition.y + 0.1f, 0.2f, Axis.Y, Space.Self).Ease(Curve.CubeIn),
                doc.transform.MoveTo(doc.transform.localPosition.z - lift, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeIn)
            );
            yield return null;
        }

        #endregion // routines

    }
}