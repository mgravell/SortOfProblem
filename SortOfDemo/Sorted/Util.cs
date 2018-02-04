using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sorted
{
    internal static class Util
    {
        internal const sbyte MSB8 = unchecked((sbyte)(1 << 7));
        internal const byte MSB8U = 1 << 7;
        internal const short MSB16 = unchecked((short)(1 << 15));
        internal const ushort MSB16U = 1 << 15;
        internal const int MSB32 = 1 << 31;
        internal const uint MSB32U = 1U << 31;
        internal const long MSB64 = 1L << 63;
        internal const ulong MSB64U = 1UL << 63;

        internal static int WorkerCount(int count, int maxCount)
        {
            int groupCount = ((count - 1) / 1024) + 1;
            if (groupCount <= 1) return 1;

            if (maxCount <= 0 || maxCount > _processorCount)
                maxCount = _processorCount;
            return Math.Min(groupCount, maxCount);
        }
        private readonly static int _processorCount = Environment.ProcessorCount;

        internal static int ChooseBitCount<T>(int r, int @default)
        {
            var tBits = Unsafe.SizeOf<T>() << 3;
            if (r > tBits) r = tBits;
            if (r < 1) return ChooseBitCount<T>(@default, 8);
            if (r > 16) return 16;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountAscending8(Span<uint> buckets, Span<byte> keys, int start, int end, int shift, byte groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountDescending8(Span<uint> buckets, Span<byte> keys, int start, int end, int shift, byte groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((~keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountAscending16(Span<uint> buckets, Span<ushort> keys, int start, int end, int shift, ushort groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountDescending16(Span<uint> buckets, Span<ushort> keys, int start, int end, int shift, ushort groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((~keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountAscending32(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCount32(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountDescending32(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((~keys[i] >> shift) & groupMask)]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountAscending64(Span<uint> buckets, Span<ulong> keys, int start, int end, int shift, ulong groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountDescending64(Span<uint> buckets, Span<ulong> keys, int start, int end, int shift, ulong groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((~keys[i] >> shift) & groupMask)]++;
        }

        internal static bool ComputeOffsets(Span<int> bucketMap, Span<uint> countsOffsets, int length, uint offset = 0)
        {
            if (bucketMap.IsEmpty)
            {
                for (int i = 0; i < countsOffsets.Length; i++)
                {
                    var grpCount = countsOffsets[i];
                    if (grpCount == length) return false;
                    countsOffsets[i] = offset;
                    offset += grpCount;
                }
            }
            else
            {
                for (int i = 0; i < countsOffsets.Length; i++)
                {
                    var bucket = bucketMap[i];
                    var grpCount = countsOffsets[bucket];
                    if (grpCount == length) return false;
                    countsOffsets[bucket] = offset;
                    offset += grpCount;
                }
            }
            return length > 1;
        }

        internal static bool ComputeOffsets(Span<uint> countsOffsets, int length, int bucketOffset, uint offset = 0)
        {
            int bucketCount = countsOffsets.Length;
            for (int i = bucketOffset; i < bucketCount; i++)
            {
                var grpCount = countsOffsets[i];
                if (grpCount == length) return false;
                countsOffsets[i] = offset;
                offset += grpCount;
            }
            for (int i = 0; i < bucketOffset; i++)
            {
                var grpCount = countsOffsets[i];
                if (grpCount == length) return false;
                countsOffsets[i] = offset;
                offset += grpCount;
            }
            return length > 1;
        }
        public static void BuildBucketMap(Span<int> map, bool ascending)
        {
            if (ascending)
            {
                for (int i = 0; i < map.Length; i++)
                    map[i] = i;
            }
            else
            {
                int bucketIndex = map.Length;
                for (int i = 0; i < map.Length; i++)
                    map[i] = --bucketIndex;
            }

        }
        public static void BuildFinalSignedBucketMap<T>(Span<int> map, int r, bool ascending)
        {
            // we have "bits" bits left to use; the first half of those are the +ves,
            // the second half (with 1 in the MSB) are the -ves; everything else *shouldn't be touched*
            int bits = (Unsafe.SizeOf<T>() << 3) % r;
            if (bits == 0) bits = r;
            int split = 1 << (bits - 1), index;
            
            if(ascending)
            {
                index = 0;
                for (int i = split; i < map.Length; i++) map[index++] = i;
                for (int i = 0; i < split; i++) map[index++] = i;
            }
            else
            {
                index = map.Length;
                for (int i = split; i < map.Length; i++) map[--index] = i;
                for (int i = 0; i < split; i++) map[--index] = i;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyAscending8(Span<uint> offsets, Span<byte> keys, Span<byte> workspace,
       int start, int end, int shift, byte groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyDescending8(Span<uint> offsets, Span<byte> keys, Span<byte> workspace,
            int start, int end, int shift, byte groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((~keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyAscending16(Span<uint> offsets, Span<ushort> keys, Span<ushort> workspace,
       int start, int end, int shift, ushort groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyDescending16(Span<uint> offsets, Span<ushort> keys, Span<ushort> workspace,
            int start, int end, int shift, ushort groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((~keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyAscending32(Span<uint> offsets, Span<uint> keys, Span<uint> workspace,
               int start, int end, int shift, uint groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyDescending32(Span<uint> offsets, Span<uint> keys, Span<uint> workspace,
            int start, int end, int shift, uint groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((~keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyAscending64(Span<uint> offsets, Span<ulong> keys, Span<ulong> workspace,
       int start, int end, int shift, ulong groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyDescending64(Span<uint> offsets, Span<ulong> keys, Span<ulong> workspace,
            int start, int end, int shift, ulong groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((~keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShortSortAscending(Span<uint> keys, int offset, uint count)
        {
            switch (count)
            {
                case 0:
                case 1:
                    return false;
                case 2:
                    UpSwap(ref keys[offset++], ref keys[offset]);
                    return false;
                case 3:
                    UpSwap(ref keys[offset++], ref keys[offset++], ref keys[offset]);
                    return false;
                case 4:
                    UpSwap(ref keys[offset++], ref keys[offset++], ref keys[offset++], ref keys[offset]);
                    return false;
                case 5:
                    UpSwap(ref keys[offset++], ref keys[offset++], ref keys[offset++], ref keys[offset++], ref keys[offset]);
                    return false;
                default:
                    return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShortSortDescending(Span<uint> keys, int offset, uint count)
        {
            switch (count)
            {
                case 0:
                case 1:
                    return false;
                case 2:
                    DownSwap(ref keys[offset++], ref keys[offset]);
                    return false;
                case 3:
                    DownSwap(ref keys[offset++], ref keys[offset++], ref keys[offset]);
                    return false;
                case 4:
                    DownSwap(ref keys[offset++], ref keys[offset++], ref keys[offset++], ref keys[offset]);
                    return false;
                case 5:
                    DownSwap(ref keys[offset++], ref keys[offset++], ref keys[offset++], ref keys[offset++], ref keys[offset]);
                    return false;
                default:
                    return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpSwap(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e, ref uint f)
        {
            UpSwap(ref a, ref b);
            UpSwap(ref a, ref c);
            UpSwap(ref a, ref d);
            UpSwap(ref a, ref e);
            UpSwap(ref a, ref f);
            UpSwap(ref b, ref c);
            UpSwap(ref b, ref d);
            UpSwap(ref b, ref e);
            UpSwap(ref b, ref f);
            UpSwap(ref c, ref d);
            UpSwap(ref c, ref e);
            UpSwap(ref c, ref f);
            UpSwap(ref d, ref e);
            UpSwap(ref d, ref f);
            UpSwap(ref e, ref f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpSwap(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e)
        {
            UpSwap(ref a, ref b);
            UpSwap(ref a, ref c);
            UpSwap(ref a, ref d);
            UpSwap(ref a, ref e);
            UpSwap(ref b, ref c);
            UpSwap(ref b, ref d);
            UpSwap(ref b, ref e);
            UpSwap(ref c, ref d);
            UpSwap(ref c, ref e);
            UpSwap(ref d, ref e);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpSwap(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            UpSwap(ref a, ref b);
            UpSwap(ref a, ref c);
            UpSwap(ref a, ref d);
            UpSwap(ref b, ref c);
            UpSwap(ref b, ref d);
            UpSwap(ref c, ref d);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpSwap(ref uint a, ref uint b, ref uint c)
        {
            UpSwap(ref a, ref b);
            UpSwap(ref a, ref c);
            UpSwap(ref b, ref c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpSwap(ref uint a, ref uint b)
        {
            if (a > b)
            {
                var tmp = a;
                a = b;
                b = a;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DownSwap(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e, ref uint f)
        {
            DownSwap(ref a, ref b);
            DownSwap(ref a, ref c);
            DownSwap(ref a, ref d);
            DownSwap(ref a, ref e);
            DownSwap(ref a, ref f);
            DownSwap(ref b, ref c);
            DownSwap(ref b, ref d);
            DownSwap(ref b, ref e);
            DownSwap(ref b, ref f);
            DownSwap(ref c, ref d);
            DownSwap(ref c, ref e);
            DownSwap(ref c, ref f);
            DownSwap(ref d, ref e);
            DownSwap(ref d, ref f);
            DownSwap(ref e, ref f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DownSwap(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e)
        {
            DownSwap(ref a, ref b);
            DownSwap(ref a, ref c);
            DownSwap(ref a, ref d);
            DownSwap(ref a, ref e);
            DownSwap(ref b, ref c);
            DownSwap(ref b, ref d);
            DownSwap(ref b, ref e);
            DownSwap(ref c, ref d);
            DownSwap(ref c, ref e);
            DownSwap(ref d, ref e);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DownSwap(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            DownSwap(ref a, ref b);
            DownSwap(ref a, ref c);
            DownSwap(ref a, ref d);
            DownSwap(ref b, ref c);
            DownSwap(ref b, ref d);
            DownSwap(ref c, ref d);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DownSwap(ref uint a, ref uint b, ref uint c)
        {
            DownSwap(ref a, ref b);
            DownSwap(ref a, ref c);
            DownSwap(ref b, ref c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DownSwap(ref uint a, ref uint b)
        {
            if (a < b)
            {
                var tmp = a;
                a = b;
                b = a;
            }
        }

        public static void Swap<T>(ref Span<T> x, ref Span<T> y, ref bool reversed) where T : struct
        {
            var tmp = x;
            x = y;
            y = tmp;
            reversed = !reversed;
        }
        public static void Swap<T>(ref Memory<T> x, ref Memory<T> y, ref bool reversed) where T : struct
        {
            var tmp = x;
            x = y;
            y = tmp;
            reversed = !reversed;
        }
        public static void Swap<T>(ref T x, ref T y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }
        public static int GroupCount<T>(int r)
        {
            int bits = Unsafe.SizeOf<T>() << 3;
            return ((bits - 1) / r) + 1;
        }

        private static void InsertionSortAscending(Span<uint> keys, int start, int end)
        {
            for (int i = start; i < end - 1; i++)
            {
                var j = i + 1;
                uint x, y;
                while (j > 0)
                {
                    if ((x = keys[j - 1]) > (y = keys[j]))
                    {
                        keys[j - 1] = y;
                        keys[j] = x;
                    }
                    j--;
                }
            }
        }
    }
}
