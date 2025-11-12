using NBitcoin;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tests.TestCommon;
using WalletWasabi.WabiSabi.Models;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Models;

/// <summary>
/// Tests for <see cref="InputsRemovalRequest"/> class.
/// </summary>
public class InputsRemovalRequestTestsTests
{
	[Fact]
	public void EqualityTest()
	{
		var rnd = TestRandom.Get();
		uint256 roundId = BitcoinFactory.CreateUint256(rnd);
		Guid guid = Guid.NewGuid();

		// Request #1.
		InputsRemovalRequest request1 = new(RoundId: roundId, AliceId: guid);

		// Request #2.
		InputsRemovalRequest request2 = new(RoundId: roundId, AliceId: guid);

		Assert.Equal(request1, request2);

		// Request #3.
		InputsRemovalRequest request3 = new(RoundId: BitcoinFactory.CreateUint256(rnd), AliceId: guid);

		Assert.NotEqual(request1, request3);
	}
}
