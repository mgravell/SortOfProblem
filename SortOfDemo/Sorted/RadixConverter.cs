using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public enum NumberSystem
    {
        Unsigned, // example: uint/ulong
        OnesComplement, // not common, but can be treated the same as twos-complement for the purposes of sorting
        TwosComplement, // example: int/long
        SignBit // example: float/double
    }
    public static partial class RadixConverter
    {
        internal const uint MSB32U = 1U << 31; // IEEE first bit is the sign bit
        internal const int MSB32 = 1 << 31; // IEEE first bit is the sign bit
        public static int ToSignedRadix(float value)
        {
            var val = BitConverter.SingleToInt32Bits(value);
            return (val & MSB32) == 0 ? val : ((~val) | MSB32);
        }
        public static void Register<TValue, TRadix>(NumberSystem numberSystem) where TValue : struct where TRadix : struct
        {
            RadixConverter<TRadix> converter;
            switch(numberSystem)
            {
                case NumberSystem.Unsigned: converter = RadixConverter<TRadix>.Unsigned; break;
                case NumberSystem.OnesComplement: converter = RadixConverter<TRadix>.OnesComplement; break;
                case NumberSystem.TwosComplement: converter = RadixConverter<TRadix>.TwosComplement; break;
                case NumberSystem.SignBit: converter = RadixConverter<TRadix>.SignBit; break;
                default: throw new ArgumentOutOfRangeException(nameof(numberSystem));
            }
            Register<TValue, TRadix>(converter);
        }
        public static void Register<TValue, TRadix>(RadixConverter<TRadix> converter) where TValue : struct where TRadix : struct
        {
            if (Unsafe.SizeOf<TValue>() != Unsafe.SizeOf<TRadix>())
                throw new InvalidOperationException($"The size of '{typeof(TValue).Name}' ({Unsafe.SizeOf<TValue>()} bytes) and '{typeof(TRadix).Name}' ({Unsafe.SizeOf<TRadix>()} bytes) must match");

            var old = Cache<TValue, TRadix>.Instance;
            if (old != null && old.IsInbuilt && !(converter.IsInbuilt))
                throw new InvalidOperationException($"The existing converter for '{typeof(TValue).Name}' is inbuilt and cannot be replaced");

            if (converter == null) throw new ArgumentNullException(nameof(converter));

            switch(converter.NumberSystem)
            {
                case NumberSystem.Unsigned:
                case NumberSystem.OnesComplement:
                case NumberSystem.TwosComplement:
                case NumberSystem.SignBit:
                    break; // fine
                default:
                    throw new ArgumentException($"{converter.GetType().Name} has a {nameof(RadixConverter<TRadix>.NumberSystem)} of {converter.NumberSystem}, which is not expected");
            }

            Cache<TValue, TRadix>.Instance = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public static RadixConverter<TRadix> Get<TValue, TRadix>() where TValue : struct where TRadix : struct
            => Cache<TValue, TRadix>.Instance ?? throw new InvalidOperationException($"No radix converter is registered to map between '{typeof(TValue).Name}' and '{typeof(TRadix).Name}'");
        internal static RadixConverter<TRadix> GetNonPassthruWithSignSupport<TValue, TRadix>(out NumberSystem numberSystem) where TValue : struct where TRadix : struct
        {
            var converter = Get<TValue, TRadix>();
            numberSystem = NumberSystem.Unsigned;
            if (converter != null)
            {
                numberSystem = converter.NumberSystem;
                if (converter.IsPassThru) converter = null;
            }
            return converter;
        }

        static class Cache<TValue, TRadix> where TValue : struct where TRadix : struct
        {
            public static RadixConverter<TRadix> Instance { get; set; }
        }
    }
}
