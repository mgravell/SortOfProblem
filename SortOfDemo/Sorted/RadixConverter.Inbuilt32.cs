using System;

namespace Sorted
{
    partial class RadixConverter
    {
        static RadixConverter()
        {
            Register<uint, uint>(RadixConverter<uint>.UnsignedInternal);
            Register<int, uint>(RadixConverter<uint>.SignedInternal);
            Register<float, uint>(new RadixConverterSingle());
        }
        private sealed class RadixConverterSingle : RadixConverter<uint>
        {
            // algo can cope with signed 1s/2s-complement
            public override bool IsSigned => true;

            public override void ToRadix(Span<uint> source, Span<uint> destination)
            {
                Identify();
                unchecked
                {
                    // convert from IEEE754 MSB=sign to 1s-complement
                    for (int i = 0; i < source.Length; i++)
                    {
                        var val = source[i];
                        //if ((val & MSB32U) != 0)
                        //{   // preserve MSB; invert other bits
                        //    val = (~val) | MSB32U;
                        //}
                        //destination[i] = val;

                        // or: same thing without any branches;
                        var ifNeg = (uint)((int)val >> 31); // 11...11 or 00...00
                        destination[i] = (ifNeg & (~val | MSB32U)) | (~ifNeg & val);
                    }
                }
            }
            public override void FromRadix(Span<uint> source, Span<uint> destination)
            {
                Identify();
                unchecked
                {
                    for (int i = 0; i < source.Length; i++)
                    {
                        var val = source[i];
                        //if ((val & MSB32U) != 0)
                        //{
                        //    val = (~val) | MSB32U;
                        //}
                        //destination[i] = val;

                        // or: without any branches;
                        var ifNeg = (uint)((int)val >> 31);
                        destination[i] = (ifNeg & (~val | MSB32U)) | (~ifNeg & val);
                    }
                }
            }

            internal override bool IsInbuilt => true;
        }
    }
}
