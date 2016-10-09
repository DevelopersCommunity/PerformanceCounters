using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DevelopersCommunity.PerformanceCounters
{
    public class PCReader : IEnumerable<IReadOnlyList<PCItem>>
    {
        private string fileName;
        private string[] counters;
        private DateTime? start;
        private DateTime? end;

        public PCReader(string fileName, IEnumerable<string> counters, bool expandCounter) : this(fileName, counters, expandCounter, null, null)
        {

        }

        public PCReader(string fileName, IEnumerable<string> counters, bool expandCounter, DateTime? start, DateTime? end)
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

        public IReadOnlyList<string> GetMachines()
        {
            uint len = 0;
            uint status = NativeMethods.PdhEnumMachines(fileName, null, ref len);
            char[] computers = null;
            if (status == NativeMethods.PDH_MORE_DATA)
            {
                computers = new char[len];
                NativeUtil.CheckPdhStatus(NativeMethods.PdhEnumMachines(fileName, computers, ref len));
            }

            return NativeUtil.MultipleStringsToList(computers);
        }

        public static IEnumerable<string> ExpandWildCard(string fileName, string wildCard)
        {
            return NativeUtil.ExpandWildCard(fileName, wildCard);
        }

        private static DateTime GetEndTime(string name)
        {
            uint numEntries;
            NativeMethods.PDH_TIME_INFO timeInfo;
            uint size = (uint)Marshal.SizeOf<NativeMethods.PDH_TIME_INFO>();

            NativeUtil.CheckPdhStatus(NativeMethods.PdhGetDataSourceTimeRange(name, out numEntries, out timeInfo, ref size));
            return NativeUtil.DateTimeFromFileTime(timeInfo.EndTime);
        }

        public IEnumerator<IReadOnlyList<PCItem>> GetEnumerator()
        {
            return new PCReaderEnumerator(fileName, counters, start, end);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
