using System.Reflection.Emit;

// sealed class DefinitionListener : GrammarBaseListener
// {
// 	private readonly DynamicMethod rootMethod;
// 	private readonly ILGenerator rootGenerator;
// 	
// 	public DefinitionListener(Scope rootScope)
// 	{
// 		scope = rootScope;
// 		rootMethod = new DynamicMethod("Main", typeof(Int32), new[] { typeof(Int32[]) }, typeof(DefinitionListener).Module);
// 		rootGenerator = rootMethod.GetILGenerator();
// 	}
//
// 	private Scope scope;
//
// 	public override void EnterVariableDeclaration(VariableDeclarationContext context)
// 	{
// 		var name = context.Id().GetText();
// 		var local = rootGenerator.DeclareLocal(typeof(Int32));
// 		if (!scope.LocalIndexes.TryAdd(name, scope.LocalIndexes.Count))
// 			throw new InvalidOperationException($"Variable is already defined: `{name}`");
// 	}
//
// 	public override void EnterFunctionDefinition(FunctionDefinitionContext context)
// 	{
// 		var functionName = context.name.Text;
// 		var functionScope = scope.AddChild(functionName);
// 		for (var i = 0; i < context._params.Count; i++)
// 			functionScope.ParameterIndexes.Add(context._params[i].Text, i);
// 		if (!scope.Functions.Add(functionName, (functionScope, v => v.Visit(context.functionBody()))))
// 			throw new InvalidOperationException($"Function is already defined: `{functionName}`");
// 		scope = functionScope;
// 	}
//
// 	public override void ExitFunctionDefinition(FunctionDefinitionContext context)
// 	{
// 		scope = scope.ParentScope ?? throw new InvalidOperationException("No parent scope");
// 	}
// }

sealed class FunctionDefinition : IEquatable<FunctionDefinition>
{
	public static FunctionDefinition Root { get; } = new(String.Empty, null!);

	public FunctionDefinition AddChild(String name) => new(name, this);

	private FunctionDefinition(String name, FunctionDefinition parent)
	{
		Name = name;
		Parent = parent;
		if (Parent?.children.Add(this) == false)
			throw new InvalidOperationException($"Function is already defined: `{name}`");
	}

	public String Name { get; }
	public FunctionDefinition? Parent { get; }

	public Dictionary<String, Int32> ParameterIndexes { get; } = new();
	public Dictionary<String, Int32> LocalIndexes { get; } = new();

	public IReadOnlySet<FunctionDefinition> Children => children;
	private readonly HashSet<FunctionDefinition> children = new();

	private DynamicMethod? method;
	public DynamicMethod Method => method ??= CreateDynamicMethod();
	private DynamicMethod CreateDynamicMethod()
	{
		var array = new Type[ParameterIndexes.Count];
		Array.Fill(array, typeof(Int32));
		return new DynamicMethod(Name, typeof(Int32), array, typeof(FunctionDefinition).Module);
	}

	public override Int32 GetHashCode() => HashCode.Combine(Parent, Name, ParameterIndexes.Count);
	public override Boolean Equals(Object? obj) => ReferenceEquals(this, obj) || obj is Scope other && Equals(other);
	public Boolean Equals(FunctionDefinition? other) => !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || MemberwiseEquals(other));
	private Boolean MemberwiseEquals(FunctionDefinition other) =>
		ReferenceEquals(Parent, other.Parent) &&
		Name == other.Name &&
		Children.Count == other.Children.Count &&
		ParameterIndexes.Count == other.ParameterIndexes.Count &&
		LocalIndexes.Count == other.LocalIndexes.Count &&
		Children.ToHashSet().SetEquals(other.Children) &&
		ParameterIndexes.ToHashSet().SetEquals(other.ParameterIndexes) &&
		LocalIndexes.ToHashSet().SetEquals(other.LocalIndexes);
}