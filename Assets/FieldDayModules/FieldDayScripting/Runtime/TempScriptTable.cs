using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;

namespace FieldDay.Scripting {
    /// <summary>
    /// Temporary variable table.
    /// </summary>
    public struct TempVarTable : IDisposable {
        private TempAlloc<VariantTable> m_Alloc;

        internal TempVarTable(TempAlloc<VariantTable> tempTable) {
            m_Alloc = tempTable;
        }

        public void Set(StringHash32 key, Variant value) {
            Assert.True(m_Alloc.IsAllocated, "Stale TempVarTable - no longer allocated");
            m_Alloc.Object.Set(key, value);
        }

        public void Clear() {
            Assert.True(m_Alloc.IsAllocated, "Stale TempVarTable - no longer allocated");
            m_Alloc.Object?.Clear();
        }

        public void Dispose() {
            m_Alloc.Dispose();
        }

        static public implicit operator VariantTable(TempVarTable table) {
            Assert.True(table.m_Alloc.IsAllocated, "Stale TempVarTable - no longer allocated");
            return table.m_Alloc.Object;
        }

        static public TempVarTable Alloc() {
            var alloc = ScriptUtility.Runtime.TablePool.TempAlloc();
            return new TempVarTable(alloc);
        }
    }
}