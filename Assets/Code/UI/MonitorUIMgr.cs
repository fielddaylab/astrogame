using UnityEngine;
using TMPro;
using Leaf.Runtime;
using FieldDay.Scripting;
using BeauUtil;
using BeauRoutine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class MonitorUIMgr : ScriptActorComponent {
    // Singleton
    public static MonitorUIMgr Instance { get; private set; } 
    
    [Header("Radio Download Indicator")]
    [SerializeField] private Image CERESLogo;

    [Header("Radio Download Indicator")]
    [SerializeField] private CanvasGroup ElementGroup;
    [SerializeField] private RectTransform ProgressBar;
    [Tooltip("Represents the width & height of the progress bar when loading starts")]
    [SerializeField] private Vector2 StartingSize;
    [Tooltip("Represents the width & height of the progress bar when loading is complete")]
    [SerializeField] private Vector2 EndingSize;

    private List<MonitorUIElement> m_Elements = new List<MonitorUIElement>();

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        m_Elements.AddRange(GetComponentsInChildren<MonitorUIElement>(true));
        
        foreach (MonitorUIElement e in m_Elements) e.element.alpha = 0f;
    }

    public MonitorUIElement GetElement(StringHash32 id) {
        return m_Elements.Find(e => e.Id == id);
    }

    [LeafMember("PowerOn")]
    private IEnumerator LeafPowerOn() {
        MonitorUIElement offPanel = m_Elements.Find(e => e.Id == "OffPanel");
        yield return Tween.Value(1f, 0f, (f) => { offPanel.element.alpha = f; }, Mathf.Lerp, 0.5f).ForceOnCancel();
    }

    [LeafMember("PowerOff")]
    private IEnumerator LeafPowerOff() {
        MonitorUIElement offPanel = m_Elements.Find(e => e.Id == "OffPanel");
        yield return Tween.Value(0f, 1f, (f) => { offPanel.element.alpha = f; }, Mathf.Lerp, 0.5f).ForceOnCancel();
    }

    [LeafMember("ShowCERESLogo")]
    private IEnumerator LeafShowCERESLogo() {
        MonitorUIElement logo = m_Elements.Find(e => e.Id == "CeresLogo");
        yield return Tween.Value(0f, 1f, (f) => { logo.element.alpha = f; }, Mathf.Lerp, 0.5f).ForceOnCancel();
    }

    public IEnumerator ShowElement(StringHash32 _id, float duration = 0.5f) {
        MonitorUIElement e = m_Elements.Find(e => e.Id == _id);
        yield return Tween.Value(0f, 1f, (f) => { e.element.alpha = f; }, Mathf.Lerp, duration).ForceOnCancel();
    }

    public IEnumerator HideElement(StringHash32 _id, float duration = 0.5f) {
        MonitorUIElement e = m_Elements.Find(e => e.Id == _id);
        yield return Tween.Value(1f, 0f, (f) => { e.element.alpha = f; }, Mathf.Lerp, duration).ForceOnCancel();
    }

    [LeafMember("ShowElement")]
    private IEnumerator LeafShowElement(StringHash32 _id, float duration = 0.5f) {
        yield return ShowElement(_id, duration);
    }

    [LeafMember("HideElement")]
    private IEnumerator LeafHideElement(StringHash32 _id, float duration = 0.5f) {
        yield return HideElement(_id, duration);
    }

    [LeafMember("CERESLogoToCorner")]
    private IEnumerator LeafCERESLogoToCorner(float duration = 0.2f) {
        RectTransform LogoTransform = CERESLogo.gameObject.GetComponent<RectTransform>();
        yield return Routine.Combine(
            LogoTransform.AnchorPosTo(new Vector2(75f, -75f), duration).ForceOnCancel(),
            LogoTransform.ScaleTo(0.3f, duration).ForceOnCancel()
        );
    }

    [LeafMember("CERESLogoToCenter")]
    private IEnumerator LeafCERESLogoToCenter(float duration = 0.1f) {
        RectTransform LogoTransform = CERESLogo.gameObject.GetComponent<RectTransform>();
        yield return Routine.Combine(
            LogoTransform.AnchorPosTo(new Vector2(445, -296), duration).ForceOnCancel(),
            LogoTransform.ScaleTo(1f, duration).ForceOnCancel()
        );
    }

    [LeafMember("HideCERESLogo")]
    private IEnumerator LeafHideCERESLogo() {
        MonitorUIElement logo = m_Elements.Find(e => e.Id == "CeresLogo");
        yield return Tween.Value(1f, 0f, (f) => { logo.element.alpha = f; }, Mathf.Lerp, 0.5f).ForceOnCancel();
    }

    [LeafMember("PlayRadioDownload")]
    private IEnumerator LeafPlayRadioDownload(float duration) {
        ProgressBar.sizeDelta = StartingSize;

        // Fade In
        yield return Tween.Value(0f, 1f, (f) => { ElementGroup.alpha = f; }, Mathf.Lerp, 0.5f);

        // Download Bar
        yield return ProgressBar.SizeDeltaTo(EndingSize, duration);

        yield return new WaitForSeconds(0.2f);

        // Fade Out
        yield return Tween.Value(1f, 0f, (f) => { ElementGroup.alpha = f; }, Mathf.Lerp, 0.5f);

        // Reset bar
        ProgressBar.sizeDelta = new Vector2(12, 28);
    }
}
