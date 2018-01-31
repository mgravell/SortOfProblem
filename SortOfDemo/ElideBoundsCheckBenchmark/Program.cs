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
using System.Numerics;
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
        readonly int[] _values, _randomIndices, _workspaceI;

        readonly uint[] _workspace;
        readonly float[] _randomSingles;
        public PerformanceTests()
        {
            const int LENGTH = 2000000;
            _values = new int[LENGTH];
            _randomIndices = new int[LENGTH];
            _randomSingles = new float[LENGTH];
            _workspace = new uint[LENGTH];
            _workspaceI = new int[LENGTH];
            LowerInclusive = 0;
            UpperExclusive = LENGTH;
            var rand = new Random(LENGTH);
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = rand.Next();
                _randomIndices[i] = i;
                _randomSingles[i] = (float)((rand.NextDouble() * 50000) - 25000);
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
        const int OpsPerInvoke = 2;

        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Baseline = true)]
        public void SortablePerValue()
        {
            var values = _randomSingles;
            var workspace = _workspace;

            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    workspace[i] = Sortable(values[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void ToRadixPerValue()
        {
            var values = _randomSingles;
            var workspace = _workspaceI;

            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    workspace[i] = ToRadix(values[i]);
                }
            }
        }
        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void ToRadixBlock()
        {
            float[] values = _randomSingles;
            int[] radix = _workspaceI;
            SomeHelperBase<float,int> someHelper = new SomeHelper();

            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                someHelper.ToRadix(values, radix);
            }
        }

        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void ToRadixSpan() => ViaBlockHelper<SimpleBlockHelper>();

        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void Branchless() => ViaBlockHelper<BranchlessBlockHelper>();

        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void Vectorized() => ViaBlockHelper<VectorizedBlockHelper>();

        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void BranchlessXor() => ViaBlockHelper<BranchlessXorBlockHelper>();


        [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        public void VectorizedXor() => ViaBlockHelper<VectorizedXorBlockHelper>();


        void ViaBlockHelper<T>() where T : SomeBlockHelperBase, new ()
        {
            Span<float> typedSource = _randomSingles;
            Span<uint> workspace = _workspace;
            var source = typedSource.NonPortableCast<float, uint>();
            SomeBlockHelperBase converter = new T();

            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                converter?.ToRadix(source, workspace);
            }
        }

        abstract class SomeHelperBase<TFrom, TTo>
        {
            public abstract void ToRadix(TFrom[] values, TTo[] radix);
        }
        sealed class SomeHelper : SomeHelperBase<float,int>
        {
            public override unsafe void ToRadix(float[] source, int[] destination)
            {
                const int MSB = 1 << 31;
                for (int i = 0; i < source.Length; i++)
                {
                    var val = source[i];
                    int raw = *(int*)(&val);
                    // if sign bit set: flip all bits except the MSB
                    destination[i] = (raw & MSB) == 0 ? raw : ~raw | MSB;
                }
            }
        }

        abstract class SomeBlockHelperBase
        {
            public abstract void ToRadix(Span<uint> values, Span<uint> destination);
        }
        sealed class SimpleBlockHelper : SomeBlockHelperBase
        {
            public override void ToRadix(Span<uint> values, Span<uint> destination)
            {
                const uint MSB = 1U << 31;
                for (int i = 0; i < values.Length; i++)
                {
                    uint raw = values[i];
                    destination[i] = (raw & MSB) == 0 ? raw : ~raw | MSB;
                }
            }
        }

        sealed class BranchlessBlockHelper : SomeBlockHelperBase
        {
            public override void ToRadix(Span<uint> values, Span<uint> destination)
            {
                const uint MSB = 1U << 31;
                for (int i = 0; i < values.Length; i++)
                {
                    uint raw = values[i];
                    var ifNeg = (uint)(((int)raw) >> 31);
                    destination[i] =
                        (ifNeg & (~raw | MSB)) // true
                        | (~ifNeg & raw);      // false
                }
            }
        }

        sealed class BranchlessXorBlockHelper : SomeBlockHelperBase
        {
            public override void ToRadix(Span<uint> values, Span<uint> destination)
            {
                const uint MSB = 1U << 31;
                for (int i = 0; i < values.Length; i++)
                {
                    uint raw = values[i];
                    var ifNeg = (uint)(((int)raw) >> 31);
                    destination[i] =
                        (ifNeg & (raw ^ ~MSB)) // true
                        | (~ifNeg & raw);      // false
                }
            }
        }

        sealed class VectorizedBlockHelper : SomeBlockHelperBase
        {
            public override void ToRadix(Span<uint> values, Span<uint> destination)
            {
                const uint MSB = 1U << 31;

                int i = 0;
                if (Vector.IsHardwareAccelerated)
                {
                    var vSource = values.NonPortableCast<uint, Vector<uint>>();
                    var vDest = destination.NonPortableCast<uint, Vector<uint>>();

                    var vMSB = new Vector<uint>(MSB);
                    var vNOMSB = ~vMSB;
                    for (int j = 0; j < vSource.Length; j++)
                    {
                        var vec = vSource[j];
                        vDest[j] = Vector.ConditionalSelect(
                            condition: Vector.GreaterThan(vec, vNOMSB),
                            left: ~vec | vMSB, // when true
                            right: vec // when false
                        );
                    }

                    // change our root offset for the remainder of the values
                    i = vSource.Length * Vector<uint>.Count;
                }
                for (; i < values.Length; i++)
                {
                    uint raw = values[i];
                    var ifNeg = (uint)(((int)raw) >> 31);
                    destination[i] =
                        (ifNeg & (~raw | MSB)) // true
                        | (~ifNeg & raw);      // false
                }
            }
        }

        sealed class VectorizedXorBlockHelper : SomeBlockHelperBase
        {
            public override void ToRadix(Span<uint> values, Span<uint> destination)
            {
                const uint MSB = 1U << 31;

                int i = 0;
                if (Vector.IsHardwareAccelerated)
                {
                    var vSource = values.NonPortableCast<uint, Vector<uint>>();
                    var vDest = destination.NonPortableCast<uint, Vector<uint>>();

                    var vMSB = new Vector<uint>(MSB);
                    var vNOMSB = ~vMSB;
                    for (int j = 0; j < vSource.Length; j++)
                    {
                        var vec = vSource[j];
                        vDest[j] = Vector.ConditionalSelect(
                            condition: Vector.GreaterThan(vec, vNOMSB),
                            left: vec ^ vNOMSB, // when true
                            right: vec // when false
                        );
                    }

                    // change our root offset for the remainder of the values
                    i = vSource.Length * Vector<uint>.Count;
                }
                for (; i < values.Length; i++)
                {
                    uint raw = values[i];
                    var ifNeg = (uint)(((int)raw) >> 31);
                    destination[i] =
                        (ifNeg & (raw ^ ~MSB)) // true
                        | (~ifNeg & raw);      // false
                }
            }
        }

        unsafe int ToRadix(float value)
        {
            const int MSB = 1 << 31;
            int raw = *(int*)(&value);
            // if sign bit set: flip all bits except the MSB
            return (raw & MSB) == 0 ? raw : ~raw | MSB;
        }

        uint Sortable(int value)
        {
            // re-base eveything upwards, so anything
            // that was the min-value is now 0, etc
            var val = unchecked((uint)(value - int.MinValue));
            return val;
        }
        unsafe uint Sortable(float value)
        {
            const int MSB = 1 << 31;
            int raw = *(int*)(&value);
            if ((raw & MSB) != 0) // IEEE first bit is the sign bit
            {
                // is negative; shoult interpret as -(the value without the MSB) - not the same as just
                // dropping the bit, since integer math is twos-complement
                raw = -(raw & ~MSB);
            }
            return Sortable(raw);
        }

        //[Benchmark(OperationsPerInvoke = OpsPerInvoke, Baseline = true)]
        //[BenchmarkCategory("radix")]
        //public void SingleToUnsignedRadixBasic()
        //{
        //    Span<int> sourceInt32 = new Span<float>(_randomSingles).NonPortableCast<float, int>(),
        //        destinationInt32 = new Span<uint>(_workspace).NonPortableCast<uint, int>();

        //    for (int i = 0; i < OpsPerInvoke; i++)
        //    {
        //        ToUnsignedRadixBasic(sourceInt32, destinationInt32);
        //    }
        //}

        //private void ToUnsignedRadixBasic(Span<int> source, Span<int> destination)
        //{
        //    const int MSB = 1 << 31;
        //    for (int i = 0; i < source.Length; i++)
        //    {
        //        var val = source[i];
        //        destination[i] = ((val & MSB) == 0 ? val : -(val & ~MSB) - 1)
        //            -int.MinValue;
        //    }
        //}

        //[Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        //[BenchmarkCategory("radix")]
        //public void SingleToSignedRadixBasic()
        //{
        //    Span<uint> sourceUInt32 = new Span<float>(_randomSingles).NonPortableCast<float, uint>(),
        //        destinationUInt32 = new Span<uint>(_workspace);

        //    for (int i = 0; i < OpsPerInvoke; i++)
        //    {
        //        ToSignedRadixBasic(sourceUInt32, destinationUInt32);
        //    }
        //}
        //const uint MSB32U = 1U << 31;

        //private void ToSignedRadixBasic(Span<uint> source, Span<uint> destination)
        //{
        //    for (int i = 0; i < source.Length; i++)
        //    {
        //        var val = source[i];
        //        destination[i] = (val & MSB32U) == 0 ? val : ~val | MSB32U;
        //    }
        //}

        //[Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        //[BenchmarkCategory("radix")]
        //public void SingleToSignedRadixNoBranching()
        //{
        //    Span<uint> sourceUInt32 = new Span<float>(_randomSingles).NonPortableCast<float, uint>(),
        //        destinationUInt32 = new Span<uint>(_workspace);

        //    for (int i = 0; i < OpsPerInvoke; i++)
        //    {
        //        ToSignedRadixNoBranching(sourceUInt32, destinationUInt32);
        //    }
        //}
        //private void ToSignedRadixNoBranching(Span<uint> source, Span<uint> destination)
        //{
        //    for (int i = 0; i < source.Length; i++)
        //    {
        //        var val = source[i];
        //        var ifNeg = (uint)((int)val >> 31); // 11...11 or 00...00
        //        destination[i] = (ifNeg & (~val | MSB32U)) | (~ifNeg & val);
        //    }
        //}

        //[Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        //[BenchmarkCategory("radix")]
        //public void SingleToSignedRadixVectorized()
        //{
        //    Span<uint> sourceUInt32 = new Span<float>(_randomSingles).NonPortableCast<float, uint>(),
        //        destinationUInt32 = new Span<uint>(_workspace);

        //    for (int i = 0; i < OpsPerInvoke; i++)
        //    {
        //        ToSignedRadixVectorized(sourceUInt32, destinationUInt32);
        //    }
        //}
        //private void ToSignedRadixVectorized(Span<uint> source, Span<uint> destination)
        //{
        //    int i = 0;
        //    if (Vector.IsHardwareAccelerated) // note the JIT removes this test
        //    {                               // (and all the code if it isn't true)
        //        var vSource = source.NonPortableCast<uint, Vector<uint>>();
        //        var vDest = destination.NonPortableCast<uint, Vector<uint>>();
        //        var MSB = new Vector<uint>(MSB32U);
        //        var NOMSB = ~MSB;
        //        for (int j = 0; j < vSource.Length; j++)
        //        {
        //            var vec = vSource[j];
        //            vDest[j] = Vector.ConditionalSelect(
        //                condition: Vector.GreaterThan(vec, NOMSB),
        //                left: ~vec | MSB, // when true
        //                right: vec // when false
        //            );
        //        }
        //        // change our root offset for the remainder of the values
        //        i = vSource.Length * Vector<uint>.Count;
        //    }
        //    for (; i < source.Length; i++)
        //    {
        //        var val = source[i];
        //        var ifNeg = (uint)((int)val >> 31); // 11...11 or 00...00
        //        destination[i] = (ifNeg & (~val | MSB32U)) | (~ifNeg & val);
        //    }
        //}

        //[Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        //[BenchmarkCategory("radix")]
        //public void SingleToUnsignedRadixNoBranching()
        //{
        //    Span<int> sourceInt32 = new Span<float>(_randomSingles).NonPortableCast<float, int>(),
        //        destinationInt32 = new Span<uint>(_workspace).NonPortableCast<uint, int>();

        //    for (int i = 0; i < OpsPerInvoke; i++)
        //    {
        //        ToUnsignedRadixNoBranching(sourceInt32, destinationInt32);
        //    }
        //}

        //private void ToUnsignedRadixNoBranching(Span<int> source, Span<int> destination)
        //{
        //    const int MSB = 1 << 31;
        //    for (int i = 0; i < source.Length; i++)
        //    {
        //        var val = source[i];
        //        var ifNeg = val >> 31; // 11...11 or 00...00
        //        destination[i] = (
        //            (ifNeg & (-(val & ~MSB) - 1)) // true
        //            | (~ifNeg & val) // false
        //        ) - int.MinValue;
        //    }
        //}

        //[Benchmark(OperationsPerInvoke = OpsPerInvoke)]
        //[BenchmarkCategory("radix")]
        //public void SingleToUnsignedRadixVectorized()
        //{
        //    Span<int> sourceInt32 = new Span<float>(_randomSingles).NonPortableCast<float, int>(),
        //        destinationInt32 = new Span<uint>(_workspace).NonPortableCast<uint, int>();

        //    for (int i = 0; i < OpsPerInvoke; i++)
        //    {
        //        ToUnsignedRadixVectorized(sourceInt32, destinationInt32);
        //    }
        //}

        //private void ToUnsignedRadixVectorized(Span<int> source, Span<int> destination)
        //{
        //    const int MSB = 1 << 31;
        //    int i = 0;
        //    if (Vector.IsHardwareAccelerated) // note the JIT removes this test
        //    {                               // (and all the code if it isn't true)
        //        var vSource = source.NonPortableCast<int, Vector<int>>();
        //        var vDest = destination.NonPortableCast<int, Vector<int>>();
        //        var vMSB = new Vector<int>(MSB);
        //        var vNOMSB = ~vMSB;
        //        var vMin = new Vector<int>(int.MinValue);
        //        for (int j = 0; j < vSource.Length; j++)
        //        {
        //            var vec = vSource[j];
        //            vDest[j] = Vector.ConditionalSelect(
        //                condition: Vector.LessThan(vec, Vector<int>.Zero),
        //                left: -(vec & vNOMSB) - Vector<int>.One, // when true
        //                right: vec // when false
        //            ) - vMin;
        //        }
        //        // change our root offset for the remainder of the values
        //        i = vSource.Length * Vector<int>.Count;
        //    }

        //    for (; i < source.Length; i++)
        //    {
        //        var val = source[i];
        //        var ifNeg = val >> 31; // 11...11 or 00...00
        //        destination[i] = (
        //            (ifNeg & (-(val & ~MSB) - 1)) // true
        //            | (~ifNeg & val) // false
        //        ) - int.MinValue;
        //    }
        //}

