using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leaf.Runtime;
using FieldDay.Scripting;
using BeauUtil;
using BeauRoutine;

public class ModalHintMgr : ScriptActorComponent {
    [HideInInspector] public List<ModalHint> HintModals;

    void Start() {
        HintModals.AddRange(GetComponentsInChildren<ModalHint>(true));
        
        foreach (ModalHint modal in HintModals) modal.gameObject.SetActive(false);
    }

    [LeafMember("Show")]
    private IEnumerator LeafShowModal(StringHash32 Id, float fadeDuration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        modal.gameObject.SetActive(true);
        yield return Tween.Value(0f, 1f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, fadeDuration);
    }

    [LeafMember("SlideInFromRight")]
    private IEnumerator LeafSlideInFromRight(StringHash32 Id, float duration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        modal.gameObject.SetActive(true);

        float endPosX = modal.Rect.anchoredPosition.x;
        float PosY = modal.Rect.anchoredPosition.y;
        float startPosX = modal.Rect.sizeDelta.x + 20f; // 20 here provides a little right padding
        modal.Rect.anchoredPosition = new Vector2(startPosX, PosY);
        yield return Routine.Combine(
            Tween.Value(0f, 1f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, duration),
            Tween.Value(startPosX, endPosX, (f) => { modal.Rect.anchoredPosition = new Vector2(f, PosY); }, Mathf.Lerp, duration)
        );

    }
    [LeafMember("SlideInFromLeft")]
    private IEnumerator LeafSlideInFromLeft(StringHash32 Id, float duration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        modal.gameObject.SetActive(true);

        float endPosX = modal.Rect.anchoredPosition.x;
        float PosY = modal.Rect.anchoredPosition.y;
        float startPosX = 0 - modal.Rect.sizeDelta.x - 20f; // 20 here provides a little right padding
        modal.Rect.anchoredPosition = new Vector2(startPosX, PosY);
        yield return Routine.Combine(
            Tween.Value(0f, 1f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, duration),
            Tween.Value(startPosX, endPosX, (f) => { modal.Rect.anchoredPosition = new Vector2(f, PosY); }, Mathf.Lerp, duration)
        );

    }

    [LeafMember("Wiggle")]
    private IEnumerator LeafWiggle(StringHash32 Id, int wiggleFreq = 2, float wiggleOffset = 5f, float totalDuration = 0.3f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        float duration = totalDuration / wiggleFreq;

        yield return Routine.Combine(
            modal.Rect.AnchorPosTo(modal.Rect.anchoredPosition.x - wiggleOffset, totalDuration, Axis.X).Wave(Wave.Function.SinFade, wiggleFreq * 2).RevertOnCancel(),
            Tween.Color(Color.black, Color.white, (c) => { modal.Panel.color = c; }, totalDuration).Ease(Curve.QuadIn).Yoyo().RevertOnCancel()
        );

    }

    [LeafMember("Hide")]
    private IEnumerator LeafHideModal(StringHash32 Id, float fadeDuration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        yield return Tween.Value(1f, 0f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, fadeDuration);
        modal.gameObject.SetActive(false);
    }

}
