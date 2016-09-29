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
                new DateTime(2016, 9, 15, 17, 47, 00, DateTimeKind.Local));
            var enumerator = pcr.GetEnumerator();

            while (enumerator.MoveNext())
            {
                foreach (var item in enumerator.Current)
                {
                    Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
                }
            }

            //foreach (var sample in pcr)
            //{
            //    foreach (var item in sample)
            //    {
            //        Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
            //    }
            //}

            Stopwatch c = Stopwatch.StartNew();

            for (int i = 0; i < 10; i++)
            {
                enumerator.Reset();//Expensive
                while (enumerator.MoveNext()) //foreach is slower
                {
                    foreach (var item in enumerator.Current)
                    {
                        //Console.WriteLine("{0},{1},{2}", item.CounterPath, item.TimeStamp, item.Value);
                    }
                }
            }

            c.Stop();

            Console.WriteLine($"{c.ElapsedMilliseconds}ms");//Relog takes 1162ms to iterate 100x
            Console.ReadLine();
        }
    }
}

