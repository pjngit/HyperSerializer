namespace HyperSerializer.Dynamic.Syntax.Templates;

internal class ProxySyntaxTemplateUnsafe : IProxySyntaxTemplate
{
	public string PropertyTemplateSerialize => "var _{0} = ({1}) {2}; fixed (byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = {3})) *(({1}*)p) = _{0};";
	public string PropertyTemplateDeserialize => "fixed (byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = {2})) {0} = *(({1}*)p);";
	public string PropertyTemplateDeserializeLocal => "var _{0} = ({1})default; fixed (byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = {2}))  _{0} = *(({1}*)p); ";
	public string PropertyTemplateSerializeNullable => "var _{0} = {1} ?? default; offset+=offsetWritten; if(((bytes[offset++] = (byte)({1}==null ? 1 : 0)) != 1)) fixed (byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = {2})) *(({3}?*)(p)) = _{0}; else offsetWritten = 0;";
	public string PropertyTemplateDeserializeNullable => "offset+=offsetWritten; if(bytes[offset++] != 1) fixed (byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = {2}))  {0} = *(({1}?*)p);  else offsetWritten = 0;";
	public string PropertyTemplateSerializeVarLenStr => "if(_{1} >= 0 && {0} != null) MemoryMarshal.AsBytes({0}.AsSpan()).CopyTo(bytes.Slice(offset+=offsetWritten, offsetWritten = _{1}));";
	public string PropertyTemplateDeserializeVarLenStr => "{0} = (_{1} >= 0) ? MemoryMarshal.Cast<byte, char>(bytes.Slice(offset += offsetWritten, offsetWritten = _{1})).ToString() : null;";
	public string PropertyTemplateSerializeVarLenArr => "if(_{1} > 0) { fixed(byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = _{1})) { Buffer.MemoryCopy({0}.__address, (void*)p, _{1}, _{1}); } }";
	public string PropertyTemplateDeserializeVarLenArr => "{0} = (_{1} >= 0) ? MemoryMarshal.Cast<byte,{2}>(bytes.Slice(offset += offsetWritten, offsetWritten = _{1})).ToArray() : null;";
	public string PropertyTemplateDeserializeVarLenList => "{0} = (_{1} >= 0) ? new List<{2}>(MemoryMarshal.Cast<byte,{2}>(bytes.Slice(offset += offsetWritten, offsetWritten = _{1}))) : null;";
	public string PropertyTemplateSerializeListLen => "int _{0} = ({1}?.Count() ?? -1)*Unsafe.SizeOf<{2}>(); fixed(byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = 4)) *(int*)p = _{0};";
	public string PropertyTemplateSerializeArrLen => "int _{0} = ({1}?.Length ?? -1)*Unsafe.SizeOf<{2}>(); fixed(byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = 4)) *(int*)p = _{0};";
	public string PropertyTemplateSerializeDictLen => "int _{0} = ({1}?.Count ?? -1)*{2}; fixed(byte* p = bytes.Slice(offset+=offsetWritten, offsetWritten = 4)) *(int*)p = _{0};";
	public string PropertyTemplateSerializeVarLenDict => "if(_{1} > 0){{ foreach(var kvp in {0}) {{ var k = kvp.Key; var v = kvp.Value; fixed(byte* pk = bytes.Slice(offset+=offsetWritten, offsetWritten = {2})) *((({3}*)pk)) = k; fixed(byte* pv = bytes.Slice(offset+=offsetWritten, offsetWritten = {4})) *((({5}*)pv)) = v; }} }}";
	public string PropertyTemplateDeserializeVarLenDict => "{0} = (_{1} >= 0) ? DeserializeDict_{2}_{3}(bytes.Slice(offset += offsetWritten, offsetWritten = _{1})) : null;";
	public string StringLength => "({0}?.Length * 2 ?? -1)";
	public string StringLengthSpan => "({0}?.Length * 2 ?? 0)";
	public string ClassTemplate =>
        @"
					
					using System;
					using System.Runtime.CompilerServices;
					using System.Runtime.InteropServices;
					using System.Text;
					namespace ProxyGen
					{{
						public static unsafe class SerializationProxy_{0}
						{{
								#if NET5_0
								internal static readonly Encoding Utf8Encoding => new UTF8Encoding(false);
                                #elif NET6_0
                                internal static readonly Encoding Utf8Encoding => new UTF8Encoding(false);                                
								#else
								internal static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
								#endif
								private const int maxStackAlloc = 128;
								[MethodImpl(MethodImplOptions.AggressiveInlining)]
								public static Span<byte> Serialize({1} obj)
								{{
									//var len = {2};	
									//if(len <= maxStackAlloc)
         //                           	return SerializeStack(obj);
									//else
										return SerializeHeap(obj);
									
								}}	
								[MethodImpl(MethodImplOptions.AggressiveInlining)]
								public static Span<byte> SerializeStack({1} obj)
								{{
									var offset = 0;
									var offsetWritten = 0;
									var len = {2};
									Span<byte> bytes = stackalloc byte[len];	
{3}
									return bytes.ToArray();
								}}
								[MethodImpl(MethodImplOptions.AggressiveInlining)]
								public static Span<byte> SerializeHeap({1} obj)
								{{
									var offset = 0;
									var offsetWritten = 0;
									var len = {2};
									Span<byte> bytes = new byte[len];
{3}
									return bytes;									
								}}
								
								[MethodImpl(MethodImplOptions.AggressiveInlining)]
								public static {1} Deserialize(ReadOnlySpan<byte> bytes)
								{{
									{1} obj = {5}; 
									var offset = 0;
									var offsetWritten = 0;
									int len0 = 0;
					{4}
									return obj;
								}}
								[MethodImpl(MethodImplOptions.AggressiveInlining)]
								public static Memory<byte> SerializeAsync({1} obj)
								{{

									return Serialize(obj).ToArray();
                                    
								}}	
								
								[MethodImpl(MethodImplOptions.AggressiveInlining)]
								public static {1} DeserializeAsync(ReadOnlyMemory<byte> bytes)
								{{
									return Deserialize(bytes.Span);
								}}
								
						}}
						
		}}";
}