using System.Globalization;
using System.Numerics;

namespace AeterniUI.Components;

/// <summary>
/// Numeric helpers shared by <c>InputNumber</c> and <c>Slider</c>. Both controls
/// are generic over <see cref="INumber{TSelf}" />, so parsing, formatting, decimal
/// places and step snapping are written once against the interface instead of once
/// per value type.
/// </summary>
internal static class NumericValue
{
    /// <summary>
    /// Upper bound for the fractional digits the controls render. Fifteen is what
    /// a <see cref="double" /> round-trips; anything more would display digits the
    /// value type cannot actually hold.
    /// </summary>
    internal const int MaxPrecision = 15;

    /// <summary>Rejects the NaN and infinity spellings floating-point parses accept.</summary>
    internal static bool IsFinite<TValue>(TValue value)
        where TValue : struct, INumber<TValue> =>
        !TValue.IsNaN(value) && !TValue.IsInfinity(value);

    /// <summary>
    /// Parses display text with the current culture's conventions. Blank text,
    /// unparseable text and non-finite numbers (NaN, infinity) all fail, so
    /// callers keep the previous value instead of committing a broken one.
    /// </summary>
    internal static bool TryParse<TValue>(string? text, IFormatProvider culture, out TValue value)
        where TValue : struct, INumber<TValue>
    {
        value = default;
        return !string.IsNullOrWhiteSpace(text)
            && TValue.TryParse(text, culture, out value)
            && IsFinite(value);
    }

    /// <summary>
    /// Formats a value at a fixed number of fractional digits; a null value
    /// renders as an empty string so the two states stay distinct. The
    /// non-nullable overload exists because type inference cannot flow from a
    /// type-parameter argument into a <c>Nullable&lt;T&gt;</c> parameter.
    /// </summary>
    internal static string Format<TValue>(TValue? value, int precision, IFormatProvider culture)
        where TValue : struct, INumber<TValue> =>
        value.HasValue ? Format(value.Value, precision, culture) : string.Empty;

    /// <summary>Formats a non-null value at a fixed number of fractional digits.</summary>
    internal static string Format<TValue>(TValue value, int precision, IFormatProvider culture)
        where TValue : struct, INumber<TValue> =>
        value.ToString($"F{precision}", culture);

