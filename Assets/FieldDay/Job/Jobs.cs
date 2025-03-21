using BeauRoutine;
using System;
using System.Collections;
using Unity.Jobs;

namespace FieldDay.Jobs {
    static public class Jobs {
        static public AsyncHandle Push(IEnumerator asyncJob, AsyncFlags flags) {
            return Async.Schedule(asyncJob, flags);
        }

        static public AsyncHandle Push(Action asyncJob, AsyncFlags flags) {
            return Async.Schedule(asyncJob, flags);
        }

        static public AsyncHandle PushLoadDependency(IEnumerator asyncJob, AsyncFlags flags) {
            AsyncHandle handle = Push(asyncJob, flags);
            Game.Scenes.RegisterLoadDependency(handle);
            return handle;
        }

        static public AsyncHandle PushLoadDependency(Action asyncJob, AsyncFlags flags) {
            AsyncHandle handle = Push(asyncJob, flags);
            Game.Scenes.RegisterLoadDependency(handle);
            return handle;
        }

        static public JobHandle Push<T>(in T jobStruct) where T : struct, IJob {
            return jobStruct.Schedule();
        }

        static public JobHandle Push<T>(in T jobStruct, JobHandle dependency) where T : struct, IJob {
            return jobStruct.Schedule(dependency);
        }
    }
}