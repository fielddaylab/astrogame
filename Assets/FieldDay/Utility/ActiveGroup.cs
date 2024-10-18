using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay {
    /// <summary>
    /// Group of objects that can be activated and deactivated.
    /// </summary>
    [Serializable]
    public class ActiveGroup {
        private enum State {
            Uninitialized,
            Active,
            Inactive
        }

        public GameObject[] GameObjects = Array.Empty<GameObject>();
        public Behaviour[] Behaviours = Array.Empty<Behaviour>();

        [NonSerialized] private State m_State = State.Uninitialized;

        public bool IsEmpty {
            get { return GameObjects.Length == 0 && Behaviours.Length == 0; }
        }

        public bool IsActive {
            get { return m_State == State.Active; }
        }

        public bool SetActive(bool state) {
            State s = state ? State.Active : State.Inactive;
            if (m_State == s) {
                return false;
            }

            InternalSetActive(state);
            m_State = s;
            return true;
        }

        public bool SetActive(bool state, bool force) {
            if (force) {
                m_State = state ? State.Active : State.Inactive;
                InternalSetActive(state);
                return true;
            }

            return SetActive(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceActive(bool state) {
            InternalSetActive(state);
            m_State = state ? State.Active : State.Inactive;
        }

        private void InternalSetActive(bool active) {
            for(int i = 0, len = GameObjects.Length; i < len; i++) {
                GameObjects[i].SetActive(active);
            }
            for (int i = 0, len = Behaviours.Length; i < len; i++) {
                Behaviours[i].enabled = active;
            }
        }
    }
}