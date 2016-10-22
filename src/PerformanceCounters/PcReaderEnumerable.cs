using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DevelopersCommunity.PerformanceCounters
{
    public class PcReaderEnumerable : IEnumerable<PCItem>
    {
        private string fileName;
        private string[] counters;
        private DateTime? start;
        private DateTime? end;

        public PcReaderEnumerable(string fileName, IEnumerable<string> counters, bool expandCounter) : this(fileName, counters, expandCounter, null, null)
        {

        }

        public PcReaderEnumerable(string fileName, IEnumerable<string> counters, bool expandCounter, DateTime? start, DateTime? end)
        {
            if (end <= start)
            {
                throw new ArgumentException("End time <= start time", nameof(end));
            }
            this.fileName = fileName;
            this.start = start;
            this.end = end;

            if (expandCounter)
            {
                List<string> expandedCounters = new List<string>();
                foreach (string wildCard in counters)
                {
                    expandedCounters.AddRange(NativeUtil.ExpandWildCard(fileName, wildCard));
                }
                this.counters = expandedCounters.ToArray();
            }
            else
            {
                this.counters = counters.ToArray();
            }
        }

        public IEnumerator<PCItem> GetEnumerator()
        {
            PdhQueryHandle queryHandle = null;
            try
            {
                NativeUtil.CheckPdhStatus(NativeMethods.PdhOpenQuery(fileName, IntPtr.Zero, out queryHandle));

                if (start.HasValue || end.HasValue)
                {
                    var timeInfo = new NativeMethods.PDH_TIME_INFO();
                    timeInfo.StartTime = start.HasValue ? NativeUtil.FileTimeFromDateTime(start.Value) : 0;
                    timeInfo.EndTime = end.HasValue ? NativeUtil.FileTimeFromDateTime(end.Value) : long.MaxValue;
                    NativeUtil.CheckPdhStatus(NativeMethods.PdhSetQueryTimeRange(queryHandle, ref timeInfo));
                }

                Dictionary<string, IntPtr> counterHandles = new Dictionary<string, IntPtr>();

                foreach (string counter in counters)
                {
                    IntPtr counterHandle;
                    NativeUtil.CheckPdhStatus(NativeMethods.PdhAddCounter(queryHandle, counter, IntPtr.Zero, out counterHandle));
                    counterHandles.Add(counter, counterHandle);
                }

                var status = NativeMethods.PdhCollectQueryData(queryHandle);
                if (status != NativeMethods.PDH_NO_MORE_DATA && status != NativeMethods.PDH_NO_DATA)
                {
                    NativeUtil.CheckPdhStatus(status);
                }

                DateTime currentTimeStamp;

                while (true)
                {
                    long date;
                    status = NativeMethods.PdhCollectQueryDataWithTime(queryHandle, out date);
                    if (status == NativeMethods.PDH_NO_MORE_DATA || status == NativeMethods.PDH_NO_DATA)
                    {
                        yield break;
                    }
                    NativeUtil.CheckPdhStatus(status);
                    currentTimeStamp = NativeUtil.DateTimeFromFileTime(date);

                    // Workaround: if supplied date range is ahead of date range available in blg, PDH return the whole list
                    if (start > currentTimeStamp)
                    {
                        yield break;
                    }

                    foreach (var pair in counterHandles)
                    {
                        NativeMethods.PDH_FMT_COUNTERVALUE value;
                        double formattedValue;
                        status = NativeMethods.PdhGetFormattedCounterValue(pair.Value, NativeMethods.PDH_FMT_DOUBLE, IntPtr.Zero, out value);

                        if (status == NativeMethods.PDH_CALC_NEGATIVE_DENOMINATOR ||
                            status == NativeMethods.PDH_CALC_NEGATIVE_VALUE ||
                            status == NativeMethods.PDH_CALC_NEGATIVE_TIMEBASE ||
                            status == NativeMethods.PDH_INVALID_DATA)
                        {
                            formattedValue = double.NaN;
                        }
                        else
                        {
                            NativeUtil.CheckPdhStatus(status);
                            formattedValue = value.doubleValue;
                        }

                        yield return new PCItem(currentTimeStamp, pair.Key, formattedValue);
                    }
                }

            }
            finally
            {
                queryHandle?.Dispose();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}