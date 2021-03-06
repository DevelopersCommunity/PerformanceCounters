﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DevelopersCommunity.PerformanceCounters
{
    internal static class NativeMethods
    {
        internal const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        internal const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        internal const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        internal const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        internal const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        internal const uint FORMAT_MESSAGE_FROM_STRING = 0x00000400;

        internal const uint ERROR_SUCCESS = 0;
        internal const uint PDH_NO_DATA = 0x800007D5;
        internal const uint PDH_MORE_DATA = 0x800007D2;
        internal const uint PDH_CALC_NEGATIVE_DENOMINATOR = 0x800007D6;
        internal const uint PDH_CALC_NEGATIVE_VALUE = 0x800007D8;
        internal const uint PDH_CALC_NEGATIVE_TIMEBASE = 0x800007D7;
        internal const uint PDH_INVALID_DATA = 0xC0000BC6;
        internal const uint PDH_NO_MORE_DATA = 0xC0000BCC;
        internal const uint PDH_ENTRY_NOT_IN_LOG_FILE = 0xC0000BCD;
        internal const uint PDH_CSTATUS_NO_OBJECT = 0xC0000BB8;
        internal const uint PDH_RETRY = 0x800007D4;
        internal const uint PDH_DIALOG_CANCELLED = 0x800007D9;

        internal const uint PDH_MAX_COUNTER_PATH = 2048;

        internal const uint PERF_DETAIL_NOVICE = 100;
        internal const uint PERF_DETAIL_ADVANCED = 200;
        internal const uint PERF_DETAIL_EXPERT = 300;
        internal const uint PERF_DETAIL_WIZARD = 400;

        internal const uint PDH_FMT_DOUBLE = 0x00000200;

        [StructLayout(LayoutKind.Explicit)]
        internal struct PDH_FMT_COUNTERVALUE
        {
            [FieldOffset(0)]
            public uint CStatus;

            [FieldOffset(8)]
            public int longValue;

            [FieldOffset(8)]
            public double doubleValue;

            [FieldOffset(8)]
            public Int64 largeValue;

            [FieldOffset(8)]
            public IntPtr AnsiStringValue;

            [FieldOffset(8)]
            public IntPtr WideStringValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PDH_TIME_INFO
        {
            public long StartTime;
            public long EndTime;
            public uint SampleCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        public delegate uint CounterPathCallBack(IntPtr arg);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct PDH_BROWSE_DLG_CONFIG
        {
            public PDH_BROWSE_DLG_CONFIG_Flags Flags;
            public IntPtr hWndOwner;
            public string szDataSource;
            public IntPtr szReturnPathBuffer;
            public uint cchReturnPathLength;
            public CounterPathCallBack pCallBack;
            public IntPtr dwCallBackArg;
            public uint CallBackStatus;//PDH_STATUS
            public uint dwDefaultDetailLevel;
            public string szDialogBoxCaption;
        }

        [Flags]
        internal enum PDH_BROWSE_DLG_CONFIG_Flags : uint
        {
            bIncludeInstanceIndex = 1,
            bSingleCounterPerAdd = 2,
            bSingleCounterPerDialog = 4,
            bLocalCountersOnly = 8,
            bWildCardInstances = 16,
            bHideDetailBox = 32,
            bInitializePath = 64,
            bDisableMachineSelection = 128,
            bIncludeCostlyObjects = 256,
            bShowObjectBrowser = 512
        }


        //[StructLayout(LayoutKind.Sequential)]
        //internal struct FILETIME
        //{
        //    public ulong LowDateTime;
        //    public uint dwHighDateTime;
        //}

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FileTimeToSystemTime(ref long lpFileTime, out SYSTEMTIME lpSystemTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SystemTimeToFileTime(ref SYSTEMTIME lpSystemTime, out long lpFileTime);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpModuleName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint FormatMessage(uint dwFlags, IntPtr lpSource,
            uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer,
            uint nSize, IntPtr Arguments);

        [DllImport("Pdh.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PdhEnumMachines(string szDataSource,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] char[] mszMachineNameList,
            ref uint pcchBufferLength
            );

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PdhOpenQuery(string szDataSource, IntPtr dwUserData, out PdhQueryHandle phQuery);

        [DllImport("pdh.dll")]
        internal static extern uint PdhCloseQuery(IntPtr hQuery);

        [DllImport("pdh.dll")]
        internal static extern uint PdhSetQueryTimeRange(PdhQueryHandle hQuery, ref PDH_TIME_INFO pInfo);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PdhAddCounter(PdhQueryHandle hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);

        [DllImport("pdh.dll")]
        internal static extern uint PdhCollectQueryData(PdhQueryHandle hQuery);

        [DllImport("pdh.dll")]
        internal static extern uint PdhCollectQueryDataWithTime(PdhQueryHandle hQuery, out long pllTimeStamp);

        [DllImport("pdh.dll")]
        internal static extern uint PdhGetFormattedCounterValue(IntPtr hCounter, uint dwFormat, IntPtr lpdwType,
            out PDH_FMT_COUNTERVALUE pValue);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PdhExpandWildCardPath(string szDataSource, string szWildCardPath,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] mszExpandedPathList,
            ref uint pcchPathListLength, uint dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PdhGetDataSourceTimeRange(string szDataSource, out uint pdwNumEntries,
            out PDH_TIME_INFO pInfo, ref uint pdwBufferSize);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        internal static extern uint PdhBrowseCounters(ref PDH_BROWSE_DLG_CONFIG config);
    }
}
