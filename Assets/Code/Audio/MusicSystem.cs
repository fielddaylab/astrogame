using Astro.Audio;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate, 1000)]
    public class MusicSystem : SharedStateSystemBehaviour<MusicState> {
        public override void ProcessWork(float deltaTime) {
            switch(m_State.CurrentState) {
                case MusicState.State.FadeOut: {
                    if (!Sfx.IsActive(m_State.MusicTrack)) {
                        m_State.MusicTrack = default;
                        m_State.CurrentTrackId = default;
                        m_State.CurrentState = MusicState.State.Stopped;
                        TryBeginQueuedTrack();
                    }
                    break;
                }

                case MusicState.State.Stopped: {
                    TryBeginQueuedTrack();
                    break;
                }

                case MusicState.State.Playing: {
                    if (!Sfx.IsActive(m_State.MusicTrack)) {
                        m_State.MusicTrack = default;
                        m_State.CurrentTrackId = default;
                        m_State.CurrentState = MusicState.State.Stopped;
                    } else if (!m_State.Queued.TrackId.IsEmpty) {
                        if (m_State.Queued.TrackId == m_State.CurrentTrackId) {
                            m_State.Queued = default;
                        } else {
                            Sfx.Stop(m_State.MusicTrack, m_State.Queued.FadeIn);
                            TryBeginQueuedTrack();
                        }
                    }
                    break;
                }
            }
        }

        private void TryBeginQueuedTrack() {
            if (m_State.Queued.TrackId.IsEmpty) {
                return;
            }

            m_State.CurrentTrackId = m_State.Queued.TrackId;
            m_State.CurrentState = MusicState.State.Playing;
            m_State.MusicTrack = Sfx.Play(m_State.Queued.TrackId, new SfxPlayArgs() {
                Volume = 0,
                Pitch = 1
            });
            Sfx.OverrideTag(m_State.MusicTrack, m_State.MusicTag);
            Sfx.SetVolume(m_State.MusicTrack, 1, m_State.Queued.FadeIn);
            m_State.Queued = default;
        }
    }
}