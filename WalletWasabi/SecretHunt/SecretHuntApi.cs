using NBitcoin;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WalletWasabi.Crypto;

namespace WalletWasabi.SecretHunt;

public class SecretHuntApi
{
	public static uint256 CreateProofOfSecrets(string evenId, IEnumerable<string> secrets)
	{
		string strToHash = $"SecretHunt_{evenId}_{string.Join('/', secrets.Order())}";
		Span<byte> bytes = stackalloc byte[32];
		SHA256.HashData(Encoding.UTF8.GetBytes(strToHash), bytes);
		return new(bytes);
	}

	public static CoinJoinInputCommitmentData CreateInputCommitmentData(uint256 roundId) => new("GingerWalletSecretHunt", roundId);

	// Gives back the seed that is used to generate the random to roll for the secrets:
	// Span<byte> seed = stackalloc byte[32];
	// RandomExtensions.GenerateSeed(seed, seedString);
	// DeterministicRandom rnd = new(seed);
	// Finally rnd.GetInt(0, weights.Sum()) to get the exact result that the server used to gave the secret
	public static string GetCoinjoinSeedForSecret(string eventId, string eventSalt, uint256 coinjoinId) => $"GingerWalletSecretHunt_{eventId}_{eventSalt}_{coinjoinId}";
}

public record SecretHuntSecretRequest(uint256 RoundId, uint256 CoinjoinId, OutPoint Input, OwnershipProof OwnershipProof);

public record SecretHuntSecretResponse(Dictionary<string, string[]> Matches, string Error);

public record SecretHuntExtraSecretRequest(string EventId, uint256 ProofOfSecrets);

public record SecretHuntExtraSecretResponse(string ExtraSecret);

public record SecretHuntEventsRequest();

public record SecretHuntEventsResponse(SecretHuntEvent[] Events);

public class SecretHuntEvent
{
	public SecretHuntEvent(string id, string? description, DateTimeOffset startDate, DateTimeOffset endDate, int[] weights, string salt)
	{
		Id = id;
		Description = description ?? "";
		StartDate = startDate;
		EndDate = endDate;
		Weights = weights;
		Salt = salt;
	}

	public string Id { get; set; } = "";
	public string Description { get; set; } = "";
	public DateTimeOffset StartDate { get; set; } = DateTimeOffset.MaxValue;
	public DateTimeOffset EndDate { get; set; } = DateTimeOffset.MaxValue;

	// The weight for each of the secrets
	public int[] Weights { get; set; } = [];

	// The salt is revealed only after the EndDate + 1 day
	public string Salt { get; set; } = "";

	public static readonly SecretHuntEvent EmptyEvent = new("", "", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], "");

	// Simple Equals, no extra Equals(object?) and then GetHashCode(), we don't need them
	public bool Equals(SecretHuntEvent? other)
	{
		if (this == other)
		{
			return true;
		}
		if (other is null)
		{
			return false;
		}
		return Id == other.Id && Description == other.Description && StartDate == other.StartDate && EndDate == other.EndDate && Weights.SequenceEqual(other.Weights) && Salt == other.Salt;
	}
}
