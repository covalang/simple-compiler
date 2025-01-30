using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;
using Lokad.ILPack;

const String source = @"
	local x: I8 = 3;
	local y = 4;
	local z = x + y * 2;
	print(z);
	func add(a: I32, b: I32) => a + b;
	print(add(1, 2));
	func print_sub(a: I32, b: I32) {
		print(sub(a, b));
		func sub(a: I32, b: I32) => a - b;
	}
	print_sub(5, 3);
";
var input = new CodePointCharStream(source) { name = "source.cova"};
var lexer = new GrammarLexer(input);
var tokens = new CommonTokenStream(lexer);
var parser = new GrammarParser(tokens);
parser.RemoveErrorListeners();
var collectingErrorListener = new CollectingErrorListener();
parser.AddErrorListener(collectingErrorListener);
var program = parser.program();
foreach (var error in collectingErrorListener.Errors)
	Console.WriteLine(error);

var pv = new ProgramVisitor();
using (pv)
	pv.Visit(program);

var type = pv.TypeBuilder.CreateType();
var mainMethod = type.GetMethod("Main") ?? throw new();
var what = mainMethod.CreateDelegate<Func<ReadOnlyMemory<String>, Int32>>();
what.Invoke(args);
var assembly = Assembly.GetAssembly(type) ?? throw new();

AppContext.SetSwitch("Lokad.ILPack.AssemblyGenerator.ReplaceCoreLibWithNetStandard", true);
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

sealed class CollectingErrorListener : IAntlrErrorListener<IToken>
{
	public List<(String sourceName, IToken offendingSymbol, Int32 line, Int32 charPositionInLine, String msg, String exceptionMsg)> Errors { get; } = new();

	public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, Int32 line, Int32 charPositionInLine, String msg, RecognitionException e)
	{
		Errors.Add((recognizer.InputStream.SourceName, offendingSymbol, line, charPositionInLine, msg, e.Message));
	}
}
