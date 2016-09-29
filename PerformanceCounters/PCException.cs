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

        public PCException(uint statusCode, Exception innerException) : base(NativeUtil.FormatMessage(statusCode), innerException)
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
    }
}
