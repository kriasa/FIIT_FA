using System.ComponentModel;
using Arithmetic.BigInt.Interfaces;
using Microsoft.VisualBasic;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int MinBoundary = 32;
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
        
        var aDigit = a.GetDigits();
        var bDigit = b.GetDigits();
        if ((aDigit.Length == 1 && aDigit[0] == 0) || (bDigit.Length == 1 && bDigit[0] == 0)) return new BetterBigInteger(new uint[] { 0 });

        uint[] res = KaratsubaRecursion(a.GetDigits(), b.GetDigits());
        return new BetterBigInteger(res, a.IsNegative != b.IsNegative);
    }
    private uint[] KaratsubaRecursion(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length <= MinBoundary || b.Length <=MinBoundary) return SimpleMultiply(a, b);

        int m = Math.Max(a.Length, b.Length) / 2;

        var a0 = a.Slice(0, Math.Min(m, a.Length));
        var a1 = m < a.Length ? a.Slice(m) : ReadOnlySpan<uint>.Empty;
        var b0 = b.Slice(0, Math.Min(m, b.Length));
        var b1 = m < b.Length ? b.Slice(m) : ReadOnlySpan<uint>.Empty;

        uint[] p1 = KaratsubaRecursion(a0, b0);
        uint[] p2 = KaratsubaRecursion(a1, b1);

        uint[] sumA = Add(a0, a1);
        uint[] sumB = Add(b0, b1);
        uint[] p3 = KaratsubaRecursion(sumA, sumB);

        uint[] mid = Subtract(p3, Add(p1, p2));
        return Merger(p1, mid, p2, m);
    }

    private static uint[] SimpleMultiply(ReadOnlySpan<uint> aDigit, ReadOnlySpan<uint> bDigit)
    {
        uint[] res = new uint[aDigit.Length + bDigit.Length];

        for (int i = 0; i < aDigit.Length; ++i)
        {
            uint acc = 0;
            uint aVal = aDigit[i];
            if (aVal == 0) continue;

            uint r1 = aVal & 0xFFFF;
            uint l1 = aVal >> 16;

            for (int j = 0; j < bDigit.Length; ++j)
            {
                uint bVal = bDigit[j];
                uint r2 = bVal & 0xFFFF;
                uint l2 = bVal >> 16;

                uint s1 = r2 * r1;
                uint s2 = r2 * l1;
                uint s3 = l2 * r1;
                uint s4 = l2 * l1;
                uint curRes = res[i + j];

                uint lowSum = (s1 & 0xFFFF) + (acc & 0xFFFF) + (curRes & 0xFFFF);
                uint midSum = (s2 & 0xFFFF) + (s3 & 0xFFFF) + (s1 >> 16) + (acc >> 16) + (lowSum >> 16) + (curRes >> 16);
                uint highSum = (s4 & 0xFFFF) + (s2 >> 16) + (s3 >> 16) + (midSum >> 16);
                uint lastSum = (s4 >> 16) + (highSum >> 16);

                res[i + j] = (lowSum & 0xFFFF) | (midSum << 16);
                acc = (highSum & 0xFFFF) | (lastSum << 16);
            }
            if (acc > 0)
            {
                res[i + bDigit.Length] += acc;
            }
        }
        return res;
    }
    private static uint[] Add(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLength = Math.Max(a.Length, b.Length);
        uint[] res = new uint[maxLength + 1];

        uint acc = 0;
        for (int i = 0; i < maxLength; ++i)
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
            if (aDigit < bDigit || (aDigit == bDigit && acc == 1))
            {
                res[i] = aDigit - bDigit - acc;
                acc = 1;
            }
            else
            {
                res[i] = aDigit - bDigit - acc;
                acc = 0;
            }
        }
        return res;
    }
    private static uint[] Merger(uint[]low, uint[] mid, uint[] high, int m)
    {
        int len = high.Length + 2*m;
        if (mid.Length + m > len) len = mid.Length + m;
        if (low.Length > len) len = low.Length;
        len++;
        uint[] res = new uint [len];
        
        AddOffset(low, res, 0);
        AddOffset(mid, res, m);
        AddOffset(high, res, 2*m);
        return res;
    }
    private static void AddOffset(ReadOnlySpan<uint> digits, uint[] res, int offset)
    {
        uint acc = 0;
        for (int i = 0; i < digits.Length; ++i)
        {
            if (offset + i >= res.Length) break;

            uint resDigit = res[offset + i];
            uint digit = digits[i];

            uint r1 = resDigit & 0xFFFF;
            uint l1 = resDigit >> 16;
            uint r2 = digit & 0xFFFF;
            uint l2 = digit >> 16;

            uint low = r1 + r2 + acc;
            uint high = l1 + l2 + (low >> 16);

            res[offset + i] = (high << 16) | (low & 0xFFFF);
            acc = high >> 16;
        }

        int k = digits.Length;
        while (acc > 0 && offset + k < res.Length)
        {
            uint resDigit = res[offset + k];

            uint r1 = resDigit & 0xFFFF;
            uint l1 = resDigit >> 16;

            uint low = r1 + acc;
            uint high = l1 + (low >> 16);

            res[offset + k] = (high << 16) | (low & 0xFFFF);
            acc = high >> 16;
            k++;
        }
    }
}