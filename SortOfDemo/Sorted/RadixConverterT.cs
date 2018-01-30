using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public abstract class RadixConverter<T> where T : struct
    {
        internal virtual bool IsInbuilt => false;
        public virtual bool IsPassThru => false;
        [Conditional("DEBUG")]
        internal void Identify([CallerMemberName] string caller = null)
        {
            Console.WriteLine($"{GetType().Name}.{caller}");
        }

        public virtual NumberSystem NumberSystem => NumberSystem.Unsigned;

        internal sealed class PassThruConverter : RadixConverter<T>
        {
            internal PassThruConverter(NumberSystem numberSystem, bool isInbuilt)
            {
                NumberSystem = numberSystem;
                IsInbuilt = isInbuilt;
            }

            public override bool IsPassThru => true;
            public override NumberSystem NumberSystem { get; }
            internal override bool IsInbuilt { get; }
            public override void ToRadix(Span<T> source, Span<T> destination) { Identify(); source.CopyTo(destination); }
            public override void FromRadix(Span<T> source, Span<T> destination) { Identify(); source.CopyTo(destination); }
        }

        internal static PassThruConverter Unsigned { get; } = new PassThruConverter(NumberSystem.Unsigned, false);
        internal static PassThruConverter TwosComplement { get; } = new PassThruConverter(NumberSystem.TwosComplement, false);
        internal static PassThruConverter OnesComplement { get; } = new PassThruConverter(NumberSystem.OnesComplement, false);
        internal static PassThruConverter SignBit { get; } = new PassThruConverter(NumberSystem.SignBit, false);
        internal static PassThruConverter UnsignedInbuilt { get; } = new PassThruConverter(NumberSystem.Unsigned, true);
        internal static PassThruConverter TwosComplementInbuilt { get; } = new PassThruConverter(NumberSystem.TwosComplement, true);
        internal static PassThruConverter OnesComplementInbuilt { get; } = new PassThruConverter(NumberSystem.OnesComplement, true);
        internal static PassThruConverter SignBitInbuilt { get; } = new PassThruConverter(NumberSystem.SignBit, true);

        public abstract void ToRadix(Span<T> source, Span<T> destination);
        public abstract void FromRadix(Span<T> source, Span<T> destination);
    }
}
