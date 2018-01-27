using System;
using System.Runtime.InteropServices;

namespace Sorted
{
    partial class RadixSortUnsafe
    {
        public static void RegisterUnsafeConverters()
        {
            RadixConverter.Register<uint, uint>(RadixConverter<uint>.Null);
            RadixConverter.Register<int, uint>(new RadixConverterUnsafeInt32());
            RadixConverter.Register<float, uint>(new RadixConverterUnsafeSingle());
        }
        internal abstract unsafe class UnsafeInbuiltUInt32 : RadixConverter<uint>.Inbuilt
        {
            public override sealed void ToRadix(Span<uint> source, Span<uint> destination)
            {
                fixed (uint* from = &MemoryMarshal.GetReference(source))
                fixed (uint* to = &MemoryMarshal.GetReference(source))
                {
                    ToRadix(from, to, source.Length);
                }
            }
            public override sealed void FromRadix(Span<uint> source, Span<uint> destination)
            {
                fixed (uint* from = &MemoryMarshal.GetReference(source))
                fixed (uint* to = &MemoryMarshal.GetReference(source))
                {
                    FromRadix(from, to, source.Length);
                }
            }
            internal abstract void ToRadix(uint* source, uint* destination, int count);
            internal abstract void FromRadix(uint* source, uint* destination, int count);
        }
        private sealed class RadixConverterUnsafeInt32 : UnsafeInbuiltUInt32
        {
            internal override unsafe void ToRadix(uint* source, uint* destination, int count)
            {   
                while(count-- != 0)
                    *destination++ = (uint)((int)*source++ - int.MinValue);
            }
            internal override unsafe void FromRadix(uint* source, uint* destination, int count)
            {
                while (count-- != 0)
                    *destination++ = (uint)((int)*source++ + int.MinValue);
            }
        }
        private sealed unsafe class RadixConverterUnsafeSingle : UnsafeInbuiltUInt32
        {
            const uint MSB = 1U << 31; // IEEE first bit is the sign bit
            internal override void ToRadix(uint* source, uint* destination, int count)
            {
                unchecked
                {
                    while (count-- != 0)
                    {
                        var val = *source++;
                        if ((val & MSB) != 0)
                        {
                            // is negative; shoult interpret as -(the value without the MSB) - not the same as just
                            // dropping the bit, since integer math is twos-complement
                            val = (uint)(-((int)(val & ~MSB)));
                        }
                        *destination++ = val;
                    }
                }
            }
            internal override void FromRadix(uint* source, uint* destination, int count)
            {
                unchecked
                {
                    while (count-- != 0)
                    {
                        var val = *source++;
                        if ((val & MSB) != 0)
                        {
                            val = ((uint)-val) | MSB;
                        }
                        *destination++ = val;
                    }
                }
            }
        }
    }
}
