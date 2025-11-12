using GingerCommon.Logging;
using System;
using System.Text.Json;

namespace GingerCommon.Static;

public static class JsonUtils
{
	public static readonly JsonSerializerOptions OptionCaseInsensitive = new() { PropertyNameCaseInsensitive = true };

	public static string Serialize<TRequest>(TRequest obj)
	{
		return Serialize(obj, OptionCaseInsensitive);
	}

	public static string Serialize<TRequest>(TRequest obj, JsonSerializerOptions options)
	{
		try
		{
			return JsonSerializer.Serialize(obj, options);
		}
		catch
		{
			Logger.LogDebug($"Failed to serialize {typeof(TRequest)} from obj '{obj}'");
			throw;
		}
	}

	public static TResponse Deserialize<TResponse>(string jsonString)
	{
		try
		{
			return JsonSerializer.Deserialize<TResponse>(jsonString, OptionCaseInsensitive) ?? throw new InvalidOperationException("Deserialization error");
		}
		catch
		{
			Logger.LogDebug($"Failed to deserialize {typeof(TResponse)} from json '{jsonString}'");
			throw;
		}
	}
}
