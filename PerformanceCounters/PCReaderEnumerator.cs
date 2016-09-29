using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DevelopersCommunity.PerformanceCounters
{
    class PCReaderEnumerator : IEnumerator<IReadOnlyList<PCItem>>, IEnumerator
    {
        PdhQueryHandle queryHandle;
        Dictionary<string, IntPtr> counterHandles = new Dictionary<string, IntPtr>();
        DateTime currentTimeStamp;
        string fileName;
        DateTime? start;
        DateTime? end;
        List<string> expandedCounters = new List<string>();

        private PCReaderEnumerator()
        { }

        public PCReaderEnumerator(string fileName, string[] counters, DateTime? start, DateTime? end)
        {
            this.fileName = fileName;
            this.start = start;
            this.end = end;
            foreach (string wildCard in counters)
            {
                expandedCounters.AddRange(ExpandWildCard(wildCard));
            }

            Open();
        }

        private void Open()
        {
            CheckPdhStatus(NativeMethods.PdhOpenQuery(fileName, IntPtr.Zero, out queryHandle));

            if (start.HasValue || end.HasValue)
            {
                var timeInfo = new NativeMethods.PDH_TIME_INFO();
                timeInfo.StartTime = start.HasValue ? FileTimeFromDateTime(start.Value) : 0;
                timeInfo.EndTime = end.HasValue ? FileTimeFromDateTime(end.Value) : long.MaxValue;
                CheckPdhStatus(NativeMethods.PdhSetQueryTimeRange(queryHandle, ref timeInfo));
            }

            foreach (string counter in expandedCounters)
            {
                IntPtr counterHandle;
                CheckPdhStatus(NativeMethods.PdhAddCounter(queryHandle, counter, IntPtr.Zero, out counterHandle));
                counterHandles.Add(counter, counterHandle);
            }

            var status = NativeMethods.PdhCollectQueryData(queryHandle);//Removes the first sample. It is always PDH_INVALID_DATA
            if (status != NativeMethods.PDH_NO_MORE_DATA)
            {
                CheckPdhStatus(status);
            }
        }

        public IReadOnlyList<PCItem> Current
        {
            get
            {
                var items = new List<PCItem>();

                foreach (var pair in counterHandles)
                {
                    NativeMethods.PDH_FMT_COUNTERVALUE value;
                    double formattedValue;
                    var status = 
                        NativeMethods.PdhGetFormattedCounterValue(pair.Value, NativeMethods.PDH_FMT_DOUBLE, IntPtr.Zero, out value);

                    if (status == NativeMethods.PDH_CALC_NEGATIVE_DENOMINATOR ||
                        status == NativeMethods.PDH_CALC_NEGATIVE_VALUE ||
                        status == NativeMethods.PDH_CALC_NEGATIVE_TIMEBASE ||
                        status == NativeMethods.PDH_INVALID_DATA)
                    {
                        formattedValue = double.NaN;
                    }
                    else
                    {
                        CheckPdhStatus(status);
                        formattedValue = value.doubleValue;
                    }

                    items.Add(new PCItem(currentTimeStamp, pair.Key, formattedValue)); 
                }

                return items;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public bool MoveNext()
        {
            long date;
            var status = NativeMethods.PdhCollectQueryDataWithTime(queryHandle, out date);
            if (status == NativeMethods.PDH_NO_MORE_DATA)
                return false;
            CheckPdhStatus(status);
            currentTimeStamp = DateTimeFromFileTime(date);
            return true;
        }

        public void Reset()
        {
            queryHandle.Dispose();
            counterHandles.Clear();
            Open();
        }

        private IReadOnlyList<string> ExpandWildCard(string wildCard)
        {
            uint len = 0;
            var status = NativeMethods.PdhExpandWildCardPath(fileName, wildCard, null, ref len, 0);
            if (status != NativeMethods.PDH_MORE_DATA)
            {
                CheckPdhStatus(status);
            }
            var buffer = new char[len];
            CheckPdhStatus(NativeMethods.PdhExpandWildCardPath(fileName, wildCard, buffer, ref len, 0));
            return PCReader.MultipleStringsToList(buffer);
        }

        internal static long FileTimeFromDateTime(DateTime date)
        {
            var st = new NativeMethods.SYSTEMTIME
            {
                wYear = (ushort)date.Year,
                wMonth = (ushort)date.Month,
                wDay = (ushort)date.Day,
                wHour = (ushort)date.Hour,
                wMinute = (ushort)date.Minute,
                wSecond = (ushort)date.Second,
                wMilliseconds = (ushort)date.Millisecond,
                wDayOfWeek = (ushort)date.DayOfWeek
            };

            long ft;
            if (!NativeMethods.SystemTimeToFileTime(ref st, out ft)) throw new Win32Exception();

            return ft;
        }

        internal static DateTime DateTimeFromFileTime(long date)
        {
            NativeMethods.SYSTEMTIME st;
            if (!NativeMethods.FileTimeToSystemTime(ref date, out st)) throw new Win32Exception();

            return new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, DateTimeKind.Local);
        }

        private void CheckPdhStatus(uint status)
        {
            if (status != NativeMethods.ERROR_SUCCESS)
            {
                throw new PCException(status);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    queryHandle.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PCReaderEnumerator() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
