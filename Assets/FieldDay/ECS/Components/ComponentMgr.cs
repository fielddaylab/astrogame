using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Systems;

using ComponentIndex = BeauUtil.TypeIndex<FieldDay.Components.IComponentData>;

namespace FieldDay.Components
{
    /// <summary>
    /// Component manager.
    /// </summary>
    public sealed class ComponentMgr
    {
        private SystemsMgr m_SystemsMgr;
        private List<IComponentData>[] m_ComponentLists;
        private RingBuffer<IComponentData> m_AddQueue = new RingBuffer<IComponentData>(64, RingBufferMode.Expand);
        private RingBuffer<IComponentData> m_RemovalQueue = new RingBuffer<IComponentData>(64, RingBufferMode.Expand);
        private int m_ModificationLock;

        internal ComponentMgr(SystemsMgr systemsMgr)
        {
            Assert.NotNull(systemsMgr);
            m_SystemsMgr = systemsMgr;
            m_ComponentLists = new List<IComponentData>[ComponentIndex.Capacity];
            m_SystemsMgr.OnSystemRegistered += OnNewSystemRegistered;
        }

        #region Registry

        /// <summary>
        /// Adds the given component to the component registry
        /// and all relevant system instances.
        /// </summary>
        public void Register(IComponentData component)
        {
            if (m_ModificationLock > 0)
            {
                if (!m_RemovalQueue.FastRemove(component))
                {
                    if (!m_AddQueue.Contains(component))
                    {
                        m_AddQueue.PushBack(component);
                    }
                }
                return;
            }

            RegisterImpl(component);
        }

        /// <summary>
        /// Removes the given component from the component registry
        /// and all relevant system instances.
        /// </summary>
        public void Deregister(IComponentData component)
        {
            if (m_ModificationLock > 0)
            {
                if (!m_AddQueue.FastRemove(component))
                {
                    if (!m_RemovalQueue.Contains(component))
                    {
                        m_RemovalQueue.PushBack(component);
                    }
                }
                return;
            }

            DeregisterImpl(component);
        }

        private void RegisterImpl(IComponentData component)
        {
            Type componentType = component.GetType();
            var indices = ComponentIndex.GetAll(componentType);
            foreach (var index in indices) {

                List<IComponentData> components = m_ComponentLists[index];
                if (components == null) {
                    m_ComponentLists[index] = components = new List<IComponentData>(32);
                }
                Assert.False(components.Contains(component), "Component of type '{0}' was registered more than once", componentType.Name);
                components.Add(component);
            }

            RegistrationCallbacks.InvokeRegister(component);
            m_SystemsMgr.AddComponent(component);
        }

        private void DeregisterImpl(IComponentData component)
        {
            Type componentType = component.GetType();
            var indices = ComponentIndex.GetAll(componentType);
            bool deregistered = false;
            foreach (var index in indices) {
                List<IComponentData> components = m_ComponentLists[index];
                if (components != null && components.FastRemove(component)) {
                    deregistered = true;
                }
            }

            if (deregistered) {
                m_SystemsMgr.RemoveComponent(component);
                RegistrationCallbacks.InvokeDeregister(component);
            }
        }

        #endregion // Registry

        #region Lock/Unlock

        /// <summary>
        /// Locks the component manager from further modifications.
        /// This ensures component lists remain consistent.
        /// </summary>
        public void Lock()
        {
            m_ModificationLock++;
        }

        /// <summary>
        /// Unlocks the component manager, allowing further modifications
        /// and processing all queued modifications.
        /// </summary>
        public void Unlock()
        {
            Assert.True(m_ModificationLock > 0, "Unbalanced Lock/Unlock calls");
            if (m_ModificationLock-- == 1)
            {
                while (m_RemovalQueue.TryPopBack(out IComponentData component))
                {
                    DeregisterImpl(component);
                }
                while (m_AddQueue.TryPopBack(out IComponentData component))
                {
                    RegisterImpl(component);
                }
            }
        }

        /// <summary>
        /// Locks and returns a lock object
        /// that will unlock once disposed.
        /// </summary>
        public DisposableLock GetLock()
        {
            return new DisposableLock();
        }

        /// <summary>
        /// Disposable lock object.
        /// </summary>
        public struct DisposableLock : IDisposable
        {
            private ComponentMgr m_Mgr;

            internal DisposableLock(ComponentMgr mgr)
            {
                m_Mgr = mgr;
                mgr.Lock();
            }

            public void Dispose()
            {
                if (m_Mgr != null)
                {
                    m_Mgr.Unlock();
                    m_Mgr = null;
                }
            }
        }

