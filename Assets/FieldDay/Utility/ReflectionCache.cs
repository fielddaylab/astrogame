using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FieldDay {
    /// <summary>
    /// Reflection cache.
    /// </summary>
    static public class ReflectionCache {
        /// <summary>
        /// Cached enum information.
        /// </summary>
        public struct EnumInfoCache {
            public object[] Values;
            public string[] InspectorNames;
        }

        static private readonly Dictionary<Type, EnumInfoCache> s_CachedEnumInfo = new Dictionary<Type, EnumInfoCache>(4);

        #region Assemblies

        /// <summary>
        /// Array of all user assemblies.
        /// </summary>
        static public IEnumerable<Assembly> UserAssemblies {
            get {
                return Reflect.FindAllUserAssemblies();
            }
        }

        #endregion // Assemblies

        #region Enums

        static public EnumInfoCache EnumInfo<T>() {
            return EnumInfo(typeof(T));
        }

        static public EnumInfoCache EnumInfo(Type enumType) {
            EnumInfoCache cache;
            if (!s_CachedEnumInfo.TryGetValue(enumType, out cache)) {
                List<object> values = new List<object>();
                List<string> names = new List<string>();
                foreach(var field in enumType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                    if (field.IsDefined(typeof(HiddenAttribute)) || field.IsDefined(typeof(ObsoleteAttribute))) {
                        continue;
                    }

                    LabelAttribute label = (LabelAttribute) field.GetCustomAttribute(typeof(LabelAttribute));
                    string name;
                    if (label != null) {
                        name = label.Name;
                    } else {
                        name = InspectorName(field.Name);
                    }

                    object value = field.GetValue(null);

                    values.Add(value);
                    names.Add(name);
                }

                cache.Values = values.ToArray();
                cache.InspectorNames = names.ToArray();
                s_CachedEnumInfo.Add(enumType, cache);
            }
            return cache;
        }

        #endregion // Enums

        #region String

        /// <summary>
        /// Returns the nicified name for the given field/type name.
        /// </summary>
        static public unsafe string InspectorName(string name) {
            char* buff = stackalloc char[name.Length * 2];
            bool wasUpper = true, isUpper;
            int charsWritten = 0;

            int i = 0;
            if (name.Length > 1) {
                char first = name[0];
                if (first == '_') {
                    i = 1;
                } else if (first == 'm' || first == 's' || first == 'k') {
                    char second = name[1];
                    if (second == '_' || char.IsUpper(second)) {
                        i = 2;
                    }
                }
            }

            for (; i < name.Length; i++) {
                char c = name[i];
                isUpper = char.IsUpper(c);
                if (isUpper && !wasUpper && charsWritten > 0) {
                    buff[charsWritten++] = ' ';
                }
                buff[charsWritten++] = c;

                wasUpper = isUpper;
            }

            return new string(buff, 0, charsWritten);
        }

        /// <summary>
        /// Returns the analytics-style name for the given name.
        /// This makes all characters uppercase and places underscores
        /// where word breaks would occur in the original string.
        /// </summary>
        static public unsafe string AnalyticsNameUpper(string name) {
            char* buff = stackalloc char[name.Length * 2];
            bool wasUpper = true, isUpper;
            int charsWritten = 0;

            int i = 0;
            if (name.Length > 1) {
                char first = name[0];
                if (first == '_') {
                    i = 1;
                } else if (first == 'm' || first == 's' || first == 'k') {
                    char second = name[1];
                    if (second == '_' || char.IsUpper(second)) {
                        i = 2;
                    }
                }
            }

            for (; i < name.Length; i++) {
                char c = name[i];
                isUpper = char.IsUpper(c);
                if (char.IsWhiteSpace(c)) {
                    buff[charsWritten++] = '_';
                } else {
                    if (isUpper && !wasUpper && charsWritten > 0) {
                        buff[charsWritten++] = '_';
                    }
                    buff[charsWritten++] = StringUtils.ToUpperInvariant(c);
                }

                wasUpper = isUpper;
            }

            return new string(buff, 0, charsWritten);
        }

        /// <summary>
        /// Returns the analytics-style name for the given name.
        /// This formats similarly to InspectorName but without spaces
        /// </summary>
        static public unsafe string AnalyticsNamePascal(string name) {
            char* buff = stackalloc char[name.Length * 2];
            bool wasUpper = true, isUpper;
            int charsWritten = 0;

            int i = 0;
            if (name.Length > 1) {
                char first = name[0];
                if (first == '_') {
                    i = 1;
                } else if (first == 'm' || first == 's' || first == 'k') {
                    char second = name[1];
                    if (second == '_' || char.IsUpper(second)) {
                        i = 2;
                    }
                }
            }

            for (; i < name.Length; i++) {
                char c = name[i];
                isUpper = char.IsUpper(c);
                //if (isUpper && !wasUpper && charsWritten > 0) {
                //    buff[charsWritten++] = ' ';
                //}
                if (!char.IsWhiteSpace(c)) {
                    buff[charsWritten++] = c;
                }

                wasUpper = isUpper;
            }

            return new string(buff, 0, charsWritten);
        }

        #endregion // String
    }

    public struct EnumStringTable<T> where T : unmanaged, Enum {
        public readonly string[] Strings;

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Get(T value) {
            return Strings[Enums.ToInt(value)] ?? (Strings[Enums.ToInt(value)] = value.ToString());
        }
    }
}