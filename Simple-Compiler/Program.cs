using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime;
using FluentIL;
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
	private IEmitter body;
	private readonly Dictionary<String, ILocal> locals = new();
	private readonly Dictionary<String, Int32> parameterIndexes = new();
	private readonly Dictionary<BodyContext, Dictionary<(String name, Byte paramCount), MethodBuilder>> functions = new();

	public ProgramVisitor()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new("Assembly"), RunAndCollect);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
		TypeBuilder = moduleBuilder.DefineType("Simple", TA.Public | TA.Sealed | TA.Class | TA.BeforeFieldInit);
		var methodBuilder = TypeBuilder.DefineMethod("Main", MA.Public | MA.Static, typeof(Int32), new[] { typeof(ReadOnlyMemory<String>) });
		body = methodBuilder.Body();
	}

	public override None VisitProgram(ProgramContext context)
	{
		const String printName = "print";
		Action<Int32> print = Console.WriteLine;
		var printMethodBuilder = TypeBuilder.DefineMethod(printName, MA.Public | MA.Static, typeof(Int32), new []{ typeof(Int32) });
		printMethodBuilder.Body()
			.LdArg0()
			.Call(print.Method)
			.LdcI4_0()
			.Ret();
		functions.GetOrAdd(context.body()).Add((printName, 1), printMethodBuilder);
		return VisitBody(context.body());
	}

	public override None VisitLocalDeclaration(LocalDeclarationContext context)
	{
		var name = context.assignment().Id().GetText();
		if (parameterIndexes.ContainsKey(name))
			throw new Exception($"Variable {name} already declared as parameter");
		Console.WriteLine($"Declaring local: {name}");
		body.DeclareLocal<Int32>(name, out var local);
		locals.Add(name, local);
		return base.VisitLocalDeclaration(context);
	}

	public override None VisitAssignment(AssignmentContext context)
	{
		base.VisitAssignment(context);
		var name = context.Id().GetText();
		var local = locals[name];
		Console.WriteLine($"Storing to local: {name}");
		body.StLoc(local);
		return default;
	}

	public override None VisitVariableReference(VariableReferenceContext context)
	{
		var name = context.Id().GetText();

		if (locals.TryGetValue(name, out var local))
		{
			Console.WriteLine($"Loading local: {name}");
			body.LdLoc(local);
			return default;
		}

		if (parameterIndexes.TryGetValue(name, out var index))
		{
			Console.WriteLine($"Loading argument: {name}");
			body.LdArg(index);
			return default;
		}

		throw new Exception($"Unknown variable: {name}");
	}

	public override None VisitNumberLiteral(NumberLiteralContext context)
	{
		var numberText = context.Number().GetText();
		var number = Int32.Parse(numberText);
		Console.WriteLine($"Loading number: {number}");
		body.LdcI4(number);
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
		body.MulOvf();
		return default;
	}
	
	public override None VisitDiv(DivContext context)
	{
		Console.WriteLine("Dividing");
		body.Div();
		return default;
	}
	
	public override None VisitAdd(AddContext context)
	{
		Console.WriteLine("Adding");
		body.AddOvf();
		return default;
	}
	
	public override None VisitSub(SubContext context)
	{
		Console.WriteLine("Subtracting");
		body.SubOvf();
		return default;
	}
	
	public override None VisitFunctionInvocation(FunctionInvocationContext context)
	{
		base.VisitFunctionInvocation(context);
		var name = context.Id().GetText();
		var paramCount = (Byte) context.expr().Length;
		var function = GetFunction();
		Console.WriteLine($"Invoking function: {name}");
		body.Call(function);
		if (context.Parent is StatementContext)
			body.Pop();
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
			body.LdcI4_0();
		body.Ret();
		return default;
	}

	public override None VisitFunctionDefinition(FunctionDefinitionContext context)
	{
		var bodyContext = context.GetAncestor<BodyContext>();
		var name = context.Id().GetText();
		var paramCount = (Byte) context.parameters().Id().Length;
		var paramTypes = Enumerable.Repeat(typeof(Int32), paramCount).ToArray();
		
		var hold = body;
		var methodBuilder = TypeBuilder.DefineMethod(name, MA.Public | MA.Static | MA.HideBySig, typeof(Int32), paramTypes);
		body = methodBuilder.Body();
		locals.Clear();
		parameterIndexes.Clear();
		var id = context.parameters().Id();
		for (var i = 0; i < id.Length; i++)
			parameterIndexes.Add(id[i].GetText(), i);
		functions.GetOrAdd(bodyContext).Add((name, paramCount), methodBuilder);
		base.VisitFunctionDefinition(context);
		if (context.expr() is not null)
			body.Ret();
		body = hold;
		return default;
	}
}