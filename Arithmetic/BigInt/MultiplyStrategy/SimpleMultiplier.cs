using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    private const int HalfDigitBits = 16;
    private const uint HalfDigitMask = 0xFFFF;

    public uint[] Multiply(uint[] a, uint[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        return MultiplyMagnitude(a, b);
    }

    internal static uint[] MultiplyMagnitude(uint[] a, uint[] b)
    {
        if (IsZero(a) || IsZero(b))
            return new uint[] { 0 };

        uint[] result = new uint[a.Length + b.Length];

        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                MulUint(a[i], b[j], out uint low, out uint high);

                uint acc = 0;
                result[i + j] = AddUint(result[i + j], low, ref acc);

                int k = i + j + 1;
                result[k] = AddUint(result[k], high, ref acc);
                k++;

                while (acc != 0 && k < result.Length)
                {
                    result[k] = AddUint(result[k], 0, ref acc);
                    k++;
                }
            }
        }

        return result;
    }

    internal static ReadOnlySpan<uint> Trimmed(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
            length--;

        return digits[..length];
    }

    internal static uint[] MultiplySchoolbook(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        a = Trimmed(a);
        b = Trimmed(b);
        if (a.Length == 0 || b.Length == 0)
            return [0];

        return MultiplyMagnitude(a.ToArray(), b.ToArray());
    }

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] digits = Multiply(a.GetDigits().ToArray(), b.GetDigits().ToArray());
        return new BetterBigInteger(digits, a.IsNegative != b.IsNegative);
    }

    private static bool IsZero(uint[] digits)
    {
        return digits.Length == 0 || digits.All(x => x == 0);
    }

    private static uint AddUint(uint a, uint b, ref uint acc)
    {
        uint aLow = a & HalfDigitMask;
        uint aHigh = a >> HalfDigitBits;

        uint bLow = b & HalfDigitMask;
        uint bHigh = b >> HalfDigitBits;

        uint lowSum = aLow + bLow + acc;
        uint newLow = lowSum & HalfDigitMask;
        uint accLow = lowSum >> HalfDigitBits;

        uint highSum = aHigh + bHigh + accLow;
        uint newHigh = highSum & HalfDigitMask;

        acc = highSum >> HalfDigitBits;

        return (newHigh << HalfDigitBits) | newLow;
    }

    private static void MulUint(uint a, uint b, out uint low, out uint high)
    {
        uint aLow = a & HalfDigitMask;
        uint aHigh = a >> HalfDigitBits;

        uint bLow = b & HalfDigitMask;
        uint bHigh = b >> HalfDigitBits;

        uint p0 = aLow * bLow;
        uint p1 = aLow * bHigh;
        uint p2 = aHigh * bLow;
        uint p3 = aHigh * bHigh;

        uint middle = (p0 >> HalfDigitBits) + (p1 & HalfDigitMask) + (p2 & HalfDigitMask);

        low = (middle << HalfDigitBits) | (p0 & HalfDigitMask);
        high = p3 + (p1 >> HalfDigitBits) + (p2 >> HalfDigitBits) + (middle >> HalfDigitBits);
    }
}
