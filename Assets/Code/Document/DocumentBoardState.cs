using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.HID;
using FieldDay.SharedState;
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
        [NonSerialized] public DocumentInteractable DocZoomed;
        [NonSerialized] public Routine DocumentRoutine;

        public Transform DocumentParent;
        public Rect DraggableBounds;

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

        public static void SpawnDocument(DocumentAsset asset, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            DocumentRenderer spawned = GameObject.Instantiate(asset.Prefab, state.DocumentParent);
            spawned.Title.SetText(asset.TitleText);
            spawned.Body.SetText(asset.BodyText);
            spawned.transform.localPosition = Vector3.zero;
            spawned.ZoomOffsetOverride = asset.ZoomOffsetOverride;
        }

        public static void SpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id));
        }

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
                CursorHint.TryLock(partHint);
            } else {
                CursorHint.Unlock();
                state.DocumentRoutine.Replace(ToggleDocHover(state.SelectedDocument.transform, -state.DocHoverOffset));
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
                state.StoredDocPos = Vector3.zero;
                state.DocZoomed = null;
                InputUtility.SetClickableMaskDefault(Find.State<InputState>());
            } else {
                state.StoredDocPos = doc.transform.position;
                //state.StoredDocPos.z = state.DocumentParent.position.z;
                Vector3 zoomOffset = doc.Renderer.ZoomOffsetOverride == default ? state.DocZoomOffset : doc.Renderer.ZoomOffsetOverride;
                state.DocumentRoutine.Replace(MoveDocToCam(doc.transform, Game.Rendering.PrimaryCamera.transform, zoomOffset));
                state.DocZoomed = doc;
                SetInteractionLayer(state.DocZoomed, LayerMasks.TopLayer_Index);
                InputUtility.SetClickableMaskTopLayer(Find.State<InputState>());
            }
            state.InteractedThisFrame = true;
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
        #endregion //Selection

        #region Routines
        private static IEnumerator ToggleDocHover(Transform doc, Vector3 hoverOffset) {
            yield return doc.MoveTo(doc.localPosition + hoverOffset, 0.2f, Axis.XYZ, Space.Self).Ease(Curve.CubeIn);
            yield return null;
        }

        private static IEnumerator MoveDocToCam(Transform doc, Transform cam, Vector3 offset) {
            offset = cam.TransformVector(offset);
            yield return Routine.Combine(
                doc.MoveTo(cam.position + offset, 0.5f).Ease(Curve.QuartInOut),
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