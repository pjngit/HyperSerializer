using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using HyperSerializer.Dynamic.Syntax;
using HyperSerializer.Dynamic.Syntax.Templates;
using HyperSerializer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hyper;

/// <summary>
/// HyperSerializer\<typeparam name="T"></typeparam> default implementation with support for value types, strings arrays and lists containing value types, and reference types (e.g. your DTO class).
/// Note that reference types containing properties that are complex types (i.e. a child object/class with properties) and Dictionaries are not yet supported.  Properties of these types will be ignored during serialization and deserialization.
/// </summary>
/// <typeparam name="T">ValueType (e.g. int, Guid, string, decimal?,etc,; arrays and lists of these types are supported as well) or heap based ref type (e.g. DTO class/object) containing properties to be serialized/deserialized.
/// NOTE objects containing properties that are complex types (i.e. other objects with properties) and type Dictionary are ignored during serialization and deserialization.</typeparam>
public static class HyperSerializer<T>
{
    /// <summary>
    /// Dynamic serialization proxy name
    /// </summary>
    private static readonly string _proxyTypeName = $"ProxyGen.SerializationProxy_{typeof(T).GetClassName<T>()}";

    /// <summary>
    /// Dynamic serialization proxy type
    /// </summary>
    private static Type _proxyType;

    /// <summary>
    /// Serialization proxy delegate definition for <typeparam name="T"></typeparam>
    /// </summary>
    /// <param name="obj">object or value type to be serialized</param>
    /// <returns><seealso cref="Span{byte}"/></returns>
    internal delegate Span<byte> Serializer(T obj);

    /// <summary>
    /// Static serialization delegate cache
    /// </summary>
    internal static Serializer SerializeDynamic;
    internal delegate T Deserializer(ReadOnlySpan<byte> bytes);
    internal static Deserializer DeserializeDynamic;

    static HyperSerializer() => Compile();

    /// <summary>
    /// Serialize <typeparam name="T"></typeparam> to binary non-async
    /// </summary>
    /// <param name="obj">object or value type to be serialized</param>
    /// <returns><seealso cref="Span{byte}"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Span<byte> Serialize(T obj) => SerializeDynamic(obj);

    /// <summary>
    /// Deserialize binary to <typeparam name="T"></typeparam> non-async
    /// </summary>
    /// <param name="bytes"><seealso cref="ReadOnlySpan{byte}"/>, <seealso cref="Span{byte}"/> or byte[] to be deserialized</param>
    /// <returns><typeparam name="T"></typeparam></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Deserialize(ReadOnlySpan<byte> bytes) => DeserializeDynamic(bytes);

    /// <summary>
    /// Serialize <typeparam name="T"></typeparam> to binary async
    /// </summary>
    /// <param name="obj">object or value type to be serialized</param>
    /// <returns><seealso cref="Memory{byte}"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ValueTask<Memory<byte>> SerializeAsync(T obj)
    {
        var span = Serialize(obj);
        return new(new Memory<byte>(span.ToArray()));
    }

    /// <summary>
    /// Deserialize binary to <typeparam name="T"></typeparam> async
    /// </summary>
    /// <param name="bytes"><seealso cref="ReadOnlyMemory{byte}"/>, <seealso cref="Memory{byte}"/> or byte[] array to be deserialized</param>
    /// <returns><typeparam name="T"></typeparam></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ValueTask<T> DeserializeAsync(ReadOnlyMemory<byte> bytes) => new(Deserialize(bytes.Span));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void BuildDelegates()
    {
        var infos = _proxyType.GetMethod("Serialize");
        SerializeDynamic = (Serializer)infos.CreateDelegate(typeof(Serializer));

        var infod = _proxyType.GetMethod("Deserialize");
        DeserializeDynamic = (Deserializer) infod.CreateDelegate(typeof(Deserializer));
    }
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static void Compile()
	{
		var infos = new MemberTypeInfos<T>();

		CSharpCompilation compilation;

		// Select template based on ArrayPool setting
		if (HyperSerializerSettings.UseArrayPool)
		{
			var result = CodeGen<ProxySyntaxTemplate_Net10_ArrayPool>.GenerateCode<T>(infos);
			var syntaxTree = CSharpSyntaxTree.ParseText(result.Code);

			compilation = CSharpCompilation.Create(
					assemblyName: $"ProxyGen.SerializationProxy_{result.ClassName}_{DateTime.Now.ToFileTimeUtc()}")
				.AddSyntaxTrees(syntaxTree)
				.WithReferences(CodeGen<ProxySyntaxTemplate_Net10_ArrayPool>.GetReferences<T>(infos, includeUnsafe: true))
				.WithOptions(new CSharpCompilationOptions(
					outputKind: OutputKind.DynamicallyLinkedLibrary, 
					allowUnsafe: true, 
					optimizationLevel: OptimizationLevel.Release));

			if (HyperSerializerSettings.WriteProxyToConsoleOutput)
			{
				Console.Write(syntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
			}
		}
		else
		{
			var result = CodeGen<ProxySyntaxTemplate_Net10>.GenerateCode<T>(infos);
			var syntaxTree = CSharpSyntaxTree.ParseText(result.Code);

			compilation = CSharpCompilation.Create(
					assemblyName: $"ProxyGen.SerializationProxy_{result.ClassName}_{DateTime.Now.ToFileTimeUtc()}")
				.AddSyntaxTrees(syntaxTree)
				.WithReferences(CodeGen<ProxySyntaxTemplate_Net10>.GetReferences<T>(infos, includeUnsafe: true))
				.WithOptions(new CSharpCompilationOptions(
					outputKind: OutputKind.DynamicallyLinkedLibrary, 
					allowUnsafe: true, 
					optimizationLevel: OptimizationLevel.Release));

			if (HyperSerializerSettings.WriteProxyToConsoleOutput)
			{
				Console.Write(syntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
			}
		}

		Emit(compilation);
		BuildDelegates();
	}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Emit(CSharpCompilation _compilation)
    {
        if (_proxyType != null) return;
        using var ms = new MemoryStream();
        var result = _compilation.Emit(ms);
        if (!result.Success)
        {
            var compilationErrors = result.Diagnostics
                .Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error)
                .ToList();
            if (compilationErrors.Count > 0)
            {
                var firstError = compilationErrors[0];
                var errorNumber = firstError.Id;
                var errorDescription = firstError.GetMessage();
                var firstErrorMessage = $"{errorNumber}: {errorDescription};";
                var exception = new Exception($"Compilation failed, first error is: {firstErrorMessage}");
                foreach (var e in compilationErrors)
                {
                    if (!exception.Data.Contains(e.Id))
                        exception.Data.Add(e.Id, e.GetMessage());
                }
                throw exception;
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assemblyData = ms.ToArray();
        AssemblyLoadContext context = new CollectibleLoadContext();
        var generatedAssembly = context.LoadFromStream(new MemoryStream(assemblyData));
        _proxyType = generatedAssembly.GetType(_proxyTypeName);
    }
}