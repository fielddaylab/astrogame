using System;
using System.Collections.Generic;
using BeauUtil;

namespace FieldDay.Scenes {
    /// <summary>
    /// Contains a scene preload callback.
    /// </summary>
    public interface IScenePreload {
        IEnumerator<WorkSlicer.Result?> Preload();
    }

    /// <summary>
    /// Marks a preloaded component 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PreloadOrderAttribute : Attribute {
        public readonly int Order;
        
        public PreloadOrderAttribute(int order) {
            Order = order;
        }
    }
}