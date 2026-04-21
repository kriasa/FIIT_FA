using System.Runtime.Intrinsics.X86;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigit = a.GetDigits();
        ReadOnlySpan<uint> bDigit = b.GetDigits();
        if ((aDigit.Length == 1 && aDigit[0] == 0) || (bDigit.Length == 1 && bDigit[0] == 0)) return new BetterBigInteger(new uint[] { 0 });

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

                uint lowSum = (s1 & 0xFFFF) + (acc & 0xFFFF) + (curRes & 0xFFFF );
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
        return new BetterBigInteger(res, a.IsNegative != b.IsNegative);
    }
}