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
using UnityEngine;

namespace Astro {
    public sealed class DocumentBoardState : SharedStateComponent, IRegistrationCallbacks {
        public bool EnableDocumentInteraction;
        [NonSerialized] public DocumentInteractable SelectedDocument;
        [NonSerialized] public Vector3 LastMousePos;
        [NonSerialized] public bool InteractedThisFrame;
        [NonSerialized] public Vector3 StoredDocPos;
        [NonSerialized] public bool OverrideStoredDoc;
        [NonSerialized] public Vector3 OverrideStoredDocPos;
        [NonSerialized] public DocumentInteractable DocZoomed;
        [NonSerialized] public Routine DocumentRoutine;

        public Transform DocumentParent;
        public Rect DraggableBounds;

        public bool DraggablePlacedThisFrame = false;
        public DocumentInteractable DraggablePlaced = null;

        public AssetPack DocumentAssets;

        [Header("Interact Settings")]
        [Range(0f, 1f)] public float FollowSpeed;
        public Vector3 DocHoverOffset;
        public Vector3 DocZoomOffset;

        public void OnDeregister() {
        }

        public void OnRegister() {
        }
    }

    public static partial class DocumentUtility {

        #region Spawning

        public static DocumentRenderer SpawnDocument(DocumentAsset asset, StringHash32 id, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            DocumentRenderer spawned = GameObject.Instantiate(asset.Prefab, state.DocumentParent);
            spawned.Title.SetText(asset.TitleText);
            spawned.Body.SetText(asset.BodyText);
            if (spawned.Video) { spawned.Video.url = Application.streamingAssetsPath + "/Postcards/" + asset.VideoName; }
            spawned.transform.localPosition = asset.DefaultPinnedPos;
            spawned.ZoomOffsetOverride = asset.ZoomOffsetOverride;
            spawned.Interactable.Renderer = spawned;
            spawned.Interactable.Parts = spawned.Interactable.GetComponentsInChildren<DocumentPart>(true);
            spawned.Interactable.AssetName = id;

            return spawned;
        }

