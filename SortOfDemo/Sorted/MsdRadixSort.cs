using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public static class MsdRadixSort
    {
        public static void Sort(this Span<uint> keys, Span<uint> workspace, int r = default, bool descending = false, uint keyMask = uint.MaxValue)
        {
            Sort32(keys, workspace, r, keyMask, !descending, NumberSystem.Unsigned);
        }
        public static void Sort<T>(this Span<T> keys, Span<T> workspace, int r = default, bool descending = false) where T : struct
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

        public static int DefaultR { get; set; }

        private static void Sort32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask, bool ascending, NumberSystem numberSystem)
        {
            if ((keyMask & Util.MSB32U) == 0) numberSystem = NumberSystem.Unsigned;

            if (!ascending || numberSystem != NumberSystem.Unsigned) throw new NotImplementedException("Need to do that!");
            
            if (ascending ? Util.ShortSortAscending(keys, 0, (uint)keys.Length) : Util.ShortSortDescending(keys, 0, (uint)keys.Length))
            {
                r = Util.ChooseBitCount<uint>(r, DefaultR);
                workspace = workspace.Slice(0, keys.Length);

                if (keyMask == 0) return;

                Span<uint> offsets = stackalloc uint[1 << r];
                int bucketCount = 1 << r, groups = ((32 - 1) / r) + 1;

                uint mask = (uint)(bucketCount - 1), groupMask;
                var shift = 32;
                do
                {
                    shift -= r;
                    groupMask = (keyMask >> shift) & mask;
                    keyMask &= ~(mask << shift);
                } while (groupMask == 0);

                // no need to check groupMask is non-zero - we already checked keyMask, so we definitely expect *something*
                Sort32(keys, workspace, offsets, keyMask, groupMask, mask, r, shift, ascending, 0, keys.Length);
            }
        }

        static void Sort32(Span<uint> keys, Span<uint> workspace, Span<uint> offsets, uint keyMask, uint groupMask, uint mask, int r, int shift, bool ascending, int start, int end)
        {

            Span<uint> buckets = stackalloc uint[1 << r];
            if (ascending)
                Util.BucketCountAscending32(buckets, keys, start, end, shift, groupMask);
            else
                Util.BucketCountDescending32(buckets, keys, start, end, shift, groupMask);

            buckets.CopyTo(offsets);
            if (Util.ComputeOffsets(offsets, end - start, 0, (uint)start))
            {
                if (ascending)
                    Util.ApplyAscending32(offsets, keys, workspace, start, end, shift, groupMask);
                else
                    Util.ApplyAscending32(offsets, keys, workspace, start, end, shift, groupMask);
                workspace.Slice(start, end - start).CopyTo(keys.Slice(start, end - start));
            }


            // cascade to the sub-arrays of each sub-group

            do // get the next group mask - skip until we get a non-zero mask
            {
                shift -= r;
                groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift);
            } while (groupMask == 0 && keyMask != 0);

            if (groupMask != 0)
            {
                int offset = start;
                for (int i = 0; i < buckets.Length; i++)
                {
                    var grp = buckets[i];
                    switch (grp)
                    {
                        case 0: break;
                        case 1: offset++; break;
                        default:
                            int next = offset + (int)grp;
                            if (ascending ? Util.ShortSortAscending(keys, offset, grp) : Util.ShortSortDescending(keys, offset, grp))
                            {
                                Sort32(keys, workspace, offsets, keyMask, groupMask, mask, r, shift, ascending, offset, next);
                            }
                            offset = next;
                            break;
                    }
                }
            }
        }

    }
}
