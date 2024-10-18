using System;
using BeauUtil;

namespace FieldDay.Memory {
    public interface IMemUsageReporter {
        void ReportMemUsage(ref MemUsageReport report);
    }

    public struct MemUsageReport {
        public RingBuffer<MemUsageReport> Entries;
    }

    public struct MemUsageReportEntry {
        public string Category;
        public string Name;
        public Type Type;
        public long Bytes;
    }
}