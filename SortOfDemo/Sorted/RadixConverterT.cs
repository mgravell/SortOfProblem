using System;

namespace Sorted
{
    public abstract class RadixConverter<T> where T : struct
    {
        public bool IsNull => this is NullConverter;

        internal abstract class Inbuilt : RadixConverter<T> { }
        internal sealed class NullConverter : Inbuilt
        {
            public override void ToRadix(Span<T> source, Span<T> destination) { source.CopyTo(destination); }
            public override void FromRadix(Span<T> source, Span<T> destination) { source.CopyTo(destination); }
        }
        internal static RadixConverter<T> Null { get; } = new NullConverter();
        public abstract void ToRadix(Span<T> source, Span<T> destination);
        public abstract void FromRadix(Span<T> source, Span<T> destination);
    }
}
