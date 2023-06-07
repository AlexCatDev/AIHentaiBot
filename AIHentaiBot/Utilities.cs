using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHentaiBot
{
    public static class Utilities
    {
        private static Dictionary<string, Stopwatch> stopwatches = new();

        public static void BenchmarkStart(string name)
        {
            stopwatches.Add(name, Stopwatch.StartNew());
        }

        public static void BenchmarkStop(string name, out double genTime)
        {
            var stopwatch = stopwatches[name];
            stopwatch.Stop();

            stopwatches.Remove(name);

            genTime = ((double)stopwatch.ElapsedTicks / Stopwatch.Frequency) * 1000.0;
        }

        public static double Benchmark(Action a)
        {
            Stopwatch sw = Stopwatch.StartNew();

            a();

            sw.Stop();

            return ((double)sw.ElapsedTicks / Stopwatch.Frequency) * 1000.0;
        }

    }
}
