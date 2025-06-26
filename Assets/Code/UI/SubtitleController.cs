using UnityEngine;
using BeauUtil;
using Leaf.Runtime;
using FieldDay.Scripting;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class SubtitleController : ScriptActorComponent {
    [Required] private RectTransform SubtitlePanel;

    void Awake() {
        SubtitlePanel = GetComponent<RectTransform>();   
    }

    [LeafMember("SetSubtitlePosition")]
    private IEnumerator LeafSetSubtitlePosition(float anchorX, float anchorY, float posX, float posY) {
        // HACK we are going to try to prevent the player from seeing the subtitle move around
        yield return new WaitForSeconds(0.05f);

        Vector2 offset = new Vector2(posX, posY);
        Vector2 newAnchorPos = new Vector2(anchorX, anchorY);

        SubtitlePanel.anchorMin = SubtitlePanel.anchorMax = newAnchorPos;
        SubtitlePanel.anchoredPosition = offset;

        // HACK we are going to try to prevent the player from seeing the subtitle move around 
        yield return new WaitForSeconds(0.1f);
    }
}
