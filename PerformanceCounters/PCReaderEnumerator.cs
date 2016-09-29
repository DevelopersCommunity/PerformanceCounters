using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace PerformanceCounters
{
    class PCReaderEnumerator : IEnumerator<ReadOnlyCollection<PCItem>>, IEnumerator
    {
        PdhQueryHandle queryHandle;
        Dictionary<string, IntPtr> counterHandles = new Dictionary<string, IntPtr>();
        DateTime currentTimeStamp;
        string fileName;

        public PCReaderEnumerator(string fileName, string[] counters, DateTime start, DateTime end)
        {
            this.fileName = fileName;
            CheckPdhStatus(NativeMethods.PdhOpenQuery(fileName, IntPtr.Zero, out queryHandle));

            var timeInfo = new NativeMethods.PDH_TIME_INFO();
            timeInfo.StartTime = start == DateTime.MinValue ? 0 : FileTimeFromDateTime(start);
            timeInfo.EndTime = end == DateTime.MaxValue ? long.MaxValue : FileTimeFromDateTime(end);
            if (start != DateTime.MinValue || end != DateTime.MaxValue)
                CheckPdhStatus(NativeMethods.PdhSetQueryTimeRange(queryHandle, ref timeInfo));

            foreach (string wildCard in counters)
            {
                foreach (string counter in ExpandWildCard(wildCard))
                {
                    IntPtr counterHandle;
                    CheckPdhStatus(NativeMethods.PdhAddCounter(queryHandle, counter, IntPtr.Zero, out counterHandle));
                    counterHandles.Add(counter, counterHandle);
                }
            }

            CheckPdhStatus(NativeMethods.PdhCollectQueryData(queryHandle));
        }

        public ReadOnlyCollection<PCItem> Current
        {
            get
            {
                List<PCItem> items = new List<PCItem>();

                foreach (var pair in counterHandles)
                {
                    NativeMethods.PDH_FMT_COUNTERVALUE value;
                    double formattedValue;
                    uint status = 
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

                return new ReadOnlyCollection<PCItem>(items);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        string[] ExpandWildCard(string wildCard)
        {
            uint len = 0;
            uint status = NativeMethods.PdhExpandWildCardPath(fileName, wildCard, null, ref len, 0);
            if (status != NativeMethods.PDH_MORE_DATA)
            {
                CheckPdhStatus(status);
            }
            char[] buffer = new char[len];
            CheckPdhStatus(NativeMethods.PdhExpandWildCardPath(fileName, wildCard, buffer, ref len, 0));
            return PCReader.MultipleStringsToList(buffer).ToArray();
        }

        private long FileTimeFromDateTime(DateTime date)
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

        private DateTime DateTimeFromFileTime(long date)
        {
            NativeMethods.SYSTEMTIME st;
            if (!NativeMethods.FileTimeToSystemTime(ref date, out st)) throw new Win32Exception();

            return new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, DateTimeKind.Local);
        }

        private void CheckPdhStatus(uint status)
        {
            if (status != NativeMethods.ERROR_SUCCESS && status != NativeMethods.PDH_NO_MORE_DATA)
            {
                throw new PCException(status);
            }
            if (status == NativeMethods.PDH_NO_MORE_DATA)
            {
                System.Diagnostics.Debug.WriteLine($"{fileName} contains no data on the current filters");
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
