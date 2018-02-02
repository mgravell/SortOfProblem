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

            Span<uint> offsets = stackalloc uint[1 << r];
            int bucketCount = 1 << r, groups = ((32 - 1) / r) + 1, mask = bucketCount - 1;
            Sort32(keys, workspace, offsets, keyMask, (uint)(bucketCount - 1), r, 32 - r, ascending, 0, keys.Length);
        }
        static void Sort32(Span<uint> keys, Span<uint> workspace, Span<uint> offsets, uint keyMask, uint mask, int r, int shift, bool ascending, int start, int end)
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
                if (ascending)
                    Util.BucketCountAscending(buckets, keys, start, end, shift, groupMask);
                else
                    Util.BucketCountDescending(buckets, keys, start, end, shift, groupMask);

                buckets.CopyTo(offsets);
                if (Util.ComputeOffsets(offsets, end - start, 0, (uint)start))
                {
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
                        uint x, y, z;
                        switch (grp)
                        {
                            case 0: break;
                            case 1: offset++; break;
                            case 2:
                                x = keys[offset];
                                y = keys[offset + 1];
                                if(x > y)
                                {
                                    keys[offset] = y;
                                    keys[offset + 1] = x;
                                }
                                offset += 2;
                                break;
                            default:
                                int next = offset + (int)grp;
                                Sort32(keys, workspace, offsets, keyMask, mask, r, shift, ascending, offset, next);
                                offset = next;
                                break;
                        }
                    }
                }
            }
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
