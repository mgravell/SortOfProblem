// #define USE_TIME

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SortOfDemo
{
    struct BasicTimer : IDisposable
    {
        Stopwatch _timer;
        string _message { get; }
        public BasicTimer(string message)
        {
            _message = message;
            Console.WriteLine($"> {message}...");
            _timer = Stopwatch.StartNew();
        }
        void IDisposable.Dispose()
        {
            _timer.Stop();
            Console.WriteLine($"< {_message}, {_timer.ElapsedMilliseconds}ms");
        }
    }
    class Program
    {

        static void Main()
        {
            try
            {
                Execute();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
        static void Execute()
        {
            Console.WriteLine($"Processor count: {Environment.ProcessorCount}");
#if USE_TIME
            Console.WriteLine($"Release date is to the second; some tests disabled");
#else
            Console.WriteLine($"Release date is to the day; all tests enabled (but less resolution on date)");
#endif
            SomeType[] data;
            DateTime[] releaseDates;
            ulong[] sortKeys, keysWorkspace;
            int[] index, valuesWorkspace;
            using (new BasicTimer("allocating"))
            {
                data = new SomeType[16 * 1024 * 1024];
                releaseDates = new DateTime[data.Length];
                sortKeys = new ulong[data.Length];
                keysWorkspace = new ulong[data.Length];
                index = new int[data.Length];
                valuesWorkspace = new int[data.Length];
            }

            Populate(data);
            try
            {
                CheckData(data);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("data is unsorted, as intended");
            }

            LINQ(data);
            ArraySortComparable(data);
            ArraySortComparer(data);
            ArraySortComparison(data);
            DualArrayDates(data, releaseDates);
            DualArrayComposite(data, sortKeys);
            DualArrayIndexed(data, index, sortKeys);
            DualArrayIndexedIntroSort(data, index, sortKeys);
            DualArrayIndexedRadixSort(data, index, sortKeys, keysWorkspace, valuesWorkspace, 2);
            DualArrayIndexedRadixSort(data, index, sortKeys, keysWorkspace, valuesWorkspace, 4);
            DualArrayIndexedRadixSort(data, index, sortKeys, keysWorkspace, valuesWorkspace, 8);
            DualArrayIndexedRadixSort(data, index, sortKeys, keysWorkspace, valuesWorkspace, 10);
            DualArrayIndexedRadixSort(data, index, sortKeys, keysWorkspace, valuesWorkspace, 16);
            DualArrayIndexedRadixSortParallel(data, index, sortKeys, keysWorkspace, valuesWorkspace, 2);
            DualArrayIndexedRadixSortParallel(data, index, sortKeys, keysWorkspace, valuesWorkspace, 4);
            DualArrayIndexedRadixSortParallel(data, index, sortKeys, keysWorkspace, valuesWorkspace, 8);
            DualArrayIndexedRadixSortParallel(data, index, sortKeys, keysWorkspace, valuesWorkspace, 10);
            DualArrayIndexedRadixSortParallel(data, index, sortKeys, keysWorkspace, valuesWorkspace, 16);
#if !USE_TIME
            ArraySortCombinedIndex(data, index, sortKeys);

            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 2);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 4);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 8);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 10);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 16);

            const ulong mask = (ulong.MaxValue) << 24;
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 2, mask);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 4, mask);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 8, mask);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 10, mask);
            RadixSortCombinedIndex(data, index, sortKeys, keysWorkspace, 16, mask);
