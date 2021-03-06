﻿using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public static partial class LsdRadixSort
    {
        public static void SortSmall<T>(this Span<T> keys, int r = default, bool descending = false) where T : struct
        {
            Span<byte> workspace = stackalloc byte[keys.Length * Unsafe.SizeOf<T>()];
            Sort<T>(keys, workspace.NonPortableCast<byte, T>(), r, descending);
        }
        public static void Sort<T>(this Span<T> keys, Span<T> workspace, int r = default, bool descending = false) where T : struct
        {
            if (Unsafe.SizeOf<T>() == 4)
            {
                Sort32(
                    keys.NonPortableCast<T, uint>(),
                    workspace.NonPortableCast<T, uint>(),
                    r, uint.MaxValue, !descending, NumberSystem<T>.Value);
            }
            else if (Unsafe.SizeOf<T>() == 8)
            {
                Sort64(
                    keys.NonPortableCast<T, ulong>(),
                    workspace.NonPortableCast<T, ulong>(),
                    r, ulong.MaxValue, !descending, NumberSystem<T>.Value);
            }
            else if (Unsafe.SizeOf<T>() == 2)
            {
                Sort16(
                    keys.NonPortableCast<T, ushort>(),
                    workspace.NonPortableCast<T, ushort>(),
                    r, ushort.MaxValue, !descending, NumberSystem<T>.Value);
            }
            else if (Unsafe.SizeOf<T>() == 1)
            {
                Sort8(
                    keys.NonPortableCast<T, byte>(),
                    workspace.NonPortableCast<T, byte>(),
                    r, byte.MaxValue, !descending, NumberSystem<T>.Value);
            }
            else
            {
                throw new NotSupportedException($"Sort type '{typeof(T).Name}' is {Unsafe.SizeOf<T>()} bytes, which is not supported");
            }
        }

        public static void SortSmall(this Span<uint> keys, int r = default, bool descending = false, uint mask = uint.MaxValue)
        {
            Span<uint> workspace = stackalloc uint[keys.Length * sizeof(uint)];
            Sort32(keys, workspace, r, mask, !descending, NumberSystem.Unsigned);
        }
        public static void Sort(this Span<uint> keys, Span<uint> workspace, int r = default, bool descending = false, uint mask = uint.MaxValue)
            => Sort32(keys, workspace, r, mask, !descending, NumberSystem.Unsigned);

        public static int DefaultR { get; set; }


        private static void Sort8(Span<byte> keys, Span<byte> workspace, int r, byte keyMask, bool ascending, NumberSystem numberSystem)
        {
            r = Util.ChooseBitCount<byte>(r, DefaultR);
            if (keys.Length <= 1 || keyMask == 0) return;

            int countLength = 1 << r;
            Span<uint> countsOffsets = stackalloc uint[countLength];
            workspace = workspace.Slice(0, keys.Length);
            int groups = Util.GroupCount<byte>(r);
            byte mask = (byte)(countLength - 1);

            bool reversed = false;

            if ((keyMask & Util.MSB8U) == 0) numberSystem = NumberSystem.Unsigned; // without the MSB, sign doesn't matter
            if (numberSystem == NumberSystem.SignBit)
            {
                // sort *just* on the MSB
                var split = SortCore8(keys, workspace, 1, Util.MSB8U, 2, countsOffsets.Slice(0, 2), 8, 1, ascending, true, 7);
                if (split.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                keyMask = (byte)(keyMask & ~Util.MSB8U);

                // now sort the two chunks separately, respecting the corresponding data/workspace areas
                // note: regardless of asc/desc, we will always want the first chunk to be decreasing magnitude and the second chunk to be increasing magnitude - hence false/true
                var lower = split.Split <= 1 ? default : SortCore8(keys.Slice(0, split.Split), workspace.Slice(0, split.Split), r, keyMask, countLength, countsOffsets, groups, mask, false, false);
                var upper = split.Split >= keys.Length - 1 ? default : SortCore8(keys.Slice(split.Split), workspace.Slice(split.Split), r, keyMask, countLength, countsOffsets, groups, mask, true, false);

                if (lower.Reversed == upper.Reversed)
                { // both or neither reversed
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else if (split.Split < (keys.Length / 2)) // lower group is smaller
                {
                    if (split.Split != 0) keys.Slice(0, split.Split).CopyTo(workspace.Slice(0, split.Split));
                    // the lower-half is now in both spaces; respect the opinion of the upper-half 
                    if (upper.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else // upper group is smaller
                {
                    if (split.Split != keys.Length) keys.Slice(split.Split).CopyTo(workspace.Slice(split.Split));
                    // the upper-half is now in both spaces; respect the opinion of the lower-half 
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
            }
            else if (SortCore8(keys, workspace, r, keyMask, countLength, countsOffsets, groups, mask, ascending, numberSystem != NumberSystem.Unsigned).Reversed)
            {
                Util.Swap(ref keys, ref workspace, ref reversed);
            }

            if (reversed) keys.CopyTo(workspace);
        }
        private static void Sort16(Span<ushort> keys, Span<ushort> workspace, int r, ushort keyMask, bool ascending, NumberSystem numberSystem)
        {
            r = Util.ChooseBitCount<ushort>(r, DefaultR);
            if (keys.Length <= 1 || keyMask == 0) return;

            int countLength = 1 << r;
            Span<uint> countsOffsets = stackalloc uint[countLength];
            workspace = workspace.Slice(0, keys.Length);
            int groups = Util.GroupCount<ushort>(r);
            ushort mask = (ushort)(countLength - 1);

            bool reversed = false;

            if ((keyMask & Util.MSB16U) == 0) numberSystem = NumberSystem.Unsigned; // without the MSB, sign doesn't matter
            if (numberSystem == NumberSystem.SignBit)
            {
                // sort *just* on the MSB
                var split = SortCore16(keys, workspace, 1, Util.MSB16U, 2, countsOffsets.Slice(0, 2), 16, 1, ascending, true, 15);
                if (split.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                keyMask = (ushort)(keyMask & ~Util.MSB16U);

                // now sort the two chunks separately, respecting the corresponding data/workspace areas
                // note: regardless of asc/desc, we will always want the first chunk to be decreasing magnitude and the second chunk to be increasing magnitude - hence false/true
                var lower = split.Split <= 1 ? default : SortCore16(keys.Slice(0, split.Split), workspace.Slice(0, split.Split), r, keyMask, countLength, countsOffsets, groups, mask, false, false);
                var upper = split.Split >= keys.Length - 1 ? default : SortCore16(keys.Slice(split.Split), workspace.Slice(split.Split), r, keyMask, countLength, countsOffsets, groups, mask, true, false);

                if (lower.Reversed == upper.Reversed)
                { // both or neither reversed
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else if (split.Split < (keys.Length / 2)) // lower group is smaller
                {
                    if (split.Split != 0) keys.Slice(0, split.Split).CopyTo(workspace.Slice(0, split.Split));
                    // the lower-half is now in both spaces; respect the opinion of the upper-half 
                    if (upper.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else // upper group is smaller
                {
                    if (split.Split != keys.Length) keys.Slice(split.Split).CopyTo(workspace.Slice(split.Split));
                    // the upper-half is now in both spaces; respect the opinion of the lower-half 
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
            }
            else if (SortCore16(keys, workspace, r, keyMask, countLength, countsOffsets, groups, mask, ascending, numberSystem != NumberSystem.Unsigned).Reversed)
            {
                Util.Swap(ref keys, ref workspace, ref reversed);
            }

            if (reversed) keys.CopyTo(workspace);
        }
        private static void Sort32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask, bool ascending, NumberSystem numberSystem)
        {
            r = Util.ChooseBitCount<uint>(r, DefaultR);
            if (keys.Length <= 1 || keyMask == 0) return;

            int countLength = 1 << r;
            Span<uint> countsOffsets = stackalloc uint[countLength];
            workspace = workspace.Slice(0, keys.Length);
            int groups = Util.GroupCount<uint>(r);
            uint mask = (uint)(countLength - 1);

            bool reversed = false;

            if ((keyMask & Util.MSB32U) == 0) numberSystem = NumberSystem.Unsigned; // without the MSB, sign doesn't matter
            if (numberSystem == NumberSystem.SignBit)
            {
                // sort *just* on the MSB
                var split = SortCore32(keys, workspace, 1, Util.MSB32U, 2, countsOffsets.Slice(0, 2), 32, 1, ascending, true, 31);
                if (split.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                keyMask &= ~Util.MSB32U;

                // now sort the two chunks separately, respecting the corresponding data/workspace areas
                // note: regardless of asc/desc, we will always want the first chunk to be decreasing magnitude and the second chunk to be increasing magnitude - hence false/true
                var lower = split.Split <= 1 ? default : SortCore32(keys.Slice(0, split.Split), workspace.Slice(0, split.Split), r, keyMask, countLength, countsOffsets, groups, mask, false, false);
                var upper = split.Split >= keys.Length - 1 ? default : SortCore32(keys.Slice(split.Split), workspace.Slice(split.Split), r, keyMask, countLength, countsOffsets, groups, mask, true, false);

                if (lower.Reversed == upper.Reversed)
                { // both or neither reversed
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else if (split.Split < (keys.Length / 2)) // lower group is smaller
                {
                    if (split.Split != 0) keys.Slice(0, split.Split).CopyTo(workspace.Slice(0, split.Split));
                    // the lower-half is now in both spaces; respect the opinion of the upper-half 
                    if (upper.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else // upper group is smaller
                {
                    if (split.Split != keys.Length) keys.Slice(split.Split).CopyTo(workspace.Slice(split.Split));
                    // the upper-half is now in both spaces; respect the opinion of the lower-half 
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
            }
            else if (SortCore32(keys, workspace, r, keyMask, countLength, countsOffsets, groups, mask, ascending, numberSystem != NumberSystem.Unsigned).Reversed)
            {
                Util.Swap(ref keys, ref workspace, ref reversed);
            }

            if (reversed) keys.CopyTo(workspace);
        }

        private static void Sort64(Span<ulong> keys, Span<ulong> workspace, int r, ulong keyMask, bool ascending, NumberSystem numberSystem)
        {
            r = Util.ChooseBitCount<ulong>(r, DefaultR);
            if (keys.Length <= 1 || keyMask == 0) return;

            int countLength = 1 << r;
            Span<uint> countsOffsets = stackalloc uint[countLength];
            workspace = workspace.Slice(0, keys.Length);
            int groups = Util.GroupCount<ulong>(r);
            ulong mask = (ulong)(countLength - 1);

            bool reversed = false;

            if ((keyMask & Util.MSB64U) == 0) numberSystem = NumberSystem.Unsigned; // without the MSB, sign doesn't matter
            if (numberSystem == NumberSystem.SignBit)
            {
                // sort *just* on the MSB
                var split = SortCore64(keys, workspace, 1, Util.MSB64U, 2, countsOffsets.Slice(0, 2), 64, 1, ascending, true, 63);
                if (split.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                keyMask &= ~Util.MSB64U;

                // now sort the two chunks separately, respecting the corresponding data/workspace areas
                // note: regardless of asc/desc, we will always want the first chunk to be decreasing magnitude and the second chunk to be increasing magnitude - hence false/true
                var lower = split.Split <= 1 ? default : SortCore64(keys.Slice(0, split.Split), workspace.Slice(0, split.Split), r, keyMask, countLength, countsOffsets, groups, mask, false, false);
                var upper = split.Split >= keys.Length - 1 ? default : SortCore64(keys.Slice(split.Split), workspace.Slice(split.Split), r, keyMask, countLength, countsOffsets, groups, mask, true, false);

                if (lower.Reversed == upper.Reversed)
                { // both or neither reversed
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else if (split.Split < (keys.Length / 2)) // lower group is smaller
                {
                    if (split.Split != 0) keys.Slice(0, split.Split).CopyTo(workspace.Slice(0, split.Split));
                    // the lower-half is now in both spaces; respect the opinion of the upper-half 
                    if (upper.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
                else // upper group is smaller
                {
                    if (split.Split != keys.Length) keys.Slice(split.Split).CopyTo(workspace.Slice(split.Split));
                    // the upper-half is now in both spaces; respect the opinion of the lower-half 
                    if (lower.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                }
            }
            else if (SortCore64(keys, workspace, r, keyMask, countLength, countsOffsets, groups, mask, ascending, numberSystem != NumberSystem.Unsigned).Reversed)
            {
                Util.Swap(ref keys, ref workspace, ref reversed);
            }

            if (reversed) keys.CopyTo(workspace);
        }

        private static (bool Reversed, int Split) SortCore8(Span<byte> keys, Span<byte> workspace, int r, byte keyMask,
    int countLength, Span<uint> countsOffsets, int groups, byte mask, bool ascending, bool isSigned,
    int c = 0)
        {
            if (keys.IsEmpty) return default;
            int len = keys.Length;
            int invertC = isSigned ? groups - 1 : -1;
            bool reversed = false;
            int split = 0;
            for (int shift = c * r; c < groups; c++, shift += r)
            {
                byte groupMask = (byte)((keyMask >> shift) & mask);
                keyMask = (byte)(keyMask & ~(mask << shift)); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                if (ascending)
                    Util.BucketCountAscending8(countsOffsets, keys, 0, len, shift, groupMask);
                else
                    Util.BucketCountDescending8(countsOffsets, keys, 0, len, shift, groupMask);

                // the "split" is a trick used to sort IEEE754; tells us how many positive/negative
                // numbers we have (since we do a cheeky split on r=1/c=31); this allows us to to
                // two *inner* radix sorts on the rest of the bits
                split = (int)countsOffsets[1];

                if (!Util.ComputeOffsets(countsOffsets, len, c == invertC ? GetInvertStartIndex(8, r) : 0)) continue; // all in one group


                if (ascending)
                    Util.ApplyAscending8(countsOffsets, keys, workspace, 0, len, shift, groupMask);
                else
                    Util.ApplyDescending8(countsOffsets, keys, workspace, 0, len, shift, groupMask);

                Util.Swap(ref keys, ref workspace, ref reversed);
            }
            return (reversed, split);
        }

        private static (bool Reversed, int Split) SortCore16(Span<ushort> keys, Span<ushort> workspace, int r, ushort keyMask,
    int countLength, Span<uint> countsOffsets, int groups, ushort mask, bool ascending, bool isSigned,
    int c = 0)
        {
            if (keys.IsEmpty) return default;
            int len = keys.Length;
            int invertC = isSigned ? groups - 1 : -1;
            bool reversed = false;
            int split = 0;
            for (int shift = c * r; c < groups; c++, shift += r)
            {
                ushort groupMask = (ushort)((keyMask >> shift) & mask);
                keyMask = (ushort)(keyMask & ~(mask << shift)); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                if (ascending)
                    Util.BucketCountAscending16(countsOffsets, keys, 0, len, shift, groupMask);
                else
                    Util.BucketCountDescending16(countsOffsets, keys, 0, len, shift, groupMask);

                // the "split" is a trick used to sort IEEE754; tells us how many positive/negative
                // numbers we have (since we do a cheeky split on r=1/c=31); this allows us to to
                // two *inner* radix sorts on the rest of the bits
                split = (int)countsOffsets[1];

                if (!Util.ComputeOffsets(countsOffsets, len, c == invertC ? GetInvertStartIndex(16, r) : 0)) continue; // all in one group


                if (ascending)
                    Util.ApplyAscending16(countsOffsets, keys, workspace, 0, len, shift, groupMask);
                else
                    Util.ApplyDescending16(countsOffsets, keys, workspace, 0, len, shift, groupMask);

                Util.Swap(ref keys, ref workspace, ref reversed);
            }
            return (reversed, split);
        }
        private static (bool Reversed, int Split) SortCore32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask,
            int countLength, Span<uint> countsOffsets, int groups, uint mask, bool ascending, bool isSigned,
            int c = 0)
        {
            if (keys.IsEmpty) return default;
            int len = keys.Length;
            int invertC = isSigned ? groups - 1 : -1;
            bool reversed = false;
            int split = -1;
            Span<int> bucketMap = (isSigned || !ascending) ? stackalloc int[countLength] : default;
            Util.BuildBucketMap(bucketMap, ascending);

            for (int shift = c * r; c < groups; c++, shift += r)
            {
                uint groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }
                Util.BucketCount32(countsOffsets, keys, 0, len, shift, groupMask);

                if (c == invertC)
                {
                    Util.BuildFinalSignedBucketMap<uint>(bucketMap, r, ascending);
                    // the "split" is a trick used to sort IEEE754; tells us how many positive/negative
                    // numbers we have (since we do a cheeky split on r=1/c=31); this allows us to to
                    // two *inner* radix sorts on the rest of the bits
                    split = (int)countsOffsets[bucketMap[0]];
                }
                
                if (!Util.ComputeOffsets(bucketMap, countsOffsets, len)) continue; // all in one group

                Util.ApplyAscending32(countsOffsets, keys, workspace, 0, len, shift, groupMask);                
                Util.Swap(ref keys, ref workspace, ref reversed);
            }
            return (reversed, split);
        }

        private static (bool Reversed, int Split) SortCore64(Span<ulong> keys, Span<ulong> workspace, int r, ulong keyMask,
    int countLength, Span<uint> countsOffsets, int groups, ulong mask, bool ascending, bool isSigned,
    int c = 0)
        {
            if (keys.IsEmpty) return default;
            int len = keys.Length;
            int invertC = isSigned ? groups - 1 : -1;
            bool reversed = false;
            int split = 0;
            for (int shift = c * r; c < groups; c++, shift += r)
            {
                ulong groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                if (ascending)
                    Util.BucketCountAscending64(countsOffsets, keys, 0, len, shift, groupMask);
                else
                    Util.BucketCountDescending64(countsOffsets, keys, 0, len, shift, groupMask);

                // the "split" is a trick used to sort IEEE754; tells us how many positive/negative
                // numbers we have (since we do a cheeky split on r=1/c=31); this allows us to to
                // two *inner* radix sorts on the rest of the bits
                split = (int)countsOffsets[1];

                if (!Util.ComputeOffsets(countsOffsets, len, c == invertC ? GetInvertStartIndex(64, r) : 0)) continue; // all in one group


                if (ascending)
                    Util.ApplyAscending64(countsOffsets, keys, workspace, 0, len, shift, groupMask);
                else
                    Util.ApplyDescending64(countsOffsets, keys, workspace, 0, len, shift, groupMask);

                Util.Swap(ref keys, ref workspace, ref reversed);
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


    }
}
