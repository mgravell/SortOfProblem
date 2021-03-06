﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sorted
{
    public static partial class LsdRadixSort
    {
        public static int MaxWorkerCount { get; set; }

        public static int ParallelSort<T>(this Memory<T> keys, Memory<T> workspace, int r = default, bool descending = false) where T : struct
        {
            if (Unsafe.SizeOf<T>() == 4)
            {
                return ParallelSort32<T>(
                    keys, workspace,
                    r, descending, uint.MaxValue, NumberSystem<T>.Value);
            }
            else
            {
                throw new NotSupportedException($"Sort type '{typeof(T).Name}' is {Unsafe.SizeOf<T>()} bytes, which is not supported");
            }
        }

        public static int ParallelSort(this Memory<uint> keys, Memory<uint> workspace, int r = default, bool descending = false, uint mask = uint.MaxValue)
            => ParallelSort32<uint>(keys, workspace, r, descending, mask, NumberSystem.Unsigned);


        enum WorkerStep
        {
            Copy,
            BucketCountAscending,
            BucketCountDescending,
            ApplyAscending,
            ApplyDescending,
        }
        private sealed class Worker<T> where T : struct
        {
            private void ExecuteImpl()
            {
                var step = _step;

                while (true)
                {
                    int batchIndex = Interlocked.Increment(ref _batchIndex);
                    var start = _batchSize * batchIndex;
                    if (start >= _length) break;

                    var end = Math.Min(_batchSize + start, _length);
                    Execute(step, batchIndex, start, end);
                }

                lock(this)
                {
                    if (--_outstandingWorkersNeedsSync == 0)
                        Monitor.Pulse(this); // wake up the main thread
                }
            }

            private void Execute(WorkerStep step, int batchIndex, int start, int end)
            {
                int count = end - start;
                // Console.WriteLine($"{step}/{_shift}/{batchIndex}: [{start},{end}] ({count}) on thread {Environment.CurrentManagedThreadId}");

                switch (step)
                {
                    case WorkerStep.Copy:
                        _keys.Span.Slice(start, count).CopyTo(_workspace.Span.Slice(start, count));
                        break;
                    case WorkerStep.BucketCountAscending:
                    case WorkerStep.BucketCountDescending:
                        {
                            var keys = _keys.Span.NonPortableCast<T, uint>();
                            var buckets = CountsOffsets(batchIndex);
                            if (step == WorkerStep.BucketCountAscending)
                                Util.BucketCountAscending32(buckets, keys, start, end, _shift, _groupMask);
                            else
                                Util.BucketCountDescending32(buckets, keys, start, end, _shift, _groupMask);
                        }
                        break;
                    case WorkerStep.ApplyAscending:
                    case WorkerStep.ApplyDescending:
                        {
                            var offsets = CountsOffsets(batchIndex);
                            if (NeedsApply(offsets, _keys.Length, _bucketOffset))
                            {
                                var keys = _keys.Span.NonPortableCast<T, uint>();
                                var workspace = _workspace.Span.NonPortableCast<T, uint>();
                                if (step == WorkerStep.ApplyAscending)
                                    Util.ApplyAscending32(offsets, keys, workspace, start, end, _shift, _groupMask);
                                else
                                    Util.ApplyDescending32(offsets, keys, workspace, start, end, _shift, _groupMask);
                            }
                            else
                            {
                                _keys.Span.Slice(start, count).CopyTo(_workspace.Span.Slice((int)offsets[0], count));
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Unknown worker step: {step}");
                }

            }

            private static bool NeedsApply(Span<uint> offsets, int length, int bucketOffset)
            {
                var activeGroups = 0;
                // look at ComputeOffsets for the double-range reason; basically, this
                // checks each set of adjacent offsets to see there's an active group; if we
                // don't have at least 2 active groups, there's nothing to apply
                var offset = offsets[bucketOffset];
                for (int i = bucketOffset + 1; i < offsets.Length; i++)
                {
                    if (offset != (offset = offsets[i]) && ++activeGroups == 2) return true;
                }
                for (int i = 0; i < bucketOffset; i++)
                {
                    if (offset != (offset = offsets[i]) && ++activeGroups == 2) return true;
                }
                return offset != length && ++activeGroups == 2;
            }

            int _bucketOffset;
            public bool ComputeOffsets(int bucketOffset)
            {
                _bucketOffset = bucketOffset;
                var allBuckets = AllCountsOffsets();
                int bucketCount = _bucketCount,
                    batchCount = allBuckets.Length / bucketCount,
                    len = _length;

                uint offset = 0;
                for (int i = bucketOffset; i < bucketCount; i++)
                {
                    uint groupCount = 0;
                    int sourceOffset = i;
                    for (int j = 0; j < batchCount; j++)
                    {
                        var count = allBuckets[sourceOffset];
                        allBuckets[sourceOffset] = offset;
                        offset += count;
                        groupCount += count;

                        sourceOffset += bucketCount;
                    }
                    if (groupCount == len) return false; // all in one group
                }
                for (int i = 0; i < bucketOffset; i++)
                {
                    uint groupCount = 0;
                    int sourceOffset = i;
                    for (int j = 0; j < batchCount; j++)
                    {
                        var count = allBuckets[sourceOffset];
                        allBuckets[sourceOffset] = offset;
                        offset += count;
                        groupCount += count;

                        sourceOffset += bucketCount;
                    }
                    if (groupCount == len) return false; // all in one group
                }
                Debug.Assert(len == offset, "final offset should match length");
                return true;
            }

            readonly int _workerCount;
            int _batchSize, _length, _bucketCount;
            unsafe readonly uint* _countsOffsets;
            private unsafe Span<uint> CountsOffsets(int batchIndex) => new Span<uint>(_countsOffsets + (batchIndex * _bucketCount), _bucketCount);
            private unsafe Span<uint> AllCountsOffsets() => new Span<uint>(_countsOffsets, _workerCount * _bucketCount);

            volatile WorkerStep _step;
            int _batchIndex, _shift;
            Memory<T> _keys, _workspace;
            uint _groupMask;

            static readonly WaitCallback _executeImpl = state => ((Worker<T>)state).ExecuteImpl();
            public void Execute(WorkerStep step)
            {
                lock (this)
                {
                    Interlocked.Exchange(ref _batchIndex, -1); // -1 because Interlocked.Increment returns the *incremented* value
                    _step = step;
                    if (_outstandingWorkersNeedsSync != 0) throw new InvalidOperationException("There are still outstanding workers!");
                    _outstandingWorkersNeedsSync = _workerCount;
                }
                for (int i = 1; i < _workerCount; i++) // the current thread will be worker 0
                {
                    ThreadPool.QueueUserWorkItem(_executeImpl, this);
                }
                ExecuteImpl(); // lend a hand ourselves
                lock (this)
                {
                    if (_outstandingWorkersNeedsSync != 0 && !Monitor.Wait(this, millisecondsTimeout: 10_000))
                        throw new TimeoutException("Timeout waiting for parallel workers to complete");
                }
            }
            int _outstandingWorkersNeedsSync;

            public void Prepare(int bucketCount, Memory<T> keys, Memory<T> workspace)
            {
                _batchSize = ((keys.Length - 1) / _workerCount) + 1;
                _length = keys.Length;
                _keys = keys;
                _workspace = workspace;
                _bucketCount = bucketCount;
            }
            public unsafe Worker(int workerCount, uint* countsOffsets)
            {
                if (workerCount <= 0) throw new ArgumentOutOfRangeException(nameof(workerCount));
                _countsOffsets = countsOffsets;
                _workerCount = workerCount;
            }

            public void Swap() => Util.Swap(ref _keys, ref _workspace);

            internal void SetGroup(uint groupMask, int shift)
            {
                lock (this)
                {
                    _groupMask = groupMask;
                    _shift = shift;
                }
            }

            internal uint CountOffset(int index)
            {
                var allBuckets = AllCountsOffsets();
                int batchCount = allBuckets.Length / _bucketCount;

                uint sum = 0;
                for (int i = 0; i < batchCount; i++)
                {
                    sum += allBuckets[index];
                    index += _bucketCount;
                }
                return sum;
            }
        }

        private static unsafe int ParallelSort32<T>(Memory<T> keys, Memory<T> workspace,
            int r, bool descending, uint keyMask, NumberSystem numberSystem) where T : struct
        {
            r = Util.ChooseBitCount<uint>(r, DefaultR);
            int bucketCount = 1 << r, len = keys.Length;
            if (len <= 1 || keyMask == 0) return 0;
            
            workspace = workspace.Slice(0, len);
            int groups = Util.GroupCount<uint>(r);
            uint mask = (uint)(bucketCount - 1);

            int workerCount = Util.WorkerCount(keys.Length, MaxWorkerCount);
            // a shame that we nned to use "unsafe" for this, but we can't put a Span<uint> as
            // a field on the worker; however: the stack *won't* move, so this is in fact
            // perfectly safe, despite what it looks like
            uint* workerCountsOffsets = stackalloc uint[workerCount * bucketCount];

            var worker = new Worker<T>(workerCount, workerCountsOffsets);
            if ((keyMask & Util.MSB32U) == 0) numberSystem = NumberSystem.Unsigned; // without the MSB, sign doesn't matter

            bool reversed = false;
            if (numberSystem == NumberSystem.SignBit)
            {
                // sort *just* on the MSB
                var split = ParallelSortCore32(worker, keys, workspace, 1, !descending, Util.MSB32U, true, 2, 32, mask, 31);
                if (split.Reversed) Util.Swap(ref keys, ref workspace, ref reversed);
                keyMask &= ~Util.MSB32U;

                // now sort the two chunks separately, respecting the corresponding data/workspace areas
                // note: regardless of asc/desc, we will always want the first chunk to be decreasing magnitude and the second chunk to be increasing magnitude - hence false/true
                var lower = split.Split <= 1 ? default : ParallelSortCore32(worker, keys.Slice(0, split.Split), workspace.Slice(0, split.Split), r, false, keyMask, false, bucketCount, groups, mask);
                var upper = split.Split >= keys.Length - 1 ? default : ParallelSortCore32(worker, keys.Slice(split.Split), workspace.Slice(split.Split), r, true, keyMask, false, bucketCount, groups, mask);

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
            else if(ParallelSortCore32(worker, keys, workspace, r, !descending, keyMask, numberSystem != NumberSystem.Unsigned, bucketCount, groups, mask).Reversed)
            {
                Util.Swap(ref keys, ref workspace, ref reversed);
            }
            if (reversed)
            {
                worker.Prepare(bucketCount, keys, workspace);
                worker.Execute(WorkerStep.Copy);
            }
            return workerCount;
        }

        private static unsafe (bool Reversed, int Split) ParallelSortCore32<T>(Worker<T> worker, Memory<T> keys, Memory<T> workspace, int r, bool ascending, uint keyMask, bool isSigned, int bucketCount, int groups, uint mask, int c = 0) where T : struct
        {
            int invertC = isSigned ? groups - 1 : -1, split = -1;
            bool reversed = false;
            worker.Prepare(bucketCount, keys, workspace);
            for (int shift = c * r; c < groups; c++, shift += r)
            {
                uint groupMask = (keyMask >> shift) & mask;
                keyMask &= ~(mask << shift); // remove those bits from the keyMask to allow fast exit
                if (groupMask == 0)
                {
                    if (keyMask == 0) break;
                    else continue;
                }

                // counting elements of the c-th group
                worker.SetGroup(groupMask, shift);
                worker.Execute(ascending ? WorkerStep.BucketCountAscending : WorkerStep.BucketCountDescending);

                // the "split" is a trick used to sort IEEE754; tells us how many positive/negative
                // numbers we have (since we do a cheeky split on r=1/c=31); this allows us to to
                // two *inner* radix sorts on the rest of the bits
                if (split == -1)
                {
                    split = (int)worker.CountOffset(1);
                }

                if (!worker.ComputeOffsets(c == invertC ? GetInvertStartIndex(32, r) : 0)) continue; // all in one group

                worker.Execute(ascending ? WorkerStep.ApplyAscending : WorkerStep.ApplyDescending);
                worker.Swap();
                reversed = !reversed;
            }

            if (split < 0) split = 0;
            return (reversed, split);
        }
    }
}
