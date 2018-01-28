using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sorted
{
    public abstract class RadixConverter<T> where T : struct
    {
        [Conditional("DEBUG")]
        internal void Identify([CallerMemberName] string caller = null)
        {
            Console.WriteLine($"{GetType().Name}.{caller}");
        }
        public bool IsTrivial => this is NullConverter;
        public virtual bool IsSigned => false;
        public virtual bool IsTrivialWhenSigned => false;

        internal abstract class Inbuilt : RadixConverter<T> { }
        internal sealed class NullConverter : Inbuilt
        {
            public override void ToRadix(Span<T> source, Span<T> destination) { Identify(); source.CopyTo(destination); }
            public override void FromRadix(Span<T> source, Span<T> destination) { Identify(); source.CopyTo(destination); }
        }
        internal static RadixConverter<T> Null { get; } = new NullConverter();
        public abstract void ToRadix(Span<T> source, Span<T> destination);
        public abstract void FromRadix(Span<T> source, Span<T> destination);
    }
}
