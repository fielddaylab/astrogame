using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class ModalHint : BatchedComponent {
    [Tooltip("A unique Id that will be used in leaf to identify this modal")]
    [SerializeField] private SerializedHash32 m_Id = string.Empty;
    public StringHash32 Id { get { return m_Id.Hash(); } }

    [HideInInspector] public CanvasGroup Modal;
    [HideInInspector] public RectTransform Rect;

    public void Awake() { 
        Rect = GetComponent<RectTransform>();
        Modal = GetComponent<CanvasGroup>(); 
    }
}
