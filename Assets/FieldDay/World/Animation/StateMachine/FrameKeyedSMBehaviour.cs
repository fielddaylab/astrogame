using System.Runtime.CompilerServices;
using BeauUtil;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Animation {
    public abstract class FrameKeyedSMBehaviour : StateMachineBehaviour {
        [SerializeField, EditModeOnly] private AnimationClip m_ReferenceClip;
        [SerializeField, EditModeOnly] private int m_FrameCount;

        protected int FrameCount {
            get { return m_FrameCount; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int CurrentFrame(AnimatorStateInfo info) {
            return (int) (m_FrameCount * (info.normalizedTime % 1));
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() {
            CacheFrameInfo();
        }

        protected virtual void Reset() {
            EditorApplication.delayCall += () => {
                if (this) {
                    m_ReferenceClip = null;
                    CacheFrameInfo();
                    EditorUtility.SetDirty(this);
                }
            };
        }

        protected void CacheFrameInfo() {
            AnimUtility.Editor.CacheClip(this, ref m_ReferenceClip);
            if (m_ReferenceClip) {
                m_FrameCount = m_ReferenceClip.FrameCount();
            } else {
                m_FrameCount = 1;
            }
        }
#endif // UNITY_EDITOR
    }
}