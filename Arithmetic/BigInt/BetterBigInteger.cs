using System.ComponentModel;
using System.Runtime.CompilerServices;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private const int KaratsubaThreshold = 64;
    private const int FftThreshold = 4096;
    private int _signBit;
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        Initialize(digits, isNegative);
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        uint[] array = digits.ToArray();
        Initialize(array, isNegative);
    }
    
    private void Initialize(uint[] digits, bool isNegative)
    {
        int realLength = digits.Length;
        while (realLength > 0 && digits[realLength - 1] == 0)
        {
            realLength--;
        }

        if (realLength == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;

        }else if (realLength == 1)
        {
            _signBit = isNegative ? 1 : 0;
            _smallValue = digits[0];
            _data = null;
        }
        else
        {
            _signBit = isNegative ? 1 : 0;
            _smallValue = 0;
            _data = new uint[realLength];
            Array.Copy(digits, _data, realLength);
        }
    }
    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("empty line", nameof(value));
        if (radix < 2 || radix > 36) throw new ArgumentOutOfRangeException(nameof(radix), "radix [2; 36]");

        value = value.Trim();
        bool isNegative = false;
        int startIndex = 0;

        if (value[0] == '-')
        {
            isNegative = true;
            startIndex = 1;
        }
        else if (value[0] == '+')
        {
            startIndex = 1;
        }
        List<uint> digitsList = new List<uint> { 0 };

        for (int i = startIndex; i< value.Length; i++)
        {
            int digit = GetChar(value[i]);
            if (digit >= radix) throw new ArgumentException($"invalid character {digit}");

            MultiplyByAndAdd(digitsList, (uint)radix, (uint)digit);
        }
        uint[] finalDigits = digitsList.ToArray();
        Initialize(finalDigits, isNegative);
    }
    private int GetChar(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'z') return c - 'a' + 10;
        if (c >= 'A' && c <= 'Z') return c - 'A' + 10;
        throw new ArgumentException($"invalid character {c}");
    }
    private static void MultiplyByAndAdd(List<uint> digits, uint radix, uint znach)
    {
        uint acc = znach;
        for (int i = 0; i<=digits.Count - 1; ++i)
        {
            uint r1 = digits[i] & 0xFFFF;
            uint l1 = digits[i] >>16;
            uint r2 = radix & 0xFFFF;
            uint l2 = radix >>16;

            uint s1 = r2 * r1;
            uint s2 = r2 * l1;
            uint s3 = l2 * r1;
            uint s4 = l2 * l1;

            uint lowSum = (s1 & 0xFFFF) + (acc & 0xFFFF);
            uint midSum = (s2 & 0xFFFF) + (s3 & 0xFFFF) + (s1 >> 16) + (acc >> 16) + (lowSum >> 16);
            uint highSum = (s4 & 0xFFFF) + (s2 >> 16) + (s3 >> 16) + (midSum>>16);
            uint lastSum = (s4 >> 16) + (highSum >> 16);

            digits[i] = (lowSum & 0xFFFF) | (midSum << 16);
            acc = (highSum & 0xFFFF) | (lastSum << 16);
        }
        if (acc > 0)
        {
            digits.Add (acc);
        }
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other is null) return 1;
        if (IsNegative && !other.IsNegative)
        {
            return -1;
        }else if (!IsNegative && other.IsNegative)
        {
            return 1;
        }
        if (ReferenceEquals(this, other)) return 0;

        ReadOnlySpan<uint> digits = GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();

        int znak = IsNegative ? -1 : 1;
        if (digits.Length > otherDigits.Length) return znak;
        if (digits.Length < otherDigits.Length) return -znak;

        for (int i = digits.Length -1; i>=0; --i)
        {
            if (digits[i] > otherDigits[i]) return znak;
            if (digits[i] < otherDigits[i]) return -znak;
        }
        return 0;
    }
    public bool Equals(IBigInteger? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (IsNegative != other.IsNegative) return false;

        ReadOnlySpan<uint> digits = GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();
        if (digits.Length != otherDigits.Length) return false;

        return digits.SequenceEqual(otherDigits);

    }
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(_signBit);
        foreach (uint digit in GetDigits())
        {
            hash.Add(digit);
        }

        return hash.ToHashCode();
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
        if (a.IsNegative == b.IsNegative)
        {
            uint[] res = Add(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(res, a.IsNegative);
        }
        int cmp = CompareModule(a.GetDigits(),b.GetDigits());

        if (cmp == 0) return new BetterBigInteger(Array.Empty<uint>(), false);

        if (cmp > 0)
        {
            uint[] res = Subtract(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(res, a.IsNegative);
        }
        else
        {
            uint[] res = Subtract(b.GetDigits(), a.GetDigits());
            return new BetterBigInteger(res, b.IsNegative);
        }
    }
    private static uint[] Add(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLength = Math.Max(a.Length, b.Length);
        uint[] res = new uint[maxLength + 1];

        uint acc = 0;
        for (int i=0; i < maxLength; ++i)
        {
            uint aDigit = (i < a.Length) ? a[i] : 0;
            uint bDigit = (i < b.Length) ? b[i] : 0;

            uint r1 = aDigit & 0xFFFF;
            uint l1 = aDigit >> 16;
            uint r2 = bDigit & 0xFFFF;
            uint l2 = bDigit >> 16;

            uint lowSum = r1 + r2 + acc;
            uint midSum = l1 + l2 + (lowSum >> 16);
            acc = midSum >> 16;

            res[i] = (lowSum & 0xFFFF) | (midSum << 16);
        }
        if (acc > 0)
        {
            res[maxLength] = acc;
            return res;
        }
        
        if (res.Length > 1)
        {
            uint[] finalRes = new uint[maxLength];
            Array.Copy(res, finalRes, maxLength);
            return finalRes;
        }
        return res;
    }
    private static uint[] Subtract(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        uint[] res = new uint[a.Length];
        uint acc = 0;

        for (int i = 0; i < a.Length; ++i)
        {
            uint aDigit = a[i];
            uint bDigit = i < b.Length ? b[i] : 0;
            if  (aDigit < bDigit || (aDigit == bDigit && acc == 1))
            {
                res[i] = aDigit - bDigit - acc;
                acc = 1;
            }
            else
            {
                res[i] = aDigit - bDigit -  acc;
                acc = 0;
            }
        }
        return res;
    }
    private static int CompareModule(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length > b.Length) return 1;
        if (a.Length < b.Length) return -1;
        for (int i = a.Length -1; i>=0; --i)
        {
            if (a[i] > b[i]) return 1;
            if (a[i] < b[i]) return -1;
        }
        return 0;
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        ReadOnlySpan<uint> digits = a.GetDigits();
        if (digits.Length == 1 && digits[0] == 0) return a;
        return new BetterBigInteger(digits.ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
        return DivRem(a, b).Quotient;
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
        return DivRem(a, b).Remainder;
    }
    private static (BetterBigInteger Quotient, BetterBigInteger Remainder) DivRem(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        if (bDigits.Length == 1 && bDigits[0] == 0) throw new DivideByZeroException("division by zero");

        int cmp = CompareModule(aDigits, bDigits);
        if (cmp < 0) return (new BetterBigInteger(new uint[] { 0 }, false), a);
        if (cmp == 0) return (new BetterBigInteger(new uint[] { 1 }, a.IsNegative != b.IsNegative), new BetterBigInteger(new uint[] { 0 }, false));

        uint[] quotientRes = new uint[aDigits.Length];
        var acc = new BetterBigInteger(new uint[] { 0 }, false);
        var absB = new BetterBigInteger(bDigits.ToArray(), false);

        for (int i = aDigits.Length - 1; i >= 0; --i)
        {
            acc = (acc << 32) + new BetterBigInteger(new uint[] { aDigits[i] }, false);
            uint low = 0, 
            high = uint.MaxValue;
            uint q = 0;
            while (low <= high)
            {
                uint mid = low + (high - low) / 2;
                BetterBigInteger trial = absB * new BetterBigInteger(new uint[] { mid }, false);
                if (CompareModule(trial.GetDigits(), acc.GetDigits()) <= 0)
                {
                    q = mid;
                    if (mid == uint.MaxValue) break;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            quotientRes[i] = q;
            acc = acc - (absB * new BetterBigInteger(new uint[] { q }, false));
        }

        return (new BetterBigInteger(quotientRes, a.IsNegative != b.IsNegative), new BetterBigInteger(acc.GetDigits().ToArray(), a.IsNegative));
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));

        int maxLen = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        IMultiplier multiplier;
        if (maxLen < KaratsubaThreshold)
        {
            multiplier =  new SimpleMultiplier();
        }else if (maxLen < FftThreshold)
        {
            multiplier =  new KaratsubaMultiplier();
        }
        else
        {
            multiplier =  new FftMultiplier();
        }
        return multiplier.Multiply(a, b);
    }
    
    public static BetterBigInteger operator ~(BetterBigInteger a){
        if (a is null) throw new ArgumentNullException(nameof(a));
        
        BetterBigInteger res = new BetterBigInteger(new uint[]{1}, false);
        return - (a + res);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));

        int maxLen = Math.Max(a.GetDigits().Length + 1, b.GetDigits().Length);
        uint[] res = new uint[maxLen];
        uint [] aDigits = ToTwos(a, maxLen);
        uint[] bDigits = ToTwos(b, maxLen);

        for (int i = 0; i < maxLen; ++i)
        {
            res[i] = aDigits[i] & bDigits[i];
        }
        return FromTwos(res);
    }
    private static uint[] ToTwos (BetterBigInteger a, int len)
    {
        ReadOnlySpan<uint> digits = a.GetDigits();
        uint[] res = new uint[len];

        if (!a.IsNegative)
        {
            digits.CopyTo(res.AsSpan(0, digits.Length));
            return res;
        }

        uint  acc = 1;
        for (int i = 0; i < len; ++i)
        {
            uint digit = (i < digits.Length) ? digits[i] : 0u;
            uint inverted = ~digit;

            uint lowSum = (inverted & 0xFFFF) + acc;
            uint midSum = (inverted >> 16) + (lowSum >> 16);
            acc = midSum >> 16;
            res[i] = (lowSum & 0xFFFF) | (midSum << 16);
        }
        return res;
    }
    private static BetterBigInteger FromTwos(uint[] a)
    {
        if (a.Length == 0) return new BetterBigInteger(new uint[] { 0 });

        bool isN = (a[a.Length - 1] & (1U << 31)) != 0;
        if (!isN) return new BetterBigInteger(a, false);

        int len = a.Length;
        uint[] res = new uint[len];
        uint acc = 1;
        for (int i = 0; i< len ; ++i)
        {
            uint inverted = ~a[i];

            uint lowSum = (inverted & 0xFFFF) + acc;
            uint midSum = (inverted >> 16) + (lowSum >> 16);
            acc = midSum >> 16;
            res[i] = (lowSum & 0xFFFF) | (midSum << 16);
        }
        return new BetterBigInteger(res, true);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));

        int maxLen = Math.Max(a.GetDigits().Length + 1, b.GetDigits().Length);
        uint[] res = new uint[maxLen];
        uint[] aDigits = ToTwos(a, maxLen);
        uint[] bDigits = ToTwos(b, maxLen);

        for (int i = 0; i < maxLen; ++i)
        {
            res[i] = aDigits[i] | bDigits[i];
        }
        return FromTwos(res);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));

        int maxLen = Math.Max(a.GetDigits().Length + 1, b.GetDigits().Length);
        uint[] res = new uint[maxLen];
        uint[] aDigits = ToTwos(a, maxLen);
        uint[] bDigits = ToTwos(b, maxLen);

        for (int i = 0; i < maxLen; ++i)
        {
            res[i] = aDigits[i] ^ bDigits[i];
        }
        return FromTwos(res);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));

        if (shift == 0) return a;
        if (shift < 0) return a >> -shift;

        ReadOnlySpan<uint> digits = a.GetDigits();
        int len = digits.Length;
        if (len == 1 && digits[0] == 0) return a;

        int blockShift = shift / 32;
        int bitShift = shift % 32;

        uint [] res = new uint[len + blockShift + 1];

        if (bitShift == 0)
        {
            for (int i = 0; i < len; ++i)
            {
                res[i + blockShift] = digits[i];
            }
        }
        else
        {
            uint acc = 0;
            for (int i = 0; i < len; ++i)
            {
                uint val = digits[i];
                res[i + blockShift] = (val << bitShift) | acc;
                acc = val >> (32 - bitShift);
            }
            res[digits.Length + blockShift] = acc;
        }
        return new BetterBigInteger(res, a.IsNegative);
    }

    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (shift == 0) return a;
        if (shift < 0) return a << -shift;

        ReadOnlySpan<uint> digits = a.GetDigits();
        int len = digits.Length;
        if (len == 1 && digits[0] == 0) return a;

        int blockShift = shift / 32;
        int bitShift = shift % 32;

        if (blockShift >= len) return a.IsNegative ? new BetterBigInteger(new uint[] { 1 }, true) : new BetterBigInteger(new uint[] { 0 });

        uint[] res = new uint[len - blockShift + 1];
        uint[] tc = ToTwos(a, len + 1);

        if (bitShift == 0)
        {
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = tc[i + blockShift];
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                uint current = tc[i + blockShift];
                uint next = (i + blockShift + 1 < tc.Length) ? tc[i + blockShift + 1] : (a.IsNegative ? 0xFFFFFFFF : 0);

                res[i] = (current >> bitShift) | (next << (32 - bitShift));
            }
        }
        return FromTwos(res);
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
        if (radix < 2 || radix > 36) throw new ArgumentOutOfRangeException(nameof(radix), "radix [2,36]");

        ReadOnlySpan<uint> digits = GetDigits();
        if (digits.Length == 1 && digits[0] == 0) return "0";

        uint[] copy = digits.ToArray();
        int len = copy.Length;

        List<char> res = new List<char>();
        while (len > 0)
        {
            uint remainder = DivideByRadix(copy, ref len, (uint)radix);
            res.Add(GetChar(remainder));
        }

        if (IsNegative)
            res.Add('-');

        res.Reverse();
        return new string(res.ToArray());
    }

    private static uint DivideByRadix(uint[] digits, ref int len, uint radix)
    {
        uint acc = 0;
        for (int i = len - 1; i >= 0; i--)
        {
            uint currDigit = digits[i];

            uint upper = (acc << 16) | (currDigit >> 16);
            uint qUpper = upper / radix;
            acc = upper % radix;

            uint lower = (acc << 16) | (currDigit & 0xFFFF);
            uint qLower = lower / radix;
            acc = lower % radix;

            digits[i] = (qUpper << 16) | qLower;
        }

        while (len > 0 && digits[len - 1] == 0)
        {
            len--;
        }

        return acc;
    }
    private char GetChar (uint digit)
    {
        if (digit >= 0 && digit <= 9) return (char)(digit + '0');
        return (char)((digit - 10) + 'a');
    }
}