        #endregion // Lock/Unlock

        #region Iteration

        /// <summary>
        /// Enumerates all the components of the given type.
        /// </summary>
        public ComponentIterator<IComponentData> ComponentsOfType(Type componentType)
        {
            int index = ComponentIndex.Get(componentType);
            List<IComponentData> components = m_ComponentLists[index];
            if (components != null)
            {
                return new ComponentIterator<IComponentData>(components);
            }
            return default;
        }

        /// <summary>
        /// Enumerates all the components of the given type.
        /// </summary>
        public int ComponentsOfType(Type componentType, ICollection<IComponentData> componentOutput)
        {
            int index = ComponentIndex.Get(componentType);
            List<IComponentData> components = m_ComponentLists[index];
            if (components != null) {
                foreach(var c in components) {
                    componentOutput.Add(c);
                }
                return components.Count;
            }
            return 0;
        }

        /// <summary>
        /// Enumerates all the components of the given type.
        /// </summary>
        public ComponentIterator<T> ComponentsOfType<T>() where T : class, IComponentData
        {
            int index = ComponentIndex.Get<T>();
            List<IComponentData> components = m_ComponentLists[index];
            if (components != null)
            {
                return new ComponentIterator<T>(components);
            }
            return default;
        }

        /// <summary>
        /// Enumerates all the components of the given type.
        /// </summary>
        public int ComponentsOfType<T>(ICollection<T> componentOutput) where T : class, IComponentData
        {
            int index = ComponentIndex.Get<T>();
            List<IComponentData> components = m_ComponentLists[index];
            if (components != null) {
                foreach(var c in components) {
                    componentOutput.Add((T) c);
                }
                return components.Count;
            }
            return 0;
        }

        /// <summary>
        /// Returns the first component of the given type.
        /// </summary>
        public IComponentData FirstComponentOfType(Type componentType)
        {
            int index = ComponentIndex.Get(componentType);
            List<IComponentData> components = m_ComponentLists[index];
            if (components != null && components.Count > 0)
            {
                return components[0];
            }
            return default;
        }

        /// <summary>
        /// Returns the first component of the given type.
        /// </summary>
        public T FirstComponentOfType<T>() where T : class, IComponentData
        {
            int index = ComponentIndex.Get<T>();
            List<IComponentData> components = m_ComponentLists[index];
            if (components != null && components.Count > 0)
            {
                return (T) components[0];
            }
            return default;
        }

        #endregion // Iteration

        #region Events

        /// <summary>
        /// Ensures that new component systems get populated
        /// with the components already present in the ComponentMgr.
        /// </summary>
        private void OnNewSystemRegistered(ISystem system)
        {
            IComponentSystem componentSystem = system as IComponentSystem;
            List<IComponentData> components;
            if (componentSystem != null && (components = m_ComponentLists[ComponentIndex.Get(componentSystem.ComponentType)]) != null)
            {
                foreach (var component in components)
                {
                    componentSystem.Add(component);
                }
            }
        }

        internal void Shutdown()
        {
            m_SystemsMgr.OnSystemRegistered -= OnNewSystemRegistered;
            m_SystemsMgr = null;
            foreach (var list in m_ComponentLists)
            {
                list?.Clear();
            }
            ArrayUtils.Dispose(ref m_ComponentLists);
        }

        #endregion // Events
    }

    /// <summary>
    /// Component iterator.
    /// </summary>
    public struct ComponentIterator<T> : IEnumerator<T>, IEnumerable<T>, IDisposable where T : class, IComponentData
    {
        private List<IComponentData>.Enumerator m_Source;
        private int m_Count;

        internal ComponentIterator(List<IComponentData> source)
        {
            if (source != null) {
                m_Source = source.GetEnumerator();
                m_Count = source.Count;
            } else {
                m_Source = default;
                m_Count = 0;
            }
        }

        public T Current
        {
            get { return (T)m_Source.Current; }
        }

        /// <summary>
        /// Total number of components in this list.
        /// </summary>
        public int Count {
            get { return m_Count; }
        }

        public void Dispose()
        {
            m_Source = default;
            m_Count = 0;
        }

        public bool MoveNext()
        {
            return m_Count == 0 ? false : m_Source.MoveNext();
        }

        public ComponentIterator<T> GetEnumerator() {
            return this;
        }

        #region Interface Implementations

        object IEnumerator.Current { get { return Current; } }

        void IEnumerator.Reset() {
            ((IEnumerator)m_Source).Reset();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return this;
        }

        #endregion // Interface Implementations
    }
}