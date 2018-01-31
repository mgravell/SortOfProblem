using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public static partial class RadixSort
    {
        private const int DEFAULT_R = 4, MAX_R = 16;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyAscending(Span<uint> offsets, Span<uint> keys, Span<uint> workspace,
                int start, int end, int shift, uint groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyDescending(Span<uint> offsets, Span<uint> keys, Span<uint> workspace,
            int start, int end, int shift, uint groupMask)
        {
            for (int i = start; i < end; i++)
            {
                var j = offsets[(int)((~keys[i] >> shift) & groupMask)]++;
                workspace[(int)j] = keys[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BucketCountAscending(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((keys[i] >> shift) & groupMask)]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BucketCountDescending(Span<uint> buckets, Span<uint> keys, int start, int end, int shift, uint groupMask)
        {
            buckets.Clear();
            for (int i = start; i < end; i++)
                buckets[(int)((~keys[i] >> shift) & groupMask)]++;
        }

        public static unsafe void SortSmall<T>(this Span<T> keys, int r = DEFAULT_R, bool descending = false) where T : struct
        {
            int workspaceSize = WorkspaceSize(keys, r);
            byte* workspace = stackalloc byte[workspaceSize * Unsafe.SizeOf<T>()];
            Sort<T>(keys, new Span<T>(workspace, workspaceSize), r, descending);
        }
        public static void Sort<T>(this Span<T> keys, Span<T> workspace, int r = DEFAULT_R, bool descending = false) where T : struct
        {
            if (Unsafe.SizeOf<T>() == 4)
            {
                Sort32(RadixConverter.GetNonPassthruWithSignSupport<T, uint>(out var numberSystem),
                    keys.NonPortableCast<T, uint>(),
                    workspace.NonPortableCast<T, uint>(),
                    r, uint.MaxValue, !descending, numberSystem);
            }
            else
            {
                throw new NotSupportedException($"Sort type '{typeof(T).Name}' is {Unsafe.SizeOf<T>()} bytes, which is not supported");
            }
        }

        public static unsafe void SortSmall(this Span<uint> keys, int r = DEFAULT_R, bool descending = false, uint mask = uint.MaxValue)
        {
            int workspaceSize = WorkspaceSize(keys, r);
            uint* workspace = stackalloc uint[workspaceSize];
            Sort32(null, keys, new Span<uint>(workspace, workspaceSize), r, mask, !descending, NumberSystem.Unsigned);
        }
        public static void Sort(this Span<uint> keys, Span<uint> workspace, int r = DEFAULT_R, bool descending = false, uint mask = uint.MaxValue)
            => Sort32(null, keys, workspace, r, mask, !descending, NumberSystem.Unsigned);

        static void Swap<T>(ref Span<T> x, ref Span<T> y, ref bool reversed) where T : struct
        {
            var tmp = x;
            x = y;
            y = tmp;
            reversed = !reversed;
        }
        static void Swap<T>(ref T x, ref T y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }


        static int GroupCount<T>(int r)
        {
            int bits = Unsafe.SizeOf<T>() << 3;
            return ((bits - 1) / r) + 1;
        }

        private static void Sort32(RadixConverter<uint> converter, Span<uint> keys, Span<uint> workspace, int r, uint keyMask, bool ascending, NumberSystem numberSystem)
        {
            if (keys.Length <= 1 || keyMask == 0) return;
            if (workspace.Length < WorkspaceSize<uint>(keys.Length, r))
                throw new ArgumentException($"The workspace provided is insufficient ({workspace.Length} vs {WorkspaceSize<uint>(keys.Length, r)} needed); the {nameof(WorkspaceSize)} method can be used to determine the minimum size required", nameof(workspace));

            int countLength = 1 << r;
            var countsOffsets = workspace.Slice(0, countLength);
            workspace = workspace.Slice(countLength, keys.Length);
            int groups = GroupCount<uint>(r);
            uint mask = (uint)(countLength - 1);

            bool reversed = false;
            if (converter != null)
            {
                converter.ToRadix(keys, workspace);
                Swap(ref keys, ref workspace, ref reversed);
            }

            if ((keyMask & RadixConverter.MSB32U) == 0) numberSystem = NumberSystem.Unsigned; // without the MSB, sign doesn't matter
            if (numberSystem == NumberSystem.SignBit)
            {
                // sort *just* on the MSB
                var split = SortCore32(keys, workspace, 1, RadixConverter.MSB32U, 2, countsOffsets.Slice(0, 2), 32, 1, ascending, true, 31);
                if (split.Reversed) Swap(ref keys, ref workspace, ref reversed);
                keyMask &= ~RadixConverter.MSB32U;

                // now sort the two chunks separately, respecting the corresponding data/workspace areas
                // note: regardless of asc/desc, we will always want the first chunk to be decreasing magnitude and the second chunk to be increasing magnitude - hence false/true
                var lower = split.Split == 0 ? default : SortCore32(keys.Slice(0, split.Split), workspace.Slice(0, split.Split), r, keyMask, countLength, countsOffsets, groups, mask, false, false);
                var upper = split.Split == keys.Length ? default : SortCore32(keys.Slice(split.Split), workspace.Slice(split.Split), r, keyMask, countLength, countsOffsets, groups, mask, true, false);

                if (lower.Reversed == upper.Reversed)
                { // both or neither reversed
                    if (lower.Reversed) Swap(ref keys, ref workspace, ref reversed);
                }
                else if (split.Split < (keys.Length / 2)) // lower group is smaller
                {
                    if(split.Split != 0) keys.Slice(0, split.Split).CopyTo(workspace.Slice(0, split.Split));
                    // the lower-half is now in both spaces; respect the opinion of the upper-half 
                    if (upper.Reversed) Swap(ref keys, ref workspace, ref reversed);
                }
                else // upper group is smaller
                {
                    if(split.Split != keys.Length) keys.Slice(split.Split).CopyTo(workspace.Slice(split.Split));
                    // the upper-half is now in both spaces; respect the opinion of the lower-half 
                    if (lower.Reversed) Swap(ref keys, ref workspace, ref reversed);
                }
            }
            else if (SortCore32(keys, workspace, r, keyMask, countLength, countsOffsets, groups, mask, ascending, numberSystem != NumberSystem.Unsigned).Reversed)
            {
                Swap(ref keys, ref workspace, ref reversed);
            }

            if (converter != null)
            {
                if (reversed)
                {
                    converter.FromRadix(keys, workspace);
                }
                else
                {
                    converter.FromRadix(keys, keys);
                }
            }
            else if (reversed)
            {
                keys.CopyTo(workspace);
            }
        }

        private static (bool Reversed, int Split) SortCore32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask,
            int countLength, Span<uint> countsOffsets, int groups, uint mask, bool ascending, bool isSigned,
            int c = 0)
        {
            if (keys.IsEmpty) return default;
            int len = keys.Length;
            int invertC = isSigned ? groups - 1 : -1;
            bool reversed = false;
            int split = 0;
            for (int shift = c * r; c < groups; c++, shift += r)
            {
                uint groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                if (ascending)
                    BucketCountAscending(countsOffsets, keys, 0, len, shift, groupMask);
                else
                    BucketCountDescending(countsOffsets, keys, 0, len, shift, groupMask);

                // the "split" is a trick used to sort IEEE754; tells us how many positive/negative
                // numbers we have (since we do a cheeky split on r=1/c=31); this allows us to to
                // two *inner* radix sorts on the rest of the bits
                split = (int)countsOffsets[1];

                if (!ComputeOffsets(countsOffsets, len, c == invertC ? GetInvertStartIndex(32, r) : 0)) continue; // all in one group
                

                if (ascending)
                    ApplyAscending(countsOffsets, keys, workspace, 0, len, shift, groupMask);
                else
                    ApplyDescending(countsOffsets, keys, workspace, 0, len, shift, groupMask);

                Swap(ref keys, ref workspace, ref reversed);
            }
            return (reversed, split);
        }

        internal static int GetInvertStartIndex(int width, int r)
        {
            // e.g. if width 32 and r 2, then: all bits useful, 4 groups, invert at 2
            // if width 32 and r 3, then: 2 bits useful in final chunk, 8 groups, invert at 2
            var mod = width % r;
            return mod == 0 ? 1 << (r - 1) : 1 << (mod - 1);
        }

        static bool ComputeOffsets(Span<uint> countsOffsets, int length, int bucketOffset)
        {
            uint offset = 0;
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
            return true;
        }

        public static int WorkspaceSize<T>(Span<T> keys, int r = DEFAULT_R) => WorkspaceSize<T>(keys.Length, r);
        public static int WorkspaceSize<T>(int count, int r = DEFAULT_R)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (r < 1 || r > MAX_R) throw new ArgumentOutOfRangeException(nameof(r));
            if (count <= 1) return 0;

            int countLength = 1 << r;
            int countsOffsetsAsT = (((countLength << 2) - 1) / Unsafe.SizeOf<T>()) + 1;

            return countsOffsetsAsT + count;
        }
    }
}
