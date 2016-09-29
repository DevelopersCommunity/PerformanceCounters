using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            this.counters = counters;
            this.start = start;
            this.end = end;
        }

        public IReadOnlyList<string> GetComputers()
        {
            uint len = 0;
            uint status = NativeMethods.PdhEnumMachines(fileName, null, ref len);
            char[] computers = null;
            if (status == NativeMethods.PDH_MORE_DATA)
            {
                computers = new char[len];
                status = NativeMethods.PdhEnumMachines(fileName, computers, ref len);
            }
            if (status != NativeMethods.ERROR_SUCCESS)
            {
                throw new PCException(status);
            }

            return MultipleStringsToList(computers);
        }

        internal static IReadOnlyList<string> MultipleStringsToList(char[] multipleStrings)
        {
            var list = new List<string>();
            var item = new StringBuilder();

            for (int i = 0; i < multipleStrings.Length; i++)
            {
                if (multipleStrings[i] != '\0')
                {
                    item.Append(multipleStrings[i]);
                }
                else
                {
                    list.Add(item.ToString());
                    item.Length = 0;
                    if (multipleStrings[i + 1] == '\0')
                    {
                        break;
                    }
                }
            }

            return list;
        }

        public IEnumerator<IReadOnlyList<PCItem>> GetEnumerator()
        {
            return new PCReaderEnumerator(fileName, counters, start, end);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PCReaderEnumerator(fileName, counters, start, end);
        }
    }
}
