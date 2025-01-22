using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.SharedState;
using System.Collections;
using UnityEngine;

namespace Astro {
    public sealed class DocumentBoardState : SharedStateComponent, IRegistrationCallbacks {
        [HideInInspector] public bool EnableDocumentInteraction;
        [HideInInspector] public DocumentInteractable SelectedDocument;
        [HideInInspector] public Vector3 LastMousePos;
        [HideInInspector] public bool InteractedThisFrame;
        [HideInInspector] public Vector3 StoredDocPos;
        [HideInInspector] public bool DocZoomed;
        [HideInInspector] public Routine DocumentRoutine;

        public Transform DocumentParent;

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

        public static void ProcessDocPartInteraction(DocumentPart docPart) {

            DocumentBoardState state = Find.State<DocumentBoardState>();
            switch (docPart.PartType) {
                case DocPartFunction.Move: {
                        StartMoveDoc(docPart.Document, state);
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
        public static void StartMoveDoc(DocumentInteractable newDoc, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (state.DocumentRoutine.Exists() || state.DocZoomed) {
                return;
            }
            if (newDoc != null && state.SelectedDocument != newDoc) {
                state.SelectedDocument = newDoc;
                state.DocumentRoutine.Replace(ShiftZ(state.SelectedDocument.transform, state.DocumentParent.localPosition + state.DocHoverOffset));
            } else {            
                state.DocumentRoutine.Replace(ShiftZ(state.SelectedDocument.transform, state.DocumentParent.localPosition));
                state.SelectedDocument = null;
            }
            state.InteractedThisFrame = true;
        }

        public static void DeselectDocument(DocumentBoardState state) {
            StartMoveDoc(null, state);
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
                state.DocumentRoutine.Replace(MoveDocToPos(doc.transform, state.StoredDocPos));
                state.StoredDocPos = Vector3.zero;
                state.DocZoomed = false;
            } else {
                state.StoredDocPos = doc.transform.position;
                state.StoredDocPos.z = state.DocumentParent.position.z;
                Vector3 zoomOffset = doc.ZoomOffsetOverride == Vector3.zero ? state.DocZoomOffset : doc.ZoomOffsetOverride;
                state.DocumentRoutine.Replace(MoveDocToCam(doc.transform, Camera.main.transform, zoomOffset));
                state.DocZoomed = true;
            }
            state.InteractedThisFrame = true;
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
        private static IEnumerator ShiftZ(Transform doc, Vector3 hoverRoot) {
            yield return doc.MoveTo(hoverRoot, 0.2f, Axis.Z).Ease(Curve.CubeIn);
            yield return null;
        }

        private static IEnumerator MoveDocToCam(Transform doc, Transform cam, Vector3 offset) {
            yield return doc.MoveTo(cam.position + offset, 0.5f).Ease(Curve.QuartInOut);
            yield return null;
        }
        
        private static IEnumerator MoveDocToPos(Transform doc, Vector3 pos) {
            yield return doc.MoveTo(pos, 0.5f).Ease(Curve.QuartInOut);
            yield return null;
        }

        private static IEnumerator DocRotateY(DocumentInteractable doc, float lift, float angle) {
            yield return Routine.Combine(
                doc.transform.MoveTo(doc.transform.position.z + lift, 0.2f, Axis.Z).Ease(Curve.CubeIn),
                doc.Paper.MoveTo(doc.Paper.position.y - 0.1f, 0.2f, Axis.Y).Ease(Curve.CubeIn)
            );
            yield return doc.Paper.RotateTo(doc.Paper.rotation.y + angle, 0.3f, Axis.Y, Space.World, AngleMode.Absolute).Ease(Curve.SineInOut);
            
            yield return Routine.Combine(
                doc.Paper.MoveTo(doc.Paper.position.y + 0.1f, 0.2f, Axis.Y).Ease(Curve.CubeIn),
                doc.transform.MoveTo(doc.transform.position.z - lift, 0.2f, Axis.Z).Ease(Curve.CubeIn)
            );
            yield return null;
        }

        #endregion // routines

    }
}