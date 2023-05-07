using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Lokad.ILPack;
using static System.Reflection.Emit.AssemblyBuilderAccess;
using static GrammarParser;
using TA = System.Reflection.TypeAttributes;
using MA = System.Reflection.MethodAttributes;

const String source = @"
	local x = 3;
	local y = 4;
	local z = x + y * 2;
	print(z);
	func add(a, b) => a + b;
	print(add(1, 2));
	func print_sub(a, b) {
		print(sub(a, b));
		func sub(a, b) => a - b;
	}
	print_sub(5, 3);
";
var input = new CodePointCharStream(source);
var lexer = new GrammarLexer(input);
var tokens = new CommonTokenStream(lexer);
var parser = new GrammarParser(tokens);
var program = parser.program();

var pv = new ProgramVisitor();
pv.Visit(program);

AppContext.SetSwitch("Lokad.ILPack.AssemblyGenerator.ReplaceCoreLibWithNetStandard", true);
var type = pv.TypeBuilder.CreateType();
var mainMethod = type.GetMethod("Main") ?? throw new();
var what = mainMethod.CreateDelegate<Func<ReadOnlyMemory<String>, Int32>>();
what.Invoke(args);
var assembly = Assembly.GetAssembly(type) ?? throw new();
var generator = new AssemblyGenerator();
var dir = Path.Combine(Environment.CurrentDirectory, "bin");
Directory.CreateDirectory(dir);
var path = Path.Combine(dir, assembly.GetName().Name + ".dll");
generator.GenerateAssembly(assembly, path);
return;

Process.Start("dotnet", "publish ../../../../Runner/Runner.csproj -r osx-x64 -c Release").WaitForExit();

var loadedType = Assembly.LoadFile(path).GetType(type.Name) ?? throw new();
var main = loadedType.GetMethod("Main") ?? throw new();
var @delegate = main.CreateDelegate<Func<ReadOnlyMemory<String>, Int32>>();
@delegate.Invoke(args);
return;

enum None : Byte {}
sealed class ProgramVisitor : GrammarBaseVisitor<None>
{
	public TypeBuilder TypeBuilder { get; }
	private MethodBuilder methodBuilder;
	private readonly Dictionary<String, LocalBuilder> locals = new();
	private readonly Dictionary<String, Int32> parameterIndexes = new();
	private readonly Dictionary<BodyContext, Dictionary<(String name, Byte paramCount), MethodBuilder>> functions = new();

