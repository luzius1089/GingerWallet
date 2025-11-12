using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Fluent.Models.Wallets;
using WalletWasabi.Fluent.Navigation.ViewModels;
using ReactiveUI;
using System.Reactive.Linq;

namespace WalletWasabi.Fluent.SecretHunt.ViewModels;

[NavigationMetaData(
	IconName = "nav_wallet_24_regular",
	Order = 0,
	Category = SearchCategory.Wallet,
	NavBarPosition = NavBarPosition.None,
	NavigationTarget = NavigationTarget.DialogScreen,
	Searchable = false,
	IsLocalized = true)]
public partial class SecretHuntViewModel : RoutableViewModel
{
	private readonly CompositeDisposable _disposables = new();

	private readonly WalletModel _wallet;

	[AutoNotify] private bool _enableSecretHunt;

	public SecretHuntViewModel(WalletModel wallet)
	{
		SetupCancel(enableCancel: false, enableCancelOnEscape: true, enableCancelOnPressed: true);
		NextCommand = CancelCommand;

		_wallet = wallet;
		_enableSecretHunt = _wallet.Wallet.KeyManager.EnableSecretHunt;

		this.WhenAnyValue(x => x.EnableSecretHunt)
			.Skip(1)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Subscribe(x =>
			{
				_wallet.Settings.EnableSecretHunt = x;
				_wallet.Settings.Save();
				UpdateTree();
			});

		TreeDataSource = [];
		UpdateTree();
	}

	public void SetValues()
	{
		_wallet.Wallet.KeyManager.EnableSecretHunt = _enableSecretHunt;
	}

	public void UpdateTree()
	{
		if (!_enableSecretHunt)
		{
			TreeDataSource.Clear();
			return;
		}

		var modelList = _wallet.GetSecretHuntResults();
		for (int idx = 0, len = modelList.Count; idx < len; idx++)
		{
			var model = modelList[idx];
			SecretHuntItemViewModel? eventItem = null;
			if (TreeDataSource.Count <= idx || TreeDataSource[idx].Id != model.Id)
			{
				eventItem = new(model, -2);
				TreeDataSource.Insert(idx, eventItem);
				continue;
			}
			eventItem = TreeDataSource[idx];
			int secretOfs = 0;
			if (model.ExtraSecret is not null)
			{
				if (eventItem.Secrets.Count == 0 || !eventItem.Secrets[0].ExtraSecret)
				{
					eventItem.Secrets.Insert(0, new(model, -1));
				}
				secretOfs = 1;
			}
			for (int secretIdx = 0, secretLen = model.Secrets.Count; secretIdx < secretLen; secretIdx++)
			{
				if (eventItem.Secrets.Count <= secretIdx + secretOfs || eventItem.Secrets[secretIdx + secretOfs].Description != model.Secrets[secretIdx])
				{
					eventItem.Secrets.Insert(secretIdx + secretOfs, new(model, secretIdx));
				}
			}
			eventItem.UpdateStatus();
		}
	}

	public ObservableCollection<SecretHuntItemViewModel> TreeDataSource { get; }

	public void Dispose()
	{
		_disposables.Dispose();
	}
}
