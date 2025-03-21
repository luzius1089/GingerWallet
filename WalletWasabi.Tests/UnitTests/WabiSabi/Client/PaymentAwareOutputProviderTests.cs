using NBitcoin;
using System.Collections.Generic;
using System.Linq;
using WalletWasabi.Extensions;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.WabiSabi.Backend;
using WalletWasabi.WabiSabi.Client;
using WalletWasabi.WabiSabi.Client.Batching;
using WalletWasabi.WabiSabi.Recommendation;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Client;

public class PaymentAwareOutputProviderTests
{
	[Fact]
	public void CreateOutputsForPaymentsTest()
	{
		var rpc = new MockRpcClient();
		var wallet = new TestWallet("random-wallet", rpc);
		var paymentBatch = new PaymentBatch();
		var outputProvider = new PaymentAwareOutputProvider(wallet, paymentBatch);

		var roundParameters = WabiSabiTestFactory.CreateRoundParameters(WabiSabiTestFactory.CreateDefaultWabiSabiConfig());
		using Key key = new();
		paymentBatch.AddPayment(
			key.PubKey.GetAddress(ScriptPubKeyType.Segwit, rpc.Network),
			Money.Coins(0.00005432m));

		DenominationFactory denomFactory = new(5000L, 100_0000_0000);
		List<Money> myInputs = [Money.Coins(0.00484323m), Money.Coins(0.003m), Money.Coins(0.00004323m)];
		List<Money> allInputs = [Money.Coins(0.00484323m), Money.Coins(0.003m), Money.Coins(0.00004323m), Money.Coins(0.2m), Money.Coins(0.1m), Money.Coins(0.05m), Money.Coins(0.0025m), Money.Coins(0.0001m)];
		var denoms = denomFactory.CreateDefaultDenominations(allInputs, FeeRate.Zero);
		var outputs = outputProvider.GetOutputs(roundId: uint256.Zero, roundParameters, myInputs, denoms, [], int.MaxValue).ToArray();

		Assert.Equal(outputs[0].ScriptPubKey, key.PubKey.GetScriptPubKey(ScriptPubKeyType.Segwit));
		Assert.Equal(outputs[0].Value, Money.Coins(0.00005432m));

		var miningLoss = outputs.Length * (wallet.SupportedScriptTypes.Max(x => roundParameters.MiningFeeRate.GetFee(x.EstimateOutputVsize())) ?? Money.Zero);
		var changelessLoss = roundParameters.CalculateMinReasonableOutputAmount(wallet.SupportedScriptTypes);
		var rangeMin = (myInputs.Sum() - miningLoss - changelessLoss).ToDecimal(MoneyUnit.BTC);

		Assert.True(outputs.Length > 2, $"There were {outputs.Length} outputs."); // The rest was decomposed
		Assert.InRange(outputs.Sum(x => x.Value.ToDecimal(MoneyUnit.BTC)), rangeMin, 0.007800m); // no money was lost
	}

	[Theory]
	[InlineData(new[] { "0.2", "0.30" }, "0.176", 1_000, 0)] // Not enough money to make any of the payments.
	[InlineData(new[] { "0.1", "0.30" }, "0.176", 1_000, 1)] // It is only possible to make one payment.
	[InlineData(new[] { "0.1", "0.05" }, "0.176", 1_000, 2)] // It is possible to make the two payments.
	[InlineData(new[] { "0.1", "0.05" }, "0.150", 1_000, 1)] // It is only possible to make one payment. Not enough for fees.
	[InlineData(new[] { "0.1", "0.05", "0.025", "0.001", "0.001" }, "0.176", 1_000, 4)] // Four is the maximum number of payments.
	[InlineData(new[] { "0.1", "0.30" }, "0.176", 43 + 31, 1)] // It is only possible to make one payment.
	[InlineData(new[] { "0.1", "0.05" }, "0.176", 43 + 31, 1)] // It is possible to make the two payments.
	[InlineData(new[] { "0.1", "0.05" }, "0.176", 20, 0)] // It is possible to make the two payments.
	[InlineData(new[] { "0.1", "0.05" }, "0.14", 40 + 31, 0)] // Not enough vsize to register the payment and the change.
	public void BestPaymentSetTest(string[] amountsToPay, string availableAmountStr, int availableVsize, int expectedOutputs)
	{
		var roundParameters = WabiSabiTestFactory.CreateRoundParameters(WabiSabiTestFactory.CreateDefaultWabiSabiConfig());
		var paymentBatch = new PaymentBatch();

		var payments = amountsToPay.Select(a => (Destination: GetNewSegwitAddress(), Amount: Money.Coins(decimal.Parse(a))));
		payments.ToList().ForEach(p => paymentBatch.AddPayment(p.Destination, p.Amount));

		var availableMoney = Money.Coins(decimal.Parse(availableAmountStr));
		var paymentSet = paymentBatch.GetBestPaymentSet(availableMoney, availableVsize, roundParameters);

		Assert.True(paymentSet.TotalAmount < availableMoney);
		Assert.True(paymentSet.TotalVSize < availableVsize);
		Assert.Equal(expectedOutputs, paymentSet.Payments.Count());
	}

	private static BitcoinAddress GetNewSegwitAddress()
	{
		using Key key = new();
		return key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
	}
}
