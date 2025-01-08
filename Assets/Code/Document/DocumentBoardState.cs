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


        [Range(0f, 1f)] public float DocumentHoverDistance;
        [Range(0f, 1f)] public float FollowSpeed;
        public Transform DocumentParent;

        public AssetPack DocumentAssets;

        public Routine DocumentRoutine;

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

        #region Selection
        public static void SelectDocument(DocumentInteractable newDoc, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (state.DocumentRoutine.Exists()) {
                return;
            }
            if (newDoc != null && state.SelectedDocument != newDoc) {
                state.SelectedDocument = newDoc;
                state.DocumentRoutine.Replace(ShiftZ(state.SelectedDocument.transform, -state.DocumentHoverDistance));
            } else {
                state.DocumentRoutine.Replace(ShiftZ(state.SelectedDocument.transform, state.DocumentHoverDistance));
                state.SelectedDocument = null;
            }
            state.InteractedThisFrame = true;
        }

        public static void DeselectDocument(DocumentBoardState state) {
            SelectDocument(null, state);
        }

        private static IEnumerator ShiftZ(Transform doc, float deltaZ) {
            yield return doc.MoveTo(doc.position.z + deltaZ, 0.3f, Axis.Z).Ease(Curve.CubeIn);
            yield return null;
        }

        #endregion //Selection
    }
}