using System;
using UdonSharp;

// from https://referencesource.microsoft.com/#mscorlib/system/random.cs
/// <summary>
/// System.Random for udon.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class URandom : UdonSharpBehaviour {
    private const int MBIG = int.MaxValue;
    private const int MSEED = 161803398;

    private int inext;
    private int inextp;
    private int[] SeedArray = new int[56];

    private void Start() {
        // Method is not exposed to Udon: 'Environment.TickCount'
        //SetSeed(Environment.TickCount);
        SetSeed(UnityEngine.Random.Range(0, int.MaxValue));
    }

    public void SetSeed(int Seed) {
        int ii;
        int mj, mk;

        //Initialize our Seed array.
        //This algorithm comes from Numerical Recipes in C (2nd Ed.)
        int subtraction = (Seed == int.MinValue) ? int.MaxValue : Math.Abs(Seed);
        mj = MSEED - subtraction;
        SeedArray[55] = mj;
        mk = 1;
        for (int i = 1; i < 55; i++) {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
            ii = (21 * i) % 55;
            SeedArray[ii] = mk;
            mk = mj - mk;
            if (mk < 0) mk += MBIG;
            mj = SeedArray[ii];
        }
        for (int k = 1; k < 5; k++) {
            for (int i = 1; i < 56; i++) {
                SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
                if (SeedArray[i] < 0) SeedArray[i] += MBIG;
            }
        }
        inext = 0;
        inextp = 21;
    }

    private double Sample() {
        //Including this division at the end gives us significantly improved
        //random number distribution.
        return (InternalSample() * (1.0 / MBIG));
    }

    private int InternalSample() {
        int retVal;
        int locINext = inext;
        int locINextp = inextp;

        if (++locINext >= 56) locINext = 1;
        if (++locINextp >= 56) locINextp = 1;

        retVal = SeedArray[locINext] - SeedArray[locINextp];

        if (retVal == MBIG) retVal--;
        if (retVal < 0) retVal += MBIG;

        SeedArray[locINext] = retVal;

        inext = locINext;
        inextp = locINextp;

        return retVal;
    }

    public int Next() {
        return InternalSample();
    }

    private double GetSampleForLargeRange() {
        // The distribution of double value returned by Sample
        // is not distributed well enough for a large range.
        // If we use Sample for a range [Int32.MinValue..Int32.MaxValue)
        // We will end up getting even numbers only.

        int result = InternalSample();
        // Note we can't use addition here. The distribution will be bad if we do that.
        bool negative = InternalSample() % 2 == 0;  // decide the sign based on second sample
        if (negative) {
            result = -result;
        }
        double d = result;
        d += int.MaxValue - 1; // get a number in range [0 .. 2 * Int32MaxValue - 1)
        d /= 2 * (uint) int.MaxValue - 1;
        return d;
    }

    public int Next(int minValue, int maxValue) {
        if (minValue > maxValue) {
            UnityEngine.Debug.LogError("ArgumentOutOfRangeException(\"minValue\")");
            return 0;
        }

        long range = (long) maxValue - minValue;
        if (range <= int.MaxValue) {
            return ((int) (Sample() * range) + minValue);
        } else {
            return (int) ((long) (GetSampleForLargeRange() * range) + minValue);
        }
    }

    public int Next(int maxValue) {
        if (maxValue < 0) {
            UnityEngine.Debug.LogError("ArgumentOutOfRangeException(\"maxValue\")");
        }

        return (int) (Sample() * maxValue);
    }

    public virtual double NextDouble() {
        return Sample();
    }

    public void NextBytes(byte[] buffer) {
        if (buffer == null) {
            UnityEngine.Debug.LogError("ArgumentNullException(\"buffer\")");
            return;
        }

        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = (byte) (InternalSample() % (byte.MaxValue + 1));
        }
    }
}
