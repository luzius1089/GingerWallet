using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GingerCommon.Crypto.Random;

public static class RandomExtensions
{
	public static IList<T> Shuffle<T>(this IList<T> list, GingerRandom random)
	{
		int n = list.Count;
		while (n > 1)
		{
			int k = random.GetInt(0, n);
			n--;
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
		return list;
	}

	public static void GenerateSeed(Span<byte> seed, ReadOnlySpan<byte> seedArray)
	{
		SHA256.HashData(seedArray, seed);
	}

	public static void GenerateSeed(Span<byte> seed, string seedString)
	{
		SHA256.HashData(Encoding.UTF8.GetBytes(seedString), seed);
	}
}
