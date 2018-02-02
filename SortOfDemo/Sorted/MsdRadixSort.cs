using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public static class MsdRadixSort
    {
        public static void Sort(this Span<uint> keys, Span<uint> workspace, int r = Util.DEFAULT_R, bool descending = false, uint keyMask = uint.MaxValue)
        {
            Sort32(keys, workspace, r, keyMask, !descending, NumberSystem.Unsigned);
        }
        public static void Sort<T>(this Span<T> keys, Span<T> workspace, int r = Util.DEFAULT_R, bool descending = false) where T : struct
        {
            if (Unsafe.SizeOf<T>() == 4)
            {
                Sort32(
                    keys.NonPortableCast<T, uint>(), workspace.NonPortableCast<T, uint>(),
                    r, uint.MaxValue, !descending, NumberSystem<T>.Value);
            }
            else
            {
                throw new NotSupportedException($"Sort type '{typeof(T).Name}' is {Unsafe.SizeOf<T>()} bytes, which is not supported");
            }
        }

        private static void Sort32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask, bool ascending, NumberSystem numberSystem)
        {
            Util.CheckR(r);
            if (keys.Length <= 1) return;
            workspace = workspace.Slice(0, keys.Length);

            int bucketCount = 1 << r, groups = ((32 - 1) / r) + 1, mask = bucketCount - 1;
            Sort32(keys, workspace, keyMask, (uint)(bucketCount - 1), r, 32 - r, ascending, 0, keys.Length);
        }
        static void Sort32(Span<uint> keys, Span<uint> workspace, uint keyMask, uint mask, int r, int shift, bool ascending, int start, int end)
        {
            var groupMask = (keyMask >> shift) & mask;
            keyMask &= ~(mask << shift);
            if (groupMask == 0)
            {
                if (keyMask == 0) return;
            }
            else
            {
                Span<uint> buckets = stackalloc uint[1 << r];
                Span<uint> offsets = stackalloc uint[1 << r];
                // Console.WriteLine($"counting [{start},{end})");
                if (ascending)
                    Util.BucketCountAscending(buckets, keys, start, end, shift, groupMask);
                else
                    Util.BucketCountDescending(buckets, keys, start, end, shift, groupMask);

                buckets.CopyTo(offsets);
                if (Util.ComputeOffsets(offsets, end - start, 0, (uint)start))
                {
                    // Console.WriteLine($"applying [{start},{end})");
                    if (ascending)
                        Util.ApplyAscending(offsets, keys, workspace, start, end, shift, groupMask);
                    else
                        Util.ApplyAscending(offsets, keys, workspace, start, end, shift, groupMask);
                    workspace.Slice(start, end - start).CopyTo(keys.Slice(start, end - start));
                }


                int offset = start;
                shift -= r;
                //if (shift >= -r)
                {
                    for (int i = 0; i < buckets.Length; i++)
                    {
                        var grp = buckets[i];
                        if (grp != 0)
                        {
                            int next = offset + (int)grp;
                            // Console.WriteLine($"\tgrp {i}, {grp} elements");
                            Sort32(keys, workspace, keyMask, mask, r, shift, ascending, offset, next);
                            offset = next;
                        }
                    }
                }
            }
        }
    }
}
