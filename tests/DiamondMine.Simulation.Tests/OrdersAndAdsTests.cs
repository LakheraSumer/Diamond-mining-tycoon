using DiamondMine.Runtime;
using DiamondMine.Simulation;
using NUnit.Framework;

namespace DiamondMine.Simulation.Tests
{
    public class OrdersAndAdsTests
    {
        EconomyConfig Config() => EconomyLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "economy.json")));

        [Test]
        public void Delivery_RequiresSoldDiamondsAndPaysOnceAcrossSaveAndPrestige()
        {
            var config = Config();
            var state = GameFactory.NewGame(config);
            var order = config.Orders[0];
            Assert.That(OrderService.TryClaim(state, config, order.Id), Is.False);
            state.Mines[0].DiamondsSold = order.Diamonds;
            Assert.That(OrderService.TryClaim(state, config, order.Id), Is.True);
            Assert.That(state.Coins, Is.EqualTo(order.Bonus));
            Assert.That(state.Mines[0].DiamondsSold, Is.EqualTo(order.Diamonds));
            state = SaveCodec.Read(SaveEnvelope.Wrap(SaveCodec.Write(state)), config);
            Assert.That(OrderService.TryClaim(state, config, order.Id), Is.False);
            Wallet.Credit(state, config.PrestigeMinimumCoins * 2);
            UpgradeService.TryBuy(state, config, "warmstone", "hire", 1);
            Assert.That(PrestigeService.TryPrestige(state, config), Is.True);
            state.Mines[0].DiamondsSold = order.Diamonds;
            Assert.That(OrderService.TryClaim(state, config, order.Id), Is.False);
        }

        [TestCase(TestAdOutcome.Cancelled)]
        [TestCase(TestAdOutcome.Failed)]
        [TestCase(TestAdOutcome.NoFill)]
        [TestCase(TestAdOutcome.Timeout)]
        public void UnfinishedAd_NeverInvokesGrant(TestAdOutcome outcome)
        {
            var provider = new LocalTestRewardedAds(true);
            Assert.That(provider.TryBegin(new PendingReward { Kind = RewardKind.ProductionBoost }), Is.True);
            Assert.That(provider.Resolve(outcome, _ => throw new AssertionException("Must not grant")), Is.False);
            Assert.That(provider.Pending, Is.Null);
        }

        [Test]
        public void DuplicateCompletion_OnlyInvokesDurableGrantOnce()
        {
            var provider = new LocalTestRewardedAds(true);
            int grants = 0;
            provider.TryBegin(new PendingReward { Kind = RewardKind.ProductionBoost });
            Assert.That(provider.TryBegin(new PendingReward { Kind = RewardKind.OfflineBonus }), Is.False);
            Assert.That(provider.Resolve(TestAdOutcome.Completed, _ => { grants++; return true; }), Is.True);
            Assert.That(provider.Resolve(TestAdOutcome.Completed, _ => { grants++; return true; }), Is.False);
            Assert.That(grants, Is.EqualTo(1));
        }

        [Test]
        public void SaveFailure_RetainsOfferForRetry_AndDisabledProviderCannotBegin()
        {
            var provider = new LocalTestRewardedAds(true);
            provider.TryBegin(new PendingReward { Kind = RewardKind.OfflineBonus });
            Assert.That(provider.Resolve(TestAdOutcome.Completed, _ => false), Is.False);
            Assert.That(provider.Pending, Is.Not.Null);
            Assert.That(provider.Resolve(TestAdOutcome.Completed, _ => true), Is.True);
            Assert.That(provider.Pending, Is.Null);
            var release = new LocalTestRewardedAds(false);
            Assert.That(release.TryBegin(new PendingReward { Kind = RewardKind.OfflineBonus }), Is.False);
        }
    }
}
