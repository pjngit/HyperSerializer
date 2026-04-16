namespace Hyper;

public static class HyperSerializerSettings
{
	/// <summary>
	/// Set to true to write the serialize proxy code to Console.Write. Default is false;
	/// </summary>
	public static bool WriteProxyToConsoleOutput { get; set; } = false;

	/// <summary>
	/// Set to true to serialize fields. Default is true.
	/// </summary>
	public static bool SerializeFields { get; set; } = true;

	/// <summary>
	/// Set to true to use ArrayPool<byte>.Shared for buffer allocation during serialization.
	/// This reduces GC pressure in high-frequency scenarios but adds overhead for single operations.
	/// Default is false (direct allocation for optimal single-operation performance).
	/// 
	/// Recommended: Enable for server applications with many serialization operations per second.
	/// Not recommended for: Microbenchmarks, single/infrequent serialization calls.
	/// </summary>
	public static bool UseArrayPool { get; set; } = false;
}