    /// <summary>
    /// Counts the fractional digits a step actually needs — 1 for 0.1, 0 for 1,
    /// 5 for 1E-05 — by reading the shortest round-trippable representation, so
    /// binary floating-point noise (0.30000000000000004) cannot inflate the count
    /// beyond the cap.
    /// </summary>
    internal static int DecimalPlaces<TValue>(TValue step)
        where TValue : struct, INumber<TValue>
    {
        var text = step.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        var exponentIndex = text.IndexOfAny(['e', 'E']);
        var exponent = 0;

        if (exponentIndex >= 0)
        {
            _ = int.TryParse(text[(exponentIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out exponent);
            text = text[..exponentIndex];
        }

        var pointIndex = text.IndexOf('.');
        var fraction = pointIndex >= 0 ? text[(pointIndex + 1)..].TrimEnd('0') : string.Empty;
        return Math.Clamp(fraction.Length - exponent, 0, MaxPrecision);
    }

    /// <summary>
    /// Rounds to a fixed number of fractional digits, half away from zero. The
    /// type switch exists because the static abstract
    /// <c>Round</c> lives on <see cref="IFloatingPoint{TSelf}" />, which the
    /// <see cref="INumber{TSelf}" /> constraint does not include; integral types
    /// and <see cref="BigInteger" /> cannot carry fractions and pass through.
    /// </summary>
    internal static TValue Round<TValue>(TValue value, int precision)
        where TValue : struct, INumber<TValue>
    {
        if (precision >= MaxPrecision || TValue.IsInteger(value))
        {
            return value;
        }

        return value switch
        {
            decimal decimalValue => (TValue)(object)decimal.Round(decimalValue, precision, MidpointRounding.AwayFromZero),
            double doubleValue => (TValue)(object)Math.Round(doubleValue, precision, MidpointRounding.AwayFromZero),
            float floatValue => (TValue)(object)MathF.Round(floatValue, precision, MidpointRounding.AwayFromZero),
            Half halfValue => (TValue)(object)(Half)Math.Round((double)halfValue, precision, MidpointRounding.AwayFromZero),
            _ => value
        };
    }

    internal static double ToDouble<TValue>(TValue value)
        where TValue : struct, INumber<TValue> =>
        double.CreateSaturating(value);

    internal static TValue FromDouble<TValue>(double value)
        where TValue : struct, INumber<TValue> =>
        TValue.CreateSaturating(value);

    /// <summary>
    /// Snaps to the nearest point on the step grid anchored at
    /// <paramref name="min" />. The step count is computed in <see cref="double" />
    /// on purpose: dividing in <typeparamref name="TValue" /> would truncate for
    /// integer types (33/5 is 6, not 6.6) and snap to the wrong grid stop. The
    /// count that comes back is an integer, so the final
    /// <c>min + step * count</c> stays exact in the value's own arithmetic.
    /// </summary>
    internal static TValue SnapToStep<TValue>(TValue value, TValue min, TValue step)
        where TValue : struct, INumber<TValue>
    {
        if (!IsFinite(step) || step <= TValue.Zero)
        {
            return value;
        }

        var steps = Math.Round(ToDouble(value - min) / ToDouble(step), MidpointRounding.AwayFromZero);
        return min + step * FromDouble<TValue>(steps);
    }

    /// <summary>
    /// Adds two values without wrapping or throwing: a result that leaves
    /// <typeparamref name="TValue" />'s range saturates at its extreme. Stepping an
    /// unbounded <c>InputNumber&lt;int&gt;</c> past <c>int.MaxValue</c> used to wrap
    /// to the most negative value, and the decimal shape threw
    /// <see cref="OverflowException" /> instead of stopping.
    /// </summary>
    internal static TValue SaturatingAdd<TValue>(TValue left, TValue right)
        where TValue : struct, INumber<TValue>
    {
        // The guards compare against the headroom that is left, so the addition
        // itself never sees an out-of-range operand pair.
        if (right > TValue.Zero && left > MaxOf<TValue>() - right)
        {
            return MaxOf<TValue>();
        }

        if (right < TValue.Zero && left < MinOf<TValue>() - right)
        {
            return MinOf<TValue>();
        }

        return left + right;
    }

    /// <summary>
    /// Scales a value by a small integer factor with the same saturation contract
    /// as <see cref="SaturatingAdd{TValue}" />. The multiplication is expressed as
    /// repeated additions because a single product would overflow before any guard
    /// could look at it.
    /// </summary>
    internal static TValue SaturatingMultiply<TValue>(TValue value, int factor)
        where TValue : struct, INumber<TValue>
    {
        var count = factor < 0 ? -(long)factor : factor;
        var addend = factor < 0 ? -value : value;
        var result = TValue.Zero;
        for (long i = 0; i < count; i++)
        {
            result = SaturatingAdd(result, addend);
        }

        return result;
    }

    /// <summary>
    /// Largest value <typeparamref name="TValue" /> can hold. Generic math exposes
    /// no <c>MaxValue</c> member across every numeric type it supports, so the
    /// extreme is obtained by saturating <see cref="double" />'s own extreme.
    /// </summary>
    private static TValue MaxOf<TValue>()
        where TValue : struct, INumber<TValue> =>
        TValue.CreateSaturating(double.MaxValue);

    /// <summary>Smallest value <typeparamref name="TValue" /> can hold.</summary>
    private static TValue MinOf<TValue>()
        where TValue : struct, INumber<TValue> =>
        TValue.CreateSaturating(double.MinValue);
}
