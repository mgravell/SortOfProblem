namespace Sorted
{
    partial class RadixConverter
    {
        static RadixConverter()
        {
            Register<uint, uint>(RadixConverter<uint>.UnsignedInbuilt);
            Register<int, uint>(RadixConverter<uint>.TwosComplementInbuilt);
            Register<float, uint>(RadixConverter<uint>.SignBit);
        }
    }
}
