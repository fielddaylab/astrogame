using UnityEngine;
using BeauUtil;
using FieldDay.Components;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class MonitorUIElement : BatchedComponent {
    [Tooltip("A unique Id that will be used in leaf to identify item")]
    [SerializeField] private SerializedHash32 m_Id = string.Empty;
    public StringHash32 Id { get { return m_Id.Hash(); } }
    [HideInInspector] public CanvasGroup element;

    void Awake() { element = GetComponent<CanvasGroup>(); }
}
