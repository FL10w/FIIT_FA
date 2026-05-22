using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    private const int HalfDigitBits = 16;
    private const uint HalfDigitMask = 0xFFFF;

    public uint[] Multiply(uint[] a, uint[] b)
    {
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
