using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public static class RadixSort
    {
        private const int DEFAULT_R = 4, MAX_R = 16;
        public static void Sort<T>(this Span<T> keys, Span<T> workspace, int r = DEFAULT_R, bool descending = false) where T : struct
        {
            if (Unsafe.SizeOf<T>() == 4)
            {
                Sort32(RadixConverter.GetNonPassthru<T, uint>(),
                    keys.NonPortableCast<T, uint>(),
                    workspace.NonPortableCast<T, uint>(),
                    r, descending, uint.MaxValue);
            }
            else
            {
                throw new NotSupportedException($"Sort type '{typeof(T).Name}' is {Unsafe.SizeOf<T>()} bytes, which is not supported");
            }
        }
        public static void Sort(this Span<uint> keys, Span<uint> workspace, int r = DEFAULT_R, bool descending = false, uint mask = uint.MaxValue)
            => Sort32(null, keys, workspace, r, descending, mask);

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

        private static void Sort32(RadixConverter<uint> converter, Span<uint> keys, Span<uint> workspace, int r, bool descending, uint keyMask)
        {
            if (keys.Length <= 1 || keyMask == 0) return;
            if (workspace.Length < WorkspaceSize<uint>(keys.Length, r))
                throw new ArgumentException($"The workspace provided is insufficient ({workspace.Length} vs {WorkspaceSize<uint>(keys.Length, r)} needed); the {nameof(WorkspaceSize)} method can be used to determine the minimum size required", nameof(workspace));

            int countLength = 1 << r, len = keys.Length;
            Span<uint> countsOffsets = workspace.Slice(0, countLength);
            workspace = workspace.Slice(countLength, len);
            int groups = GroupCount<uint>(r);
            uint mask = (uint)(countLength - 1);

            bool reversed = false;
            if (converter != null)
            {
                converter.ToRadix(keys, workspace);
                Swap(ref keys, ref workspace, ref reversed);
            }

            if(descending ? SortDescending32(keys, workspace, r, keyMask, countLength, len, countsOffsets, groups, mask)
                : SortAscending32(keys, workspace, r, keyMask, countLength, len, countsOffsets, groups, mask))
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

        private static bool SortAscending32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask, int countLength, int len, Span<uint> countsOffsets, int groups, uint mask)
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
                countsOffsets.Clear();
                for (int i = 0; i < len; i++)
                    countsOffsets[(int)((keys[i] >> shift) & groupMask)]++;


                // calculating prefixes
                uint offset = 0;
                for (int i = 0; i < countLength; i++)
                {
                    var prev = offset;
                    var grpCount = countsOffsets[i];
                    if (grpCount == len) goto NextLoop; // all in one group
                    offset += grpCount;
                    countsOffsets[i] = prev;
                }

                // from a[] to t[] elements ordered by c-th group

                for (int i = 0; i < len; i++)
                {
                    int j = (int)countsOffsets[(int)((keys[i] >> shift) & groupMask)]++;
                    workspace[j] = keys[i];
                }


                Swap(ref keys, ref workspace, ref reversed);

                NextLoop:
                ;
            }
            return reversed;
        }

        private static bool SortDescending32(Span<uint> keys, Span<uint> workspace, int r, uint keyMask, int countLength, int len, Span<uint> countsOffsets, int groups, uint mask)
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
                countsOffsets.Clear();
                for (int i = 0; i < len; i++)
                    countsOffsets[(int)((~keys[i] >> shift) & groupMask)]++;
                

                // calculating prefixes
                uint offset = 0;
                for (int i = 0; i < countLength; i++)
                {
                    var prev = offset;
                    var grpCount = countsOffsets[i];
                    if (grpCount == len) goto NextLoop; // all in one group
                    offset += grpCount;
                    countsOffsets[i] = prev;
                }

                // from a[] to t[] elements ordered by c-th group
                for (int i = 0; i < len; i++)
                {
                    int j = (int)countsOffsets[(int)((~keys[i] >> shift) & groupMask)]++;
                    workspace[j] = keys[i];
                }

                Swap(ref keys, ref workspace, ref reversed);

                NextLoop:
                ;
            }
            return reversed;
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
