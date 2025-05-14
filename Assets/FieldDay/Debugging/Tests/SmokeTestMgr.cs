#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace FieldDay.Debugging {
    static public class SmokeTestMgr {
#if DEVELOPMENT
        private enum SmokeTestState {
            Uninitialized,
            Running,
            Success,
            TimedOut,
            EncounteredError,
            EncounteredException,
            EncounteredAssert
        }

        static private readonly RingBuffer<SmokeTestData> s_ScheduledTests = new RingBuffer<SmokeTestData>(64, RingBufferMode.Fixed);
        static private Routine s_CurrentTestRoutine;
        static private SmokeTestState s_TestState;
        static private float s_TimeOutAccumulator;
        static private readonly StringBuilder s_LogAccumulator = new StringBuilder(4096);

        static private void BeginQueue() {
            s_TestState = SmokeTestState.Uninitialized;
            s_LogAccumulator.Length = 0;
            s_CurrentTestRoutine.Stop();

            Time.timeScale = 1;

            DebugInput.Pause();
            DebugFlags.SetAutomatedTestActive(true);

            Application.logMessageReceived -= OnApplicationLog;
            Application.logMessageReceived += OnApplicationLog;

            Assert.DeregisterLogHook();
            Assert.SetFailureMode(Assert.FailureMode.Automatic);

            GameLoop.OnDebugUpdate.Register(Tick);
        }

        static private void EndQueue() {
            Assert.SetFailureMode(Assert.FailureMode.User);
            Assert.RegisterLogHook();

            Application.logMessageReceived -= OnApplicationLog;

            DebugFlags.SetAutomatedTestActive(false);
            DebugInput.Resume();

            GameLoop.OnDebugUpdate.Deregister(Tick);

            s_TestState = SmokeTestState.Uninitialized;
            s_LogAccumulator.Length = 0;
            s_CurrentTestRoutine.Stop();
        }

        static private void Tick(float deltaTime) {
            if (!s_ScheduledTests.TryPeekFront(out SmokeTestData test)) {
                EndQueue();
                return;
            }

            if (s_TestState == SmokeTestState.Running) {
                if (s_CurrentTestRoutine) {
                    s_TimeOutAccumulator += deltaTime;
                    if (s_TimeOutAccumulator > test.TimeOut) {
                        FailTest(SmokeTestState.TimedOut);
                    }
                } else {
                    s_TestState = SmokeTestState.Success;
                }
            } else if (s_TestState == SmokeTestState.Uninitialized) {
                s_TimeOutAccumulator = 0;
                s_LogAccumulator.Length = 0;
                s_TestState = SmokeTestState.Running;
                BeginTest(test);
                if (s_TestState == SmokeTestState.Running) {
                    test.Execute?.Invoke();
                }
                if (s_TestState == SmokeTestState.Running) {
                    if (test.ExecuteAsync != null) {
                        s_CurrentTestRoutine.Replace(test.ExecuteAsync()).SetPriority(1000000);
                    }
                }
            } else {
                bool pass = s_TestState == SmokeTestState.Success;
                EndTest(test);
                // TODO: Report out
                s_ScheduledTests.PopFront();
            }
        }

        static private void FailTest(SmokeTestState state) {
            s_CurrentTestRoutine.Stop();
            if (s_TestState < state) {
                s_TestState = state;
            }
        }

        static private void BeginTest(in SmokeTestData test) {
            try {
                test.Prolog?.Invoke();
            }
            catch(Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        static private void EndTest(in SmokeTestData test) {
            try {
                test.Epilog?.Invoke();
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        #region Handlers

        static private void OnApplicationLog(string condition, string stackTrace, UnityEngine.LogType type) {
            Report(s_LogAccumulator, condition, stackTrace, type);
            switch(type) {
                case LogType.Error: {
                    FailTest(SmokeTestState.EncounteredError);
                    break;
                }
                case LogType.Exception: {
                    FailTest(SmokeTestState.EncounteredException);
                    break;
                }
                case LogType.Assert: {
                    FailTest(SmokeTestState.EncounteredAssert);
                    break;
                }
            }
        }

        static private void Report(StringBuilder sb, string condition, string stackTrace, UnityEngine.LogType type) {
            switch (type) {
                case LogType.Assert: {
                        sb.Append("ASSERT: ");
                        break;
                    }

                case LogType.Error: {
                        sb.Append("ERROR: ");
                        break;
                    }

                case LogType.Exception: {
                        sb.Append("EXCEPTION: ");
                        break;
                    }

                case LogType.Warning: {
                        sb.Append("WARN: ");
                        break;
                    }
            }
            sb.Append(condition).Append('\n').Append(stackTrace).Append('\n');
        }

        #endregion // Handlers

#endif // DEVELOPMENT
    }

    public struct SmokeTestData {
        public string Name;
        public Action Prolog;
        public Action Execute;
        public Func<IEnumerator> ExecuteAsync;
        public Action Epilog;
        public float TimeOut;
    }
}