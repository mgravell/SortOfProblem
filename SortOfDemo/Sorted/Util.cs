using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    internal static class Util
    {
        internal const int MSB32 = 1 << 31;
        internal const uint MSB32U = 1U << 31;
        internal const int DEFAULT_R = 4, MAX_R = 16;
        internal static int WorkerCount(int count)
        {
            if (count <= 0) return 0;
            return Math.Min(((count - 1) / 1024) + 1, MaxWorkerCount);
        }

        internal static readonly int MaxWorkerCount = Environment.ProcessorCount;

        internal static void CheckR(int count, int r = DEFAULT_R)
        {
            if (r < 1 || r > MAX_R) throw new ArgumentOutOfRangeException(nameof(r));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountAscending(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BucketCountDescending(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((~keys[i] >> shift) & groupMask)]++;
        }

        internal static bool ComputeOffsets(Span<uint> countsOffsets, int length, int bucketOffset, uint offset = 0)
        {
            int bucketCount = countsOffsets.Length;
            for (int i = bucketOffset; i < bucketCount; i++)
            {
                var prev = offset;
                var grpCount = countsOffsets[i];
                if (grpCount == length) return false;
                offset += grpCount;
                countsOffsets[i] = prev;
            }
            for (int i = 0; i < bucketOffset; i++)
            {
                var prev = offset;
                var grpCount = countsOffsets[i];
                if (grpCount == length) return false;
                offset += grpCount;
                countsOffsets[i] = prev;
            }
            return length > 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyAscending(Span<uint> offsets, Span<uint> keys, Span<uint> workspace,
               int start, int end, int shift, uint groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplyDescending(Span<uint> offsets, Span<uint> keys, Span<uint> workspace,
            int start, int end, int shift, uint groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((~keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }


    }
}
