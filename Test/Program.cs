using System;
using DevelopersCommunity.PerformanceCounters;
using System.Diagnostics;
using System.Collections.Generic;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //PCReader.BrowseCounters(IntPtr.Zero, "Title", args[0]);
            //string[] counters = { @"\\DESKTOP-355R7FE\Processor(*)\% Processor Time" };
            string[] counters = { @"\\*\Processor(*)\% Processor Time" };

            List<string> expandedCounters = new List<string>();
            foreach (string wildCard in counters)
            {
                expandedCounters.AddRange(PCReader.ExpandWildCard(args[0], wildCard));
            }

            //PCReader pcr = new PCReader(args[0], counters);
            PCReader pcr = new PCReader(args[0], expandedCounters, false,
                new DateTime(2016, 9, 15, 17, 32, 00, DateTimeKind.Unspecified),
                new DateTime(2016, 9, 15, 17, 33, 00, DateTimeKind.Unspecified));
            var enumerator = pcr.GetEnumerator();

            while (enumerator.MoveNext())
            {
                foreach (var item in enumerator.Current)
                {
                    Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
                }
            }

            Benchmark(args[0], counters);

            Console.ReadLine();
        }

        static void Benchmark(string file, string[] counters)
        {
            Console.WriteLine("Start");

            PCReader pcr = new PCReader(file, counters, true, new DateTime(2016, 9, 15, 17, 32, 00, DateTimeKind.Unspecified), new DateTime(2016, 9, 15, 17, 47, 00, DateTimeKind.Unspecified));
            PcReaderEnumerable pcEnumerable = new PcReaderEnumerable(file, counters, true, new DateTime(2016, 9, 15, 17, 32, 00, DateTimeKind.Unspecified), new DateTime(2016, 9, 15, 17, 47, 00, DateTimeKind.Unspecified));

            GetEnumeratorResetDispose(pcr, file, counters);

            GetEnumerator(pcr);

            Foreach(pcr);

            PcReaderEnumerable(pcEnumerable);

            //Relog takes 2229ms
        }

        private static PCReader GetEnumeratorResetDispose(PCReader pcr, string file, string[] counters)
        {
            Stopwatch c = Stopwatch.StartNew();


            using (var enumerator = pcr.GetEnumerator())
            {
                for (int i = 0; i < 10; i++)
                {
                    enumerator.Reset();

                    while (enumerator.MoveNext())
                    {
                        foreach (var item in enumerator.Current)
                        {
                            
                        }
                    }
                }
            }

            c.Stop();

            Console.WriteLine($"GetEnumerator (1) + Reset (N) + Dispose (1): {c.ElapsedMilliseconds}ms");
            return pcr;
        }

        private static void GetEnumerator(PCReader pcr)
        {
            Stopwatch c = Stopwatch.StartNew();

            for (int i = 0; i < 10; i++)
            {
                using (var enumerator = pcr.GetEnumerator())
                {
                    while (enumerator.MoveNext()) //foreach is slower
                    {
                        foreach (var item in enumerator.Current)
                        {
                            //Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
                        }
                    }
                }
            }

            c.Stop();

            Console.WriteLine($"GetEnumerator (N) + Dispose(N): {c.ElapsedMilliseconds}ms");
        }

        private static void Foreach(PCReader pcr)
        {
            Stopwatch c = Stopwatch.StartNew();

            for (int i = 0; i < 10; i++)
            {
                foreach (var sample in pcr)
                {
                    foreach (var item in sample)
                    {
                        //Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
                    }
                }
            }

            c.Stop();

            Console.WriteLine($"Foreach: {c.ElapsedMilliseconds}ms");
        }

        private static void PcReaderEnumerable(PcReaderEnumerable pcEnumerable)
        {
            Stopwatch c = Stopwatch.StartNew();

            for (int i = 0; i < 10; i++)
            {
                foreach (var sample in pcEnumerable)
                {

                }
            }

            c.Stop();

            Console.WriteLine($"Foreach novo: {c.ElapsedMilliseconds}ms");
        }
    }
}

