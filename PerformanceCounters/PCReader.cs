using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DevelopersCommunity.PerformanceCounters
{
    public class PCReader : IEnumerable<IReadOnlyList<PCItem>>
    {
        private string fileName;
        private string[] counters;
        private DateTime? start;
        private DateTime? end;

        public PCReader(string fileName, string[] counters) : this(fileName, counters, null, null)
        {

        }

        public PCReader(string fileName, string[] counters, DateTime? start, DateTime? end)
        {
            if (end <= start)
            {
                throw new ArgumentException("End time <= start time", nameof(end));
            }
            this.fileName = fileName;
            this.start = start;
            this.end = end;

            List<string> expandedCounters = new List<string>();
            foreach (string wildCard in counters)
            {
                expandedCounters.AddRange(ExpandWildCard(wildCard));
            }
            this.counters = expandedCounters.ToArray();
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

        private static DateTime GetEndTime(string name)
        {
            uint numEntries;
            NativeMethods.PDH_TIME_INFO timeInfo;
            uint size = (uint)Marshal.SizeOf<NativeMethods.PDH_TIME_INFO>();

            NativeUtil.CheckPdhStatus(NativeMethods.PdhGetDataSourceTimeRange(name, out numEntries, out timeInfo, ref size));
            return NativeUtil.DateTimeFromFileTime(timeInfo.EndTime);
        }

        private IReadOnlyList<string> ExpandWildCard(string wildCard)
        {
            uint len = 0;
            var status = NativeMethods.PdhExpandWildCardPath(fileName, wildCard, null, ref len, 0);
            if (status != NativeMethods.PDH_MORE_DATA)
            {
                NativeUtil.CheckPdhStatus(status);
            }
            var buffer = new char[len];
            NativeUtil.CheckPdhStatus(NativeMethods.PdhExpandWildCardPath(fileName, wildCard, buffer, ref len, 0));
            return NativeUtil.MultipleStringsToList(buffer);
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
