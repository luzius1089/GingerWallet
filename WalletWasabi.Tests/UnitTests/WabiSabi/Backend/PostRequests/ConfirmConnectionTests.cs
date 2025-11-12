using NBitcoin;
using System.Threading;
using System.Threading.Tasks;
using WabiSabi.CredentialRequesting;
using WabiSabi.Crypto;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tests.TestCommon;
using WalletWasabi.WabiSabi.Backend;
using WalletWasabi.WabiSabi.Backend.Models;
using WalletWasabi.WabiSabi.Backend.Rounds;
using WalletWasabi.WabiSabi.Models;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Backend.PostRequests;

public class ConfirmConnectionTests
{
	[Fact]
	public async Task SuccessInInputRegistrationPhaseAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		var preDeadline = alice.Deadline;
		round.Alices.Add(alice);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);

		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round);
		var minAliceDeadline = DateTimeOffset.UtcNow + (cfg.ConnectionConfirmationTimeout * 0.9);

		var resp = await arena.ConfirmConnectionAsync(req, CancellationToken.None);
		Assert.NotNull(resp);
		Assert.NotNull(resp.ZeroAmountCredentials);
		Assert.NotNull(resp.ZeroVsizeCredentials);
		Assert.Null(resp.RealAmountCredentials);
		Assert.Null(resp.RealVsizeCredentials);
		Assert.NotEqual(preDeadline, alice.Deadline);
		Assert.True(minAliceDeadline <= alice.Deadline);
		Assert.False(alice.ConfirmedConnection);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task SuccessInConnectionConfirmationPhaseAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);

		round.SetPhase(Phase.ConnectionConfirmation);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		var preDeadline = alice.Deadline;
		round.Alices.Add(alice);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);

		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round);

		var resp = await arena.ConfirmConnectionAsync(req, CancellationToken.None);
		Assert.NotNull(resp);
		Assert.NotNull(resp.ZeroAmountCredentials);
		Assert.NotNull(resp.ZeroVsizeCredentials);
		Assert.NotNull(resp.RealAmountCredentials);
		Assert.NotNull(resp.RealVsizeCredentials);
		Assert.Equal(preDeadline, alice.Deadline);
		Assert.True(alice.ConfirmedConnection);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task RoundNotFoundAsync()
	{
		var rnd = TestRandom.Get();
		var cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var nonExistingRound = WabiSabiTestFactory.CreateRound(cfg);
		using Arena arena = await ArenaTestFactory.Default.CreateAndStartAsync(rnd);
		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, nonExistingRound);

		var ex = await Assert.ThrowsAsync<WabiSabiProtocolException>(
			async () => await arena.ConfirmConnectionAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiProtocolErrorCode.RoundNotFound, ex.ErrorCode);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task WrongPhaseAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		Round round = WabiSabiTestFactory.CreateRound(cfg);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		var preDeadline = alice.Deadline;
		round.Alices.Add(alice);
		await arena.TriggerAndWaitRoundAsync(TimeSpan.FromSeconds(21));

		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round);
		foreach (Phase phase in Enum.GetValues(typeof(Phase)))
		{
			if (phase != Phase.InputRegistration && phase != Phase.ConnectionConfirmation)
			{
				round.SetPhase(phase);
				var ex = await Assert.ThrowsAsync<WrongPhaseException>(
					async () => await arena.ConfirmConnectionAsync(req, CancellationToken.None));

				Assert.Equal(WabiSabiProtocolErrorCode.WrongPhase, ex.ErrorCode);
			}
		}
		Assert.Equal(preDeadline, alice.Deadline);
		Assert.False(alice.ConfirmedConnection);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task AliceNotFoundAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);

		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round);
		var ex = await Assert.ThrowsAsync<WabiSabiProtocolException>(async () => await arena.ConfirmConnectionAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiProtocolErrorCode.AliceNotFound, ex.ErrorCode);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task IncorrectRequestedVsizeCredentialsAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);
		round.SetPhase(Phase.ConnectionConfirmation);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		round.Alices.Add(alice);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);
		Assert.Contains(alice, round.Alices);

		var incorrectVsizeCredentials = WabiSabiTestFactory.CreateRealCredentialRequests(rnd, round, null, 234).vsizeRequest;
		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round) with { RealVsizeCredentialRequests = incorrectVsizeCredentials };

		var ex = await Assert.ThrowsAsync<WabiSabiProtocolException>(async () => await arena.ConfirmConnectionAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiProtocolErrorCode.IncorrectRequestedVsizeCredentials, ex.ErrorCode);
		Assert.False(alice.ConfirmedConnection);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task IncorrectRequestedAmountCredentialsAsync()
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);
		round.SetPhase(Phase.ConnectionConfirmation);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		round.Alices.Add(alice);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);

		var incorrectAmountCredentials = WabiSabiTestFactory.CreateRealCredentialRequests(rnd, round, Money.Coins(3), null).amountRequest;
		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round) with { RealAmountCredentialRequests = incorrectAmountCredentials };

		var ex = await Assert.ThrowsAsync<WabiSabiProtocolException>(async () => await arena.ConfirmConnectionAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiProtocolErrorCode.IncorrectRequestedAmountCredentials, ex.ErrorCode);
		Assert.False(alice.ConfirmedConnection);

		await arena.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task InvalidRequestedAmountCredentialsAsync()
	{
		await InvalidRequestedCredentialsAsync(
			(round) => (round.AmountCredentialIssuer, round.VsizeCredentialIssuer),
			(request) => request.RealAmountCredentialRequests);
	}

	[Fact]
	public async Task InvalidRequestedVsizeCredentialsAsync()
	{
		await InvalidRequestedCredentialsAsync(
			(round) => (round.VsizeCredentialIssuer, round.AmountCredentialIssuer),
			(request) => request.RealVsizeCredentialRequests);
	}

	private async Task InvalidRequestedCredentialsAsync(
		Func<Round, (CredentialIssuer, CredentialIssuer)> credentialIssuerSelector,
		Func<ConnectionConfirmationRequest, RealCredentialsRequest> credentialsRequestSelector)
	{
		var rnd = TestRandom.Get();
		WabiSabiConfig cfg = WabiSabiTestFactory.CreateDefaultWabiSabiConfig();
		var round = WabiSabiTestFactory.CreateRound(cfg);
		round.SetPhase(Phase.ConnectionConfirmation);
		var alice = WabiSabiTestFactory.CreateAlice(rnd, round);
		round.Alices.Add(alice);
		using Arena arena = await ArenaTestFactory.From(cfg).CreateAndStartAsync(rnd, round);

		var req = WabiSabiTestFactory.CreateConnectionConfirmationRequest(rnd, round);
		var (issuer, issuer2) = credentialIssuerSelector(round);
		var credentialsRequest = credentialsRequestSelector(req);

		// invalidate serial numbers
		issuer.HandleRequest(credentialsRequest);

		var ex = await Assert.ThrowsAsync<WabiSabiCryptoException>(async () => await arena.ConfirmConnectionAsync(req, CancellationToken.None));
		Assert.Equal(WabiSabiCryptoErrorCode.SerialNumberAlreadyUsed, ex.ErrorCode);
		Assert.False(alice.ConfirmedConnection);
		await arena.StopAsync(CancellationToken.None);
	}
}
