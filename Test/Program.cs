using System;
using DevelopersCommunity.PerformanceCounters;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //string[] counters = { @"\\DESKTOP-355R7FE\Processor(*)\% Processor Time" };
            string[] counters = { @"\\*\Processor(*)\% Processor Time" };

            //PCReader pcr = new PCReader(args[0], counters);
            PCReader pcr = new PCReader(args[0], counters,
                new DateTime(2016, 9, 15, 17, 32, 00, DateTimeKind.Local),
                new DateTime(2016, 9, 15, 17, 33, 00, DateTimeKind.Local));
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

            Stopwatch c = Stopwatch.StartNew();

            PCReader pcr = new PCReader(file, counters, new DateTime(2016, 9, 15, 17, 32, 00, DateTimeKind.Local), new DateTime(2016, 9, 15, 17, 33, 00, DateTimeKind.Local));
            using (var enumerator = pcr.GetEnumerator())
            {
                for (int i = 0; i < 10; i++)
                {
                    enumerator.Reset();

                    while (enumerator.MoveNext())
                    {
                        foreach (var item in enumerator.Current)
                        {
                            //Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
                        }
                    }
                }
            }

            c.Stop();

            Console.WriteLine($"GetEnumerator (1) + Reset (N) + Dispose (1): {c.ElapsedMilliseconds}ms");

            c.Restart();

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

            c.Restart();

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

            //Relog takes 1162ms to iterate 100x
            //1 - We are taking 2255ms to iterate 10x using MoveNext+Reset and 6600ms using foreach
            //2 - Now we are iterating using foreach 2100ms
        }
    }
}

