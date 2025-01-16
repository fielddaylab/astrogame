#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using ScriptableBake;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Data {
    [CreateAssetMenu(menuName = "Field Day/Core/Reflection Boot Data")]
    public sealed class ReflectionBootData : ScriptableObject, IBaked {
        public const BindingFlags DefaultFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [SerializeField, HideInEditor] internal SerializedAttributeSet InvokePreBoot;
        [SerializeField, HideInEditor] internal SerializedAttributeSet InvokeBoot;
        [SerializeField, HideInEditor] internal SerializedAttributeSet ConfigVars;
        [SerializeField, HideInEditor] internal SerializedAttributeSet DebugMenu;
        [SerializeField, HideInEditor] internal SerializedAttributeSet EngineMenu;
        [SerializeField, HideInEditor] internal SerializedAttributeSet QuickMenu;

        #region Mounting

        static private ReflectionBootData s_Mounted;

        static internal void Mount(ReflectionBootData bootData) {
            if (s_Mounted == bootData) {
                return;
            }

            if (s_Mounted != null) {
                if (bootData == null) {
                    s_Mounted = null;
                    Log.Msg("[ReflectionBootData] Unmounted instance");
                } else {
                    Log.Error("[ReflectionBootData] A ReflectionBootData has already been mounted");
                }
            } else {
                s_Mounted = bootData;
#if UNITY_EDITOR
                Log.Warn("[ReflectionBootData] Mounted instance (editor) - make sure it's up to date!");
#else
                Log.Msg("[ReflectionBootData] Mounted instance");
#endif // UNITY_EDITOR
            }
        }

        #endregion // Mounting

        #region Reading Data

        static internal IEnumerable<AttributeBinding<ConfigVar, MemberInfo>> GetAllConfigVars() {
            if (s_Mounted != null && !string.IsNullOrEmpty(s_Mounted.ConfigVars.AttributeTypeName)) {
                return s_Mounted.ConfigVars.Read<ConfigVar>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMembers<ConfigVar>(ReflectionCache.UserAssemblies, DefaultFlags, false);
        }

        static internal IEnumerable<AttributeBinding<InvokePreBootAttribute, MethodInfo>> GetPreBoot() {
            if (s_Mounted != null && !string.IsNullOrEmpty(s_Mounted.InvokePreBoot.AttributeTypeName)) {
                return s_Mounted.InvokePreBoot.Read<InvokePreBootAttribute, MethodInfo>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMethods<InvokePreBootAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
        }

        static internal IEnumerable<AttributeBinding<InvokeOnBootAttribute, MethodInfo>> GetBoot() {
            if (s_Mounted != null && !string.IsNullOrEmpty(s_Mounted.InvokeBoot.AttributeTypeName)) {
                return s_Mounted.InvokeBoot.Read<InvokeOnBootAttribute, MethodInfo>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMethods<InvokeOnBootAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
        }

#if DEVELOPMENT

        static internal IEnumerable<AttributeBinding<DebugMenuFactoryAttribute, MethodInfo>> DebugMenus() {
            if (s_Mounted != null && !string.IsNullOrEmpty(s_Mounted.DebugMenu.AttributeTypeName)) {
                return s_Mounted.DebugMenu.Read<DebugMenuFactoryAttribute, MethodInfo>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMethods<DebugMenuFactoryAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
        }

        static internal IEnumerable<AttributeBinding<EngineMenuFactoryAttribute, MethodInfo>> EngineMenus() {
            if (s_Mounted != null && !string.IsNullOrEmpty(s_Mounted.EngineMenu.AttributeTypeName)) {
                return s_Mounted.EngineMenu.Read<EngineMenuFactoryAttribute, MethodInfo>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMethods<EngineMenuFactoryAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
        }

        static internal IEnumerable<AttributeBinding<QuickMenuFactoryAttribute, MethodInfo>> QuickMenus() {
            if (s_Mounted != null && !string.IsNullOrEmpty(s_Mounted.QuickMenu.AttributeTypeName)) {
                return s_Mounted.QuickMenu.Read<QuickMenuFactoryAttribute, MethodInfo>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMethods<QuickMenuFactoryAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
        }

#endif // DEVELOPMENT

        #endregion // Reading Data

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order { get { return 100000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            InvokePreBoot = SerializedAttributeSet.Create<InvokePreBootAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
            InvokeBoot = SerializedAttributeSet.Create<InvokeOnBootAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
            ConfigVars = SerializedAttributeSet.Create<ConfigVar>(ReflectionCache.UserAssemblies, DefaultFlags);

            if ((flags & BakeFlags.IsDevelopment) != 0) {
                DebugMenu = SerializedAttributeSet.Create<DebugMenuFactoryAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
                EngineMenu = SerializedAttributeSet.Create<EngineMenuFactoryAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
                QuickMenu = SerializedAttributeSet.Create<QuickMenuFactoryAttribute>(ReflectionCache.UserAssemblies, DefaultFlags);
            } else {
                DebugMenu = QuickMenu = EngineMenu = null;
            }

            return true;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked

        #region Editor Integration

        static public bool ShouldUse() {
#if UNITY_EDITOR
            return EditorPrefs.GetBool(EditorTestPrefsKey);
#else
            return true;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR

        private const string EditorTestPrefsKey = "FieldDay/UseCachedReflectionData";
        private const string EditorTestMenuItem = "Field Day/Test with Cached Reflection Data";

        [MenuItem(EditorTestMenuItem, validate = false)]
        static private void TestingCheckbox() {
            bool isSet = EditorPrefs.GetBool(EditorTestPrefsKey);
            EditorPrefs.SetBool(EditorTestPrefsKey, !isSet);
            Menu.SetChecked(EditorTestMenuItem, !isSet);
        }

        [MenuItem(EditorTestMenuItem, validate = true)]
        static private bool TestingCheckbox_Validate() {
            bool isSet = EditorPrefs.GetBool(EditorTestPrefsKey);
            Menu.SetChecked(EditorTestMenuItem, isSet);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

#endif // UNITY_EDITOR

        #endregion // Editor Integration
    }
}