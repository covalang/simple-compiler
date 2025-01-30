sealed class StackFrame
{
	public static readonly StackFrame Root = new(null, Scope.Root, default(Boolean));
	
	public StackFrame? Parent { get; }
	public Scope Scope { get; }
	public Int32[] Arguments { get; }
	public Int32[] Locals { get; }

	private StackFrame(StackFrame? parent, Scope scope, Boolean _)
	{
		Parent = parent;
		Scope = scope;
		Arguments = new Int32[scope.ParameterIndexes.Count];
		Locals = new Int32[scope.LocalIndexes.Count];
	}
	
	public StackFrame(StackFrame parent, Scope scope) : this(parent, scope, default(Boolean)) {}

	public StackFrame(StackFrame parent, Scope scope, Int32[] arguments) : this(parent, scope)
	{
		if (arguments.Length != scope.ParameterIndexes.Count)
			throw new ArgumentException("Invalid number of arguments", nameof(arguments));
		Arguments = arguments;
	}
	
	public ref Int32 GetArgument(String name)
	{
		if (Scope.ParameterIndexes.TryGetValue(name, out var value))
			return ref Arguments[value];
		throw new InvalidOperationException($"Parameter not defined: `{name}`");
	}
	
	public ref Int32 GetLocal(String name)
	{
		if (Scope.LocalIndexes.TryGetValue(name, out var value))
			return ref Locals[value];
		throw new InvalidOperationException($"Variable not defined: `{name}`");
	}
}