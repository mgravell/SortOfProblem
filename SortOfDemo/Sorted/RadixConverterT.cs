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
        
        public virtual bool IsSigned => false;

        internal sealed class PassThruConverter : RadixConverter<T>
        {
            private readonly byte _flags;
            internal PassThruConverter(byte flags) { _flags = flags; }

            public override bool IsPassThru => true;
            public override bool IsSigned => (_flags & 1) != 0;
            internal override bool IsInbuilt => (_flags & 2) != 0;
            public override void ToRadix(Span<T> source, Span<T> destination) { Identify(); source.CopyTo(destination); }
            public override void FromRadix(Span<T> source, Span<T> destination) { Identify(); source.CopyTo(destination); }
        }

        internal static PassThruConverter Unsigned { get; } = new PassThruConverter(0);
        internal static PassThruConverter Signed { get; } = new PassThruConverter(1);
        internal static PassThruConverter UnsignedInternal { get; } = new PassThruConverter(2);
        internal static PassThruConverter SignedInternal { get; } = new PassThruConverter(3);

        public abstract void ToRadix(Span<T> source, Span<T> destination);
        public abstract void FromRadix(Span<T> source, Span<T> destination);
    }
}
