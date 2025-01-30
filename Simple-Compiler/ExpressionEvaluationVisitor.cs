using static GrammarParser;

sealed class ExpressionEvaluationVisitor : GrammarBaseVisitor<Int32>
{
	private readonly Scope scope;
	public StackFrame StackFrame { get; private set; }

	public ExpressionEvaluationVisitor(Scope scope, StackFrame stackStackFrame)
	{
		this.scope = scope;
		this.StackFrame = stackStackFrame;
	}

	public override Int32 VisitAssignment(AssignmentContext context)
	{
		var name = context.Id().GetText();
		var value = Visit(context.expr());
		StackFrame.GetLocal(name) = value;
		return value;
	}

	public override Int32 VisitVariableReference(VariableReferenceContext context)
	{
		return StackFrame.GetLocal(context.GetText());
	}

	public override Int32 VisitIntegerLiteral(IntegerLiteralContext context)
	{
		return Int32.Parse(context.GetText());
	}

	public override Int32 VisitFunctionInvocation(FunctionInvocationContext context)
	{
		var name = context.Id().GetText();
		//var function = scope.Functions[name];
		var arguments = new Int32[context.expr().Length];
		for (var i = 0; i < arguments.Length; i++)
			arguments[i] = Visit(context.expr(i));
		StackFrame = new StackFrame(StackFrame, scope, arguments);
		//var result = function.Invoke(this);
		StackFrame = StackFrame.Parent ?? throw new InvalidOperationException("Stack frame is null");
		return 0; //return result;
	}

	public override Int32 VisitSubExpr(SubExprContext context) => Visit(context.expr());

	// public override Int32 VisitMul(MulContext ctx) => Visit(ctx.expr(0)) * Visit(ctx.expr(1));
	// public override Int32 VisitDiv(DivContext ctx) => Visit(ctx.expr(0)) / Visit(ctx.expr(1));
	// public override Int32 VisitAdd(AddContext ctx) => Visit(ctx.expr(0)) + Visit(ctx.expr(1));
	// public override Int32 VisitSub(SubContext ctx) => Visit(ctx.expr(0)) - Visit(ctx.expr(1));
}
