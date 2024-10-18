#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Reflection;
using BeauData;
using System.Diagnostics;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Scripting;
using System.IO;
using FieldDay.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Data {
    /// <summary>
    /// Attribute marking a static field as a configurable variable,
    /// that will be serialized and automatically show up in the debug menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigVar : PreserveAttribute {
        public enum FieldType {
            Int,
            UInt,
            Float,
            Double,
            Bool,
            Enum
        }

        public string DisplayName;
        public string Category;

        private string m_DataName;
        private FieldType m_Type;
        private FieldInfo m_Field;
        private object m_ProgrammerDefault;
        private object m_ActiveDefault;

        private ReflectionCache.EnumInfoCache m_EnumInfo;
        private float m_MinValue;
        private float m_MaxValue;
        private float m_Increment = float.NaN;

        private readonly int m_CreationIndex;
        static private int s_NextCreationIndex;

        static private readonly Dictionary<StringHash32, CastableEvent<string>> s_Callbacks = new Dictionary<StringHash32, CastableEvent<string>>(16);

        #region Constructors

        public ConfigVar(string displayName) : this(displayName, null) { }

        public ConfigVar(string displayName, string category) {
            DisplayName = displayName;
            Category = category;
            m_CreationIndex = s_NextCreationIndex++;
        }

        public ConfigVar(string displayName, int minValue, int maxValue, int increment = 1) : this(displayName, null, minValue, maxValue, increment) { }

        public ConfigVar(string displayName, string category, int minValue, int maxValue, int increment = 1) : this(displayName, category) {
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Increment = increment;
        }

        public ConfigVar(string displayName, float minValue, float maxValue, float increment = 0) : this(displayName, null, minValue, maxValue, increment) { }

        public ConfigVar(string displayName, string category, float minValue, float maxValue, float increment = 0) : this(displayName, category) {
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Increment = increment;
        }

        #endregion // Constructors

        internal void Bind(FieldInfo field) {
            m_Field = field;
            if (!field.IsStatic) {
                throw new ArgumentException(string.Format("Non-static field '{0}::{1}' cannot be used as a config var", field.DeclaringType.FullName, field.Name));
            }
            m_ProgrammerDefault = field.GetValue(null);
            m_ActiveDefault = m_ProgrammerDefault;

            if (string.IsNullOrEmpty(DisplayName)) {
                DisplayName = ReflectionCache.InspectorName(field.Name);
            }

            if (string.IsNullOrEmpty(Category)) {
                Category = ReflectionCache.InspectorName(field.DeclaringType.Name);
            }

            m_DataName = string.Concat(field.DeclaringType.FullName, "::", field.Name);

            switch (Type.GetTypeCode(field.FieldType)) {
                case TypeCode.Boolean: {
                    m_Type = FieldType.Bool;
                    break;
                }
                case TypeCode.Int32: {
                    m_Type = FieldType.Int;
                    EstablishDefaultRanges(1, true);
                    break;
                }
                case TypeCode.UInt32: {
                    m_Type = FieldType.UInt;
                    EstablishDefaultRanges(1, true);
                    break;
                }
                case TypeCode.Single: {
                    m_Type = FieldType.Float;
                    EstablishDefaultRanges(0, false);
                    break;
                }
                case TypeCode.Double: {
                    m_Type = FieldType.Double;
                    EstablishDefaultRanges(0, false);
                    break;
                }
                default: {
                    Type fieldType = field.FieldType;
                    if (fieldType.IsEnum) {
                        m_Type = FieldType.Enum;
                        m_EnumInfo = ReflectionCache.EnumInfo(fieldType);
                        m_MinValue = 0;
                        m_MaxValue = m_EnumInfo.Values.Length - 1;
                        m_Increment = 1;
                    } else {
                        throw new ArgumentException(string.Format("Field '{0}::{1}' cannot be used as a config var - incompatible type '{2}'", field.DeclaringType.FullName, field.Name, field.FieldType.FullName));
                    }
                    break;
                }
            }
        }

        internal void BindCurrentAsDefault() {
            m_ActiveDefault = m_Field.GetValue(null);
        }

        #region Helpers

        private void EstablishDefaultRanges(float increment, bool round) {
            if (float.IsNaN(m_Increment)) {
                Log.Warn("[ConfigVar] Var '{0}' does not have ranges set - defaulting to 0-100", m_DataName);
                m_MinValue = 0;
                m_MaxValue = 100;
                m_Increment = increment;
            } else if (round) {
                m_MinValue = (int) m_MinValue;
                m_MaxValue = (int) m_MaxValue;
                m_Increment = Math.Max(0, (int) m_Increment);
            }
        }

        static private T Get<T>(FieldInfo field) {
            return (T) field.GetValue(null);
        }

        static private void Set<T>(FieldInfo field, string category, T value) {
            field.SetValue(null, value);
            InvokeChangedCallback(field, category);
        }

        [Conditional("DEVELOPMENT")]
        static private void InvokeChangedCallback(FieldInfo field, string category) {
#if DEVELOPMENT
            CastableEvent<string> categoryEvents;
            if (s_Callbacks.TryGetValue(category, out categoryEvents)) {
                categoryEvents.Invoke(field.Name);
            }
            if (s_Callbacks.TryGetValue(field.DeclaringType.Name, out categoryEvents)) {
                categoryEvents.Invoke(field.Name);
            }
#endif // DEVELOPMENT
        }

        #endregion // Helpers

        #region Debug Menu

#if DEVELOPMENT

        [DebugMenuFactory]
        static private DMInfo DebugMenuFactory() {
            // config vars
            DMInfo configVarMenu = new DMInfo("Config Vars", 8);

            configVarMenu.AddButton("Save Changes", ConfigVar.WriteUserToPlayerPrefs);
            configVarMenu.AddButton("Reload Changes", ConfigVar.ReadUserFromPlayerPrefs, ConfigVar.HasUserPrefs);
            configVarMenu.AddDivider();

            configVarMenu.AddButton("Commit Changes", ConfigVar.WriteAllToResources, () => Application.isEditor);
            configVarMenu.AddButton("Reset (Committed)", () => ConfigVar.Reset(ConfigVar.AllVars));
            configVarMenu.AddButton("Reset (Programmer)", () => ConfigVar.ProgrammerReset(ConfigVar.AllVars));
            configVarMenu.AddDivider();

            configVarMenu.AddDivider();

            string currentCategory = null;
            DMInfo categoryMenu = null;
            foreach (var cvar in ConfigVar.AllVars) {
                if (cvar.Category != currentCategory) {
                    currentCategory = cvar.Category;
                    categoryMenu = DMInfo.FindOrCreateSubmenu(configVarMenu, currentCategory);
                }

                ConfigVar.CreateDebugMenu(categoryMenu, cvar);
            }

            return configVarMenu;
        }

#endif // DEVELOPMENT

        /// <summary>
        /// Creates a debug menu for the given config variable.
        /// </summary>
        static public void CreateDebugMenu(DMInfo menu, ConfigVar cvar, DMPredicate predicate = null, int indent = 0) {
            switch (cvar.m_Type) {
                case FieldType.Bool: {
                    menu.AddToggle(cvar.DisplayName, () => Get<bool>(cvar.m_Field), (v) => Set(cvar.m_Field, cvar.Category, v), predicate, indent);
                    break;
                }
                case FieldType.Int: {
                    menu.AddSlider(cvar.DisplayName, () => Get<int>(cvar.m_Field), (v) => Set(cvar.m_Field, cvar.Category, (int) v), cvar.m_MinValue, cvar.m_MaxValue, cvar.m_Increment, (DMFloatTextDelegate) null, predicate, indent);
                    break;
                }
                case FieldType.UInt: {
                    menu.AddSlider(cvar.DisplayName, () => Get<uint>(cvar.m_Field), (v) => Set(cvar.m_Field, cvar.Category, (uint) v), cvar.m_MinValue, cvar.m_MaxValue, cvar.m_Increment, (DMFloatTextDelegate) null, predicate, indent);
                    break;
                }
                case FieldType.Float: {
                    menu.AddSlider(cvar.DisplayName, () => Get<float>(cvar.m_Field), (v) => Set(cvar.m_Field, cvar.Category, (float) v), cvar.m_MinValue, cvar.m_MaxValue, cvar.m_Increment, (DMFloatTextDelegate) null, predicate, indent);
                    break;
                }
                case FieldType.Double: {
                    menu.AddSlider(cvar.DisplayName, () => (float) Get<double>(cvar.m_Field), (v) => Set(cvar.m_Field, cvar.Category, (double) v), cvar.m_MinValue, cvar.m_MaxValue, cvar.m_Increment, (DMFloatTextDelegate) null, predicate, indent);
                    break;
                }
                case FieldType.Enum: {
                    menu.AddSlider(cvar.DisplayName, () => {
                        return Array.IndexOf(cvar.m_EnumInfo.Values, Get<object>(cvar.m_Field));
                    }, (f) => {
                        int idx = (int) f;
                        if (idx >= 0 && idx < cvar.m_EnumInfo.Values.Length) {
                            Set(cvar.m_Field, cvar.Category, cvar.m_EnumInfo.Values[idx]);
                        }
                    }, 0, cvar.m_EnumInfo.Values.Length - 1, 1,
                    (f) => {
                        int idx = (int) f;
                        if (idx < 0 || idx >= cvar.m_EnumInfo.Values.Length) {
                            return string.Empty;
                        } else {
                            return cvar.m_EnumInfo.InspectorNames[idx];
                        }
                    }, predicate, indent);
                    break;
                }
            }
        }

        #endregion // Debug Menu

        #region Reading/Writing

        /// <summary>
        /// Writes the given config vars out to a JSON object.
        /// </summary>
        static public JSON Write(IEnumerable<ConfigVar> vars, WriteFlags flags = WriteFlags.SkipDefaultValues) {
            JSON json = JSON.CreateObject();
            Write(vars, json, flags);
            return json;
        }

        /// <summary>
        /// Writes the given config vars to JSON.
        /// </summary>
        static public void Write(IEnumerable<ConfigVar> vars, JSON output, WriteFlags flags = WriteFlags.SkipDefaultValues) {
            foreach(var cvar in vars) {
                JSON target = output[cvar.m_DataName];
                object val = cvar.m_Field.GetValue(null);
                if ((flags & WriteFlags.SkipProgrammerDefaultValues) != 0 && val.Equals(cvar.m_ProgrammerDefault)) {
                    continue;
                } else if ((flags & WriteFlags.SkipDefaultValues) != 0 && val.Equals(cvar.m_ActiveDefault)) {
                    continue;
                }
                switch (cvar.m_Type) {
                    case FieldType.Bool: {
                        target.AsBool = (bool) val;
                        break;
                    }
                    case FieldType.Int: {
                        target.AsInt = (int) val;
                        break;
                    }
                    case FieldType.UInt: {
                        target.AsUInt = (uint) val;
                        break;
                    }
                    case FieldType.Float: {
                        target.AsFloat = (float) val;
                        break;
                    }
                    case FieldType.Double: {
                        target.AsDouble = (double) val;
                        break;
                    }
                    case FieldType.Enum: {
                        target.AsString = val.ToString();
                        break;
                    }
                }
                if ((flags & WriteFlags.BindCurrentAsDefault) != 0) {
                    cvar.BindCurrentAsDefault();
                }
            }
        }

        /// <summary>
        /// Reads config variables in from JSON.
        /// </summary>
        static public void Read(IEnumerable<ConfigVar> vars, JSON input, ReadFlags flags = ReadFlags.OverwriteMissingWithDefaults) {
            foreach(var cvar in vars) {
                JSON target = input[cvar.m_DataName];
                if (!target.IsUndefined) {
                    object val = null;
                    switch (cvar.m_Type) {
                        case FieldType.Bool: {
                            val = target.AsBool;
                            break;
                        }
                        case FieldType.Int: {
                            val = target.AsInt;
                            break;
                        }
                        case FieldType.UInt: {
                            val = target.AsUInt;
                            break;
                        }
                        case FieldType.Float: {
                            val = target.AsFloat;
                            break;
                        }
                        case FieldType.Double: {
                            val = target.AsDouble;
                            break;
                        }
                        case FieldType.Enum: {
                            Enum.TryParse(cvar.m_Field.FieldType, target.AsString, true, out val);
                            break;
                        }
                    }
                    if (val != null) {
                        if ((flags & ReadFlags.WarnOnPotentialConflicts) != 0) {
                            object current = cvar.m_Field.GetValue(null);
                            if (!current.Equals(cvar.m_ProgrammerDefault) && !current.Equals(cvar.m_ActiveDefault)) {
                                Log.Warn("[ConfigVar] Potential conflict on '{0}' - overwriting existing non-default value {1} with {2}", cvar.m_DataName, current, val);
                            }
                        }
                        cvar.m_Field.SetValue(null, val);
                        if ((flags & ReadFlags.BindValuesAsDefault) != 0) {
                            cvar.m_ActiveDefault = val;
                        }
                        InvokeChangedCallback(cvar.m_Field, cvar.Category);
                    }
                } else if ((flags & ReadFlags.OverwriteMissingWithDefaults) != 0) {
                    cvar.m_Field.SetValue(null, cvar.m_ActiveDefault);
                    InvokeChangedCallback(cvar.m_Field, cvar.Category);
                } else if ((flags & ReadFlags.OverwriteMissingWithProgrammerDefaults) != 0) {
                    cvar.m_Field.SetValue(null, cvar.m_ProgrammerDefault);
                    InvokeChangedCallback(cvar.m_Field, cvar.Category);
                }
            }
        }

        /// <summary>
        /// Resets the given config vars to their code default values.
        /// </summary>
        static public void ProgrammerReset(IEnumerable<ConfigVar> vars) {
            foreach (var cvar in vars) {
                cvar.m_Field.SetValue(null, cvar.m_ProgrammerDefault);
                cvar.m_ActiveDefault = cvar.m_ProgrammerDefault;
                InvokeChangedCallback(cvar.m_Field, cvar.Category);
            }
        }

        /// <summary>
        /// Resets the given config vars to their last active default values.
        /// </summary>
        static public void Reset(IEnumerable<ConfigVar> vars) {
            foreach (var cvar in vars) {
                cvar.m_Field.SetValue(null, cvar.m_ActiveDefault);
                InvokeChangedCallback(cvar.m_Field, cvar.Category);
            }
        }

        [Flags]
        public enum WriteFlags {
            SkipDefaultValues = 0x01,
            SkipProgrammerDefaultValues = 0x02,
            BindCurrentAsDefault = 0x04
        }

        [Flags]
        public enum ReadFlags {
            OverwriteMissingWithDefaults = 0x01,
            OverwriteMissingWithProgrammerDefaults = 0x02,
            WarnOnPotentialConflicts = 0x04,
            BindValuesAsDefault = 0x08
        }

        #endregion // Reading/Writing

        #region Reflection

        static private ConfigVar[] s_AllVars;

        /// <summary>
        /// All configuration variables.
        /// </summary>
        static public ConfigVar[] AllVars {
            get {
                if (s_AllVars == null) {
                    s_AllVars = FindAllVars();
                    Log.Msg("[ConfigVar] Found {0} config variables", s_AllVars.Length);
                }
                return s_AllVars;
            }
        }

        static private ConfigVar[] FindAllVars() {
            List<ConfigVar> list = new List<ConfigVar>(512);
            foreach(var kv in ReflectionBootData.GetAllConfigVars()) {
                try {
                    kv.Attribute.Bind((FieldInfo) kv.Info);
                    list.Add(kv.Attribute);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            }
            list.Sort((a, b) => {
                int categorySort = a.Category.CompareTo(b.Category);
                if (categorySort == 0) {
                    return a.m_CreationIndex - b.m_CreationIndex;
                } else {
                    return categorySort;
                }
            });
            return list.ToArray();
        }

        #endregion // Reflection

        #region Serialization

        private const string ResourceFileName = "__ConfigVarValues";
        private const string ResourceFilePath = "Assets/Resources/" + ResourceFileName + ".json";
        private const string PlayerPrefsKey = "ConfigVar::UserOverrides";

        /// <summary>
        /// Writes all config vars to resources.
        /// </summary>
        static public void WriteAllToResources() {
#if UNITY_EDITOR
            JSON changes = Write(AllVars, WriteFlags.SkipProgrammerDefaultValues | WriteFlags.BindCurrentAsDefault);
            if (!Directory.Exists("Assets/Resources")) {
                Directory.CreateDirectory("Assets/Resources");
            }
            using(StreamWriter writer = new StreamWriter(File.Open(ResourceFilePath, FileMode.Create))) {
                changes.WriteTo(writer, 4);
            }
            Log.Msg("[ConfigVar] Wrote all {0} modified values to '" + ResourceFilePath + "'", changes.Count);
            AssetDatabase.ImportAsset(ResourceFilePath);
#else
            UnityEngine.Debug.LogError("[ConfigVar] Cannot write config vars to resources outside of the editor");
#endif // UNITY_EDITOR
        }

        /// <summary>
        /// Reads all config vars from resources.
        /// </summary>
        static public void ReadAllFromResources() {
            JSON changes = null;

            var all = AllVars;
            
            TextAsset resource = UnityEngine.Resources.Load<TextAsset>(ResourceFileName); 
            if (resource != null) {
                try {
                    changes = JSON.Parse(resource.text);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                    changes = null;
                } finally {
                    UnityEngine.Resources.UnloadAsset(resource);
                }
            }
            
            if (changes != null) {
                Log.Msg("[ConfigVar] Config file was found - reading in all {0} values...", changes.Count);
                Read(all, changes, ReadFlags.OverwriteMissingWithProgrammerDefaults | ReadFlags.BindValuesAsDefault);
            } else {
                Log.Msg("[ConfigVar] Config file was unable to be located or parsed - resetting all to defaults...");
                Reset(all);
            }
        }

        /// <summary>
        /// Reads all user overrides to player prefs.
        /// </summary>
        static public void WriteUserToPlayerPrefs() {
#if DEVELOPMENT
            JSON changes = Write(AllVars, WriteFlags.SkipDefaultValues);
            PlayerPrefs.SetString(PlayerPrefsKey, changes.ToString());
            PlayerPrefs.Save();
            Log.Msg("[ConfigVar] Wrote all {0} user override values to PlayerPrefs", changes.Count);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Reads all user overrides to player prefs.
        /// </summary>
        static public void ReadUserFromPlayerPrefs() {
#if DEVELOPMENT
            JSON changes = null;
            if (PlayerPrefs.HasKey(PlayerPrefsKey)) {
                string val = PlayerPrefs.GetString(PlayerPrefsKey);
                try {
                    changes = JSON.Parse(val);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                    changes = null;
                }
            }

            if (changes != null) {
                Log.Msg("[ConfigVar] User config overrides were found - reading in all {0} values...", changes.Count);
                Read(AllVars, changes, ReadFlags.WarnOnPotentialConflicts);
            } else {
                Log.Msg("[ConfigVar] User config overrides not found");
            }
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Returns if there are user overrides in player prefs.
        /// </summary>
        static public bool HasUserPrefs() {
#if DEVELOPMENT
            return PlayerPrefs.HasKey(PlayerPrefsKey);
#else
            return false;
#endif // DEVELOPMENT
        }

        #endregion // Serialization

        #region Callbacks

        /// <summary>
        /// Registers a callback to be invoked when a config variable is modified.
        /// </summary>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR")]
        static public void RegisterModifiedCallback(string categoryOrGroup, Action<string> callback) {
#if DEVELOPMENT
            StringHash32 id = categoryOrGroup;
            if (!s_Callbacks.TryGetValue(id, out CastableEvent<string> callbacks)) {
                callbacks = new CastableEvent<string>(4);
                s_Callbacks.Add(id, callbacks);
            }
            callbacks.Register(callback);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Deregisters a callback to be invoked when a config variable is modified.
        /// </summary>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR")]
        static public void DeregisterModifiedCallback(string categoryOrGroup, Action<string> callback) {
#if DEVELOPMENT
            StringHash32 id = categoryOrGroup;
            if (s_Callbacks.TryGetValue(id, out CastableEvent<string> callbacks)) {
                callbacks.Deregister(callback);
            }
#endif // DEVELOPMENT
        }

        #endregion // Callbacks
    }
}