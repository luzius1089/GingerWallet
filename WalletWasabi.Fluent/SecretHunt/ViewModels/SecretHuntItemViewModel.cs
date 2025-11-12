using Avalonia.Controls;
using System.Collections.ObjectModel;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.SecretHunt;

namespace WalletWasabi.Fluent.SecretHunt.ViewModels;

// Using multiple ViewModels in a tree is a pain in the neck in Avalonia, so we don't use it
public class SecretHuntItemViewModel
{
	public SecretHuntItemViewModel(SecretHuntEventResultModel model, int idx)
	{
		if (idx >= -1)
		{
			ExtraSecret = idx == -1;
			Description = idx == -1 ? (model.ExtraSecret ?? "") : model.Secrets[idx];
			Icon = ExtraSecret ? "double_shield_regular" : "private_key_regular";
			ToolTip = ExtraSecret ? Lang.Resources.SecretHuntExtraSecret : Lang.Resources.SecretHuntSecret;
			return;
		}

		IsEventItem = true;
		Id = model.Id;
		StartDate = model.StartDate;
		EndDate = model.EndDate;
		Description = model.Description;

		if (model.ExtraSecret is not null)
		{
			Secrets.Add(new(model, -1));
		}
		for (int secretIdx = 0, len = model.Secrets.Count; secretIdx < len; secretIdx++)
		{
			Secrets.Add(new(model, secretIdx));
		}

		UpdateStatus();
	}

	public void UpdateStatus()
	{
		if (IsEventItem)
		{
			bool extraSecret = Secrets.Count > 0 && Secrets[0].ExtraSecret;
			Icon = extraSecret ? "checkmark_circle_filled" : "book_question_mark_regular";
			ToolTip = extraSecret ? Lang.Resources.SecretHuntEventSolved : Lang.Resources.SecretHuntEventUnsolved;
		}
	}

	private static GridLength GridEmpty = new(0);
	private static GridLength GridDate = new(100);

	public GridLength GridDateLength => IsEventItem ? GridDate : GridEmpty;

	public string StartDateString => StartDate.ToUserFacingStringFixLength(false);
	public string StartDateToolTipString => StartDate.ToUserFacingString();
	public string EndDateString => EndDate.ToUserFacingStringFixLength(false);
	public string EndDateToolTipString => EndDate.ToUserFacingString();

	public bool ExtraSecret { get; set; } = false;
	public bool IsEventItem { get; set; } = false;

	public string Icon { get; set; } = "";
	public string ToolTip { get; set; } = "";

	// Event data
	public string Id { get; set; } = "";

	public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UnixEpoch;
	public DateTimeOffset EndDate { get; set; } = DateTimeOffset.UnixEpoch;
	public string Description { get; set; } = "";

	public ObservableCollection<SecretHuntItemViewModel> Secrets { get; set; } = [];
}
