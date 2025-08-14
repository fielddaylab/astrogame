using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using FieldDay.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Astro
{
    public class SaveIcon : MonoBehaviour
    {
        static private readonly string Label_Saving = "SAVING";
        static private readonly string Label_SaveError = "ERROR";
        static private readonly string Label_SaveSuccess = "SAVED";

        #region Inspector

        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private TMP_Text m_Label = null;

        [Header("Animations")]
        [SerializeField] private Sprite m_SavingSprite;
        [SerializeField] private Sprite m_SuccessSprite;
        [SerializeField] private Sprite m_FailureSprite;

        #endregion // Inspector

        private float m_DisplayTime;
        private Routine m_DisplayRoutine;
        private bool m_Success;

        private void Awake()
        {
            m_Group.alpha = 0;
            m_Icon.sprite = m_SavingSprite;
            m_Success = false;

            Game.Events.Register(GameEvents.ProfileSaveBegin, OnSaveBegin, this)
                .Register(GameEvents.ProfileSaveError, OnSaveError, this)
                .Register(GameEvents.ProfileSaveSuccess, OnSaveSuccess, this)
                .Register(GameEvents.ProfileSaveAttemptCompleted, OnSaveAttemptComplete, this);
        }

        private void OnDestroy()
        {
            Game.Events?.Deregister(GameEvents.ProfileSaveBegin, OnSaveBegin)
                .Deregister(GameEvents.ProfileSaveError, OnSaveError)
                .Deregister(GameEvents.ProfileSaveSuccess, OnSaveSuccess)
                .Deregister(GameEvents.ProfileSaveAttemptCompleted, OnSaveAttemptComplete);
        }

        private void OnSaveBegin()
        {
            m_DisplayTime = Time.realtimeSinceStartup;
            m_DisplayRoutine.Replace(this, Show());
            m_Success = false;
        }

        private void OnSaveError()
        {
            m_Success = false;
        }

        private void OnSaveSuccess()
        {
            m_Success = true;
        }

        private void OnSaveAttemptComplete()
        {
            m_DisplayRoutine.Replace(this, Complete());
        }

        private IEnumerator Show()
        {
            m_DisplayTime = Time.realtimeSinceStartup;

            m_Icon.sprite = m_SavingSprite;
            m_Label.SetText(Label_Saving);
            yield return m_Group.FadeTo(1, 0.1f);
        }

        private IEnumerator Complete()
        {
            float tsThreshold = m_DisplayTime + 1f;

            // finish showing
            if (m_Group.alpha < 1)
                yield return m_Group.FadeTo(1, (1 - m_Group.alpha) * 0.1f);

            // give time to show saving
            while (Time.realtimeSinceStartup < tsThreshold)
                yield return null;

            if (m_Success) {
                m_Label.SetText(Label_SaveSuccess);
                m_Icon.sprite = m_SuccessSprite;
            }
            else {
                m_Label.SetText(Label_SaveError);
                m_Icon.sprite = m_FailureSprite;
            }

            // give time to show result
            while (Time.realtimeSinceStartup < tsThreshold + 1f)
                yield return null;

            // hide
            yield return m_Group.FadeTo(0, 0.1f).DelayBy(0.1f);
        }
    }
}