using System.Collections;
using UnityEngine;
using Leaf.Runtime;
using FieldDay.Scripting;
using BeauUtil;
using BeauRoutine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class MonitorUI : ScriptActorComponent {
    [Header("Radio Download Indicator")]
    public Image CERESLogo;

    [Header("Radio Download Indicator")]
    public CanvasGroup ElementGroup;
    public RectTransform ProgressBar;
    [Tooltip("Represents the width & height of the progress bar when loading starts")]
    public Vector2 StartingSize;
    [Tooltip("Represents the width & height of the progress bar when loading is complete")]
    public Vector2 EndingSize;

    [LeafMember("ShowCERESLogo")]
    private IEnumerator LeafShowCERESLogo() {
        CERESLogo.SetAlpha(0);
        yield return Tween.Value(0f, 1f, (f) => { CERESLogo.SetAlpha(f); }, Mathf.Lerp, 0.5f);
    }

    [LeafMember("HideCERESLogo")]
    private IEnumerator LeafHideCERESLogo() {
        CERESLogo.SetAlpha(1);
        yield return Tween.Value(1f, 0f, (f) => { CERESLogo.SetAlpha(f); }, Mathf.Lerp, 0.5f);
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
