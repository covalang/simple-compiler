// sealed class StatementListener : GrammarBaseListener
// {
// 	private readonly Dictionary<String, Int32> variables;
// 	private readonly Dictionary<String, Func<Int32[], Int32>> functions;
// 	private readonly ExpressionEvaluationVisitor expressionEvaluationVisitor;
//
// 	public StatementListener()
// 	{
// 		variables = new();
// 		functions = new();
// 		expressionEvaluationVisitor = new(variables, functions);
// 		
// 		functions["print"] = x => { Console.WriteLine(String.Join(", ", x)); return 0; };
// 		functions["sqrt"] = x => (Int32)Math.Sqrt(x.Single());
// 	}
// 	
// 	public override void EnterVariableDeclaration(GrammarParser.VariableDeclarationContext context)
// 	{
// 		var name = context.Identifier().GetText();
// 		var value = expressionEvaluationVisitor.Visit(context.expr());
// 		variables.Add(name, value);
// 	}
// 	
// 	public override void EnterAssignment(GrammarParser.AssignmentContext context)
// 	{
// 		var name = context.Identifier().GetText();
// 		var value = expressionEvaluationVisitor.Visit(context.expr());
// 		variables[name] = value;
// 	}
// 	
// 	public override void EnterFunctionInvocation(GrammarParser.FunctionInvocationContext context)
// 	{
// 		var functionName = context.Identifier().GetText();
// 		var function = functions[functionName];
// 		var arguments = context.expr().Select(expressionEvaluationVisitor.Visit).ToArray();
// 		function.DynamicInvoke(arguments);
// 	}
// }