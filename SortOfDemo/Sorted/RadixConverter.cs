using System;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public static partial class RadixConverter
    {
        public static void Register<TValue, TRadix>(RadixConverter<TRadix> converter) where TValue : struct where TRadix : struct
        {
            if (Unsafe.SizeOf<TValue>() != Unsafe.SizeOf<TRadix>())
                throw new InvalidOperationException($"The size of '{typeof(TValue).Name}' ({Unsafe.SizeOf<TValue>()} bytes) and '{typeof(TRadix).Name}' ({Unsafe.SizeOf<TRadix>()} bytes) must match");

            var old = Cache<TValue, TRadix>.Instance;
            if (old is RadixConverter<TRadix>.Inbuilt)
                throw new InvalidOperationException($"The existing converter for '{typeof(TValue).Name}' is inbuilt and cannot be replaced");

            Cache<TValue, TRadix>.Instance = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public static RadixConverter<TRadix> Get<TValue, TRadix>() where TValue : struct where TRadix : struct
            => Cache<TValue, TRadix>.Instance ?? throw new InvalidOperationException($"No radix converter is registered to map between '{typeof(TValue).Name}' and '{typeof(TRadix).Name}'");

        internal static RadixConverter<TRadix> GetNonPassthru<TValue, TRadix>() where TValue : struct where TRadix : struct
        {
            var converter = Get<TValue, TRadix>();
            return converter is RadixConverter<TRadix>.NullConverter ? null : converter;
        }

        static class Cache<TValue, TRadix> where TValue : struct where TRadix : struct
        {
            public static RadixConverter<TRadix> Instance { get; set; }
        }
    }
}
