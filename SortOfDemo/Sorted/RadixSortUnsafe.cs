using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sorted
{
    public static unsafe class RadixSortUnsafe
    {
        private const int DEFAULT_R = 4, MAX_R = 16;
        public static void Sort<T>(this Span<T> keys, Span<T> workspace, int r = DEFAULT_R, bool descending = false) where T : struct
        {
            if (keys.Length <= 1) return;
            if (workspace.Length <= keys.Length) throw new ArgumentException("Insufficient workspace", nameof(workspace));

            if (Unsafe.SizeOf<T>() == 4)
            {
                fixed (uint* k = &MemoryMarshal.GetReference(keys.NonPortableCast<T, uint>()))
                fixed (uint* w = &MemoryMarshal.GetReference(workspace.NonPortableCast<T, uint>()))
                {
                    Sort32(RadixConverter.GetNonPassthru<T, uint>(), k, w, keys.Length, r, descending, uint.MaxValue);
                }
            }
            else
            {
                throw new NotSupportedException($"Sort type '{typeof(T).Name}' is {Unsafe.SizeOf<T>()} bytes, which is not supported");
            }
        }
        public static void Sort(uint* keys, uint* workspace, int length, int r = DEFAULT_R, bool descending = false, uint mask = uint.MaxValue)
            => Sort32(null, keys, workspace, length, r, descending, mask);

        public static void Sort(this Span<uint> keys, Span<uint> workspace, int r = DEFAULT_R, bool descending = false, uint mask = uint.MaxValue)
        {
            if (keys.Length <= 1) return;
            if (workspace.Length <= keys.Length) throw new ArgumentException("Insufficient workspace", nameof(workspace));

            fixed (uint* k = &MemoryMarshal.GetReference(keys))
            fixed (uint* w = &MemoryMarshal.GetReference(workspace))
            {
                Sort32(null, k, w, keys.Length, r, descending, mask);
            }
        }


        static void Swap(ref uint* x, ref uint* y, ref bool reversed)
        {
            var tmp = x;
            x = y;
            y = tmp;
            reversed = !reversed;
        }


        static int GroupCount<T>(int r)
        {
            int bits = Unsafe.SizeOf<T>() << 3;
            return ((bits - 1) / r) + 1;
        }

        private static void Sort32(RadixConverter<uint> converter, uint* keys, uint* workspace, int len, int r, bool descending, uint keyMask)
        {
            if (len <= 1 || keyMask == 0) return;

            if(r < 1 || r > MAX_R) throw new ArgumentOutOfRangeException(nameof(r));

            int countLength = 1 << r;
            int groups = GroupCount<uint>(r);
            uint* countsOffsets = stackalloc uint[countLength];
            uint mask = (uint)(countLength - 1);

            bool reversed = false;
            if (converter != null)
            {
                converter.ToRadix(new Span<uint>(keys, len), new Span<uint>(workspace, len));
                Swap(ref keys, ref workspace, ref reversed);
            }

            if (descending ? SortDescending32(keys, workspace, r, keyMask, countLength, len, countsOffsets, groups, mask)
                : SortAscending32(keys, workspace, r, keyMask, countLength, len, countsOffsets, groups, mask))
            {
                Swap(ref keys, ref workspace, ref reversed);
            }

            if (converter != null)
            {
                if (reversed)
                {
                    converter.FromRadix(new Span<uint>(keys, len), new Span<uint>(workspace, len));
                }
                else
                {
                    var s = new Span<uint>(keys, len);
                    converter.FromRadix(s, s);
                }
            }
            else if (reversed)
            {
                Unsafe.CopyBlock(workspace, keys, (uint)len << 2);
            }
        }

        private static bool SortAscending32(uint* keys, uint* workspace, int r, uint keyMask, int countLength, int len, uint* countsOffsets, int groups, uint mask)
        {
            bool reversed = false;
            for (int c = 0, shift = 0; c < groups; c++, shift += r)
            {
                uint groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                // counting elements of the c-th group
                Unsafe.InitBlock(countsOffsets, 0, (uint)countLength << 2);
                var ptr = keys;
                for (int i = 0; i < len; i++)
                    countsOffsets[(int)((*ptr++ >> shift) & groupMask)]++;


                // calculating prefixes
                uint offset = 0;
                ptr = countsOffsets;
                for (int i = 0; i < countLength; i++)
                {
                    var prev = offset;
                    var grpCount = *ptr;
                    if (grpCount == len) goto NextLoop; // all in one group
                    offset += grpCount;
                    *ptr++ = prev;
                }

                // from a[] to t[] elements ordered by c-th group
                ptr = keys;
                for (int i = 0; i < len; i++)
                {
                    int j = (int)countsOffsets[(int)((*ptr >> shift) & groupMask)]++;
                    workspace[j] = *ptr++;
                }


                Swap(ref keys, ref workspace, ref reversed);

                NextLoop:
                ;
            }
            return reversed;
        }

        private static bool SortDescending32(uint* keys, uint* workspace, int r, uint keyMask, int countLength, int len, uint* countsOffsets, int groups, uint mask)
        {
            bool reversed = false;
            for (int c = 0, shift = 0; c < groups; c++, shift += r)
            {
                uint groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                // counting elements of the c-th group 
                Unsafe.InitBlock(countsOffsets, 0, (uint)countLength << 2);
                var ptr = keys;
                for (int i = 0; i < len; i++)
                    countsOffsets[(int)((~*ptr++ >> shift) & groupMask)]++;


                // calculating prefixes
                uint offset = 0;
                ptr = countsOffsets;
                for (int i = 0; i < countLength; i++)
                {
                    var prev = offset;
                    var grpCount = *ptr;
                    if (grpCount == len) goto NextLoop; // all in one group
                    offset += grpCount;
                    *ptr++ = prev;
                }

                // from a[] to t[] elements ordered by c-th group
                ptr = keys;
                for (int i = 0; i < len; i++)
                {
                    int j = (int)countsOffsets[(int)((~*ptr >> shift) & groupMask)]++;
                    workspace[j] = *ptr++;
                }

                Swap(ref keys, ref workspace, ref reversed);

                NextLoop:
                ;
            }
            return reversed;
        }
    }
}
