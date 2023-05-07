using System.Runtime.InteropServices;
using Antlr4.Runtime;

static class Extensions
{
	public static T? GetAncestorOrDefault<T>(this ParserRuleContext context) where T : ParserRuleContext
	{
		for (var parent = context.Parent; parent is not null; parent = parent.Parent)
			if (parent is T found)
				return found;
		return null;
	}

	public static T GetAncestor<T>(this ParserRuleContext context) where T : ParserRuleContext =>
		context.GetAncestorOrDefault<T>() ?? throw new InvalidOperationException($"No ancestor of type {typeof(T).Name} found");
	
	public static IEnumerable<T> GetAncestors<T>(this ParserRuleContext context) where T : ParserRuleContext
	{
		for (var parent = context.Parent; parent is not null; parent = parent.Parent)
			if (parent is T found)
				yield return found;
	}

	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory) where TKey : notnull
	{
		ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out var exists);
		if (!exists)
			value = valueFactory();
		return value!;
	}

	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull where TValue : new()
	{
		ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out var exists);
		if (!exists)
			value = new TValue();
		return value!;
	}
}