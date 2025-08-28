using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leaf.Runtime;
using FieldDay.Scripting;
using BeauUtil;
using BeauRoutine;
using Astro;
using FieldDay;

public class ModalHintMgr : ScriptActorComponent {
    [HideInInspector] public List<ModalHint> HintModals;
    public ModalHint activeHint = null;
    private HintLogData m_CurrLogData = default;

    void Start() {
        HintModals.AddRange(GetComponentsInChildren<ModalHint>(true));
        
        foreach (ModalHint modal in HintModals) modal.gameObject.SetActive(false);
    }

    private IEnumerator ShowModal(StringHash32 Id, float fadeDuration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        modal.gameObject.SetActive(true);
        ShowModalCommon(modal.Source, modal.Text.text);

        yield return Tween.Value(0f, 1f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, fadeDuration).ForceOnCancel().OnComplete( () => { activeHint = modal; } ); 
    }

    [LeafMember("Show")]
    private IEnumerator LeafShowModal(StringHash32 Id, float fadeDuration = 0f) {
        if (activeHint == null) {
            yield return ShowModal(Id, fadeDuration);
        } else {
            yield return HideModal(activeHint.Id, 0.1f); 
            yield return ShowModal(Id, fadeDuration);
        }
    }

    private IEnumerator SlideInFromRight(StringHash32 Id, float duration = 0f) { 
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        modal.gameObject.SetActive(true);
        ShowModalCommon(modal.Source, modal.Text.text);

        float endPosX = modal.Rect.anchoredPosition.x;
        float PosY = modal.Rect.anchoredPosition.y;
        float startPosX = modal.Rect.sizeDelta.x + 20f; // 20 here provides a little right padding
        modal.Rect.anchoredPosition = new Vector2(startPosX, PosY);
        yield return Routine.Combine(
            Tween.Value(0f, 1f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, duration).ForceOnCancel(),
            Tween.Value(startPosX, endPosX, (f) => { modal.Rect.anchoredPosition = new Vector2(f, PosY); }, Mathf.Lerp, duration).OnComplete( () => { activeHint = modal; } ).ForceOnCancel()
        );
    }

    [LeafMember("SlideInFromRight")]
    private IEnumerator LeafSlideInFromRight(StringHash32 Id, float duration = 0f) {
        if (activeHint == null) {
            yield return SlideInFromRight(Id, duration);
        } else {
            yield return HideModal(activeHint.Id, 0.1f); 
            yield return SlideInFromRight(Id, duration);
        }
    }

    private IEnumerator SlideInFromLeft(StringHash32 Id, float duration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        modal.gameObject.SetActive(true);
        ShowModalCommon(modal.Source, modal.Text.text);

        float endPosX = modal.Rect.anchoredPosition.x;
        float PosY = modal.Rect.anchoredPosition.y;
        float startPosX = 0 - modal.Rect.sizeDelta.x - 20f; // 20 here provides a little right padding
        modal.Rect.anchoredPosition = new Vector2(startPosX, PosY);
        yield return Routine.Combine(
            Tween.Value(0f, 1f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, duration).ForceOnCancel(),
            Tween.Value(startPosX, endPosX, (f) => { modal.Rect.anchoredPosition = new Vector2(f, PosY); }, Mathf.Lerp, duration).OnComplete( () => { activeHint = modal; } ).ForceOnCancel()
        );
    }

    [LeafMember("SlideInFromLeft")]
    private IEnumerator LeafSlideInFromLeft(StringHash32 Id, float duration = 0f) {
        if (activeHint == null) {
            yield return SlideInFromLeft(Id, duration);
        } else {
            yield return HideModal(activeHint.Id, 0.1f); 
            yield return SlideInFromLeft(Id, duration);
        }
    }

    [LeafMember("Wiggle")]
    private IEnumerator LeafWiggle(StringHash32 Id, int wiggleFreq = 2, float wiggleOffset = 5f, float totalDuration = 0.3f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);
        if (modal != activeHint || !modal.gameObject.activeInHierarchy) yield break;

        float duration = totalDuration / wiggleFreq;

        yield return Routine.Combine(
            modal.Rect.AnchorPosTo(modal.Rect.anchoredPosition.x - wiggleOffset, totalDuration, Axis.X).Wave(Wave.Function.SinFade, wiggleFreq * 2).RevertOnCancel(),
            Tween.Color(Color.black, Color.white, (c) => { modal.Panel.color = c; }, totalDuration).Ease(Curve.QuadIn).Yoyo().RevertOnCancel()
        );
    }

    private IEnumerator HideModal(StringHash32 Id, float fadeDuration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);

        yield return Tween.Value(1f, 0f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, fadeDuration).OnComplete( () => { activeHint = null; } ).ForceOnCancel();
        modal.gameObject.SetActive(false);
        HideModalCommon();
    }

    private IEnumerator HideModal(ModalHint modal, float fadeDuration = 0f) {
        yield return Tween.Value(1f, 0f, (f) => { modal.Modal.alpha = f; }, Mathf.Lerp, fadeDuration).OnComplete( () => { activeHint = null; } ).ForceOnCancel();
        modal.gameObject.SetActive(false);
        HideModalCommon();
    }

    [LeafMember("Hide")]
    private IEnumerator LeafHideModal(StringHash32 Id, float fadeDuration = 0f) {
        ModalHint modal = HintModals.Find(m => m.Id == Id);
        if (!modal.gameObject.activeInHierarchy) yield break;

        yield return HideModal(Id, fadeDuration);
    }

    [LeafMember("CloseAllModals")]
    private void LeafCloseAllModals() {
        foreach (ModalHint modal in HintModals) {
            if (!modal.gameObject.activeInHierarchy) continue;
            StartCoroutine(HideModal(modal, 0.05f));
        }
    }

    private void ShowModalCommon(string id, string content)
    {
        m_CurrLogData.Id = id;
        m_CurrLogData.Content = content;
        AstroGame.Events.Dispatch(GameEvents.HintChanged, EvtArgs.Box(m_CurrLogData));
        AstroGame.Events.Dispatch(GameEvents.HintDisplayed);
    }

    private void HideModalCommon()
    {
        AstroGame.Events.Dispatch(GameEvents.HintHidden);
    }
}
