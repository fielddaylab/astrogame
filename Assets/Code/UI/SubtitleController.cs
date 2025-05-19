using UnityEngine;
using BeauUtil;
using Leaf.Runtime;
using FieldDay.Scripting;

[RequireComponent(typeof(RectTransform))]
public class SubtitleController : ScriptActorComponent {
    [Required] private RectTransform SubtitlePanel;

    void Awake() {
        SubtitlePanel = GetComponent<RectTransform>();   
    }

    [LeafMember("SetSubtitlePosition")]
    private void LeafSetSubtitlePosition(float anchorX, float anchorY, float posX, float posY) {
        Vector2 offset = new Vector2(posX, posY);
        Vector2 newAnchorPos = new Vector2(anchorX, anchorY);

        SubtitlePanel.anchorMin = SubtitlePanel.anchorMax = newAnchorPos;
        SubtitlePanel.anchoredPosition = offset;
    }
}
