using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        //TODO se for possivel tirar o unsafe, tirar tb a flag do projeto
        public static IEnumerable<string> BrowseCounters(IntPtr hWndOwner, string title, string fileName)
        {
            NativeMethods.PDH_BROWSE_DLG_CONFIG config = new NativeMethods.PDH_BROWSE_DLG_CONFIG();
            uint pathSize = NativeMethods.PDH_MAX_COUNTER_PATH;
            char[] counters = new char[pathSize];

            GCHandle handle = GCHandle.Alloc(counters, GCHandleType.Pinned);

            try
            {
                config.Flags |= NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.bWildCardInstances
                    | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.bIncludeInstanceIndex
                    | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.bIncludeCostlyObjects
                    //| NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.bSingleCounterPerAdd
                    | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.bSingleCounterPerDialog
                    ;
                config.hWndOwner = hWndOwner;
                config.szDataSource = fileName;
                config.szDialogBoxCaption = title;
                config.dwDefaultDetailLevel = NativeMethods.PERF_DETAIL_WIZARD;//TODO validar
                config.szReturnPathBuffer = handle.AddrOfPinnedObject();
                config.cchReturnPathLength = pathSize;

                List<string> addedCounters = new List<string>();

                config.pCallBack = x =>
                {
                    var status = config.CallBackStatus;

                    if (status == NativeMethods.PDH_MORE_DATA)
                    {
                        config.CallBackStatus = NativeMethods.PDH_RETRY;
                        handle.Free();
                        pathSize += NativeMethods.PDH_MAX_COUNTER_PATH;
                        counters = new char[pathSize];
                        handle = GCHandle.Alloc(counters, GCHandleType.Pinned);
                        config.szReturnPathBuffer = handle.AddrOfPinnedObject();
                        config.cchReturnPathLength = pathSize;
                        return NativeMethods.ERROR_SUCCESS;
                    }
                    NativeUtil.CheckPdhStatus(status);

                    addedCounters.AddRange(NativeUtil.MultipleStringsToList(counters));

                    return NativeMethods.ERROR_SUCCESS;
                };

                var result = NativeMethods.PdhBrowseCounters(ref config);
                if (result == NativeMethods.PDH_DIALOG_CANCELLED)
                {
                    addedCounters.Clear();
                }

                NativeUtil.CheckPdhStatus(result);

                return addedCounters;
            }
            finally
            {
                handle.Free();
            }
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
