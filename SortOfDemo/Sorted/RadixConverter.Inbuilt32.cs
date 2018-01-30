using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sorted
{
    partial class RadixConverter
    {
        static RadixConverter()
        {
            Register<uint, uint>(RadixConverter<uint>.UnsignedInbuilt);
            Register<int, uint>(RadixConverter<uint>.TwosComplementInbuilt);
            Register<float, uint>(new RadixConverterSingle());
        }
        private sealed class RadixConverterSingle : RadixConverter<uint>
        {
            static void InvertNegativesRetainingMSB(Span<uint> source, Span<uint> destination)
            {
                unchecked
                {
                    // convert from IEEE754 MSB=sign to 1s-complement
                    // "human" version:

                    //if ((val & MSB32U) != 0)
                    //{   // preserve MSB; invert other bits
                    //    val = ~val | MSB32U;
                    //}
                    //destination[i] = val;

                    int i = 0;
                    if (Vector.IsHardwareAccelerated) // note the JIT removes this test
                    {                               // (and all the code if it isn't true)
                        var vSource = source.NonPortableCast<uint, Vector<uint>>();
                        var vDest = destination.NonPortableCast<uint, Vector<uint>>();
                        var MSB = new Vector<uint>(MSB32U);
                        var NOMSB = ~MSB;
                        for (int j = 0; j < vSource.Length; j++)
                        {
                            var vec = vSource[j];
                            vDest[j] = Vector.ConditionalSelect(
                                condition: Vector.GreaterThan(vec, NOMSB),
                                left: ~vec | MSB, // when true
                                right: vec // when false
                            );
                        }
                        // change our root offset for the remainder of the values
                        i = vSource.Length * Vector<uint>.Count;
                    }

                    for (; i < source.Length; i++)
                    {
                        var val = source[i];
                        var ifNeg = (uint)((int)val >> 31); // 11...11 or 00...00
                        destination[i] = (ifNeg & (~val | MSB32U)) | (~ifNeg & val);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void ToRadix(Span<uint> source, Span<uint> destination)
                => InvertNegativesRetainingMSB(source, destination);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void FromRadix(Span<uint> source, Span<uint> destination)
                => InvertNegativesRetainingMSB(source, destination);

            public override NumberSystem NumberSystem => NumberSystem.OnesComplement; 

            internal override bool IsInbuilt => true;
        }
    }
}
