using System.Collections.Generic;
using System.Linq;

namespace WalletWasabi.SecretHunt;

public class SecretHuntEventResultModel
{
	public SecretHuntEventResultModel(SecretHuntEventResult result)
	{
		var event_ = result.Event;
		Id = event_.Id;
		Description = event_.Description;
		StartDate = event_.StartDate;
		EndDate = event_.EndDate;

		ExtraSecret = result.ExtraSecret;
		Secrets = result.Secrets.Keys.Select(x => x.StartsWith('_') ? x[(x.IndexOf('_', 1) + 1)..] : x).Order().ToList();
	}

	public string Id { get; set; } = "";
	public string Description { get; set; } = "";
	public DateTimeOffset StartDate { get; set; } = DateTimeOffset.MaxValue;
	public DateTimeOffset EndDate { get; set; } = DateTimeOffset.MaxValue;

	public string? ExtraSecret { get; set; } = null;
	public List<string> Secrets { get; set; } = [];
}
