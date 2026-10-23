using DiamondMine.Simulation;
using NUnit.Framework;

namespace DiamondMine.Simulation.Tests
{
    public class CartRegressionTests
    {
        EconomyConfig Config() => EconomyLoader.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"economy.json")));

        [TestCase(0d)]
        [TestCase(23d)]
        public void SaturatedMultipleCarts_KeepIndividualCapacityAndReload(double mastery)
        {
            EconomyConfig config=Config();
            GameState state=GameFactory.NewGame(config);
            state.Mastery=mastery;
            Wallet.Credit(state,10000000);
            foreach(MineDefinition def in config.Mines)
            {
                MineUnlocks.TryUnlock(state,config,def.Id);
                Assert.That(UpgradeService.TryBuy(state,config,def.Id,"hire",1),Is.True);
            }
            ProductionSimulator.Advance(state,config,600);
            foreach(MineState mine in state.Mines)
            {
                MineRates rates=MineStats.Compute(config.FindMine(mine.Id),mine,config,state.Mastery,false);
                foreach(CartState cart in mine.Carts)
                {
                    Assert.That(cart.Load,Is.InRange(0,rates.CartCapacity+1e-6),mine.Id);
                    Assert.That(cart.Timer,Is.GreaterThanOrEqualTo(0),mine.Id);
                }
                Assert.That(mine.OreAtFace+mine.ProcessInput+mine.Carts.Sum(c=>c.Load)+mine.OreProcessed,Is.EqualTo(mine.OreMined).Within(1e-6),mine.Id);
            }
            Assert.DoesNotThrow(()=>SaveCodec.Read(SaveCodec.Write(state),config));
        }

        [Test]
        public void EmptyLoadingCartAtBoundary_DoesNotPersistNegativeTimer()
        {
            EconomyConfig config=Config();
            GameState state=GameFactory.NewGame(config);
            Wallet.Credit(state,1000);
            Assert.That(UpgradeService.TryBuy(state,config,"warmstone","hire",1),Is.True);
            state.FindMine("warmstone").Carts[0].Timer=0;
            ProductionSimulator.Advance(state,config,0.01);
            Assert.That(state.FindMine("warmstone").Carts[0].Timer,Is.GreaterThanOrEqualTo(0));
            Assert.DoesNotThrow(()=>SaveCodec.Read(SaveCodec.Write(state),config));
        }
    }
}
