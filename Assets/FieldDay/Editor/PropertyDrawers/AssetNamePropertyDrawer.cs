using System;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil;
using BeauUtil.Editor;
using FieldDay.Assets;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CustomPropertyDrawer(typeof(AssetNameAttribute), true)]
    public class AssetNamePropertyDrawer : PropertyDrawer {
        private const double RebuildCacheDelay = 150;

        private struct SimpleCacheEntry {
            public double LastUpdateTime;
            public Dictionary<StringHash32, UnityEngine.Object> Items;
        }

        private struct FilteredCacheEntry {
            public NamedItemList<string> Items;
        }

        private class AssetImportHook : AssetPostprocessor {
            static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
                if (importedAssets.Length > 0 || movedAssets.Length > 0) {
                    s_HashCache.InvalidateAll();
                    s_ListCache.InvalidateAll();
                }

                if (deletedAssets.Length > 0) {
                    s_ListCache.InvalidateAll();
                }
            }
        }

        static private LruCache<long, SimpleCacheEntry> s_HashCache = new LruCache<long, SimpleCacheEntry>(32);
        static private LruCache<uint, FilteredCacheEntry> s_ListCache = new LruCache<uint, FilteredCacheEntry>(32);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            FieldInfo field = SerializedObjectUtils.GetFieldInfoFromProperty(property, out Type fieldType);
            AssetNameAttribute attr = (AssetNameAttribute) attribute;
            Render(position, property, label, fieldType, attr);
        }

        static private void HashAssetNames(Type type, ref Dictionary<StringHash32, UnityEngine.Object> items) {
            var all = AssetDBUtils.FindAssets(type);
            if (items != null) {
                items.Clear();
                items.EnsureCapacity(all.Length);
            } else {
                items = new Dictionary<StringHash32, UnityEngine.Object>(all.Length, CompareUtils.DefaultEquals<StringHash32>());
            }

            Console.WriteLine("[AssetNameAttributePropertyDrawer] Rebuilding cache for asset type " + type.Name);

            foreach(var asset in all) {
                StringHash32 key = new StringHash32(asset.name);
                if (items.ContainsKey(key)) {
                    continue;
                }

                items.Add(key, asset);
            }
        }

        static private void FillFilteredEntries(Dictionary<StringHash32, UnityEngine.Object> simpleCache, AssetNameAttribute attr, ref NamedItemList<string> items) {
            if (items != null) {
                items.Clear();
            } else {
                items = new NamedItemList<string>(simpleCache.Count + 1);
            }

            items.Add(string.Empty, "[Null]", -1);

            foreach(var obj in simpleCache.Values) {
                if (!attr.Predicate(obj)) {
                    continue;
                }

                string named = attr.Name(obj);
                items.Add(obj.name, named);
            }
        }

        /// <summary>
        /// Renders an inspector for the given property and attribute.
        /// </summary>
        static public void Render(Rect position, SerializedProperty property, GUIContent label, Type fieldType, AssetNameAttribute attribute) {
            SerializedProperty hashProperty, stringProperty;
            if (fieldType == typeof(StringHash32)) {
                hashProperty = property.FindPropertyRelative("m_HashValue");
                stringProperty = null;
            } else if (fieldType == typeof(SerializedHash32)) {
                hashProperty = property.FindPropertyRelative("m_HashValue");
                stringProperty = property.FindPropertyRelative("m_Source");
            } else if (fieldType == typeof(string)) {
                hashProperty = null;
                stringProperty = property;
            } else {
                EditorGUI.LabelField(position, string.Format("Invalid type {0}", fieldType.Name));
                return;
            }

            long typeHandle = attribute.AssetType.TypeHandle.Value.ToInt64();
            SimpleCacheEntry cache = s_HashCache.Read(typeHandle);
            double lastTime = cache.LastUpdateTime;
            double currentTime = EditorApplication.timeSinceStartup;
            bool cacheUpdated = false;
            if (lastTime == 0 || (currentTime - lastTime) > RebuildCacheDelay) {
                cache.LastUpdateTime = currentTime;
                HashAssetNames(attribute.AssetType, ref cache.Items);
                s_HashCache.Write(typeHandle, cache);
                cacheUpdated = true;
            }

            string reverseLookup;
            if (stringProperty != null) {
                reverseLookup = stringProperty.stringValue;
            } else {
                reverseLookup = new StringHash32((uint) hashProperty.longValue).ToDebugString();
            }

            label = EditorGUI.BeginProperty(position, label, property);
            Rect line = position;
            EditorGUI.showMixedValue = hashProperty.hasMultipleDifferentValues;
            
            if (attribute.UseDropdown) {
                FilteredCacheEntry filteredCacheEntry = s_ListCache.Read(attribute.GetCacheKey());
                if (cacheUpdated) {
                    FillFilteredEntries(cache.Items, attribute, ref filteredCacheEntry.Items);
                    s_ListCache.Write(attribute.GetCacheKey(), filteredCacheEntry);
                }

                EditorGUI.BeginChangeCheck();
                string next = ListGUI.Popup(line, label, reverseLookup, filteredCacheEntry.Items);
                if (EditorGUI.EndChangeCheck() && next != reverseLookup) {
                    if (hashProperty != null) {
                        hashProperty.longValue = new StringHash32(next).HashValue;
                    }
                    if (stringProperty != null) {
                        stringProperty.stringValue = next ?? string.Empty;
                    }
                }
            } else {
                EditorGUI.BeginChangeCheck();
                cache.Items.TryGetValue(reverseLookup, out UnityEngine.Object obj);
                UnityEngine.Object next = EditorGUI.ObjectField(line, label, obj, attribute.AssetType, false);
                if (EditorGUI.EndChangeCheck() && obj != next) {
                    if (hashProperty != null) {
                        hashProperty.longValue = next ? new StringHash32(next.name).HashValue : 0;
                    }
                    if (stringProperty != null) {
                        stringProperty.stringValue = next ? next.name : string.Empty;
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
}