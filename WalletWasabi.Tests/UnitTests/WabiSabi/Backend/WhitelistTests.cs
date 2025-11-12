using System.Linq;
using System.Threading.Tasks;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tests.TestCommon;
using WalletWasabi.WabiSabi.Backend.Banning;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Backend;

public class WhitelistTests
{
	[Fact]
	public async Task WhitelistChangeTrafficAsync()
	{
		var rnd = TestRandom.Get();
		var cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		cfg.ReleaseFromWhitelistAfter = TimeSpan.FromSeconds(1);

		Whitelist whitelist = new(Enumerable.Empty<Innocent>(), string.Empty, cfg);
		var currentChangeId = whitelist.ChangeId;

		var outpoint = BitcoinFactory.CreateOutPoint(rnd);
		whitelist.Add(outpoint);
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);
		currentChangeId = whitelist.ChangeId;

		var outpoint2 = BitcoinFactory.CreateOutPoint(rnd);
		whitelist.Add(outpoint2);
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);
		currentChangeId = whitelist.ChangeId;

		Assert.True(whitelist.TryRelease(outpoint));
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);

		var outpoint3 = BitcoinFactory.CreateOutPoint(rnd);
		whitelist.Add(outpoint3);
		Assert.NotEqual(currentChangeId, whitelist.ChangeId);

		await Task.Delay(1000);
		whitelist.RemoveAllExpired();

		Assert.Equal(0, whitelist.CountInnocents());
	}
}
