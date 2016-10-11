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
        public unsafe static IEnumerable<string> BrowseCounters(string fileName)
        {
            NativeMethods.PDH_BROWSE_DLG_CONFIG config = new NativeMethods.PDH_BROWSE_DLG_CONFIG();

            config.Flags |= NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.WildCardInstances
                | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.IncludeInstanceIndex
                | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.HideDetailBox
                | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.DisableMachineSelection
                | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.IncludeCostlyObjects
                //| NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.SingleCounterPerAdd
                | NativeMethods.PDH_BROWSE_DLG_CONFIG_Flags.SingleCounterPerDialog
                ;
            config.DataSource = fileName;
            //config.DialogBoxCaption = "Teste 123";
            config.DefaultDetailLevel = 400;//TODO validar

            const int bufferSize = 10000;
            var temp = new byte[bufferSize];
            List<string> addedCounters = new List<string>();

            config.CallBack = x => {
                var status = config.CallBackStatus;
                addedCounters.AddRange(NativeUtil.MultipleStringsToList(Encoding.UTF8.GetString(temp)));
                return 0;
            };

            //TODO separar e implementar iterator
            fixed (byte* t = temp)
            {
                config.ReturnPathBuffer = t;
                config.ReturnPathLength = bufferSize;

                var status = NativeMethods.PdhBrowseCounters(ref config);

                if (status != NativeMethods.PDH_DIALOG_CANCELLED)
                {
                    NativeUtil.CheckPdhStatus(status);
                }              
            }

            return addedCounters;
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
