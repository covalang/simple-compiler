using System.Globalization;
using System.Numerics;
using MathNet.Numerics;
using BR = MathNet.Numerics.BigRational;

struct BigRational :
	INumber<BigRational>,
	ISignedNumber<BigRational>,
	ISpanParsable<BigRational>,
	IPowerFunctions<BigRational>
{
	private readonly BR value;

	private BigRational(BR value) => this.value = value;

	public Int32 CompareTo(Object? obj) => obj is BigRational other ? CompareTo(other) : throw new ArgumentException("Object must be of type BigRational.", nameof(obj));

	public Int32 CompareTo(BigRational other) => throw new NotImplementedException();

	public Boolean Equals(BigRational other) => value == other.value;

	public static BigRational Pow(BigRational x, BigRational y) => new(BR.Pow(x.value, BR.ToInt32(y.value)));

	public String ToString(String? format, IFormatProvider? formatProvider) => value.ToString();

	public Boolean TryFormat(Span<Char> destination, out Int32 charsWritten, ReadOnlySpan<Char> format, IFormatProvider? provider)
	{
		throw new NotImplementedException();
	}

	public static BigRational Parse(String s, IFormatProvider? provider) => new(BR.Parse(s));

	public static Boolean TryParse(String? s, IFormatProvider? provider, out BigRational result)
	{
		try
		{
			result = new(BR.Parse(s));
			return true;
		}
		catch
		{
			result = default;
			return false;
		}
	}

	public static BigRational Parse(ReadOnlySpan<Char> s, IFormatProvider? provider) => throw new NotImplementedException();
	public static Boolean TryParse(ReadOnlySpan<Char> s, IFormatProvider? provider, out BigRational result) => throw new NotImplementedException();
	public static BigRational operator +(BigRational left, BigRational right) => throw new NotImplementedException();

	public static BigRational AdditiveIdentity => new(BR.Zero);
	public static Boolean operator ==(BigRational left, BigRational right) => left.value == right.value;
	public static Boolean operator !=(BigRational left, BigRational right) => left.value != right.value;
	public static Boolean operator >(BigRational left, BigRational right) => left.value > right.value;
	public static Boolean operator >=(BigRational left, BigRational right) => left.value >= right.value;
	public static Boolean operator <(BigRational left, BigRational right) => left.value < right.value;
	public static Boolean operator <=(BigRational left, BigRational right) => left.value <= right.value;

	public static BigRational operator --(BigRational value) => new(value.value - BR.One);

	public static BigRational operator /(BigRational left, BigRational right) => new(left.value / right.value);
	public static BigRational operator ++(BigRational value) => new(value.value + BR.One);

	public static BigRational operator %(BigRational left, BigRational right) => throw new NotImplementedException();

	public static BigRational MultiplicativeIdentity => new(BR.One);
	public static BigRational operator *(BigRational left, BigRational right) => new(left.value * right.value);

	public static BigRational operator -(BigRational left, BigRational right) => new(left.value - right.value);
	public static BigRational operator -(BigRational value) => new(-value.value);
	public static BigRational operator +(BigRational value) => new(+value.value);
	public static BigRational Abs(BigRational value) => new(BR.Abs(value.value));
	public static Boolean IsCanonical(BigRational value) => throw new NotImplementedException();
	public static Boolean IsComplexNumber(BigRational value) => false;
	public static Boolean IsEvenInteger(BigRational value) => value.value.IsInteger && BR.ToInt32(value.value).IsEven();
	public static Boolean IsFinite(BigRational value) => true;
	public static Boolean IsImaginaryNumber(BigRational value) => false;
	public static Boolean IsInfinity(BigRational value) => value.value.Denominator == 0;
	public static Boolean IsInteger(BigRational value) => value.value.IsInteger;
	public static Boolean IsNaN(BigRational value) => false;
	public static Boolean IsNegative(BigRational value) => value.value.IsNegative;
	public static Boolean IsNegativeInfinity(BigRational value) => value.value.Numerator.Sign == -1 && value.value.Denominator == 0;
	public static Boolean IsNormal(BigRational value) => throw new NotImplementedException();
	public static Boolean IsOddInteger(BigRational value) => !IsEvenInteger(value);
	public static Boolean IsPositive(BigRational value) => value.value.IsPositive;
	public static Boolean IsPositiveInfinity(BigRational value) => value.value.Numerator.Sign == 1 && value.value.Denominator == 0;
	public static Boolean IsRealNumber(BigRational value) => !value.value.IsInteger;
	public static Boolean IsSubnormal(BigRational value) => throw new NotImplementedException();
	public static Boolean IsZero(BigRational value) => value.value.IsZero;
	public static BigRational MaxMagnitude(BigRational x, BigRational y) => throw new NotImplementedException();
	public static BigRational MaxMagnitudeNumber(BigRational x, BigRational y) => throw new NotImplementedException();
	public static BigRational MinMagnitude(BigRational x, BigRational y) => throw new NotImplementedException();
	public static BigRational MinMagnitudeNumber(BigRational x, BigRational y) => throw new NotImplementedException();
	public static BigRational Parse(ReadOnlySpan<Char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
	public static BigRational Parse(String s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
	public static Boolean TryConvertFromChecked<TOther>(TOther value, out BigRational result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static Boolean TryConvertFromSaturating<TOther>(TOther value, out BigRational result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static Boolean TryConvertFromTruncating<TOther>(TOther value, out BigRational result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static Boolean TryConvertToChecked<TOther>(BigRational value, out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static Boolean TryConvertToSaturating<TOther>(BigRational value, out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static Boolean TryConvertToTruncating<TOther>(BigRational value, out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
	public static Boolean TryParse(ReadOnlySpan<Char> s, NumberStyles style, IFormatProvider? provider, out BigRational result) => throw new NotImplementedException();
	public static Boolean TryParse(String? s, NumberStyles style, IFormatProvider? provider, out BigRational result) => throw new NotImplementedException();
	public static Int32 Radix => 2;
	public static BigRational One => new(BR.One);
	public static BigRational Zero => new(BR.Zero);
	public static BigRational NegativeOne => new(-BR.One);
}