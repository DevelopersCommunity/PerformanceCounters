using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace DevelopersCommunity.PerformanceCounters
{
    internal static class NativeUtil
    {
        internal static string FormatMessage(uint statusCode)
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

        internal static long FileTimeFromDateTime(DateTime date)
        {
            return date.ToFileTimeUtc();//TODO Test to avoid a PInvoke call

            //TODO It is apparently losing the timezone, but is working. Maybe BLG files doesn't track timezones and record the local time as UTC time.
            //Needs to ensure if we will have problem we analyzing servers in different timezones, some customers uses UTC.
            //Due to this behavior, we need to set the Kind to Utc before calling ToFileTimeUtc

            //date.ToString("O")
            //"2016-09-15T17:32:00.0000000-03:00"

            //DateTime.FromFileTimeUtc(ft).ToString("O")
            //"2016-09-15T17:32:00.0000000Z"

            //DateTime.FromFileTime(ft).ToString("O")
            //"2016-09-15T14:32:00.0000000-03:00"

            //var st = new NativeMethods.SYSTEMTIME
            //{
            //    wYear = (ushort)date.Year,
            //    wMonth = (ushort)date.Month,
            //    wDay = (ushort)date.Day,
            //    wHour = (ushort)date.Hour,
            //    wMinute = (ushort)date.Minute,
            //    wSecond = (ushort)date.Second,
            //    wMilliseconds = (ushort)date.Millisecond,
            //    wDayOfWeek = (ushort)date.DayOfWeek
            //};

            //long ft;
            //if (!NativeMethods.SystemTimeToFileTime(ref st, out ft)) throw new Win32Exception();            

            //return ft;
        }

        internal static DateTime DateTimeFromFileTime(long date)
        {
            return DateTime.FromFileTimeUtc(date);//TODO Test to avoid a PInvoke call

            //NativeMethods.SYSTEMTIME st;
            //if (!NativeMethods.FileTimeToSystemTime(ref date, out st)) throw new Win32Exception();

            //return new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, DateTimeKind.Local);
        }

        internal static void CheckPdhStatus(uint status)
        {
            if (status != NativeMethods.ERROR_SUCCESS)
            {
                throw new PCException(status);
            }
        }

        internal static IEnumerable<string> MultipleStringsToList(string multipleStrings)
        {
            var result = multipleStrings.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

            return result;
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
                    if (item.Length > 0)
                    {
                        list.Add(item.ToString());
                        item.Length = 0;
                    }
                    if (multipleStrings[i + 1] == '\0')
                    {
                        break;
                    }
                }
            }

            return list;
        }

        internal static IReadOnlyList<string> ExpandWildCard(string fileName, string wildCard)
        {
            uint len = 0;
            var status = NativeMethods.PdhExpandWildCardPath(fileName, wildCard, null, ref len, 0);
            if (status != NativeMethods.PDH_MORE_DATA)
            {
                if (status == NativeMethods.PDH_ENTRY_NOT_IN_LOG_FILE)
                {
                    System.Diagnostics.Debug.WriteLine("PDH_ENTRY_NOT_IN_LOG_FILE");
                    return new List<string>();
                }
                if (status == NativeMethods.PDH_CSTATUS_NO_OBJECT)
                {
                    System.Diagnostics.Debug.WriteLine("PDH_CSTATUS_NO_OBJECT");
                    return new List<string>();
                }
                CheckPdhStatus(status);
            }
            var buffer = new char[len];
            CheckPdhStatus(NativeMethods.PdhExpandWildCardPath(fileName, wildCard, buffer, ref len, 0));
            return MultipleStringsToList(buffer);
        }
    }
}
