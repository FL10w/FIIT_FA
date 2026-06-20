using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int SchoolbookThreshold = 32;

    public uint[] Multiply(uint[] a, uint[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        return Karatsuba(a, b);
    }

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var x = SimpleMultiplier.Trimmed(a.GetDigits());
        var y = SimpleMultiplier.Trimmed(b.GetDigits());
        if (x.Length == 0 || y.Length == 0) return new BetterBigInteger([0]);

        uint[] mag = Karatsuba(x, y);
        bool neg = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(mag, neg);
    }

    internal static uint[] Karatsuba(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        a = SimpleMultiplier.Trimmed(a);
        b = SimpleMultiplier.Trimmed(b);
        if (a.Length == 0 || b.Length == 0) return [0u];
        if (a.Length <= SchoolbookThreshold || b.Length <= SchoolbookThreshold)
            return SimpleMultiplier.MultiplySchoolbook(a, b);

        int n = Math.Max(a.Length, b.Length);
        int m = (n + 1) / 2;

        var a0 = a[..Math.Min(m, a.Length)];
        var a1 = a.Length > m ? a[m..] : ReadOnlySpan<uint>.Empty;
        var b0 = b[..Math.Min(m, b.Length)];
        var b1 = b.Length > m ? b[m..] : ReadOnlySpan<uint>.Empty;

        uint[] z0 = Karatsuba(a0, b0);
        uint[] z2 = Karatsuba(a1, b1);

        uint[] a0PlusA1 = Add(a0, a1);
        uint[] b0PlusB1 = Add(b0, b1);
        uint[] z1 = Karatsuba(a0PlusA1, b0PlusB1);

        z1 = Sub(z1, z0);
        z1 = Sub(z1, z2);

        uint[] res = AddShifted(z0, z1, m);
        res = AddShifted(res, z2, 2 * m);
        return Trim(res);
    }

    private static uint[] Add(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int n = Math.Max(a.Length, b.Length);
        var res = new uint[n + 1];
        ulong carry = 0;
        for (int i = 0; i < n; i++)
        {
            ulong av = i < a.Length ? a[i] : 0;
            ulong bv = i < b.Length ? b[i] : 0;
            ulong sum = av + bv + carry;
            res[i] = (uint)sum;
            carry = sum >> 32;
        }
        if (carry != 0) res[n] = (uint)carry;
        return Trim(res);
    }

    private static uint[] Sub(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        a = SimpleMultiplier.Trimmed(a);
        b = SimpleMultiplier.Trimmed(b);
        var res = new uint[a.Length];
        long borrow = 0;
        for (int i = 0; i < a.Length; i++)
        {
            long av = (long)a[i];
            long bv = i < b.Length ? (long)b[i] : 0;
            long diff = av - bv - borrow;
            if (diff < 0)
            {
                diff += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }
            res[i] = (uint)diff;
        }
        return Trim(res);
    }

    private static uint[] AddShifted(ReadOnlySpan<uint> baseArr, ReadOnlySpan<uint> addArr, int shiftLimbs)
    {
        baseArr = SimpleMultiplier.Trimmed(baseArr);
        addArr = SimpleMultiplier.Trimmed(addArr);
        if (addArr.Length == 0) return baseArr.ToArray();

        int need = Math.Max(baseArr.Length, addArr.Length + shiftLimbs) + 1;
        var res = new uint[need];
        baseArr.CopyTo(res);

        ulong carry = 0;
        int i = 0;
        for (; i < addArr.Length; i++)
        {
            int idx = i + shiftLimbs;
            ulong sum = (ulong)res[idx] + addArr[i] + carry;
            res[idx] = (uint)sum;
            carry = sum >> 32;
        }
        int k = i + shiftLimbs;
        while (carry != 0)
        {
            ulong sum = (ulong)res[k] + carry;
            res[k] = (uint)sum;
            carry = sum >> 32;
            k++;
        }
        return Trim(res);
    }

    private static uint[] Trim(uint[] arr)
    {
        int len = arr.Length;
        while (len > 1 && arr[len - 1] == 0) len--;
        if (len == arr.Length) return arr;
        var outArr = new uint[len];
        Array.Copy(arr, outArr, len);
        return outArr;
    }
}