	public ProgramVisitor()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new("Assembly"), RunAndCollect);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
		TypeBuilder = moduleBuilder.DefineType("Simple", TA.Public | TA.Sealed | TA.Class | TA.BeforeFieldInit);
		methodBuilder = TypeBuilder.DefineMethod("Main", MA.Public | MA.Static, typeof(Int32), new[] { typeof(ReadOnlyMemory<String>) });
	}

	public override None VisitProgram(ProgramContext context)
	{
		const String printName = "print";
		var printMethodBuilder = TypeBuilder.DefineMethod(printName, MA.Public | MA.Static, typeof(Int32), new []{ typeof(Int32) });
		Action<Int32> print = Console.WriteLine;
		
		var il = printMethodBuilder.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Call, print.Method);
		il.Emit(OpCodes.Ldc_I4_0);
		il.Emit(OpCodes.Ret);
		
		functions.GetOrAdd(context.body()).Add((printName, 1), printMethodBuilder);
		return VisitBody(context.body());
	}

	public override None VisitLocalDeclaration(LocalDeclarationContext context)
	{
		var name = context.assignment().Id().GetText();
		Console.WriteLine($"Declaring local: {name}");
		var local = methodBuilder.GetILGenerator().DeclareLocal(typeof(Int32));
		locals.Add(name, local);
		return base.VisitLocalDeclaration(context);
	}

	public override None VisitAssignment(AssignmentContext context)
	{
		base.VisitAssignment(context);
		var name = context.Id().GetText();
		var local = locals[name];
		Console.WriteLine($"Storing to local: {name}");
		methodBuilder.GetILGenerator().Emit(OpCodes.Stloc, local);
		return default;
	}

	public override None VisitVariableReference(VariableReferenceContext context)
	{
		var name = context.Id().GetText();

		if (locals.TryGetValue(name, out var local))
		{
			Console.WriteLine($"Loading local: {name}");
			methodBuilder.GetILGenerator().Emit(OpCodes.Ldloc, local);
			return default;
		}

		if (parameterIndexes.TryGetValue(name, out var index))
		{
			Console.WriteLine($"Loading argument: {name}");
			methodBuilder.GetILGenerator().Emit(OpCodes.Ldarg, index);
			return default;
		}

		throw new Exception($"Unknown variable: {name}");
	}

	public override None VisitNumberLiteral(NumberLiteralContext context)
	{
		var numberText = context.Number().GetText();
		var number = Int32.Parse(numberText);
		Console.WriteLine($"Loading number: {number}");
		methodBuilder.GetILGenerator().Emit(OpCodes.Ldc_I4, number);
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
		methodBuilder.GetILGenerator().Emit(OpCodes.Mul_Ovf);
		return default;
	}
	
	public override None VisitDiv(DivContext context)
	{
		Console.WriteLine("Dividing");
		methodBuilder.GetILGenerator().Emit(OpCodes.Div);
		return default;
	}
	
	public override None VisitAdd(AddContext context)
	{
		Console.WriteLine("Adding");
		methodBuilder.GetILGenerator().Emit(OpCodes.Add_Ovf);
		return default;
	}
	
	public override None VisitSub(SubContext context)
	{
		Console.WriteLine("Subtracting");
		methodBuilder.GetILGenerator().Emit(OpCodes.Sub_Ovf);
		return default;
	}
	
	public override None VisitFunctionInvocation(FunctionInvocationContext context)
	{
		base.VisitFunctionInvocation(context);
		var name = context.Id().GetText();
		var paramCount = (Byte) context.expr().Length;
		var function = GetFunction();
		Console.WriteLine($"Invoking function: {name}");
		var il = methodBuilder.GetILGenerator();
		il.Emit(OpCodes.Call, function);
		if (context.Parent is StatementContext)
			il.Emit(OpCodes.Pop);
		return default;
		
		MethodBuilder GetFunction()
		{
			foreach (var body in context.GetAncestors<BodyContext>())
				if (functions.TryGetValue(body, out var scoped) && scoped.TryGetValue((name, paramCount), out var found))
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
		var il = methodBuilder.GetILGenerator();
		var last = context.statement().LastOrDefault();
		if (last?.@return() is null)
			il.Emit(OpCodes.Ldc_I4_0);
		il.Emit(OpCodes.Ret);
		return default;
	}

	public override None VisitFunctionDefinition(FunctionDefinitionContext context)
	{
		var body = context.GetAncestor<BodyContext>();
		var name = context.Id().GetText();
		var paramCount = (Byte) context.parameters().Id().Length;
		var paramTypes = Enumerable.Repeat(typeof(Int32), paramCount).ToArray();
		
		var hold = this.methodBuilder;
		var methodBuilder = TypeBuilder.DefineMethod(name, MA.Public | MA.Static | MA.HideBySig, typeof(Int32), paramTypes);
		var il = methodBuilder.GetILGenerator();
		this.methodBuilder = methodBuilder;
		locals.Clear();
		parameterIndexes.Clear();
		var id = context.parameters().Id();
		for (var i = 0; i < id.Length; i++)
			parameterIndexes.Add(id[i].GetText(), i);
		functions.GetOrAdd(body).Add((name, paramCount), methodBuilder);
		base.VisitFunctionDefinition(context);
		if (context.expr() is not null)
			il.Emit(OpCodes.Ret);
		this.methodBuilder = hold;
		return default;
	}
}