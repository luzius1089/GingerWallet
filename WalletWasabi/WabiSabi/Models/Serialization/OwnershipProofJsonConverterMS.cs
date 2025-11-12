using NBitcoin;
using System.Text.Json;
using System.Text.Json.Serialization;
using WalletWasabi.Crypto;

namespace WalletWasabi.WabiSabi.Models.Serialization;

internal class OwnershipProofJsonConverterMS : JsonConverter<OwnershipProof>
{
	public override OwnershipProof? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string? serialized = reader.GetString();
		return serialized is not null ? OwnershipProof.FromBytes(Convert.FromHexString(serialized)) : null;
	}

	public override void Write(Utf8JsonWriter writer, OwnershipProof? value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(Convert.ToHexString(value.ToBytes()));
	}
}
