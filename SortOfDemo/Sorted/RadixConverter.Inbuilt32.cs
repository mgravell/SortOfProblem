using System;
using System.Numerics;
using System.Runtime.CompilerServices;

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
            public override void ToRadix(Span<uint> unsignedSource, Span<uint> unsignedDestination)
            {
                var source = unsignedSource.NonPortableCast<uint, int>();
                var destination = unsignedDestination.NonPortableCast<uint, int>();

                const int MSB = 1 << 31;
                int i = 0;
                if (Vector.IsHardwareAccelerated) // note the JIT removes this test
                {                               // (and all the code if it isn't true)
                    var vSource = source.NonPortableCast<int, Vector<int>>();
                    var vDest = destination.NonPortableCast<int, Vector<int>>();
                    var vMSB = new Vector<int>(MSB);
                    var vNOMSB = ~vMSB;
                    var vMin = new Vector<int>(int.MinValue);
                    for (int j = 0; j < vSource.Length; j++)
                    {
                        var vec = vSource[j];
                        vDest[j] = Vector.ConditionalSelect(
                            condition: Vector.LessThan(vec, Vector<int>.Zero),
                            left: -(vec & vNOMSB) - Vector<int>.One, // when true
                            right: vec // when false
                        ) - vMin;
                    }
                    // change our root offset for the remainder of the values
                    i = vSource.Length * Vector<int>.Count;
                }

                for (; i < source.Length; i++)
                {
                    var val = source[i];
                    var ifNeg = val >> 31; // 11...11 or 00...00
                    destination[i] = (
                        (ifNeg & (-(val & ~MSB) - 1)) // true
                        | (~ifNeg & val) // false
                    ) - int.MinValue;
                }
            }

            public override void FromRadix(Span<uint> unsignedSource, Span<uint> unsignedDestination)
            {
                var source = unsignedSource.NonPortableCast<uint, int>();
                var destination = unsignedDestination.NonPortableCast<uint, int>();

                const int MSB = 1 << 31;
                int i = 0;
                if (Vector.IsHardwareAccelerated) // note the JIT removes this test
                {                               // (and all the code if it isn't true)
                    var vSource = source.NonPortableCast<int, Vector<int>>();
                    var vDest = destination.NonPortableCast<int, Vector<int>>();
                    var vMSB = new Vector<int>(MSB);
                    var vMin = new Vector<int>(int.MinValue);
                    for (int j = 0; j < vSource.Length; j++)
                    {
                        var vec = vSource[j] + vMin;
                        vDest[j] = Vector.ConditionalSelect(
                            condition: Vector.LessThan(vec, Vector<int>.Zero),
                            left: -(vec + Vector<int>.One) | vMSB, // when true
                            right: vec // when false
                        );
                    }
                    // change our root offset for the remainder of the values
                    i = vSource.Length * Vector<int>.Count;
                }

                for (; i < source.Length; i++)
                {
                    var val = source[i] + int.MinValue;
                    var ifNeg = val >> 31; // 11...11 or 00...00
                    destination[i] =
                        (ifNeg & (-(val + 1) | MSB)) // true
                        | (~ifNeg & val); // false
                }
            }
            //static void InvertNegativesRetainingMSB(Span<uint> source, Span<uint> destination)
            //{
            //    unchecked
            //    {
            //        // convert from IEEE754 MSB=sign to 1s-complement
            //        // "human" version:

            //        //if ((val & MSB32U) != 0)
            //        //{   // preserve MSB; invert other bits
            //        //    val = ~val | MSB32U;
            //        //}
            //        //destination[i] = val;

            //        int i = 0;
            //        if (Vector.IsHardwareAccelerated) // note the JIT removes this test
            //        {                               // (and all the code if it isn't true)
            //            var vSource = source.NonPortableCast<uint, Vector<uint>>();
            //            var vDest = destination.NonPortableCast<uint, Vector<uint>>();
            //            var MSB = new Vector<uint>(MSB32U);
            //            var NOMSB = ~MSB;
            //            for (int j = 0; j < vSource.Length; j++)
            //            {
            //                var vec = vSource[j];
            //                vDest[j] = Vector.ConditionalSelect(
            //                    condition: Vector.GreaterThan(vec, NOMSB),
            //                    left: ~vec | MSB, // when true
            //                    right: vec // when false
            //                );
            //            }
            //            // change our root offset for the remainder of the values
            //            i = vSource.Length * Vector<uint>.Count;
            //        }

            //        for (; i < source.Length; i++)
            //        {
            //            var val = source[i];
            //            var ifNeg = (uint)((int)val >> 31); // 11...11 or 00...00
            //            destination[i] = (ifNeg & (~val | MSB32U)) | (~ifNeg & val);
            //        }
            //    }
            //}

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public override void ToRadix(Span<uint> source, Span<uint> destination)
            //    => InvertNegativesRetainingMSB(source, destination);
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public override void FromRadix(Span<uint> source, Span<uint> destination)
            //    => InvertNegativesRetainingMSB(source, destination);

            // algo can cope with signed 1s/2s-complement
            //public override bool IsSigned => true;

            internal override bool IsInbuilt => true;
        }
    }
}
