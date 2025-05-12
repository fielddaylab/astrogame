using Astro.Audio;
using BeauUtil.Debugger;
using FieldDay.Audio;
using FieldDay.Systems;

namespace Astro {
    public class MusicSystem : SharedStateSystemBehaviour<MusicState> {
        public override bool HasWork() {
            if (base.HasWork()) {
                return !Sfx.IsActive(m_State.MusicTrack) && m_State.TrackQueue.Count > 0;
            } else return false;
        }

        public override void ProcessWork(float deltaTime) {
            m_State.CurrentTrackId = m_State.TrackQueue.PopFront();
            m_State.MusicTrack = Sfx.Play(m_State.CurrentTrackId);
            Sfx.OverrideTag(m_State.MusicTrack, m_State.MusicTag);
        }
    }
}