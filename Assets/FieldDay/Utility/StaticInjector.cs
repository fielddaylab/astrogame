using System;
using BeauUtil;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil.Debugger;

namespace FieldDay {
    /// <summary>
    /// Static injection cache. Helps inject references to static fields and properties.
    /// </summary>
    public sealed class StaticInjector<TAttr, TBaseType> where TAttr : Attribute where TBaseType : class {
        private FieldInfo[] m_InjectedFields;
        private PropertyInfo[] m_InjectedProperties;

        private void ScanForStaticInjection() {
            if (m_InjectedFields != null && m_InjectedProperties != null) {
                return;
            }

            List<FieldInfo> fields = new List<FieldInfo>(64);
            List<PropertyInfo> properties = new List<PropertyInfo>(64);

            Type refType = typeof(TBaseType);

            foreach (var info in Reflect.FindMembers<TAttr>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (info.Info.DeclaringType.ContainsGenericParameters) {
                    continue;
                }

                if (info.Info is FieldInfo) {
                    FieldInfo field = (FieldInfo) info.Info;
                    Assert.True(refType.IsAssignableFrom(field.FieldType), "[StaticInjector] Field '{0} {1}::{2}' is not a '{3}'", field.FieldType.Name, field.DeclaringType.FullName, field.Name, refType.FullName);
                    fields.Add(field);
                } else if (info.Info is PropertyInfo) {
                    PropertyInfo prop = (PropertyInfo) info.Info;
                    Assert.True(refType.IsAssignableFrom(prop.PropertyType), "[StaticInjector] Property '{0} {1}::{2}' is not a '{3}'", prop.PropertyType.Name, prop.DeclaringType.FullName, prop.Name, refType.FullName);
                    Assert.True(prop.CanWrite, "[StaticInjector] Property '{0} {1}::{2}' is not writable", prop.PropertyType.Name, prop.DeclaringType.FullName, prop.Name);
                    properties.Add((PropertyInfo) info.Info);
                }
            }

            m_InjectedFields = fields.ToArray();
            m_InjectedProperties = properties.ToArray();
        }

        /// <summary>
        /// Injects a reference to the given object to all static references that are assignable to the object's type.
        /// </summary>
        public void Inject(TBaseType reference) {
            ScanForStaticInjection();

            Type sysType = reference.GetType();

            for (int i = 0; i < m_InjectedFields.Length; i++) {
                FieldInfo field = m_InjectedFields[i];
                if (field.GetValue(null) == null && field.FieldType.IsAssignableFrom(sysType)) {
                    field.SetValue(null, reference);
                }
            }

            for (int i = 0; i < m_InjectedProperties.Length; i++) {
                PropertyInfo prop = m_InjectedProperties[i];
                if (prop.GetValue(null) == null && prop.PropertyType.IsAssignableFrom(sysType)) {
                    prop.SetValue(null, reference);
                }
            }
        }

        /// <summary>
        /// Removes a reference to the given object to all static references that are assignable to the object's type.
        /// </summary>
        public void Remove(TBaseType reference) {
            if (m_InjectedFields == null || m_InjectedProperties == null) {
                return;
            }

            for (int i = 0; i < m_InjectedFields.Length; i++) {
                FieldInfo field = m_InjectedFields[i];
                if (field.GetValue(null) == reference) {
                    field.SetValue(null, null);
                }
            }

            for (int i = 0; i < m_InjectedProperties.Length; i++) {
                PropertyInfo prop = m_InjectedProperties[i];
                if (prop.GetValue(null) == reference) {
                    prop.SetValue(null, null);
                }
            }
        }
    }
}