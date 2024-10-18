using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Components {

    /// <summary>
    /// Interface for a component that can be used with a component system.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface IComponentData { }

    /// <summary>
    /// Tuple of two component types.
    /// </summary>
    public struct ComponentTuple<TPrimary, TSecondary>
        where TPrimary : class, IComponentData
        where TSecondary : class, IComponentData {

        public TPrimary Primary;
        public TSecondary Secondary;

        public ComponentTuple(TPrimary primary, TSecondary additional) {
            Primary = primary;
            Secondary = additional;
        }
    }

    /// <summary>
    /// Tuple of three component types.
    /// </summary>
    public struct ComponentTuple<TPrimary, TComponentA, TComponentB>
        where TPrimary : class, IComponentData
        where TComponentA : class, IComponentData
        where TComponentB : class, IComponentData {

        public TPrimary Primary;
        public TComponentA ComponentA;
        public TComponentB ComponentB;

        public ComponentTuple(TPrimary primary, TComponentA additionalA, TComponentB additionalB) {
            Primary = primary;
            ComponentA = additionalA;
            ComponentB = additionalB;
        }
    }

    /// <summary>
    /// Tuple of three component types.
    /// </summary>
    public struct ComponentTuple<TPrimary, TComponentA, TComponentB, TComponentC>
        where TPrimary : class, IComponentData
        where TComponentA : class, IComponentData
        where TComponentB : class, IComponentData
        where TComponentC : class, IComponentData {

        public TPrimary Primary;
        public TComponentA ComponentA;
        public TComponentB ComponentB;
        public TComponentC ComponentC;

        public ComponentTuple(TPrimary primary, TComponentA additionalA, TComponentB additionalB, TComponentC additionalC) {
            Primary = primary;
            ComponentA = additionalA;
            ComponentB = additionalB;
            ComponentC = additionalC;
        }
    }

    /// <summary>
    /// Component utility.
    /// </summary>
    static public class ComponentUtility {
        static private readonly Type IComponentDataType = typeof(IComponentData);
        static private readonly Type UnityComponentType = typeof(UnityEngine.Component);

        /// <summary>
        /// Filter for getting a component tuple from a primary component.
        /// </summary>
        private delegate IComponentData ComponentSiblingPredicate(in IComponentData primary, Type secondary);

        static private readonly ComponentSiblingPredicate UnityComponentPredicate = (in IComponentData primary, Type secondary) => {
            return (IComponentData) ((Component) primary).GetComponent(secondary);
        };

        static private class PrimaryLookup<TPrimary> where TPrimary : class, IComponentData {
            /// <summary>
            /// Predicate that retrieves a sibling component.
            /// </summary>
            static private readonly ComponentSiblingPredicate SiblingFilter = GetSiblingPredicate(typeof(TPrimary));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public TComponent GetSibling<TComponent>(TPrimary primary) where TComponent : class, IComponentData {
                return (TComponent) SiblingFilter(primary, typeof(TComponent));
            }
        }

        /// <summary>
        /// Retrieves a predicate that can get sibling behaviours for instances of the given primary component type.
        /// </summary>
        static private ComponentSiblingPredicate GetSiblingPredicate(Type primaryType) {
            Assert.True(IComponentDataType.IsAssignableFrom(primaryType), "Component type '{0}' is not an IComponentData type", primaryType.FullName);
            if (UnityComponentType.IsAssignableFrom(primaryType)) {
                return UnityComponentPredicate;
            }

            Assert.Fail("Component type '{0}' does not have any corresponding default sibling predicate", primaryType.FullName);
            return null;
        }

        /// <summary>
        /// Retrieves the subling of the given type from the given primary component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public TComponent Sibling<TPrimary, TComponent>(this TPrimary primary)
            where TPrimary : class, IComponentData
            where TComponent : class, IComponentData {
            return PrimaryLookup<TPrimary>.GetSibling<TComponent>(primary);
        }

        /// <summary>
        /// Retrieves the subling of the given type from the given primary component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Sibling<TPrimary, TComponent>(TPrimary primary, out TComponent component)
            where TPrimary : class, IComponentData
            where TComponent : class, IComponentData {
            component = PrimaryLookup<TPrimary>.GetSibling<TComponent>(primary);
            return component != null;
        }

        /// <summary>
        /// Retrieves the sublings of the given type from the given primary component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Siblings<TPrimary, TComponentA, TComponentB>(TPrimary primary, out TComponentA componentA, out TComponentB componentB)
            where TPrimary : class, IComponentData
            where TComponentA : class, IComponentData
            where TComponentB : class, IComponentData {
            componentA = PrimaryLookup<TPrimary>.GetSibling<TComponentA>(primary);
            componentB = PrimaryLookup<TPrimary>.GetSibling<TComponentB>(primary);
            return componentA != null && componentB != null;
        }

        /// <summary>
        /// Retrieves the sublings of the given type from the given primary component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Siblings<TPrimary, TComponentA, TComponentB, TComponentC>(TPrimary primary, out TComponentA componentA, out TComponentB componentB, out TComponentC componentC)
            where TPrimary : class, IComponentData
            where TComponentA : class, IComponentData
            where TComponentB : class, IComponentData
            where TComponentC : class, IComponentData {
            componentA = PrimaryLookup<TPrimary>.GetSibling<TComponentA>(primary);
            componentB = PrimaryLookup<TPrimary>.GetSibling<TComponentB>(primary);
            componentC = PrimaryLookup<TPrimary>.GetSibling<TComponentC>(primary);
            return componentA != null && componentB != null && componentC != null;
        }
    }
}