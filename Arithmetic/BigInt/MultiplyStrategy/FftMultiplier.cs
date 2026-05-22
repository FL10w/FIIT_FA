using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    public uint[] Multiply(uint[] a, uint[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        BetterBigInteger left = new(a, false);
        BetterBigInteger right = new(b, false);
        BetterBigInteger product = Multiply(left, right);
        return product.GetDigits().ToArray();
    }

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (IsZero(a) || IsZero(b))
        {
            return new BetterBigInteger([0]);
        }

        bool isNegative = a.IsNegative ^ b.IsNegative;
        string left = new BetterBigInteger(a.GetDigits().ToArray(), false).ToString(10);
        string right = new BetterBigInteger(b.GetDigits().ToArray(), false).ToString(10);

        int[] leftDigits = ToReversedDigits(left);
        int[] rightDigits = ToReversedDigits(right);
        long[] convolution = Convolve(leftDigits, rightDigits);
        string product = NormalizeDecimalDigits(convolution);

        if (isNegative && product != "0")
        {
            product = "-" + product;
        }

        return new BetterBigInteger(product, 10);
    }

    private static bool IsZero(BetterBigInteger value)
    {
        ReadOnlySpan<uint> digits = value.GetDigits();
        return digits.Length == 0 || digits.Length == 1 && digits[0] == 0;
    }

    private static int[] ToReversedDigits(string value)
    {
        int[] digits = new int[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            digits[i] = value[value.Length - 1 - i] - '0';
        }

        return digits;
    }

    private static long[] Convolve(int[] left, int[] right)
    {
        int size = 1;
        int need = left.Length + right.Length;
        while (size < need)
        {
            size <<= 1;
        }

        Complex[] fa = new Complex[size];
        Complex[] fb = new Complex[size];
        for (int i = 0; i < left.Length; i++)
        {
            fa[i] = new Complex(left[i], 0);
        }

        for (int i = 0; i < right.Length; i++)
        {
            fb[i] = new Complex(right[i], 0);
        }

        Fft(fa, invert: false);
        Fft(fb, invert: false);

        for (int i = 0; i < size; i++)
        {
            fa[i] *= fb[i];
        }

        Fft(fa, invert: true);

        long[] result = new long[need];
        for (int i = 0; i < need; i++)
        {
            result[i] = (long)Math.Round(fa[i].Real);
        }

        return result;
    }

    private static void Fft(Complex[] values, bool invert)
    {
        int n = values.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }

            j ^= bit;
            if (i < j)
            {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = 2 * Math.PI / len * (invert ? -1 : 1);
            Complex wLen = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;
                int half = len >> 1;
                for (int j = 0; j < half; j++)
                {
                    Complex u = values[i + j];
                    Complex v = values[i + j + half] * w;
                    values[i + j] = u + v;
                    values[i + j + half] = u - v;
                    w *= wLen;
                }
            }
        }

        if (!invert)
        {
            return;
        }

        for (int i = 0; i < n; i++)
        {
            values[i] /= n;
        }
    }

    private static string NormalizeDecimalDigits(long[] digits)
    {
        long carry = 0;
        for (int i = 0; i < digits.Length; i++)
        {
            long current = digits[i] + carry;
            carry = current / 10;
            long remainder = current % 10;
            if (remainder < 0)
            {
                remainder += 10;
                carry--;
            }

            digits[i] = remainder;
        }

        while (carry > 0)
        {
            Array.Resize(ref digits, digits.Length + 1);
            digits[^1] = carry % 10;
            carry /= 10;
        }

        int last = digits.Length - 1;
        while (last > 0 && digits[last] == 0)
        {
            last--;
        }

        char[] chars = new char[last + 1];
        for (int i = 0; i <= last; i++)
        {
            chars[i] = (char)('0' + digits[last - i]);
        }

        return new string(chars);
    }
}
