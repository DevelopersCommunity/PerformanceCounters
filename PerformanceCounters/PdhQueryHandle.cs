using Microsoft.Win32.SafeHandles;

namespace DevelopersCommunity.PerformanceCounters
{
    internal class PdhQueryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhQueryHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.PdhCloseQuery(handle) == NativeMethods.ERROR_SUCCESS;
        }
    }
}
