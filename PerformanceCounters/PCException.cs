using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace DevelopersCommunity.PerformanceCounters
{
    [Serializable]
    public class PCException : Exception, ISerializable
    {
        public uint StatusCode { get; }

        protected PCException()
        {
        }

        public PCException(uint statusCode) : this(statusCode, null)
        {
        }

        public PCException(uint statusCode, Exception innerException) : base(FormatMessage(statusCode), innerException)
        {
            StatusCode = statusCode;
        }

        protected PCException(string message) : base(message)
        {
        }

        protected PCException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PCException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        private static string FormatMessage(uint statusCode)
        {
            const string pdhModuleName = "pdh.dll";

            IntPtr pdhModule = NativeMethods.GetModuleHandle(pdhModuleName);
            if (pdhModule == IntPtr.Zero)
            {
                pdhModule = NativeMethods.LoadLibrary(pdhModuleName);
                if (pdhModule == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }

            IntPtr messageBuffer = IntPtr.Zero;
            try
            {
                if (NativeMethods.FormatMessage(
                    NativeMethods.FORMAT_MESSAGE_FROM_HMODULE | NativeMethods.FORMAT_MESSAGE_ALLOCATE_BUFFER | NativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS,
                    pdhModule, statusCode, 0, ref messageBuffer, 0, IntPtr.Zero) == 0)
                {
                    throw new Win32Exception();
                }
                return Marshal.PtrToStringUni(messageBuffer);
            }
            finally
            {
                if (messageBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(messageBuffer);
                }
            }
        }
    }
}
