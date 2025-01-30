using System.Numerics;
using static GrammarParser;
using String = System.String;

sealed class ExpressionTypeEvaluator : GrammarBaseVisitor<Type>
{
	public override Type VisitIntegerLiteral(IntegerLiteralContext context)
	{
		var text = context.Integer().GetText();
		var number = BigInteger.Parse(text.Replace("_", String.Empty));
		if (number == 0)
			return Types.GetType(Types.Primitive.U8);
		if (number > 0)
		{
			if (number <= Byte.MaxValue)
				return Types.GetType(Types.Primitive.U8);
			if (number <= UInt16.MaxValue)
				return Types.GetType(Types.Primitive.U16);
			if (number <= UInt32.MaxValue)
				return Types.GetType(Types.Primitive.U32);
			if (number <= UInt64.MaxValue)
				return Types.GetType(Types.Primitive.U64);
			if (number <= UInt128.MaxValue)
				return Types.GetType(Types.Primitive.U128);
			throw new InvalidOperationException($"Number is too large: {text}");
		}
		if (number < 0)
		{
			if (number >= SByte.MinValue)
				return Types.GetType(Types.Primitive.I8);
			if (number >= Int16.MinValue)
				return Types.GetType(Types.Primitive.I16);
			if (number >= Int32.MinValue)
				return Types.GetType(Types.Primitive.I32);
			if (number >= Int64.MinValue)
				return Types.GetType(Types.Primitive.I64);
			if (number >= Int128.MinValue)
				return Types.GetType(Types.Primitive.I128);
			throw new InvalidOperationException($"Number is too small: {text}");
		}
		throw new InvalidOperationException("ruh roh");
	}
}
