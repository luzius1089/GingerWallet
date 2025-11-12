using NBitcoin;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tests.TestCommon;
using WalletWasabi.WabiSabi.Backend;
using WalletWasabi.WabiSabi.Backend.DoSPrevention;
using WalletWasabi.WabiSabi.Backend.Models;
using WalletWasabi.WabiSabi.Backend.Rounds;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Backend.PostRequests;

public class RegisterInputToBlameRoundTests
{
	[Fact]
	public async Task InputNotWhitelistedAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		using Key key = new();
		var coin = WabiSabiTestFactory.CreateCoin(key);
		var mockRpc = WabiSabiTestFactory.CreatePreconfiguredRpcClient(rnd, coin);

		var round = WabiSabiTestFactory.CreateRound(cfg);
		round.Alices.Add(WabiSabiTestFactory.CreateAlice(rnd, round));
		Round blameRound = WabiSabiTestFactory.CreateBlameRound(round, cfg);
		using Arena arena = await ArenaTestFactory.From(cfg).With(mockRpc).CreateAndStartAsync(rnd, round, blameRound);

		var req = WabiSabiTestFactory.CreateInputRegistrationRequest(rnd, round: blameRound, key, coin.Outpoint);
		var ex = await Assert.ThrowsAsync<WabiSabiProtocolException>(async () => await arena.RegisterInputAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiProtocolErrorCode.InputNotWhitelisted, ex.ErrorCode);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task InputWhitelistedAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		round.Alices.Add(alice);
		Round blameRound = WabiSabiTestFactory.CreateBlameRound(round, cfg);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round, blameRound);

		var req = WabiSabiTestFactory.CreateInputRegistrationRequest(rnd, prevout: alice.Coin.Outpoint, round: blameRound);

		var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await arena.RegisterInputAsync(req, CancellationToken.None));
		if (ex is WabiSabiProtocolException wspex)
		{
			Assert.NotEqual(WabiSabiProtocolErrorCode.InputNotWhitelisted, wspex.ErrorCode);
		}

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task InputWhitelistedButBannedAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);

		using Key key = new();
		var alice = WabiSabiTestFactory.CreateAlice(rnd, key, round);
		var bannedCoin = alice.Coin.Outpoint;

		round.Alices.Add(alice);
		Round blameRound = WabiSabiTestFactory.CreateBlameRound(round, cfg);
		var mockRpc = WabiSabiTestFactory.CreatePreconfiguredRpcClient(rnd, alice.Coin);

		Prison prison = WabiSabiTestFactory.CreatePrison();
		using Arena arena = await ArenaTestFactory.From(cfg, mockRpc, prison).CreateAndStartAsync(rnd, round, blameRound);

		prison.FailedToConfirm(bannedCoin, alice.Coin.Amount, round.Id);

		var req = WabiSabiTestFactory.CreateInputRegistrationRequest(rnd, key: key, round: blameRound, prevout: bannedCoin);
		var ex = await Assert.ThrowsAsync<WabiSabiProtocolException>(async () => await arena.RegisterInputAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiProtocolErrorCode.InputBanned, ex.ErrorCode);

		await arena.StopAsync(CancellationToken.None);
	}
}
