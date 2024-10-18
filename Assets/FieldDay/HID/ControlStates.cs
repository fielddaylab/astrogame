using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using TinyIL;

namespace FieldDay.HID {
    /// <summary>
    /// State of a set of digital controls (buttons).
    /// </summary>
    public struct DigitalControlStates {
        public uint Current;
        public uint Prev;

        public uint Pressed;
        public uint Released;

        #region Modifiers

        public bool Update(uint current) {
            Prev = Current;
            Current = current;

            uint changes = Current ^ Prev;
            Pressed = changes & Current;
            Released = changes & (~Current);
            return changes != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Current = Prev = Pressed = Released = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChanges() {
            Pressed = Released = 0;
        }

        /// <summary>
        /// Consumes a press.
        /// Returns if it was pressed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumePress(uint mask) {
            bool had = (Pressed & mask) != 0;
            Pressed &= ~mask;
            return had;
        }

        /// <summary>
        /// Consumes a release.
        /// Returns if it was released.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumeRelease(uint mask) {
            bool had = (Released & mask) != 0;
            Released &= ~mask;
            return had;
        }

        #endregion // Modifiers

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDown(uint mask) {
            return (Current & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDownAll(uint mask) {
            return (Current & mask) == mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly  bool IsPressed(uint mask) {
            return (Pressed & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsReleased(uint mask) {
            return (Released & mask) != 0;
        }

        #endregion // Accessors

        #region Operators

        static public DigitalControlStates operator |(DigitalControlStates a, DigitalControlStates b) {
            DigitalControlStates s;
            s.Current = a.Current | b.Current;
            s.Prev = a.Prev | b.Prev;
            s.Pressed = a.Pressed | b.Pressed;
            s.Released = a.Released | b.Released;
            return s;
        }

        static public DigitalControlStates operator &(DigitalControlStates a, DigitalControlStates b) {
            DigitalControlStates s;
            s.Current = a.Current & b.Current;
            s.Prev = a.Prev & b.Prev;
            s.Pressed = a.Pressed & b.Pressed;
            s.Released = a.Released & b.Released;
            return s;
        }

        static public DigitalControlStates operator ^(DigitalControlStates a, DigitalControlStates b) {
            DigitalControlStates s;
            s.Current = a.Current ^ b.Current;
            s.Prev = a.Prev ^ b.Prev;
            s.Pressed = a.Pressed ^ b.Pressed;
            s.Released = a.Released ^ b.Released;
            return s;
        }

        #endregion // Operators
    }

    /// <summary>
    /// State of a set of digital controls (buttons), represented by an Enum.
    /// </summary>
    public struct DigitalControlStates<TEnum> where TEnum : unmanaged, Enum {
        public TEnum Current;
        public TEnum Prev;

        public TEnum Pressed;
        public TEnum Released;

        #region Modifiers

        public bool Update(TEnum current) {
            Prev = Current;
            Current = current;

            TEnum changes = Xor(Current, Prev);;
            Pressed = And(changes, Current);
            Released = And(changes, Not(Current));
            return NotZero(changes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Current = Prev = Pressed = Released = default(TEnum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChanges() {
            Pressed = Released = default(TEnum);
        }

        /// <summary>
        /// Consumes a press.
        /// Returns if it was pressed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumePress(TEnum mask) {
            bool had = NotZero(And(Pressed, mask));
            Pressed = And(Pressed, Not(mask));
            return had;
        }

        /// <summary>
        /// Consumes a release.
        /// Returns if it was released.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumeRelease(TEnum mask) {
            bool had = NotZero(And(Released, mask));
            Released = And(Released, Not(mask));
            return had;
        }

        #endregion // Modifiers

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDown(TEnum mask) {
            return NotZero(And(Current, mask));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDownAll(TEnum mask) {
            return AreEqual(And(Current, mask), mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsPressed(TEnum mask) {
            return NotZero(And(Pressed, mask));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsReleased(TEnum mask) {
            return NotZero(And(Released, mask));
        }

        #endregion // Accessors

        #region Operators

        static public DigitalControlStates<TEnum> operator |(DigitalControlStates<TEnum> a, DigitalControlStates<TEnum> b) {
            DigitalControlStates<TEnum> s;
            s.Current = Or(a.Current, b.Current);
            s.Prev = Or(a.Prev, b.Prev);
            s.Pressed = Or(a.Pressed, b.Pressed);
            s.Released = Or(a.Released, b.Released);
            return s;
        }

        static public DigitalControlStates<TEnum> operator &(DigitalControlStates<TEnum> a, DigitalControlStates<TEnum> b) {
            DigitalControlStates<TEnum> s;
            s.Current = And(a.Current, b.Current);
            s.Prev = And(a.Prev, b.Prev);
            s.Pressed = And(a.Pressed, b.Pressed);
            s.Released = And(a.Released, b.Released);
            return s;
        }

        static public DigitalControlStates<TEnum> operator ^(DigitalControlStates<TEnum> a, DigitalControlStates<TEnum> b) {
            DigitalControlStates<TEnum> s;
            s.Current = Xor(a.Current, b.Current);
            s.Prev = Xor(a.Prev, b.Prev);
            s.Pressed = Xor(a.Pressed, b.Pressed);
            s.Released = Xor(a.Released, b.Released);
            return s;
        }

        #endregion // Operators

        #region Helper Functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private TEnum And(TEnum a, TEnum b) {
            return Enums.And(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private TEnum Or(TEnum a, TEnum b) {
            return Enums.Or(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private TEnum Xor(TEnum a, TEnum b) {
            return Enums.Xor(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private TEnum Not(TEnum a) {
            return Enums.Not(a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool NotZero(TEnum a) {
            return !Enums.AreEqual(a, default(TEnum));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool AreEqual(TEnum a, TEnum b) {
            return Enums.AreEqual(a, b);
        }

        #endregion // Helper Functions
    }

    /// <summary>
    /// State of an axis.
    /// </summary>
    public struct AxisControlState8 {
        public float Raw;
        public float Adjusted;
        public unsafe fixed float AdjustedHistory[8];
    }
}