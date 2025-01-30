using System.Numerics;

static class Types
{
	public enum Primitive
	{
		Void,
		U8,
		U16,
		U32,
		U64,
		U128,
		I8,
		I16,
		I32,
		I64,
		I128,
		Int,
		Rat,
		F16,
		F32,
		F64,
		D128,
		Bool,
		Char,
		String,
	}

	private static readonly Dictionary<Primitive, Type> types = new()
	{
		[Primitive.U8] = typeof(Byte),
		[Primitive.U16] = typeof(UInt16),
		[Primitive.U32] = typeof(UInt32),
		[Primitive.U64] = typeof(UInt64),
		[Primitive.U128] = typeof(UInt128),
		[Primitive.I8] = typeof(SByte),
		[Primitive.I16] = typeof(Int16),
		[Primitive.I32] = typeof(Int32),
		[Primitive.I64] = typeof(Int64),
		[Primitive.I128] = typeof(Int128),
		[Primitive.Int] = typeof(BigInteger),
		[Primitive.Rat] = typeof(BigRational),
		[Primitive.F16] = typeof(Half),
		[Primitive.F32] = typeof(Single),
		[Primitive.F64] = typeof(Double),
		[Primitive.D128] = typeof(Decimal),
		[Primitive.Bool] = typeof(Boolean),
		[Primitive.Char] = typeof(Char),
		[Primitive.String] = typeof(String),
		[Primitive.Void] = typeof(void)
	};

	public static Type GetType(Primitive primitive) =>
		types.TryGetValue(primitive, out var found)
			? found
			: throw new Exception($"Primitive type not found: {primitive}");

	public static Type GetType(String type) =>
		Enum.TryParse(type, out Primitive p)
			? GetType(p)
			: throw new Exception($"Unknown type: {type}");
}
