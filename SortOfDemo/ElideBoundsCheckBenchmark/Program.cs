using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElideBoundsCheckBenchmark
{
    class Program
    {
        static void Main()
        {
            // tell BenchmarkDotNet not to force GC.Collect after benchmark iteration 
            // (single iteration contains of multiple (usually millions) of invocations)
            // it can influence the allocation-heavy Task<T> benchmarks
            var gcMode = new GcMode { Force = false };

            var customConfig = ManualConfig
                .Create(DefaultConfig.Instance) // copies all exporters, loggers and basic stuff
                .With(JitOptimizationsValidator.FailOnError) // Fail if not release mode
                .With(MemoryDiagnoser.Default) // use memory diagnoser
                .With(StatisticColumn.OperationsPerSecond) // add ops/s
                .With(Job.Default.With(gcMode));


#if NET462
            // enable the Inlining Diagnoser to find out what does not get inlined
            // uncomment it first, it produces a lot of output
            //customConfig = customConfig.With(new BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser(logFailuresOnly: true, filterByNamespace: true));
#endif

            var summary = BenchmarkRunner.Run<PerformanceTests>(customConfig);
            Console.WriteLine(summary);
        }
    }

    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class PerformanceTests
    {
        readonly int[] _values, _randomIndices;
        public PerformanceTests()
        {
            const int LENGTH = 1024 * 1024;
            _values = new int[LENGTH];
            _randomIndices = new int[LENGTH];
            LowerInclusive = 0;
            UpperExclusive = LENGTH;
            var rand = new Random(LENGTH);
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = rand.Next();
                _randomIndices[i] = i;
            }

            // shuffle the indices
            for (int i = 0; i < _values.Length * 16; i++)
            {
                int x, y;
                do
                {
                    x = rand.Next(LENGTH);
                    y = rand.Next(LENGTH);
                } while (x == y);

                var tmp = _randomIndices[x];
                _randomIndices[x] = _randomIndices[y];
                _randomIndices[y] = tmp;
            }
        }
        const int OpsPerInvoke = 64;
        [Benchmark(Description = "array [0,len)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int ArraySequentialFull()
        {
            int xor = 0;
            var values = _values;
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < values.Length; i++)
                    xor ^= values[i];
            }
            return xor;
        }

        [Benchmark(Description = "array [lower,upper)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int ArraySequentialPartial()
        {
            int xor = 0;
            var values = _values;
            int from = LowerInclusive, to = UpperExclusive;
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = from; i < to; i++)
                    xor ^= values[i];
            }
            return xor;
        }

        [Benchmark(Description = "fixed [0,len), index", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public unsafe int FixedSequentialFullByIndex()
        {
            int xor = 0;
            fixed (int* values = _values)
            {
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    for (int i = 0; i < _values.Length; i++)
                        xor ^= values[i];
                }
            }
            return xor;
        }

        [Benchmark(Description = "fixed [lower,upper), index", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public unsafe int FixedSequentialPartialByIndex()
        {
            int xor = 0;
            fixed (int* values = _values)
            {
                int from = LowerInclusive, to = UpperExclusive;
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    for (int i = from; i < to; i++)
                        xor ^= values[i];
                }
            }
            return xor;
        }

        [Benchmark(Description = "fixed [0,len), incr", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public unsafe int FixedSequentialFullByIncr()
        {
            int xor = 0;
            fixed (int* values = _values)
            {
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    var ptr = values;
                    for (int i = 0; i < _values.Length; i++)
                        xor ^= *ptr++;
                }
            }
            return xor;
        }

        [Benchmark(Description = "fixed [lower,upper), incr", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public unsafe int FixedSequentialPartialByIncr()
        {
            int xor = 0;
            fixed (int* values = _values)
            {
                int from = LowerInclusive, to = UpperExclusive;
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    var ptr = values + from;
                    for (int i = from; i < to; i++)
                        xor ^= *ptr++;
                }
            }
            return xor;
        }

        [Benchmark(Description = "span [0,len)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int SpanSequentialFull()
        {
            int xor = 0;
            var span = new Span<int>(_values);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < span.Length; i++)
                    xor ^= span[i];
            }
            return xor;
        }

        [Benchmark(Description = "span [lower,upper)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int SpanSequentialPartial()
        {
            int xor = 0;
            int from = LowerInclusive, to = UpperExclusive;
            var span = new Span<int>(_values);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = from; i < to; i++)
                    xor ^= span[i];
            }
            return xor;
        }

        [Benchmark(Description = "span slice", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int SpanSequentialSliced()
        {
            int xor = 0;
            int from = LowerInclusive, to = UpperExclusive;
            var span = new Span<int>(_values).Slice(from, to - from);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < span.Length; i++)
                    xor ^= span[i];
            }
            return xor;
        }


        int LowerInclusive { [MethodImpl(MethodImplOptions.NoInlining)] get; }
        int UpperExclusive { [MethodImpl(MethodImplOptions.NoInlining)] get; }
    }
}