        [LeafMember("SpawnDocument")]
        public static void LeafSpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id), id);
        }

        [LeafMember("SpawnDocumentToCamera")]
        public static void LeafSpawnDocumentToCamera(StringHash32 id) {
            DocumentBoardState state = Find.State<DocumentBoardState>();

            if (state.DocumentRoutine.Exists()) {
                // wait for previous document routine to complete
                state.DocumentRoutine.OnComplete(() => { SpawnDocumentToCamera(state, id); });
            }
            else {
                SpawnDocumentToCamera(state, id);
            }
        }

        public static void SpawnDocumentToCamera(DocumentBoardState state, StringHash32 id) {
            var asset = Find.NamedAsset<DocumentAsset>(id);
            var spawned = SpawnDocument(asset, id, state);
            // Init pinned position
            spawned.transform.SetParent(state.DocumentParent, false);
            spawned.transform.localPosition = FindAvailablePos(state, spawned);
            state.OverrideStoredDocPos = spawned.transform.position;
            state.OverrideStoredDoc = true;
            // Spawn below player view
            spawned.transform.SetParent(Game.Rendering.PrimaryCamera.transform, true);
            spawned.transform.localPosition = Vector3.zero;
            // Move to zoomed view
            ToggleZoomDoc(spawned.Interactable, state);
            SetDocumentInteractionEnabled(true);
            if (spawned.Video.clip != null) { spawned.Video.Play(); }
        }

        public static void SpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id), id);
        }

        public static Vector3 FindAvailablePos(DocumentBoardState state, DocumentRenderer doc) {

            Vector2 finalPos = Vector3.zero;
            int maxTries = 10;
            for (int i = 0; i < maxTries; i++) {
                // random point on board
                float xExtents = state.DraggableBounds.x / 2;
                float yExtents = state.DraggableBounds.y / 2;
                var offset = state.DocumentParent.transform.position;
                Vector3 pos = offset + new Vector3(UnityEngine.Random.Range(-xExtents, xExtents), UnityEngine.Random.Range(-yExtents, yExtents), 0);

                Vector3 docExtents = new Vector3(doc.Size.width / 2, doc.Size.height / 2, 1);
                if (!Physics.CheckBox(pos, docExtents, state.DocumentParent.transform.rotation, LayerMasks.DocumentInteract_Mask)) {
                    finalPos = pos - offset;
                    break;
                }
            }

            return finalPos;
        }

        public static bool OverlapBoxAtPos(DocumentBoardState state, DocumentRenderer doc, out DocumentRenderer hit)
        {
            hit = null;

            var offset = state.DocumentParent.transform.position;
            var localPos = doc.transform.localPosition;
            localPos.z = 0;
            Vector3 pos = offset + localPos;

            Vector3 docExtents = new Vector3(doc.Size.width / 2, doc.Size.height / 2, 1);
            var hits = Physics.OverlapBox(pos, docExtents, state.DocumentParent.transform.rotation, LayerMasks.DocumentInteract_Mask);
            for (int i = 0; i < hits.Length; i++)
            {
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
        }

        public static void DeselectDocument(DocumentBoardState state) {
            StartMoveDoc(null, null, state);
        }

        public static void ToggleZoomDoc(DocumentInteractable doc, DocumentBoardState state = null) {
            if (doc == null) {
                return;
            }
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (state.SelectedDocument) {
                DeselectDocument(state);
            }
            if (state.DocZoomed) {
                SetInteractionLayer(state.DocZoomed, LayerMasks.DocumentInteract_Index);
                state.DocumentRoutine.Replace(MoveDocToPos(doc.transform, state.StoredDocPos));
                if (doc.Renderer.Video) {
                    doc.Renderer.Video.Stop();
                }
                state.StoredDocPos = Vector3.zero;
                state.DocZoomed = null;
                InputUtility.SetClickableMaskDefault(Find.State<InputState>());
                doc.transform.SetParent(state.DocumentParent, true);
                using (var table = TempVarTable.Alloc()) {
                    table.Set("documentId", doc.AssetName);
                    ScriptUtility.Trigger(ScriptEvents.DocumentInspectEnd, table);
                }
            } else {
                if (state.OverrideStoredDoc) {
                    state.StoredDocPos = state.OverrideStoredDocPos;
                    state.OverrideStoredDoc = false;
                }
                else {
                    state.StoredDocPos = doc.transform.position;
                }
                //state.StoredDocPos.z = state.DocumentParent.position.z;
                Vector3 zoomOffset = doc.Renderer.ZoomOffsetOverride == default ? state.DocZoomOffset : doc.Renderer.ZoomOffsetOverride;
                var viewState = Find.State<ViewState>();
                state.DocumentRoutine.Replace(MoveDocToCam(viewState, doc.transform, Game.Rendering.PrimaryCamera.transform, zoomOffset))
                    .OnComplete(() => 
                    {
                        using (var table = TempVarTable.Alloc()) {
                            table.Set("documentId", doc.AssetName);
                            ScriptUtility.Trigger(ScriptEvents.DocumentInspectStart, table);
                        }
                        if (doc.Renderer.Video.clip) {
                            doc.Renderer.Video.Play();
                        }
                    });
                state.DocZoomed = doc;
                doc.transform.SetParent(Game.Rendering.PrimaryCamera.transform, true);
                SetInteractionLayer(state.DocZoomed, LayerMasks.TopLayer_Index);
                InputUtility.SetClickableMaskTopLayer(Find.State<InputState>());
            }
            state.InteractedThisFrame = true;
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
        }

        #endregion // Interaction

        #region Routines
        public static IEnumerator MoveAboveRelativeToDoc(DocumentInteractable doc, DocumentRenderer relativeTo)
        {
            // align to bottom-left with out-sticking margin
            var margin = 0.2f;
            var localOffset = new Vector3(-relativeTo.Size.width / 2 + doc.Renderer.Size.width / 2 - margin, -relativeTo.Size.height + doc.Renderer.Size.height / 2, -0.01f);
            var globalOffset = relativeTo.transform.TransformVector(localOffset);
            yield return doc.transform.MoveTo(relativeTo.transform.position + globalOffset, 0.2f).Ease(Curve.CubeIn);
        }

        private static IEnumerator ToggleDocHover(Transform doc, Vector3 hoverOffset) {
            yield return doc.MoveTo(doc.localPosition + hoverOffset, 0.2f, Axis.XYZ, Space.Self).Ease(Curve.CubeIn);
            yield return null;
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
                doc.Paper.MoveTo(doc.Paper.localPosition.y - 0.1f, 0.2f, Axis.Y, Space.Self).Ease(Curve.CubeIn)
            );
            yield return doc.Paper.RotateTo(doc.Paper.localRotation.y + angle, 0.3f, Axis.Y, Space.Self, AngleMode.Absolute).Ease(Curve.SineInOut);
            
            yield return Routine.Combine(
                doc.Paper.MoveTo(doc.Paper.localPosition.y + 0.1f, 0.2f, Axis.Y, Space.Self).Ease(Curve.CubeIn),
                doc.transform.MoveTo(doc.transform.localPosition.z - lift, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeIn)
            );
            yield return null;
        }

        #endregion // routines

    }
}