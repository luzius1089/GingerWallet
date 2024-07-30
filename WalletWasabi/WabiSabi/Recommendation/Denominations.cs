using NBitcoin;
using System.Collections.Generic;
using System.Linq;
using WalletWasabi.Extensions;

namespace WalletWasabi.WabiSabi.Recommendation;

public class Denominations
{
	public Denominations(Money minAllowedOutputAmount, Money maxAllowedOutputAmount)
	{
		MinAllowedOutputAmount = minAllowedOutputAmount;
		MaxAllowedOutputAmount = maxAllowedOutputAmount;

		StandardDenominations = CreateStandardDenominations();
	}

	public Money MinAllowedOutputAmount { get; }
	public Money MaxAllowedOutputAmount { get; }

	public List<Money> StandardDenominations { get; }

	public List<Money> CreatePreferedDenominations(IEnumerable<Money> inputEffectiveValues, FeeRate miningFee)
	{
		var histogram = GetDenominationFrequencies(inputEffectiveValues, miningFee);

		// Filter out and order denominations those have occurred in the frequency table at least twice.
		var preFilteredDenoms = histogram
			.Where(x => x.Value > 1)
			.OrderByDescending(x => x.Key)
			.Select(x => x.Key)
			.ToArray();

		// Filter out denominations very close to each other.
		// Heavy filtering on the top, little to no filtering on the bottom,
		// because in smaller denom levels larger users are expected to participate,
		// but on larger denom levels there's little chance of finding each other.
		var increment = 0.5 / preFilteredDenoms.Length;
		List<Money> denoms = new();
		var currentLength = preFilteredDenoms.Length;
		foreach (var denom in preFilteredDenoms)
		{
			var filterSeverity = 1 + currentLength * increment;
			if (denoms.Count == 0 || denom.Satoshi <= (long)(denoms.Last().Satoshi / filterSeverity))
			{
				denoms.Add(denom);
			}
			currentLength--;
		}

		return denoms;
	}

	public bool IsValidDenomination(List<Money> denoms, IEnumerable<Money> inputEffectiveValues)
	{
		if (denoms.Count == 0 || !inputEffectiveValues.Any())
		{
			return false;
		}
		// Should be reverse ordered, unique, without big differences
		for (int idx = 0, len = denoms.Count - 1; idx < len; idx++)
		{
			if (denoms[idx] <= denoms[idx + 1] || (idx > 0 && denoms[idx] > 4 * denoms[idx + 1]))
			{
				return false;
			}
		}
		// Should use standard denomination levels
		if (denoms.Any(x => !StandardDenominations.Contains(x)))
		{
			return false;
		}

		if (denoms.Last() > inputEffectiveValues.Min() || denoms.First() > inputEffectiveValues.Max())
		{
			return false;
		}

		return true;
	}

	/// <returns>Pair of denomination and the number of times we found it in a breakdown.</returns>
	public SortedDictionary<Money, int> GetDenominationFrequencies(IEnumerable<Money> inputEffectiveValues, FeeRate miningFee)
	{
		var minimumOutputFee = miningFee.GetFee(ScriptType.P2WPKH.EstimateOutputVsize());
		// We can't change this function significantly as the coinjoin heavily based on this function's deterministic nature:
		// each client gets about same result of the denominations from the input list

		SortedDictionary<Money, int> inputs = new();
		foreach (var input in inputEffectiveValues)
		{
			inputs.AddValue(input, 1);
		}

		// the highest output is allowed only we have enough of them
		var outputLimit = inputs.Last().Value > 1 ? inputs.Last().Key : inputs.SkipLast(1).Last().Key;
		var denomsForBreakDown = StandardDenominations.Where(x => x <= outputLimit); // Take only affordable denominations.

		SortedDictionary<Money, int> denomFrequencies = new();
		foreach (var input in inputs)
		{
			Money amount = input.Key;
			Money? denom = null;
			while ((denom = denomsForBreakDown.FirstOrDefault(x => x + minimumOutputFee <= amount)) != null)
			{
				denomFrequencies.AddValue(denom, input.Value);
				amount -= denom + minimumOutputFee;
			}
		}

		return denomFrequencies;
	}

	private void AddDenominations(List<Money> dest, Func<int, double> generator)
	{
		Money amount = Money.Zero;
		for (int i = 0; amount <= MaxAllowedOutputAmount && i < int.MaxValue; i++)
		{
			amount = Money.Satoshis((ulong)generator(i));
			if (amount >= MinAllowedOutputAmount)
			{
				dest.Add(amount);
			}
		}
	}

	public List<Money> CreateStandardDenominations()
	{
		List<Money> result = new();
		AddDenominations(result, i => Math.Pow(2, i));
		AddDenominations(result, i => Math.Pow(3, i));
		AddDenominations(result, i => 2 * Math.Pow(3, i));
		AddDenominations(result, i => Math.Pow(10, i));
		AddDenominations(result, i => 2 * Math.Pow(10, i));
		AddDenominations(result, i => 5 * Math.Pow(10, i));

		result.Sort((x, y) => y.CompareTo(x));
		return result;
	}
}
