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

        //static void Execute()
        //{
        //    LsdRadixSort.MaxWorkerCount = 1;
        //    var x = new float[] { 7, 0, -4, 12, }; // 9, 1, 0, 1, 4, 511, 2 };
        //    //var r = new Random(x.Length);
        //    //for (int i = 0; i < x.Length; i++)
        //    //    x[i] = (uint)r.Next();
        //    var y = new float[x.Length];
        //    LsdRadixSort.ParallelSort<float>(x, y);
        //    CheckSort<float>(x);
        //}
        static void Execute()
        {

            var rand = new Random(12345);
#if DEBUG
            const int LOOP = 1, DATA_COUNT = 16 * 1024;
#else
            const int LOOP = 4, DATA_COUNT = 16 * 1024 * 1024;
#endif
            float[] origFloat = new float[DATA_COUNT], valsFloat = new float[origFloat.Length];
            uint[] origUInt32 = new uint[origFloat.Length], valsUInt32 = new uint[origFloat.Length];
            int[] origInt32 = new int[origFloat.Length], valsInt32 = new int[origFloat.Length];

            ulong[] origUInt64 = new ulong[origFloat.Length], valsUInt64 = new ulong[origFloat.Length];
            long[] origInt64 = new long[origFloat.Length], valsInt64 = new long[origFloat.Length];
            double[] origDouble = new double[origFloat.Length], valsDouble = new double[origFloat.Length];

            for (int i = 0; i < origFloat.Length; i++)
            {
                var x = ((rand.NextDouble() * 50000) - 15000);
                origDouble[i] = x;
                origFloat[i] = (float)x;
                int ival = rand.Next(int.MinValue, int.MaxValue);
                origInt32[i] = ival;
                origUInt32[i] = unchecked((uint)ival);
                long lo = rand.Next(int.MinValue, int.MaxValue), hi = rand.Next(int.MinValue, int.MaxValue);
                origInt64[i] = ((hi << 32) | lo);
                origUInt64[i] = (ulong)((hi << 32) | lo);
            }
            var wFloat = new float[origFloat.Length];
            var w64 = new ulong[origFloat.Length];
            var wDouble = new double[origFloat.Length];
            Console.WriteLine($"Workspace length: {wFloat.Length}");


            //Console.WriteLine();
            //Console.WriteLine(">> ulong <<");
            //Console.WriteLine();
            //var wUInt64 = new Span<ulong>(w64);
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origUInt64.CopyTo(valsUInt64, 0);
            //    using (new BasicTimer("RadixSort.Sort/64"))
            //    {
            //        LsdRadixSort.Sort<ulong>(valsUInt64, wUInt64);
            //    }
            //    CheckSort<ulong>(valsUInt64);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origUInt64.CopyTo(valsUInt64, 0);
            //    using (new BasicTimer("RadixSort.Sort/64/descending"))
            //    {
            //        LsdRadixSort.Sort<ulong>(valsUInt64, wUInt64, descending: true);
            //    }
            //    CheckSortDescending<ulong>(valsUInt64);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origUInt64.CopyTo(valsUInt64, 0);
            //    using (new BasicTimer("Array.Sort/64"))
            //    {
            //        Array.Sort<ulong>(valsUInt64);
            //    }
            //    CheckSort<ulong>(valsUInt64);
            //}

            //Console.WriteLine();
            //Console.WriteLine(">> long <<");
            //Console.WriteLine();
            //var wInt64 = wUInt64.NonPortableCast<ulong, long>();
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt64.CopyTo(valsInt64, 0);
            //    using (new BasicTimer("RadixSort.Sort/64"))
            //    {
            //        LsdRadixSort.Sort<long>(valsInt64, wInt64);
            //    }
            //    CheckSort<long>(valsInt64);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt64.CopyTo(valsInt64, 0);
            //    using (new BasicTimer("RadixSort.Sort/64/descending"))
            //    {
            //        LsdRadixSort.Sort<long>(valsInt64, wInt64, descending: true);
            //    }
            //    CheckSortDescending<long>(valsInt64);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt64.CopyTo(valsInt64, 0);
            //    using (new BasicTimer("Array.Sort/64"))
            //    {
            //        Array.Sort<long>(valsInt64);
            //    }
            //    CheckSort<long>(valsInt64);
            //}

            //Console.WriteLine();
            //Console.WriteLine(">> double <<");
            //Console.WriteLine();
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origDouble.CopyTo(valsDouble, 0);
            //    using (new BasicTimer("RadixSort.Sort"))
            //    {
            //        LsdRadixSort.Sort<double>(valsDouble, wDouble);
            //    }
            //    CheckSort<double>(valsDouble);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origDouble.CopyTo(valsDouble, 0);
            //    using (new BasicTimer("RadixSort.Sort/descending"))
            //    {
            //        LsdRadixSort.Sort<double>(valsDouble, wDouble, descending: true);
            //    }
            //    CheckSortDescending<double>(valsDouble);
            //}
            ////for (int i = 0; i < LOOP; i++)
            ////{
            ////    origDouble.CopyTo(valsDouble, 0);
            ////    using (new BasicTimer("RadixSort.ParallelSort"))
            ////    {
            ////        LsdRadixSort.ParallelSort<double>(valsDouble, wDouble);
            ////    }
            ////    CheckSort<double>(valsDouble);
            ////}
            ////for (int i = 0; i < LOOP; i++)
            ////{
            ////    origDouble.CopyTo(valsDouble, 0);
            ////    using (new BasicTimer("RadixSort.ParallelSort/descending"))
            ////    {
            ////        LsdRadixSort.ParallelSort<double>(valsDouble, wDouble, descending: true);
            ////    }
            ////    CheckSortDescending<double>(valsDouble);
            ////}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origDouble.CopyTo(valsDouble, 0);
            //    using (new BasicTimer("Array.Sort"))
            //    {
            //        Array.Sort<double>(valsDouble);
            //    }
            //    CheckSort<double>(valsDouble);
            //}

            Console.WriteLine();
            Console.WriteLine(">> float <<");
            Console.WriteLine();
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("RadixSort.Sort"))
                {
                    LsdRadixSort.Sort<float>(valsFloat, wFloat);
                }
                CheckSort<float>(valsFloat);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("RadixSort.Sort/descending"))
                {
                    LsdRadixSort.Sort<float>(valsFloat, wFloat, descending: true);
                }
                CheckSortDescending<float>(valsFloat);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("RadixSort.ParallelSort"))
                {
                    var workers = LsdRadixSort.ParallelSort<float>(valsFloat, wFloat);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSort<float>(valsFloat);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origFloat.CopyTo(valsFloat, 0);
                using (new BasicTimer("RadixSort.ParallelSort/descending"))
                {
                    var workers = LsdRadixSort.ParallelSort<float>(valsFloat, wFloat, descending: true);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSortDescending<float>(valsFloat);
            }
            ////for (int i = 0; i < LOOP; i++)
            ////{
            ////    origFloat.CopyTo(valsFloat, 0);
            ////    using (new BasicTimer("RadixSortUnsafe.Sort"))
            ////    {
            ////        RadixSortUnsafe.Sort<float>(valsFloat, wFloat);
            ////    }
            ////    CheckSort<float>(valsFloat);
            ////}
            ////for (int i = 0; i < LOOP; i++)
            ////{
            ////    origFloat.CopyTo(valsFloat, 0);
            ////    using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
            ////    {
            ////        RadixSortUnsafe.Sort<float>(valsFloat, wFloat, descending: true);
            ////    }
            ////    CheckSortDescending<float>(valsFloat);
            ////}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("Array.Sort"))
            //    {
            //        Array.Sort<float>(valsFloat);
            //    }
            //    CheckSort<float>(valsFloat);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("Array.Sort+Array.Reverse"))
            //    {
            //        Array.Sort<float>(valsFloat);
            //        Array.Reverse<float>(valsFloat);
            //    }
            //    CheckSortDescending<float>(valsFloat);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origFloat.CopyTo(valsFloat, 0);
            //    using (new BasicTimer("Array.Sort/neg CompareTo"))
            //    {
            //        Array.Sort<float>(valsFloat, (x, y) => y.CompareTo(x));
            //    }
            //    CheckSortDescending<float>(valsFloat);
            //}



            Console.WriteLine();
            Console.WriteLine($">> int <<");
            Console.WriteLine();
            var wInt = new Span<float>(wFloat).NonPortableCast<float, int>();
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.Sort"))
                {
                    LsdRadixSort.Sort<int>(valsInt32, wInt);
                }
                CheckSort<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.Sort/descending"))
                {
                    LsdRadixSort.Sort<int>(valsInt32, wInt, descending: true);
                }
                CheckSortDescending<int>(valsInt32);
            }
            int[] mem32 = new int[wFloat.Length];
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort"))
                {
                    var workers = LsdRadixSort.ParallelSort<int>(valsInt32, mem32);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSort<int>(valsInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origInt32.CopyTo(valsInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort/descending"))
                {
                    var workers = LsdRadixSort.ParallelSort<int>(valsInt32, mem32, descending: true);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSortDescending<int>(valsInt32);
            }
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt32.CopyTo(valsInt32, 0);
            //    using (new BasicTimer("RadixSortUnsafe.Sort"))
            //    {
            //        LsdRadixSortUnsafe.Sort<int>(valsInt32, wInt);
            //    }
            //    CheckSort<int>(valsInt32);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt32.CopyTo(valsInt32, 0);
            //    using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
            //    {
            //        LsdRadixSortUnsafe.Sort<int>(valsInt32, wInt, descending: true);
            //    }
            //    CheckSortDescending<int>(valsInt32);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt32.CopyTo(valsInt32, 0);
            //    using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
            //    {
            //        LsdRadixSortUnsafe.Sort<int>(valsInt32, wInt, descending: true);
            //    }
            //    CheckSortDescending<int>(valsInt32);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt32.CopyTo(valsInt32, 0);
            //    using (new BasicTimer("Array.Sort"))
            //    {
            //        Array.Sort<int>(valsInt32);
            //    }
            //    CheckSort<int>(valsInt32);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt32.CopyTo(valsInt32, 0);
            //    using (new BasicTimer("Array.Sort+Array.Reverse"))
            //    {
            //        Array.Sort<int>(valsInt32);
            //        Array.Reverse<int>(valsInt32);
            //    }
            //    CheckSortDescending<int>(valsInt32);
            //}
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origInt32.CopyTo(valsInt32, 0);
            //    using (new BasicTimer("Array.Sort/neg CompareTo"))
            //    {
            //        Array.Sort<int>(valsInt32, (x, y) => y.CompareTo(x));
            //    }
            //    CheckSortDescending<int>(valsInt32);
            //}


            Console.WriteLine();
            Console.WriteLine(">> uint <<");
            Console.WriteLine();
            var wUint = new Span<float>(wFloat).NonPortableCast<float, uint>();
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.Sort"))
                {
                    LsdRadixSort.Sort<uint>(valsUInt32, wUint);
                }
                CheckSort<uint>(valsUInt32);
                using (new BasicTimer("RadixSort.Sort (sorted)"))
                {
                    LsdRadixSort.Sort<uint>(valsUInt32, wUint);
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("MSD RadixSort.Sort"))
                {
                    MsdRadixSort.Sort<uint>(valsUInt32, wUint);
                }
                CheckSort<uint>(valsUInt32);
                using (new BasicTimer("MSD RadixSort.Sort (sorted)"))
                {
                    MsdRadixSort.Sort<uint>(valsUInt32, wUint);
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                var slice = new Span<uint>(valsUInt32, 0, 2048);
                using (new BasicTimer("RadixSort.SortSmall (on stack)"))
                {
                    LsdRadixSort.SortSmall<uint>(slice);
                }
                CheckSort<uint>(slice);
                using (new BasicTimer("RadixSort.SortSmall (on stack, sorted)"))
                {
                    LsdRadixSort.SortSmall<uint>(slice);
                }
                CheckSort<uint>(slice);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.Sort/descending"))
                {
                    LsdRadixSort.Sort<uint>(valsUInt32, wUint, descending: true);
                }
                CheckSortDescending<uint>(valsUInt32);
            }
            //for (int i = 0; i < LOOP; i++)
            //{
            //    origUInt32.CopyTo(valsUInt32, 0);
            //    using (new BasicTimer("MSD RadixSort.Sort/descending"))
            //    {
            //        MsdRadixSort.Sort<uint>(valsUInt32, wUint, descending: true);
            //    }
            //    CheckSortDescending<uint>(valsUInt32);
            //}
            var memu32 = new uint[wFloat.Length];
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort"))
                {
                    var workers = LsdRadixSort.ParallelSort<uint>(valsUInt32, memu32);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSort.ParallelSort/descending"))
                {
                    var workers = LsdRadixSort.ParallelSort<uint>(valsUInt32, memu32, descending: true);
                    Console.Write($" using {workers} worker(s)");
                }
                CheckSortDescending<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort"))
                {
                    LsdRadixSortUnsafe.Sort(valsUInt32, wUint, mask: uint.MaxValue);
                }
                CheckSort<uint>(valsUInt32);
            }
            for (int i = 0; i < LOOP; i++)
            {
                origUInt32.CopyTo(valsUInt32, 0);
                using (new BasicTimer("RadixSortUnsafe.Sort/descending"))
                {
                    LsdRadixSortUnsafe.Sort(valsUInt32, wUint, descending: true, mask: uint.MaxValue);
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
