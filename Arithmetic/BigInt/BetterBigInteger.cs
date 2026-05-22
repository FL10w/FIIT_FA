using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit; // 0+ 1-
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;

    private static readonly IMultiplier Multiplier = new SimpleMultiplier(); 
    private const int HalfDigitBits = 16;
    private const uint HalfDigitMask = 0xFFFF;

    
    /// От массива цифр
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null) throw new ArgumentNullException(nameof(digits));
        if (digits.Length == 0)
        {
            _smallValue = 0;
            _data = null;
            _signBit = 0;
            return;
        }
        int last = digits.Length - 1;
        while (last >= 0 && digits[last] == 0) last--; // пропускаем все нули с конца
        if (last < 0) // значит у нас 0
        {
            _smallValue = 0;
            _data = null;
            _signBit = 0;
            return;
        }
        if (last == 0)
        {
            _smallValue = digits[0];
            _data = null;
            if (isNegative) _signBit = 1;
            else _signBit = 0;

            if (_smallValue == 0) _signBit = 0;
            return;
        }
        _data = new uint[last + 1];
        Array.Copy(digits, _data, last + 1); // копируем last+1 элементов в _data
        _smallValue = 0;
        if (isNegative) _signBit = 1;
        else _signBit = 0;
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
        : this(digits.ToArray(), isNegative)
    {
    }
    
    public BetterBigInteger(string value, int radix)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (radix < 2 || radix > 36) throw new ArgumentOutOfRangeException(nameof(radix));
        value = value.Trim();
        if (value.Length == 0) throw new FormatException("Empty string");
        bool isNegative = false;
        int start = 0;
        if (value[0] == '-')
        {
            isNegative = true;
            start = 1;
        }
        else if (value[0] == '+') start = 1;
        if (start == value.Length) throw new FormatException("No digits");
        BetterBigInteger result = new BetterBigInteger(new uint[] { 0 });
        BetterBigInteger baseValue = new BetterBigInteger(new uint[] { (uint)radix });
        for (int i = start; i < value.Length; i++)
        {
            int digit;
            char ch = value[i];
            if (ch >= '0' && ch <= '9') digit = ch - '0';
            else if (ch >= 'A' && ch <= 'Z') digit = ch - 'A' + 10;
            else if (ch >= 'a' && ch <= 'z') digit = ch - 'a' + 10;
            else throw new FormatException("Invalid digit");
            if (digit >= radix) throw new FormatException("Digit is too lafge gor radix");
            result = result * baseValue + new BetterBigInteger(new uint[] { (uint)digit});
        }
        _smallValue = result._smallValue;
        _data = result._data;
        _signBit = isNegative ? 1 : 0;

        if (IsZero()) _signBit = 0;
    }
    
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue]; // если _data не null, то вернуть _data
    }

    private static int CompareModule(BetterBigInteger a, BetterBigInteger b) // сравнение модулей (для вычитания)
    {
        var ad = a.GetDigits();
        var bd = b.GetDigits();

        if (ad.Length > bd.Length) return 1;
        if (bd.Length > ad.Length) return -1;
        for (int i = ad.Length - 1; i >= 0; i--)
        {
            if (ad[i] > bd[i]) return 1;
            if (ad[i] < bd[i]) return -1;
        }
        return 0;
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other == null) return 1; // текущее больше
        BetterBigInteger b = (BetterBigInteger)other;
        if (this._signBit != b._signBit)
        {
            if (this._signBit == 1) return -1;
            else return 1;
        }
        int cmp = CompareModule(this, b);
        if (this._signBit == 0) return cmp;
        else return -cmp;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other == null) return false;
        BetterBigInteger b = (BetterBigInteger)other;
        if (this._signBit != b._signBit) return false;
        return CompareModule(this, b) == 0;

    }
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 13;
        hash = hash * 31 + this._signBit;
        var digits = GetDigits(); 
        for (int i = 0; i < digits.Length; i++) hash = 31 * hash + digits[i].GetHashCode();
        return hash;
    }

    private static uint AddUint(uint a, uint b, ref uint acc)
    {
        uint aLow = a & HalfDigitMask; // младшие 16 бит (справа)
        uint aHigh = a >> HalfDigitBits; // страшие 16 бит (слева)

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

    private static BetterBigInteger AddUnsigned(BetterBigInteger a, BetterBigInteger b)
    {
        var aDigits = a.GetDigits();
        var bDigits = b.GetDigits();
        int maxLength = Math.Max(aDigits.Length, bDigits.Length);
        uint[] result = new uint[maxLength + 1];
        uint acc = 0;
        for (int i = 0; i < maxLength; i++)
        {
            uint aDigit = 0;
            if (i < aDigits.Length) aDigit = aDigits[i];
            uint bDigit = 0;
            if (i < bDigits.Length) bDigit = bDigits[i];
            result[i] = AddUint(aDigit, bDigit, ref acc);
        }
        result[maxLength] = acc;
        return new BetterBigInteger(result, false);
    }

    private static uint SubUint(uint a, uint b, ref uint borrow)
    {
        uint aLow = a & HalfDigitMask;
        uint aHigh = a >> HalfDigitBits;

        uint bLow = b & HalfDigitMask;
        uint bHigh = b >> HalfDigitBits;

        uint lowSub;
        uint borrowLow;
        if (aLow >= bLow + borrow)
        {
            lowSub = aLow - bLow - borrow;
            borrowLow = 0;
        }
        else
        {
            lowSub = (uint)(0x10000 + aLow - bLow - borrow);
            borrowLow = 1;
        }

        uint highSub;
        if (aHigh >= bHigh + borrowLow)
        {
            highSub = aHigh - bHigh - borrowLow;
            borrow = 0;
        }
        else
        {
            highSub = (uint)(0x10000 + aHigh - bHigh - borrowLow);
            borrow = 1;
        }
        return (highSub << HalfDigitBits) | lowSub;
    }

    private static BetterBigInteger SubUnsigned(BetterBigInteger a, BetterBigInteger b)
    {
        var aDigits = a.GetDigits();
        var bDigits = b.GetDigits();
        int maxLength = aDigits.Length;
        uint[] result = new uint[maxLength];
        uint borrow = 0;
        for (int i = 0; i < maxLength; i++)
        {
            uint aDigit = aDigits[i];
            uint bDigit;
            if (i < bDigits.Length) bDigit = bDigits[i];
            else bDigit = 0;
            result[i] = SubUint(aDigit, bDigit, ref borrow);
        }
        return new BetterBigInteger(result, false);
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        if (a._signBit == b._signBit)
        {
            BetterBigInteger result = AddUnsigned(a, b);
            result._signBit = a._signBit;
            if (result.GetDigits().Length == 1 && result.GetDigits()[0] == 0) result._signBit = 0;
            return result;
        }
        int cmp = CompareModule(a, b);
        if (cmp == 0) return new BetterBigInteger(new uint[] { 0 });
        if (cmp > 0)
        {
            BetterBigInteger result = SubUnsigned(a, b);
            result._signBit = a._signBit;
            return result;
        }
        else
        {
            BetterBigInteger result = SubUnsigned(b, a);
            result._signBit = b._signBit;
            return result;
        }
    }
    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        return a + (-b); // BetterBigInteger.operator+(a, -b); компилятор возвращает это
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        BetterBigInteger result = new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);
        if (result.GetDigits().Length == 1 && result.GetDigits()[0] == 0) result._signBit = 0;
        return result;
    }

    private bool IsZero()
    {
        var d = GetDigits();
        return d.Length == 1 && d[0] == 0;
    }

    private static int BitLength(BetterBigInteger x) // сколько бит занимает число. где находится самый левый бит числа
    {
        var xd = x.GetDigits();

        if (xd.Length == 1 && xd[0] == 0) return 0;

        uint top = xd[xd.Length - 1];
        int bits = 32;
        while((top & 0x80000000) == 0)
        {
            top <<= 1;
            bits--;
        }
        return (xd.Length - 1) * 32 + bits;
    }

    private static bool GetBit(BetterBigInteger x, int bitIndex)
    {
        var xd = x.GetDigits();
        int digitIndex = bitIndex / 32; // в каком uint
        int offset = bitIndex % 32;

        if (digitIndex >= xd.Length) return false;
        return ((xd[digitIndex] >> offset) & 1) == 1;
    }

    private static void SetBit(uint[] digits, int bitIndex)
    {
        int digitIndex = bitIndex / 32;
        int offset = bitIndex % 32;
        digits[digitIndex] |= 1u << offset; 
    }

    private static BetterBigInteger ShiftLeftOne(BetterBigInteger x)
    {
        var xd = x.GetDigits();
        uint[] result = new uint[xd.Length + 1];
        uint carry = 0;

        for (int i = 0; i < xd.Length; i++)
        {
            uint newCarry = xd[i] >> 31;
            result[i] = (xd[i] << 1) | carry;
            carry = newCarry;
        }
        result[xd.Length] = carry;
        return new BetterBigInteger(result, false);
    }

    private static BetterBigInteger Abs(BetterBigInteger x)
    {
        return new BetterBigInteger(x.GetDigits().ToArray(), false);
    }

    private static void DivUnsigned(
        BetterBigInteger a,
        BetterBigInteger b,
        out BetterBigInteger q,
        out BetterBigInteger r)
    {
        if (b.IsZero()) throw new DivideByZeroException();
        if (CompareModule(a, b) < 0)
        {
            q = new BetterBigInteger(new uint[] { 0 });
            r = new BetterBigInteger(a.GetDigits().ToArray(), false);
            return;
        }
        int bitLength = BitLength(a);
        uint[] qDigits = new uint[(bitLength + 31) / 32];
        BetterBigInteger currentR = new BetterBigInteger(new uint[] { 0 }); // текущий остаток

        for (int i = bitLength - 1; i >= 0; i--)
        {
            currentR = ShiftLeftOne(currentR);
            if (GetBit(a, i))
            {
                currentR = AddUnsigned(currentR, new BetterBigInteger(new uint[] { 1 }));
            }
            if (CompareModule(currentR, b) >= 0)
            {
                currentR = SubUnsigned(currentR, b);
                SetBit(qDigits, i);
            }
        }
        q = new BetterBigInteger(qDigits, false);
        r = currentR;
    }


    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero()) throw new DivideByZeroException();
        BetterBigInteger absA = Abs(a);
        BetterBigInteger absB = Abs(b);
        DivUnsigned(absA, absB, out BetterBigInteger q, out BetterBigInteger r);
        q._signBit = a._signBit ^ b._signBit;
        if (q.IsZero()) q._signBit = 0;
        return q;
    }
    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.IsZero()) throw new DivideByZeroException();
        BetterBigInteger absA = Abs(a);
        BetterBigInteger absB = Abs(b);
        DivUnsigned(absA, absB, out BetterBigInteger q, out BetterBigInteger r);
        r._signBit = a._signBit;
        if (r.IsZero()) r._signBit = 0;
        return r;
    }
    
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] digits = Multiplier.Multiply(Abs(a).GetDigits().ToArray(), Abs(b).GetDigits().ToArray());
        BetterBigInteger result = new BetterBigInteger(digits, a._signBit != b._signBit);
        if (result.IsZero()) result._signBit = 0;
        return result;
    }

    private static uint[] ToTwoComplement(BetterBigInteger x, int length)
    {
        uint[] result = new uint[length];
        var xd = x.GetDigits();
        for (int i = 0; i < length; i++)
        {
            if (i < xd.Length) result[i] = xd[i];
            else result[i] = 0;
        }
        if (!x.IsNegative) return result;

        for (int i = 0; i < length; i++)
        {
            result[i] = ~result[i];
        }
        uint acc = 1;
        for (int i = 0; i < length; i++)
        {
            result[i] = AddUint(result[i], 0, ref acc);
            if (acc == 0) break;
        }
        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] digits)
    {
        bool isNegative = (digits[digits.Length - 1] & 0x80000000) != 0;
        if (!isNegative) return new BetterBigInteger(digits, false);
        uint[] module = new uint[digits.Length];
        Array.Copy(digits, module, digits.Length);
        for (int i = 0; i < module.Length; i++)
            module[i] = ~module[i];
        uint acc  = 1;
        for (int i = 0; i < module.Length; i++)
        {
            module[i] = AddUint(module[i], 0, ref acc);
            if (acc == 0) break;
        }
        return new BetterBigInteger(module, true);
    }

    private static BetterBigInteger BitUnsigned(
        BetterBigInteger a,
        BetterBigInteger b,
        Func<uint, uint, uint> operation)
    {
        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        uint[] ad = ToTwoComplement(a, length);
        uint[] bd = ToTwoComplement(b, length);
        uint[] result = new uint[length];
        for (int i = 0; i < length; i++)
            result[i] = operation(ad[i], bd[i]);
        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        int length = a.GetDigits().Length + 1;
        uint[] ad = ToTwoComplement(a, length);
        for (int i = 0; i < length; i++)
            ad[i] = ~ad[i];
        return FromTwosComplement(ad);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        return BitUnsigned(a, b, (x, y) => x & y);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        return BitUnsigned(a, b, (x, y) => x | y);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        return BitUnsigned(a, b, (x, y) => x ^ y);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0) throw new ArgumentOutOfRangeException(nameof(shift));
        if (a.IsZero() || shift == 0)
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);

        var ad = a.GetDigits();
        int digitShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[ad.Length + digitShift + 1];
        uint acc = 0;
        for (int i = 0; i < ad.Length; i++)
        {
            uint current = ad[i];
            result[i + digitShift] = (current << bitShift) | acc;
            if (bitShift == 0)
                acc = 0;
            else
                acc = current >> (32 - bitShift);
        }
        result[ad.Length + digitShift] = acc;
        return new BetterBigInteger(result, a.IsNegative);
    }

    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        if (shift < 0) throw new ArgumentOutOfRangeException(nameof(shift));
        if (a.IsNegative)
        {
            BetterBigInteger absA = Abs(a);
            BetterBigInteger one = new BetterBigInteger(new uint[] { 1 });
            BetterBigInteger add = (one << shift) - one;
            BetterBigInteger shifted = (absA + add) >> shift;
            return -shifted;
        }
        if (a.IsZero() || shift == 0) return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        var ad = a.GetDigits();
        int digitshift = shift / 32;
        int bitshift = shift % 32;
        if (digitshift >= ad.Length) return new BetterBigInteger(new uint[] { 0 });
        int newLength = ad.Length - digitshift;
        uint[] result = new uint[newLength];
        uint acc = 0;
        for (int i = newLength - 1; i >= 0; i--)
        {
            uint current = ad[i + digitshift];
            if (bitshift == 0) result[i] = current;
            else
            {
                result[i] = (current >> bitshift) | acc;
                acc = current << (32 - bitshift);
            }
        }
        return new BetterBigInteger(result, a.IsNegative);
    }
    
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36) throw new ArgumentOutOfRangeException(nameof(radix));
        if (IsZero()) return "0";
        BetterBigInteger current = Abs(this);
        BetterBigInteger baseValue = new BetterBigInteger(new uint[] { (uint)radix });
        string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        List<char> result = new List<char>();
        while (!current.IsZero())
        {
            DivUnsigned(current, baseValue, out BetterBigInteger q, out BetterBigInteger r);

            uint digit = r.GetDigits()[0];
            result.Add(chars[(int)digit]);
            current = q;
        }
        if (IsNegative) result.Add('-');
        result.Reverse();
        return new string(result.ToArray());
    }
    
}