sealed class Scope : IEquatable<Scope>
{
	public String? Name { get; }
	public Scope? ParentScope { get; }
	public IReadOnlySet<Scope> ChildScopes => childScopes;
	private readonly HashSet<Scope> childScopes = new();
	public Dictionary<String, Int32> ParameterIndexes { get; } = new();
	public Dictionary<String, Int32> LocalIndexes { get; } = new();
	public HashSet<Function> Functions { get; } = new();

	private Scope(Scope parentScope, String? name)
	{
		Name = name;
		if (!parentScope.childScopes.Add(this))
			throw new InvalidOperationException($"Scope is already defined: `{name}`");
		ParentScope = parentScope;
	}
	
	public Scope AddChild(String? name) => new(this, name);

	private Scope() {}

	public static readonly Scope Root = new();

	public override Int32 GetHashCode() => HashCode.Combine(Name, ParentScope, ChildScopes.Count, ParameterIndexes.Count, LocalIndexes.Count);
	public override Boolean Equals(Object? obj) => ReferenceEquals(this, obj) || obj is Scope other && Equals(other);
	public Boolean Equals(Scope? other) => !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || MemberwiseEquals(other));
	private Boolean MemberwiseEquals(Scope other) =>
		Name == other.Name &&
		ReferenceEquals(ParentScope, other.ParentScope) &&
		ChildScopes.Count == other.ChildScopes.Count &&
		ParameterIndexes.Count == other.ParameterIndexes.Count &&
		LocalIndexes.Count == other.LocalIndexes.Count &&
		ChildScopes.ToHashSet().SetEquals(other.ChildScopes) &&
		ParameterIndexes.ToHashSet().SetEquals(other.ParameterIndexes) &&
		LocalIndexes.ToHashSet().SetEquals(other.LocalIndexes);
}

sealed class Function
{
	public required String Name { get; init; }
	public required Function? Parent { get; init; }
}