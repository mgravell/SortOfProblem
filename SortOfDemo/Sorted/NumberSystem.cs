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
    public static class NumberSystem<T>
    {
        private static NumberSystem? _value;
        internal static NumberSystem Value => _value
            ?? throw new InvalidOperationException($"No number-system is defined for '{typeof(T).Name}'");

        public static int Length => Unsafe.SizeOf<T>();
        static NumberSystem()
        {
            if (typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) || 
                typeof(T) == typeof(int) ||
                typeof(T) == typeof(long)
                ) _value = NumberSystem.TwosComplement;
            else if (
                typeof(T) == typeof(bool) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(char) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(ulong)
                ) _value = NumberSystem.Unsigned;
            else if (
                typeof(T) == typeof(float) ||
                typeof(T) == typeof(double)
                ) _value = NumberSystem.SignBit;   
        }
        public static void Set(NumberSystem numberSystem)
        {
            var existing = _value;
            if (existing == null)
            {
                switch(numberSystem)
                {
                    case NumberSystem.Unsigned:
                    case NumberSystem.OnesComplement:
                    case NumberSystem.TwosComplement:
                    case NumberSystem.SignBit:
                        _value = numberSystem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(numberSystem));
                }                
            }
            else if (numberSystem != existing.Value)
            {
                throw new InvalidOperationException($"The number-system for '{typeof(T).Name}' has already been set and cannot be changed");
            }
        }
    }
}
