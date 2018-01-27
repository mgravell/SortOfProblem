using System;
using System.Numerics;

namespace Sorted
{
    partial class RadixConverter
    {
        static RadixConverter()
        {
            Register<uint, uint>(RadixConverter<uint>.Null);
            Register<int, uint>(new RadixConverterInt32());
            Register<float, uint>(new RadixConverterSingle());
        }
        public static bool AllowVectorization { get; set; } = Vector.IsHardwareAccelerated;
        private sealed class RadixConverterInt32 : RadixConverter<uint>.Inbuilt
        {
            public override void ToRadix(Span<uint> source, Span<uint> destination)
            {
                unchecked
                {
                    int i = 0;
                    if(Vector.IsHardwareAccelerated && AllowVectorization && source.Length >= Vector<int>.Count)
                    {
                        var minValue = new Vector<int>(int.MinValue);
                        var s = source.NonPortableCast<uint, Vector<int>>();
                        var d = destination.NonPortableCast<uint, Vector<int>>();
                        for (i = 0; i < s.Length;i++)
                        {
                            d[i] = Vector.Subtract(s[i], minValue);
                        }


                        i = s.Length * Vector<int>.Count;
                    }
                    for (; i < source.Length; i++)
                        destination[i] = (uint)((int)source[i] - int.MinValue);
                }
            }
            public override void FromRadix(Span<uint> source, Span<uint> destination)
            {
                unchecked
                {
                    int i = 0;
                    if (Vector.IsHardwareAccelerated && AllowVectorization && source.Length >= Vector<int>.Count)
                    {
                        var minValue = new Vector<int>(int.MinValue);
                        var s = source.NonPortableCast<uint, Vector<int>>();
                        var d = destination.NonPortableCast<uint, Vector<int>>();
                        for (i = 0; i < s.Length; i++)
                        {
                            d[i] = Vector.Add(s[i], minValue);
                        }


                        i = s.Length * Vector<int>.Count;
                    }
                    for (; i < source.Length; i++)
                        destination[i] = (uint)((int)source[i] + int.MinValue);
                        
                }
            }
        }
        private sealed class RadixConverterSingle : RadixConverter<uint>.Inbuilt
        {
            const uint MSB = 1U << 31; // IEEE first bit is the sign bit
            public override void ToRadix(Span<uint> source, Span<uint> destination)
            {
                unchecked
                {
                    for (int i = 0; i < source.Length; i++)
                    {
                        var val = source[i];
                        if ((val & MSB) != 0)
                        {
                            // is negative; shoult interpret as -(the value without the MSB) - not the same as just
                            // dropping the bit, since integer math is twos-complement
                            val = (uint)(-((int)(val & ~MSB)));
                        }
                        destination[i] = val;
                    }

                }
            }
            public override void FromRadix(Span<uint> source, Span<uint> destination)
            {
                unchecked
                {
                    for (int i = 0; i < source.Length; i++)
                    {
                        var val = source[i];
                        if ((val & MSB) != 0)
                        {
                            val = ((uint)-val) | MSB;
                        }
                        destination[i] = val;
                    }
                }
            }
        }
    }
}
