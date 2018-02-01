using System;
using System.Diagnostics;
using System.Numerics;

namespace Sorted
{
    struct BasicTimer : IDisposable
    {
        Stopwatch _timer;
        string _message { get; }
        public BasicTimer(string message)
        {
            _message = message;
            Console.Write($"> {message}...");
            _timer = Stopwatch.StartNew();
        }
        void IDisposable.Dispose()
        {
            _timer.Stop();
            Console.WriteLine($" {_timer.ElapsedMilliseconds}ms");
        }
    }
    static class Program
    {
        static void Main()
        {
#if DEBUG
            Execute(); // so we get good break-points
#else
            try
            {
                Execute();
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.Error.WriteLine(ex);
                    ex = ex.InnerException;
                }

            }
#endif
        }

        static void Execute()
        {

            //const int MSB = 1 << 31;
            //var vMSB = new Vector<int>(MSB);
            //var vMin = new Vector<int>(int.MinValue);
            //for (int j = 0; j < vSource.Length; j++)
            //{
            //    var vec = vSource[j] + vMin;
            //    vDest[j] = Vector.ConditionalSelect(
            //        condition: Vector.LessThan(vec, Vector<int>.Zero),
            //        left: -vec | vMSB, // when true
            //        right: vec // when false
            //    ) - vMin;
            //}

            //var z = int.MinValue;
            //Console.WriteLine((uint)z);
            //var c = RadixConverter.Get<int, uint>();
            //var arr = new int[] { int.MinValue, -1, 0, 1, int.MaxValue};
            //var arr2 = new uint[arr.Length];
            //var s = new Span<int>(arr).NonPortableCast<int, uint>();
            //Console.WriteLine(string.Join(", ", arr));
            //c.ToRadix(s, arr2);
            //Console.WriteLine(string.Join(", ", arr2));
            //c.FromRadix(arr2, s);
            //Console.WriteLine(string.Join(", ", arr));

            var rand = new Random(12345);
#if DEBUG
            const int LOOP = 1, DATA_COUNT = 16 * 1024;
#else
            const int LOOP = 4, DATA_COUNT = 16 * 1024 * 1024;
#endif
            float[] origFloat = new float[DATA_COUNT], valsFloat = new float[origFloat.Length];
            uint[] origUInt32 = new uint[origFloat.Length], valsUInt32 = new uint[origFloat.Length];
            int[] origInt32 = new int[origFloat.Length], valsInt32 = new int[origFloat.Length];

            for (int i = 0; i < origFloat.Length; i++)
            {
                origFloat[i] = (float)((rand.NextDouble() * 50000) - 15000);
                int ival = rand.Next(int.MinValue, int.MaxValue);
                origInt32[i] = ival;
                origUInt32[i] = unchecked((uint)ival);
            }
            var wFloat = new float[origFloat.Length];
            Console.WriteLine($"Workspace length: {wFloat.Length}");

            Console.WriteLine();
            Console.WriteLine(">> float <<");
            Console.WriteLine();
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("RadixSort.Sort"))
                {
                    RadixSort.Sort<float>(valsFloat, wFloat);
                }
                CheckSort<float>(valsFloat);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("RadixSort.Sort/descending"))
                {
                    RadixSort.Sort<float>(valsFloat, wFloat, descending: true);
                }
                CheckSortDescending<float>(valsFloat);
            }
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("RadixSort.ParallelSort"))
            //    {
            //        var workers = RadixSort.ParallelSort<float>(valsFloat, wFloat);
            //        Console.Write($" using {workers} worker(s)");
            //    }
            //    CheckSort<float>(valsFloat);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("RadixSort.ParallelSort/descending"))
            //    {
            //        var workers = RadixSort.ParallelSort<float>(valsFloat, wFloat, descending: true);
            //        Console.Write($" using {workers} worker(s)");
            //    }
            //    CheckSortDescending<float>(valsFloat);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("RadixSortUnsafe.Sort"))
            //    {
            //        RadixSortUnsafe.Sort<float>(valsFloat, wFloat);
            //    }
            //    CheckSort<float>(valsFloat);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
            //    {
            //        RadixSortUnsafe.Sort<float>(valsFloat, wFloat, descending: true);
            //    }
            //    CheckSortDescending<float>(valsFloat);
            //}
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("Array.Sort"))
                {
                    Array.Sort<float>(valsFloat);
                }
                CheckSort<float>(valsFloat);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("Array.Sort+Array.Reverse"))
                {
                    Array.Sort<float>(valsFloat);
                    Array.Reverse<float>(valsFloat);
                }
                CheckSortDescending<float>(valsFloat);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("Array.Sort/neg CompareTo"))
                {
                    Array.Sort<float>(valsFloat, (x, y) => y.CompareTo(x));
                }
                CheckSortDescending<float>(valsFloat);
            }



            Console.WriteLine();
            Console.WriteLine($">> int <<");
            Console.WriteLine();
            var wInt = new Span<float>(wFloat).NonPortableCast<float, int>();
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.Sort"))
                {
                    RadixSort.Sort<int>(valsInt32, wInt);
                }
                CheckSort<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.Sort/descending"))
                {
                    RadixSort.Sort<int>(valsInt32, wInt, descending: true);
                }
                CheckSortDescending<int>(valsInt32);
            }
            int[] mem32 = new int[wFloat.Length];
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort"))
                {
                    var workers = RadixSort.ParallelSort<int>(valsInt32, mem32);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSort<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort/descending"))
                {
                    var workers = RadixSort.ParallelSort<int>(valsInt32, mem32, descending: true);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSortDescending<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort"))
                {
                    RadixSortUnsafe.Sort<int>(valsInt32, wInt);
                }
                CheckSort<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
                {
                    RadixSortUnsafe.Sort<int>(valsInt32, wInt, descending: true);
                }
                CheckSortDescending<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
                {
                    RadixSortUnsafe.Sort<int>(valsInt32, wInt, descending: true);
                }
                CheckSortDescending<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("Array.Sort"))
                {
                    Array.Sort<int>(valsInt32);
                }
                CheckSort<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("Array.Sort+Array.Reverse"))
                {
                    Array.Sort<int>(valsInt32);
                    Array.Reverse<int>(valsInt32);
                }
                CheckSortDescending<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("Array.Sort/neg CompareTo"))
                {
                    Array.Sort<int>(valsInt32, (x, y) => y.CompareTo(x));
                }
                CheckSortDescending<int>(valsInt32);
            }

            Console.WriteLine();
            Console.WriteLine(">> uint <<");
            Console.WriteLine();
            var wUint = new Span<float>(wFloat).NonPortableCast<float, uint>();
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.Sort"))
                {
                    RadixSort.Sort<uint>(valsUInt32, wUint);
                }
                CheckSort<uint>(valsUInt32);
                using (new BasicTimer("RadixSort.Sort (sorted)"))
                {
                    RadixSort.Sort<uint>(valsUInt32, wUint);
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                var slice = new Span<uint>(valsUInt32, 0, 2048);
                using (new BasicTimer("RadixSort.SortSmall (on stack)"))
                {
                    RadixSort.SortSmall<uint>(slice);
                }
                CheckSort<uint>(slice);
                using (new BasicTimer("RadixSort.SortSmall (on stack, sorted)"))
                {
                    RadixSort.SortSmall<uint>(slice);
                }
                CheckSort<uint>(slice);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.Sort/descending"))
                {
                    RadixSort.Sort<uint>(valsUInt32, wUint, descending: true);
                }
                CheckSortDescending<uint>(valsUInt32);
            }
            var memu32 = new uint[wFloat.Length];
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort"))
                {
                    var workers = RadixSort.ParallelSort<uint>(valsUInt32, memu32);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort/descending"))
                {
                    var workers = RadixSort.ParallelSort<uint>(valsUInt32, memu32, descending: true);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSortDescending<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort"))
                {
                    RadixSortUnsafe.Sort(valsUInt32, wUint, mask: uint.MaxValue);
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
                {
                    RadixSortUnsafe.Sort(valsUInt32, wUint, descending: true, mask: uint.MaxValue);
                }
                CheckSortDescending<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("Array.Sort"))
                {
                    Array.Sort<uint>(valsUInt32);
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("Array.Sort+Array.Reverse"))
                {
                    Array.Sort<uint>(valsUInt32);
                    Array.Reverse<uint>(valsUInt32);
                }
                CheckSortDescending<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("Array.Sort/neg CompareTo"))
                {
                    Array.Sort<uint>(valsUInt32, (x, y) => y.CompareTo(x));
                }
                CheckSortDescending<uint>(valsUInt32);
            }
        }

        private static void CheckSort<T>(Span<T> vals) where T : struct, IComparable<T>
        {
            if (vals.Length <= 1) return;
            var prev = vals[0];
            for (int i = 1; i < vals.Length; i++)
            {
                var val = vals[i];
                if (val.CompareTo(prev) < 0) throw new InvalidOperationException($"not sorted: [{i - 1}] ({prev}) vs [{i}] ({val})");
                prev = val;
            }
        }
        private static void CheckSortDescending<T>(Span<T> vals) where T : struct, IComparable<T>
        {
            if (vals.Length <= 1) return;
            var prev = vals[0];
            for (int i = 1; i < vals.Length; i++)
            {
                var val = vals[i];
                if (val.CompareTo(prev) > 0) throw new InvalidOperationException($"not sorted: [{i - 1}] ({prev}) vs [{i}] ({val})");
                prev = val;
            }
        }
    }
}
