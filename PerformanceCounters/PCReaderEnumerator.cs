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
            NativeUtil.CheckPdhStatus(NativeMethods.PdhOpenQuery(fileName, IntPtr.Zero, out queryHandle));

            if (start.HasValue || end.HasValue)
            {
                var timeInfo = new NativeMethods.PDH_TIME_INFO();
                timeInfo.StartTime = start.HasValue ? NativeUtil.FileTimeFromDateTime(start.Value) : 0;
                timeInfo.EndTime = end.HasValue ? NativeUtil.FileTimeFromDateTime(end.Value) : long.MaxValue;
                NativeUtil.CheckPdhStatus(NativeMethods.PdhSetQueryTimeRange(queryHandle, ref timeInfo));
            }

            foreach (string counter in expandedCounters)
            {
                IntPtr counterHandle;
                NativeUtil.CheckPdhStatus(NativeMethods.PdhAddCounter(queryHandle, counter, IntPtr.Zero, out counterHandle));
                counterHandles.Add(counter, counterHandle);
            }

            var status = NativeMethods.PdhCollectQueryData(queryHandle);
            if (status != NativeMethods.PDH_NO_MORE_DATA)
            {
                NativeUtil.CheckPdhStatus(status);
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
                        NativeUtil.CheckPdhStatus(status);
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
            NativeUtil.CheckPdhStatus(status);
            currentTimeStamp = NativeUtil.DateTimeFromFileTime(date);

            // Workaround: if supplied date range is ahead of date range available in blg, PDH return the whole list
            if (start > currentTimeStamp)
                return false;

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
                NativeUtil.CheckPdhStatus(status);
            }
            var buffer = new char[len];
            NativeUtil.CheckPdhStatus(NativeMethods.PdhExpandWildCardPath(fileName, wildCard, buffer, ref len, 0));
            return NativeUtil.MultipleStringsToList(buffer);
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
