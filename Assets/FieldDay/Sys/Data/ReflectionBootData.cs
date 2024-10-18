using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil;
using FieldDay.Debugging;
using ScriptableBake;
using UnityEngine;

namespace FieldDay.Data {
    [CreateAssetMenu(menuName = "Field Day/Core/Reflection Boot Data")]
    public sealed class ReflectionBootData : ScriptableObject, IBaked {
        [SerializeField, HideInEditor] internal SerializedAttributeSet InvokePreBoot;
        [SerializeField, HideInEditor] internal SerializedAttributeSet InvokeBoot;
        [SerializeField, HideInEditor] internal SerializedAttributeSet ConfigVars;
        [SerializeField, HideInEditor] internal SerializedAttributeSet DebugMenu;
        [SerializeField, HideInEditor] internal SerializedAttributeSet EngineMenu;
        [SerializeField, HideInEditor] internal SerializedAttributeSet QuickMenu;

        static private ReflectionBootData s_Mounted;

        static public IEnumerable<AttributeBinding<ConfigVar, MemberInfo>> GetAllConfigVars() {
            if (s_Mounted != null) {
                return s_Mounted.ConfigVars.Read<ConfigVar>(ReflectionCache.UserAssemblies);
            }
            return Reflect.FindMembers<ConfigVar>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, false);
        }

        //static public AttributeEnumerable<InvokePreBootAttribute, MethodInfo> GetPreBoot() {
        //    if (s_Mounted != null) {
        //        return new AttributeEnumerable<InvokePreBootAttribute, MethodInfo>(s_Mounted.InvokePreBoot.Read<InvokePreBootAttribute>(ReflectionCache.UserAssemblies));
        //    }
        //    return new AttributeEnumerable<ConfigVar, FieldInfo>(Reflect.FindFields<ConfigVar>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, false));
        //}

#if UNITY_EDITOR

        int IBaked.Order { get { return 100000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            InvokePreBoot = SerializedAttributeSet.Create<InvokePreBootAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            InvokeBoot = SerializedAttributeSet.Create<InvokeOnBootAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            ConfigVars = SerializedAttributeSet.Create<ConfigVar>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if ((flags & BakeFlags.IsDevelopment) != 0) {
                DebugMenu = SerializedAttributeSet.Create<DebugMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                EngineMenu = SerializedAttributeSet.Create<EngineMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                QuickMenu = SerializedAttributeSet.Create<QuickMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            } else {
                DebugMenu = QuickMenu = EngineMenu = null;
            }

            return true;
        }

#endif // UNITY_EDITOR
    }
}