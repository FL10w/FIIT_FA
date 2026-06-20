using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const ulong M1 = 998244353; // простые числа вида c * 2^k + 1, что позволяет делить массивы пополам k раз
    private const ulong M2 = 1004535809;
    private const ulong M3 = 469762049;

    private const ulong R1 = 3; // первообразные корни (генераторы) для соответствующих модулей, чтобы вычислять корни из единицы
    private const ulong R2 = 3;
    private const ulong R3 = 3;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsZero || b.IsZero)
            return BetterBigInteger.Zero;

        var aParts = To16Bit(a.GetDigits());
        var bParts = To16Bit(b.GetDigits());

        var c1 = PrepAndMult(aParts, bParts, M1, R1); // 3 независимых свертки (перемножения) по трем разным модулям
        var c2 = PrepAndMult(aParts, bParts, M2, R2);
        var c3 = PrepAndMult(aParts, bParts, M3, R3);

        ulong[] result = CRT(c1, c2, c3);

        return Normalize(result, a.IsNegative != b.IsNegative); // восстанавливаем переносы и запаковываем обратно в 32-битные куски
    }

    private static ulong[] PrepAndMult(uint[] a, uint[] b, ulong mod, ulong root) // свертка двух массивов
    {
        int n = 1;
        while (n < a.Length + b.Length) n <<= 1; // подбираем степень двойки

        ulong[] fa = new ulong[n];
        ulong[] fb = new ulong[n];

        for (int i = 0; i < a.Length; i++) fa[i] = a[i];
        for (int i = 0; i < b.Length; i++) fb[i] = b[i];

        NTT(fa, false, mod, root); // из коэфицентов в значения в точках
        NTT(fb, false, mod, root);

        for (int i = 0; i < n; i++)
            fa[i] = (fa[i] * fb[i]) % mod;

        NTT(fa, true, mod, root);

        return fa;
    }

    private static void NTT(ulong[] a, bool invert, ulong mod, ulong root)
    {
        int n = a.Length;

        // меняем элементы местами чтобы потом складывать in-place
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j |= bit;

            if (i < j)
                (a[i], a[j]) = (a[j], a[i]);
        }

        for (int len = 2; len <= n; len <<= 1) // длина текущего блока
        {
            ulong rootOfUnity = Pow(root, (mod - 1) / (ulong)len, mod);
            if (invert)
                rootOfUnity = ModInverse(rootOfUnity, mod); // обратный корень    

            for (int i = 0; i < n; i += len) // цикл по блокам
            {
                ulong w = 1;
                for (int j = 0; j < len / 2; j++)
                {
                    ulong u = a[i + j];
                    ulong v = a[i + j + len / 2] * w % mod;

                    a[i + j] = (u + v) % mod;
                    a[i + j + len / 2] = (u + mod - v) % mod;

                    w = (w * rootOfUnity) % mod;
                }
            }
        }

        if (invert)
        {
            ulong invN = ModInverse((ulong)n, mod);
            for (int i = 0; i < n; i++)
                a[i] = (a[i] * invN) % mod;
        }
    }

    private static ulong[] CRT(ulong[] a1, ulong[] a2, ulong[] a3) // Китайская Теорема об Остатках (Алгоритм Гарнера)
    {
        int n = a1.Length;
        ulong[] res = new ulong[n];

        ulong m1InvM2 = ModInverse(M1 % M2, M2);
        ulong m12 = M1 * M2;
        ulong m12InvM3 = ModInverse(m12 % M3, M3);

        for (int i = 0; i < n; i++) // x = a1 + t1 * M1 + t2 * (M1 * M2)
        {
            ulong x1 = a1[i];
            ulong x2 = a2[i];
            ulong x3 = a3[i];

            ulong t1 = ((x2 + M2 - x1 % M2) * m1InvM2) % M2;
            ulong r12 = x1 + t1 * M1;

            ulong t2 = ((x3 + M3 - r12 % M3) * m12InvM3) % M3;
            ulong r = r12 + t2 * m12;

            res[i] = r;
        }

        return res;
    }

    private static ulong Pow(ulong a, ulong e, ulong mod) // быстрое возведение в степень по модулю
    {
        ulong res = 1;
        while (e > 0)
        {
            if ((e & 1) != 0) // последний бит равен 1?
                res = (res * a) % mod;
            a = (a * a) % mod;
            e >>= 1;
        }
        return res;
    }

    private static ulong ModInverse(ulong x, ulong mod) // поиск обратного элемента по модулю с помощью малой теоремы ферма 
    {
        return Pow(x, mod - 2, mod); // x^(M−1) ≡ 1 (mod M)   =>   x^(-1) = a^(M-2) % M
    }

    private static uint[] To16Bit(ReadOnlySpan<uint> digits)
    {
        uint[] res = new uint[digits.Length * 2]; // тк 2 32-битных это 4 16-ти битных
        for (int i = 0; i < digits.Length; i++)
        {
            res[2 * i] = (uint)(digits[i] & 0xFFFF); // оставляем только правую часть
            res[2 * i + 1] = (uint)(digits[i] >> 16);
        }
        return res; // сдесь и левая и права половина каждого digits[i]
    }

    private static BetterBigInteger Normalize(ulong[] data, bool negative)
    {
        ulong carry = 0;

        for (int i = 0; i < data.Length; i++)
        {
            data[i] += carry;
            carry = data[i] >> 16;
            data[i] &= 0xFFFF;
        }

        List<uint> result = [];

        for (int i = 0; i < data.Length; i += 2)
        {
            uint low = (uint)data[i];
            uint high = (i + 1 < data.Length) ? (uint)data[i + 1] : 0;
            result.Add((high << 16) | low);
        }

        return new BetterBigInteger(result.ToArray(), negative);
    }
}