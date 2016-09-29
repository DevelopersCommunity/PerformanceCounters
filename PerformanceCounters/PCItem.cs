using System;

namespace PerformanceCounters
{
    public struct PCItem
    {
        public PCItem(DateTime timeStamp, string counterPath, double value)
        {
            TimeStamp = timeStamp;
            CounterPath = counterPath;
            Value = value;
        }

        public DateTime TimeStamp { get; }
        public string CounterPath { get; }
        public double Value { get; }
    }
}
