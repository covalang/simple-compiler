using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;
using GrEmit;
using static GrammarParser;

enum None : Byte {}

sealed class ProgramVisitor : GrammarBaseVisitor<None>, IDisposable
{
	public TypeBuilder TypeBuilder { get; }
	private GroboIL il;
	private readonly Dictionary<String, (GroboIL.Local local, Type type)> locals = new();
	private readonly Dictionary<String, (Int32 index, Type type)> @params = new();
	//private readonly Dictionary<BodyContext, Dictionary<(String name, List<(String name, Type type)> @params), MethodInfo>> functions = new();
	private readonly Dictionary<BodyContext, What> functions = new();

	public ProgramVisitor()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new("Assembly"), AssemblyBuilderAccess.RunAndCollect);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
		TypeBuilder = moduleBuilder.DefineType("Simple", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.BeforeFieldInit);
		var methodBuilder = TypeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(Int32), new[] { typeof(ReadOnlyMemory<String>) });
		il = new GroboIL(methodBuilder);
	}

	public void Dispose() => il.Dispose();

	private static void WriteLine<T>(T value) where T : ISpanFormattable
	{
		if (!TryStackAlloc())
			Console.WriteLine(value.ToString());

		Boolean TryStackAlloc()
		{
			Span<Char> span = stackalloc Char[128];
			if (!value.TryFormat(span, out var charsWritten, ReadOnlySpan<Char>.Empty, provider: null))
				return false;
			Console.Out.WriteLine(span[..charsWritten]);
			return true;
		}
	}

	public override None VisitProgram(ProgramContext context)
	{
		const String printName = "print";
		Action<Object> print = Console.WriteLine;
		var printMethodBuilder = TypeBuilder.DefineMethod(printName, MethodAttributes.Public | MethodAttributes.Static, typeof(void), new []{ typeof(Object) });
		using (var il = new GroboIL(printMethodBuilder))
		{
			il.Ldarg(0);
			il.Call(print.Method);
			il.Ret();
		}
		functions.GetOrAdd(context.body()).Add((printName, new() { (printName, typeof(Object)) }), print.Method);
		return VisitBody(context.body());
	}

	public override None VisitLocalDeclaration(LocalDeclarationContext context)
	{
		var name = context.name.Text;
		if (@params.ContainsKey(name))
			throw new Exception($"Variable {name} already declared as parameter");
		Console.WriteLine($"Declaring local: {name}");
		var type = context.type != null ? Types.GetType(context.type.Text) : typeof(Int32);
		var local = il.DeclareLocal(type, name);
		locals.Add(name, (local, type));
		Visit(context.expr());
		il.Stloc(local);
		return default;
	}

	public override None VisitAssignment(AssignmentContext context)
	{
		base.VisitAssignment(context);
		var name = context.Id().GetText();
		var localInfo = locals[name];
		Console.WriteLine($"Storing to local: {name}");
		il.Stloc(localInfo.local);
		return default;
	}

	public override None VisitVariableReference(VariableReferenceContext context)
	{
		var name = context.Id().GetText();

		if (locals.TryGetValue(name, out var localInfo))
		{
			Console.WriteLine($"Loading local: {name}");
			il.Ldloc(localInfo.local);
			return default;
		}

		if (@params.TryGetValue(name, out var param))
		{
			Console.WriteLine($"Loading argument: {name}");
			il.Ldarg(param.index);
			return default;
		}

		throw new Exception($"Unknown variable: {name}");
	}

	public override None VisitIntegerLiteral(IntegerLiteralContext context)
	{
		var numberText = context.Integer().GetText();
		var number = Int32.Parse(numberText);
		Console.WriteLine($"Loading number: {number}");
		il.Ldc_I4(number);
		return default;
	}

	public override None VisitSubExpr(SubExprContext context)
	{
		return Visit(context.expr());
	}

	public override None VisitBinOpExpr(BinOpExprContext context)
	{
		Visit(context.expr()[0]);
		Visit(context.expr()[1]);
		return Visit(context.binOp());
	}

	public override None VisitMul(MulContext context)
	{
		Console.WriteLine("Multiplying");
		il.Mul_Ovf(unsigned:false);
		return default;
	}

	public override None VisitDiv(DivContext context)
	{
		Console.WriteLine("Dividing");
		il.Div(unsigned:false);
		return default;
	}

	public override None VisitAdd(AddContext context)
	{
		Console.WriteLine("Adding");
		il.Add_Ovf(unsigned:false);
		return default;
	}

	public override None VisitSub(SubContext context)
	{
		Console.WriteLine("Subtracting");
		il.Sub_Ovf(unsigned:false);
		return default;
	}

	public override None VisitFunctionInvocation(FunctionInvocationContext context)
	{
		base.VisitFunctionInvocation(context);
		var name = context.Id().GetText();
		var paramCount = (Byte) context.expr().Length;
		//var @params =
		var function = GetFunction();
		Console.WriteLine($"Invoking function: {name}");
		il.Call(function);
		if (function.ReturnType != typeof(void) && context.Parent is StatementContext)
			il.Pop();
		return default;

		MethodBuilder GetFunction()
		{
			foreach (var bodyContext in context.GetAncestors<BodyContext>())
				if (functions.TryGetValue(bodyContext, out var scoped) && scoped.TryGetValue((name, paramCount), out var found))
					return found;
			throw new InvalidOperationException($"Function is not defined: `{name}`");
		}
	}

	public override None VisitBody(BodyContext context)
	{
		foreach (var fdc in context.functionDefinition())
			Visit(fdc);
		foreach (var sc in context.statement())
			Visit(sc);
		Console.WriteLine("Returning");
		var last = context.statement().LastOrDefault();
		if (last?.@return() is null)
			il.Ldc_I4(0);
		il.Ret();
		return default;
	}

	public override None VisitFunctionDefinition(FunctionDefinitionContext context)
	{
		var bodyContext = context.GetAncestor<BodyContext>();
		var name = context.Id().GetText();
		var paramTypes = context.@params().param().Select(x => (name: x.name.Text, type: Types.GetType(x.type.Text))).ToList();
		var hold = il;
		var methodBuilder = TypeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(Int32), paramTypes.Select(x => x.type).ToArray());
		using var localIl = new GroboIL(methodBuilder);
		il = localIl;
		locals.Clear();
		@params.Clear();
		for (var i = 0; i < paramTypes.Count; i++)
			@params.Add(paramTypes[i].name, (i, paramTypes[i].type));
		functions.GetOrAdd(bodyContext).Add((name, paramTypes), methodBuilder);
		base.VisitFunctionDefinition(context);
		if (context.expr() is not null)
			il.Ret();
		il = hold;
		return default;
	}

	private sealed class What : Dictionary<(String name, List<(String name, Type type)> @params), MethodInfo>
	{
		
	}
}
