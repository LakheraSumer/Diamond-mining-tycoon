using System;
using System.Globalization;

namespace DiamondMine.Simulation
{
    /// <summary>
    /// Bounds and display for currency and production numbers.
    /// </summary>
    public static class NumericFormat
    {
        public const double MaxValue = 1e300;

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static double Clamp(double value)
        {
            if (!IsFinite(value) || value < 0d)
                return 0d;
            if (value > MaxValue)
                return MaxValue;
            return value;
        }

        public static double Add(double current, double amount)
        {
            if (!IsFinite(amount) || amount <= 0d)
                return Clamp(current);
            double safeCurrent = Clamp(current);
            double sum = safeCurrent + amount;
            if (!IsFinite(sum) || sum > MaxValue)
                return MaxValue;
            return sum;
        }

        public static string Format(double value)
        {
            if (!IsFinite(value))
                return "—";
            if (value < 0d)
                return "-" + Format(-value);

            if (value < 1000d)
            {
                if (value < 10d)
                    return value.ToString("0.##", CultureInfo.InvariantCulture);
                if (value < 100d)
                    return value.ToString("0.#", CultureInfo.InvariantCulture);
                return Math.Floor(value).ToString("0", CultureInfo.InvariantCulture);
            }

            string[] suffixes = { "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
            double scaled = value;
            int index = -1;
            while (scaled >= 1000d && index + 1 < suffixes.Length)
            {
                scaled /= 1000d;
                index++;
            }

            if (scaled >= 1000d)
                return value.ToString("0.##e0", CultureInfo.InvariantCulture);

            return scaled.ToString("0.##", CultureInfo.InvariantCulture) + suffixes[index];
        }

        public static string FormatRate(double perSecond)
        {
            return Format(perSecond) + "/s";
        }
    }
}