#endif
        }



        static void CheckData(SomeType[] data, bool asFloat = false)
        {
            using (new BasicTimer("checking order"))
            {
                for (int i = 1; i < data.Length; i++)
                {
                    AssertOrdered(in data[i - 1], in data[i], asFloat);
                }
            }
        }

        static void CheckData(SomeType[] data, int[] index, bool asFloat = false)
        {
            using (new BasicTimer("checking order"))
            {
                for (int i = 1; i < data.Length; i++)
                {
                    AssertOrdered(in data[index[i - 1]], in data[index[i]], asFloat);
                }
            }
        }
        static void AssertOrdered(in SomeType x, in SomeType y, bool asFloat)
        {
            if (x.ReleaseDate > y.ReleaseDate) return;
            if (asFloat)
            {
                if (x.ReleaseDate == y.ReleaseDate && (float)x.Price <= (float)y.Price) return;
            }
            else
            {
                if (x.ReleaseDate == y.ReleaseDate && x.Price <= y.Price) return;
            }

            throw new InvalidOperationException($"incorrect sort; {x.ReleaseDate}/{x.Price} [{Sortable(x)}] vs {y.ReleaseDate}/{y.Price} [{Sortable(y)}]");
        }
        static string Me([CallerMemberName] string caller = null) => caller;

        static void LINQ(SomeType[] data)
        {
            SomeType[] sorted;
            using (new BasicTimer(Me()))
            {
                sorted = (from item in data
                          orderby item.ReleaseDate descending,
                                  item.Price
                          select item).ToArray();
            }
            CheckData(sorted);
        }

        static void ArraySortComparable(SomeType[] data)
        {
            using (new BasicTimer(Me()))
            {
                Array.Sort<SomeType>(data);
            }
            CheckData(data);
            Populate(data); // need to re-invent
        }

        static void ArraySortComparer(SomeType[] data)
        {
            using (new BasicTimer(Me()))
            {
                Array.Sort<SomeType>(data, SomeTypeComparer.Default);
            }
            CheckData(data);
            Populate(data); // need to re-invent
        }

        static void ArraySortComparison(SomeType[] data)
        {
            using (new BasicTimer(Me()))
            {
                Array.Sort<SomeType>(data, (x, y) =>
                {
                    var delta = y.ReleaseDate
                            .CompareTo(x.ReleaseDate);
                    if (delta == 0) // second property
                        delta = x.Price.CompareTo(y.Price);
                    return delta;
                });
            }
            CheckData(data);
            Populate(data); // need to re-invent
        }

        private static void DualArrayDates(SomeType[] data, DateTime[] releaseDates)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                    releaseDates[i] = data[i].ReleaseDate;
            }
            using (new BasicTimer(Me() + " sort"))
            {
                Array.Sort(releaseDates, data);
            }
            // no point checking the data; we know it is wrong
            Populate(data); // need to re-invent
        }

        static ulong Sortable(in SomeType item)
        {
            return (~(ulong)item.ReleaseDate.ToMillenialTime()) << 32
                        | Sortable((float)item.Price);
        }
        private static void DualArrayComposite(SomeType[] data, ulong[] sortKeys)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                    sortKeys[i] = Sortable(in data[i]);
            }
            using (new BasicTimer(Me() + " sort"))
            {
                Array.Sort(sortKeys, data);
            }
            CheckData(data, asFloat: true);
            Populate(data); // need to re-invent
        }

        private static void DualArrayIndexed(SomeType[] data, int[] index, ulong[] sortKeys)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    index[i] = i;
                    sortKeys[i] = Sortable(in data[i]);
                }
            }
            using (new BasicTimer(Me() + " sort"))
            {
                Array.Sort(sortKeys, index);
            }
            CheckData(data, index, asFloat: true);
            // no need to re-invent
        }

        private static void DualArrayIndexedIntroSort(SomeType[] data, int[] index, ulong[] sortKeys)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    index[i] = i;
                    sortKeys[i] = Sortable(in data[i]);
                }
            }
            using (new BasicTimer(Me() + " sort"))
            {
                Helpers.IntroSort(sortKeys, index);
            }
            CheckData(data, index, asFloat: true);
            // no need to re-invent
        }

        private static void DualArrayIndexedRadixSort(SomeType[] data, int[] index, ulong[] sortKeys, ulong[] keysWorkspace, int[] valuesWorkspace, int r = 4)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    index[i] = i;
                    sortKeys[i] = Sortable(in data[i]);
                }
            }
            using (new BasicTimer(Me() + " sort, r=" + r))
            {
                Helpers.RadixSort(sortKeys, index, keysWorkspace, valuesWorkspace, r);
            }
            CheckData(data, index, asFloat: true);
            // no need to re-invent
        }

        private static void DualArrayIndexedRadixSortParallel(SomeType[] data, int[] index, ulong[] sortKeys, ulong[] keysWorkspace, int[] valuesWorkspace, int r = 4)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    index[i] = i;
                    sortKeys[i] = Sortable(in data[i]);
                }
            }
            using (new BasicTimer(Me() + " sort, r=" + r))
            {
                Helpers.RadixSortParallel(sortKeys, index, keysWorkspace, valuesWorkspace, r);
            }
            CheckData(data, index, asFloat: true);
            // no need to re-invent
        }

        static ulong CombinedKey(in SomeType d, int i)
        {
            var key = 0ul;

            // Encode date (most important field first, inverted/descending sort)
            // Use 366 days in a year to allow leap years (or find out num days between 2005-2055)
            key += (50 * 366 - 1) - (ulong)(d.ReleaseDate - Epoch).TotalDays;

            // Encode price
            key *= 50_000_000;
            key += (ulong)(d.Price * 1000);

            // Encode index (must be in LSBs)
            // Including index in sortkey also guarantees unique keys, which could be used in optimized sort
            key *= (1 << 24);
            key += (ulong)i;
            return key;
        }
        static void ArraySortCombinedIndex(SomeType[] data, int[] index, ulong[] sortKeys)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    sortKeys[i] = CombinedKey(in data[i], i);
                }
            }
            using (new BasicTimer(Me() + " sort"))
            {
                Array.Sort(sortKeys);
            }
            using (new BasicTimer(Me() + " index recovery"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    index[i] = (int)(sortKeys[i] & ((1 << 24) - 1));
                }
            }
            CheckData(data, index);
        }

        static int HammingWeight(ulong i)
        {
            i = i - ((i >> 1) & 0x5555555555555555UL);
            i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }
        static void RadixSortCombinedIndex(SomeType[] data, int[] index, ulong[] sortKeys, ulong[] keysWorkspace, int r, ulong keyMask = ulong.MaxValue)
        {
            using (new BasicTimer(Me() + " prepare"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    sortKeys[i] = CombinedKey(in data[i], i);
                }
            }
            using (new BasicTimer(Me() + " sort, r=" + r + ", bits: " + HammingWeight(keyMask)))
            {
                Helpers.RadixSort(sortKeys, keysWorkspace, r, keyMask);
            }
            using (new BasicTimer(Me() + " index recovery"))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    index[i] = (int)(sortKeys[i] & ((1 << 24) - 1));
                }
            }
            CheckData(data, index);
        }

        static readonly DateTime Epoch = new DateTime(
            2005, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static void Populate(SomeType[] data)
        {
            using (new BasicTimer("inventing data"))
            {
                var rand = new Random(data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    int id = rand.Next();
                    var releaseDate = Epoch
                        .AddYears(rand.Next(50))
                        .AddDays(rand.Next(365))
#if USE_TIME
                        .AddSeconds(rand.Next(24 * 60 * 60))
#endif
                        ;
                    var price = rand.Next(50_000_000) / 1_000d;
                    data[i] = new SomeType(
                        id, releaseDate, price);
                }
            }
        }

        protected static ulong Sortable(int value)
        {
            // re-base eveything upwards, so anything
            // that was the min-value is now 0, etc
            var val = unchecked((uint)(value - int.MinValue));
            return val;
        }

        protected static unsafe ulong Sortable(float value)
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
    }



    readonly partial struct SomeType
    {
        public int Id { get; }
        public DateTime ReleaseDate { get; }
        public double Price { get; }

        public SomeType(int id, DateTime releaseDate, double price)
        {
            Id = id;
            ReleaseDate = releaseDate;
            Price = price;
            _some = _other = _stuff = _not = _shown = 0;
        }

#pragma warning disable CS0414 // suppress "assigned, never used"
        private readonly long _some, _other, _stuff, _not, _shown;
#pragma warning restore CS0414
    }

    partial struct SomeType : IComparable<SomeType>
    {
        int IComparable<SomeType>.CompareTo(SomeType other)
        {
            var delta = other.ReleaseDate
                .CompareTo(this.ReleaseDate);
            if (delta == 0) // second property
                delta = this.Price.CompareTo(other.Price);
            return delta;
        }
    }

    sealed class SomeTypeComparer : IComparer<SomeType>
    {
        private SomeTypeComparer() { }
        public static SomeTypeComparer Default { get; } = new SomeTypeComparer();
        int IComparer<SomeType>.Compare(SomeType x, SomeType y)
        {
            var delta = y.ReleaseDate
                    .CompareTo(x.ReleaseDate);
            if (delta == 0) // second property
                delta = x.Price.CompareTo(y.Price);
            return delta;
        }
    }

    static unsafe class Helpers
    {

        private static DateTime
                Millenium = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                MillenialMinValue = Millenium.AddSeconds(2),
                MillenialMaxValue = Millenium.AddSeconds(uint.MaxValue - 1);

        public static uint ToMillenialTime(this DateTime value)
        {
            if (value == default(DateTime)) return 0;

            if (value < MillenialMinValue || value > MillenialMaxValue) throw new ArgumentOutOfRangeException(
                nameof(value), $"{value} outside the range {MillenialMinValue} to {MillenialMaxValue}");

            return (uint)(value - Millenium).TotalSeconds;
        }

        public static void RadixSort(ulong[] keys,ulong[] keysWorkspace, int r = 4, ulong keyMask = ulong.MaxValue)
        {
            if (keyMask == 0) return;
            fixed (ulong* k = keys)
            fixed (ulong* kw = keysWorkspace)
            {
                RadixSort(k, kw, keys.Length, r, keyMask);
            }
        }

        public static void RadixSort(ulong[] keys, int[] values, ulong[] keysWorkspace, int[] valuesWorkspace, int r = 4, ulong keyMask = ulong.MaxValue)
        {
            if (keyMask == 0) return;
            fixed (ulong* k = keys)
            fixed (int* v = values)
            fixed (ulong* kw = keysWorkspace)
            fixed (int* vw = valuesWorkspace)
            {
                RadixSort(k, v, kw, vw, Math.Min(keys.Length, values.Length), r, keyMask);
            }
        }
        private static unsafe void RadixSort(ulong* keys, int* values, ulong* keysWorkspace, int* valuesWorkspace,
            int len, int r, ulong keyMask)
        {
            // number of bits in the keys
            const int b = sizeof(ulong) * 8;

            bool swapped = false;
            // counting and prefix arrays
            // (note dimensions 2^r which is the number of all possible values of a r-bit number) 
            int CountLength = 1 << r;
            int* count = stackalloc int[CountLength];
            int* pref = stackalloc int[CountLength];

            // number of groups 
            int groups = (int)Math.Ceiling(b / (double)r);

            // the mask to identify groups 
            ulong mask = (1UL << r) - 1;

            // the algorithm: 
            for (int c = 0, shift = 0; c < groups; c++, shift += r)
            {
                ulong groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                // reset count array 
                for (int j = 0; j < CountLength; j++)
                    count[j] = 0;

                // counting elements of the c-th group 
                for (int i = 0; i < len; i++)
                    count[(keys[i] >> shift) & groupMask]++;

                // calculating prefixes 
                pref[0] = 0;
                for (int i = 1; i < CountLength; i++)
                {
                    int groupCount = count[i - 1];
                    if (groupCount == len) goto NextLoop; // all in one group
                    pref[i] = pref[i - 1] + groupCount;
                }
                if (count[CountLength - 1] == len) goto NextLoop; // all in one group

                // from a[] to t[] elements ordered by c-th group 
                for (int i = 0; i < len; i++)
                {
                    int j = pref[(keys[i] >> shift) & groupMask]++;
                    keysWorkspace[j] = keys[i];
                    valuesWorkspace[j] = values[i];
                }

                // a[]=t[] and start again until the last group

                // swap the pointers for the next iteration - so we use the "keys"
                // as the "keysWorkspace" on the 2nd/4th/6th loops
                var tmp0 = keys;
                keys = keysWorkspace;
                keysWorkspace = tmp0;

                var tmp1 = values;
                values = valuesWorkspace;
                valuesWorkspace = tmp1;

                swapped = !swapped;

NextLoop:
                ;
            }
            // a is sorted

            if (swapped)
            {
                Unsafe.CopyBlock(keysWorkspace, keys, (uint)(len * sizeof(ulong)));
                Unsafe.CopyBlock(valuesWorkspace, values, (uint)(len * sizeof(int)));
            }
        }

        private static unsafe void RadixSort(ulong* keys, ulong* keysWorkspace, int len, int r, ulong keyMask)
        {
            // number of bits in the keys
            const int b = sizeof(ulong) * 8;

            bool swapped = false;
            // counting and prefix arrays
            // (note dimensions 2^r which is the number of all possible values of a r-bit number) 
            int CountLength = 1 << r;
            int* count = stackalloc int[CountLength];
            int* pref = stackalloc int[CountLength];

            // number of groups 
            int groups = (int)Math.Ceiling(b / (double)r);

            // the mask to identify groups 
            ulong mask = (1UL << r) - 1;

            // the algorithm: 
            for (int c = 0, shift = 0; c < groups; c++, shift += r)
            {
                ulong groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                // reset count array 
                for (int j = 0; j < CountLength; j++)
                    count[j] = 0;

                // counting elements of the c-th group 
                for (int i = 0; i < len; i++)
                    count[(keys[i] >> shift) & groupMask]++;

                // calculating prefixes 
                pref[0] = 0;
                for (int i = 1; i < CountLength; i++)
                {
                    int groupCount = count[i - 1];
                    if (groupCount == len) goto NextLoop; // all in one group
                    pref[i] = pref[i - 1] + groupCount;
                }
                if (count[CountLength - 1] == len) goto NextLoop; // all in one group

                // from a[] to t[] elements ordered by c-th group 
                for (int i = 0; i < len; i++)
                {
                    int j = pref[(keys[i] >> shift) & groupMask]++;
                    keysWorkspace[j] = keys[i];
                }

                // a[]=t[] and start again until the last group

                // swap the pointers for the next iteration - so we use the "keys"
                // as the "keysWorkspace" on the 2nd/4th/6th loops
                var tmp0 = keys;
                keys = keysWorkspace;
                keysWorkspace = tmp0;

                swapped = !swapped;

NextLoop:
                ;
            }
            // a is sorted

            if (swapped)
            {
                Unsafe.CopyBlock(keysWorkspace, keys, (uint)(len * sizeof(ulong)));
            }
        }


        unsafe class Worker
        {
            public int* Counts { get; set; }
            public int CountLength { get; set; }
            public ulong Mask { get; set; }
            public ulong* Keys { get; set; }
            public int Length { get; set; }
            public int Offset { get; set; }
            public int Shift { get; set; }
            public object SyncLock { get; set; }
            public void Invoke()
            {
                var len = Length;
                var mask = Mask;
                var keys = Keys + Offset;
                var shift = Shift;
                int* count = stackalloc int[CountLength];

                // count into a local buffer
                for (int i = 0; i < len; i++)
                    count[(*keys++ >> shift) & mask]++;

                // now update the origin data, synchronized
                lock (SyncLock)
                {
                    for (int i = 0; i < CountLength; i++)
                        Counts[i] += count[i];
                }
            }
        }

        public static void RadixSortParallel(ulong[] keys, int[] values, ulong[] keysWorkspace, int[] valuesWorkspace, int r = 4)
        {
            fixed (ulong* k = keys)
            fixed (int* v = values)
            fixed (ulong* kw = keysWorkspace)
            fixed (int* vw = valuesWorkspace)
            {
                RadixSortParallel(k, v, kw, vw, Math.Min(keys.Length, values.Length), r);
            }
        }
        private static unsafe void RadixSortParallel(ulong* keys, int* values, ulong* keysWorkspace, int* valuesWorkspace,
    int len, int r = 4)
        {
            // number of bits our group will be long 

            // number of bits in the keys
            const int b = sizeof(ulong) * 8;

            // counting and prefix arrays
            // (note dimensions 2^r which is the number of all possible values of a r-bit number) 
            int CountLength = 1 << r;
            int* count = stackalloc int[CountLength];
            int* pref = stackalloc int[CountLength];

            // number of groups 
            int groups = (int)Math.Ceiling(b / (double)r);

            // the mask to identify groups 
            ulong mask = (1UL << r) - 1;

            // configure our workers
            int workerCount = Environment.ProcessorCount * 2;
            int blockSize = len / workerCount;
            if ((len % workerCount) != 0) blockSize++;

            var workers = new Worker[workerCount];
            var workerInvoke = new Action[workerCount];
            int remaining = len;
            object syncLock = null;
            int offset = 0;
            for (int i = 0; i < workerCount; i++)
            {
                var lenThisBlock = Math.Min(blockSize, remaining);
                remaining -= lenThisBlock;
                var worker = new Worker
                {
                    Counts = count,
                    CountLength = CountLength,
                    Length = lenThisBlock,
                    Mask = mask,
                    Offset = offset
                };
                offset += lenThisBlock;
                if (i == 0) syncLock = worker; // use the first worker as the lock
                worker.SyncLock = syncLock;
                workers[i] = worker;
                workerInvoke[i] = worker.Invoke;
            }
            Debug.Assert(remaining == 0, "Failed to calculate block sizes correctly");

            // the algorithm:
            bool swapped = false;
            for (int c = 0, shift = 0; c < groups; c++, shift += r)
            {
                // reset count array 
                for (int j = 0; j < CountLength; j++)
                    count[j] = 0;

                // set the shift on the workers
                foreach (var worker in workers)
                {
                    worker.Shift = shift;
                    worker.Keys = keys;
                }

                // counting elements of the c-th group
                Parallel.Invoke(workerInvoke);

                // calculating prefixes 
                pref[0] = 0;
                for (int i = 1; i < CountLength; i++)
                    pref[i] = pref[i - 1] + count[i - 1];

                // from a[] to t[] elements ordered by c-th group 
                for (int i = 0; i < len; i++)
                {
                    int j = pref[(keys[i] >> shift) & mask]++;
                    keysWorkspace[j] = keys[i];
                    valuesWorkspace[j] = values[i];
                }

                // a[]=t[] and start again until the last group

                // swap the pointers for the next iteration - so we use the "keys"
                // as the "keysWorkspace" on the 2nd/4th/6th loops
                var tmp0 = keys;
                keys = keysWorkspace;
                keysWorkspace = tmp0;

                var tmp1 = values;
                values = valuesWorkspace;
                valuesWorkspace = tmp1;

                swapped = !swapped;
            }
            // a is sorted 

            if (swapped)
            {
                Unsafe.CopyBlock(keysWorkspace, keys, (uint)(len * sizeof(ulong)));
                Unsafe.CopyBlock(valuesWorkspace, values, (uint)(len * sizeof(int)));
            }
        }
        
        // borrowed from core-clr, with massive special-casing
        // https://github.com/dotnet/coreclr/blob/775003a4c72f0acc37eab84628fcef541533ba4e/src/mscorlib/src/System/Array.cs
        internal static void Sort(ulong* keys, int* values, int count)
        {
            if (count < 2) return;
            // note: remove SZSort call - doesn't apply: https://github.com/dotnet/coreclr/blob/775003a4c72f0acc37eab84628fcef541533ba4e/src/classlibnative/bcltype/arrayhelpers.cpp#L290
            IntrospectiveSort(keys, values, 0, count);
        }

        public static void IntroSort(ulong[] keys, int[] values)
        {
            fixed (ulong* k = keys)
            fixed (int* v = values)
            {
                Sort(k, v, Math.Min(keys.Length, values.Length));
            }
        }

        // borrowed from core-clr, with massive special-casing
        // https://github.com/dotnet/coreclr/blob/775003a4c72f0acc37eab84628fcef541533ba4e/src/mscorlib/src/System/Collections/Generic/ArraySortHelper.cs
        internal static void IntrospectiveSort(ulong* keys, int* values, int left, int length)
        {
            if (length < 2)
                return;

            IntroSort(keys, values, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(length));
        }

        private static void IntroSort(ulong* keys, int* values, int lo, int hi, int depthLimit)
        {
            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, values, lo, hi);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, values, lo, hi - 1);
                        SwapIfGreaterWithItems(keys, values, lo, hi);
                        SwapIfGreaterWithItems(keys, values, hi - 1, hi);
                        return;
                    }

                    InsertionSort(keys, values, lo, hi);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, values, lo, hi);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys, values, lo, hi);
                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys, values, p + 1, hi, depthLimit);
                hi = p - 1;
            }
        }
        private static void DownHeap(ulong* keys, int* values, int i, int n, int lo)
        {
            ulong d = keys[lo + i - 1];
            int dValue = values[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && keys[lo + child - 1] < keys[lo + child])
                {
                    child++;
                }
                if (keys[lo + child - 1] < d)
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }
            keys[lo + i - 1] = d;
            values[lo + i - 1] = dValue;
        }

        private static void Swap(ulong* keys, int* values, int i, int j)
        {
            if (i != j)
            {
                ulong k = keys[i];
                keys[i] = keys[j];
                keys[j] = k;

                int v = values[i];
                values[i] = values[j];
                values[j] = v;
            }
        }
        private static void Heapsort(ulong* keys, int* values, int lo, int hi)
        {
            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, values, i, n, lo);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, values, lo, lo + i - 1);
                DownHeap(keys, values, 1, i - 1, lo);
            }
        }

        private static int PickPivotAndPartition(ulong* keys, int* values, int lo, int hi)
        {
            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = lo + ((hi - lo) / 2);

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithItems(keys, values, lo, middle);  // swap the low with the mid point
            SwapIfGreaterWithItems(keys, values, lo, hi);   // swap the low with the high
            SwapIfGreaterWithItems(keys, values, middle, hi); // swap the middle with the high

            ulong pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (pivot > keys[++left]) ;
                while (pivot < keys[--right]) ;

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            Swap(keys, values, left, (hi - 1));
            return left;
        }

        private static void InsertionSort(ulong* keys, int* values, int lo, int hi)
        {
            int i, j;
            ulong t;
            int tValue;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                tValue = values[i + 1];
                while (j >= lo && t < keys[j])
                {
                    keys[j + 1] = keys[j];
                    values[j + 1] = values[j];
                    j--;
                }
                keys[j + 1] = t;
                values[j + 1] = tValue;
            }
        }
        private static void SwapIfGreaterWithItems(ulong* keys, int* values, int a, int b)
        {
            if (a != b && keys[a] > keys[b])
            {
                ulong key = keys[a];
                keys[a] = keys[b];
                keys[b] = key;

                int value = values[a];
                values[a] = values[b];
                values[b] = value;
            }
        }
        internal static class IntrospectiveSortUtilities
        {
            // This is the threshold where Introspective sort switches to Insertion sort.
            // Imperically, 16 seems to speed up most cases without slowing down others, at least for integers.
            // Large value types may benefit from a smaller number.
            internal const int IntrosortSizeThreshold = 16;

            internal const int QuickSortDepthThreshold = 32;

            internal static int FloorLog2(int n)
            {
                int result = 0;
                while (n >= 1)
                {
                    result++;
                    n = n / 2;
                }
                return result;
            }
        }

    }
}
