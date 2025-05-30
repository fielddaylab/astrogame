using BeauUtil;
using FieldDay;
using FieldDay.UI;
using FieldDay.UI.Animation;
using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(LoadingIcon))]
    public sealed class LoadingIconAnimation : MonoBehaviour, IOnGuiUpdate {
        [SerializeField] private RectTransform[] m_Stars;
        [SerializeField] private Vector2 m_AnchorPivot;
        [SerializeField] private float m_PivotDistance;
        [SerializeField] private float m_RotSpeed = 1;

        [NonSerialized] private float m_CurrentRot;
        
        private void Awake() {
            LoadingIcon ico = GetComponent<LoadingIcon>();
            ico.BeginAnimation.Register(BeginAnim);
            ico.EndAnimation.Register(EndAnim);
        }

        private void BeginAnim() {
            Game.Gui.RegisterUpdate(this);
            m_CurrentRot = RNG.Instance.NextFloat();
        }

        private void EndAnim() {
            Game.Gui.DeregisterUpdate(this);
        }

        void IOnGuiUpdate.OnGuiUpdate() {
            float interval = Mathf.PI * 2 / m_Stars.Length;
            float pivotX = m_AnchorPivot.x;
            float pivotY = m_AnchorPivot.y;
            for(int i = 0; i < m_Stars.Length; i++) {
                float angle = -m_CurrentRot + i * interval;
                float x = pivotX + Mathf.Cos(angle) * m_PivotDistance;
                float y = pivotY + Mathf.Sin(angle) * m_PivotDistance;
                m_Stars[i].anchoredPosition = new Vector2(x, y);
            }

            m_CurrentRot += m_RotSpeed * Mathf.PI * 2 * Frame.UnscaledDeltaTime;
        }
    }
}