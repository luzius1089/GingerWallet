using WalletWasabi.Blockchain.Analysis;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tests.TestCommon;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.BlockchainAnalysis;

/// <summary>
/// In these tests no inputs, nor outputs in a transaction are controlled by the user.
/// </summary>
public class WalletIrrelevantAnonScoreTests
{
	[Fact]
	public void OneInOneOut()
	{
		var analyser = new BlockchainAnalyzer();
		var tx = BitcoinFactory.CreateSmartTransaction(TestRandom.Get(), 1, 1, 0, 0);

		analyser.Analyze(tx);

		Assert.Empty(tx.WalletInputs);
		Assert.Empty(tx.WalletOutputs);
	}

	[Fact]
	public void ManyInManyOut()
	{
		var analyser = new BlockchainAnalyzer();
		var tx = BitcoinFactory.CreateSmartTransaction(TestRandom.Get(), 3, 3, 0, 0);

		analyser.Analyze(tx);

		Assert.Empty(tx.WalletInputs);
		Assert.Empty(tx.WalletOutputs);
	}
}