#if ELIDE
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
                    int len = _values.Length;
                    for (int i = 0; i < len; i++)
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
                    int len = _randomIndices.Length;
                    while (len-- > 0)
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
                    int len = to - from;
                    while (len-- > 0)
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
            var values = new Span<int>(_values);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < values.Length; i++)
                    xor ^= values[i];
            }
            return xor;
        }

        [Benchmark(Description = "span [lower,upper)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int SpanSequentialPartial()
        {
            int xor = 0;
            int from = LowerInclusive, to = UpperExclusive;
            var values = new Span<int>(_values);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = from; i < to; i++)
                    xor ^= values[i];
            }
            return xor;
        }

        [Benchmark(Description = "span slice", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("sequential")]
        public int SpanSequentialSliced()
        {
            int xor = 0;
            int from = LowerInclusive, to = UpperExclusive;
            var values = new Span<int>(_values).Slice(from, to - from);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < values.Length; i++)
                    xor ^= values[i];
            }
            return xor;
        }



        [Benchmark(Description = "array [0,len)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public int ArrayRandomFull()
        {
            int xor = 0;
            var values = _values;
            var index = _randomIndices;
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < index.Length; i++)
                    xor ^= values[index[i]];
            }
            return xor;
        }

        [Benchmark(Description = "array [lower,upper)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public int ArrayRandomPartial()
        {
            int xor = 0;
            var values = _values;
            var index = _randomIndices;
            int from = LowerInclusive, to = UpperExclusive;
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = from; i < to; i++)
                    xor ^= values[index[i]];
            }
            return xor;
        }

        [Benchmark(Description = "fixed [0,len), index", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public unsafe int FixedRandomFullByIndex()
        {
            int xor = 0;
            fixed (int* values = _values)
            fixed (int* index = _randomIndices)
            {
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    int len = _randomIndices.Length;
                    for (int i = 0; i < len; i++)
                        xor ^= values[index[i]];
                }
            }
            return xor;
        }

        [Benchmark(Description = "fixed [lower,upper), index", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public unsafe int FixedRandomPartialByIndex()
        {
            int xor = 0;
            fixed (int* values = _values)
            fixed (int* index = _randomIndices)
            {
                int from = LowerInclusive, to = UpperExclusive;
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    for (int i = from; i < to; i++)
                        xor ^= values[index[i]];
                }
            }
            return xor;
        }

        [Benchmark(Description = "fixed [0,len), incr", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public unsafe int FixedRandomFullByIncr()
        {
            int xor = 0;
            fixed (int* values = _values)
            fixed (int* index = _randomIndices)
            {
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    var ptr = index;
                    int len = _randomIndices.Length;
                    while (len-- > 0)
                        xor ^= values[*ptr++];
                }
            }
            return xor;
        }

        [Benchmark(Description = "fixed [lower,upper), incr", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public unsafe int FixedRandomPartialByIncr()
        {
            int xor = 0;
            fixed (int* values = _values)
            fixed (int* index = _randomIndices)
            {
                int from = LowerInclusive, to = UpperExclusive;
                for (int loop = 0; loop < OpsPerInvoke; loop++)
                {
                    var ptr = index + from;
                    int len = to - from;
                    while (len-- > 0)
                        xor ^= values[*ptr++];
                }
            }
            return xor;
        }

        [Benchmark(Description = "span [0,len)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public int SpanRandomFull()
        {
            int xor = 0;
            var values = new Span<int>(_values);
            var index = new Span<int>(_randomIndices);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < index.Length; i++)
                    xor ^= values[index[i]];
            }
            return xor;
        }

        [Benchmark(Description = "span [lower,upper)", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public int SpanRandomPartial()
        {
            int xor = 0;
            int from = LowerInclusive, to = UpperExclusive;
            var values = new Span<int>(_values);
            var index = new Span<int>(_randomIndices);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = from; i < to; i++)
                    xor ^= values[index[i]];
            }
            return xor;
        }

        [Benchmark(Description = "span slice", OperationsPerInvoke = OpsPerInvoke)]
        [BenchmarkCategory("indexed")]
        public int SpanRandomSliced()
        {
            int xor = 0;
            int from = LowerInclusive, to = UpperExclusive;
            var values = new Span<int>(_values);
            var index = new Span<int>(_randomIndices).Slice(from, to - from);
            for (int loop = 0; loop < OpsPerInvoke; loop++)
            {
                for (int i = 0; i < index.Length; i++)
                    xor ^= values[index[i]];
            }
            return xor;
        }

#endif
        int LowerInclusive { [MethodImpl(MethodImplOptions.NoInlining)] get; }
        int UpperExclusive { [MethodImpl(MethodImplOptions.NoInlining)] get; }

    }
}
