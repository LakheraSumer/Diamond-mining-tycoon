using System;

namespace DiamondMine.Simulation
{
    /// <summary>
    /// Authoritative production. Visuals may read this state; they never create resources.
    /// </summary>
    public static class ProductionSimulator
    {
        const double Eps = 1e-8;
        const double MaxStep = 0.5;

        public static void Advance(GameState state, EconomyConfig config, double seconds)
        {
            if (!NumericFormat.IsFinite(seconds) || seconds <= 0d)
                return;
            if (seconds > 48d * 3600d)
                seconds = 48d * 3600d;

            double boosted = Math.Min(Math.Max(0d, state.BoostRemainingSeconds), seconds);
            if (boosted > 0d)
            {
                AdvanceUnlocked(state, config, boosted, true);
                state.BoostRemainingSeconds = Math.Max(0d, state.BoostRemainingSeconds - boosted);
            }

            double rest = seconds - boosted;
            if (rest > 0d)
                AdvanceUnlocked(state, config, rest, false);

            state.TotalPlaySeconds = NumericFormat.Add(state.TotalPlaySeconds, seconds);
        }

        public static bool TryTap(GameState state, EconomyConfig config, string mineId)
        {
            MineState mine = state.FindMine(mineId);
            MineDefinition def = config.FindMine(mineId);
            if (mine == null || def == null || !mine.Unlocked)
                return false;

            MineRates rates = MineStats.Compute(def, mine, config, state.Mastery, state.BoostRemainingSeconds > 0d);
            if (state.TotalPlaySeconds - mine.LastTapTime < rates.TapCooldown - 1e-6)
                return false;
            if (mine.OreAtFace >= rates.MineBuffer - Eps)
                return false;

            double add = Math.Min(rates.TapOre, rates.MineBuffer - mine.OreAtFace);
            mine.OreAtFace += add;
            mine.OreMined += add;
            if (mine.TapCount < int.MaxValue) mine.TapCount += 1;
            mine.LastTapTime = state.TotalPlaySeconds;
            ResolveInstants(mine, rates);
            return true;
        }

        static void AdvanceUnlocked(GameState state, EconomyConfig config, double seconds, bool boost)
        {
            for (int i = 0; i < state.Mines.Count; i++)
            {
                MineState mine = state.Mines[i];
                if (!mine.Unlocked)
                    continue;
                MineDefinition def = config.FindMine(mine.Id);
                if (def == null)
                    continue;
                MineRates rates = MineStats.Compute(def, mine, config, state.Mastery, boost);
                SyncCarts(mine, rates);
                AdvanceMine(state, mine, rates, seconds);
            }
        }

        public static void SyncCarts(MineState mine, MineRates rates)
        {
            while (mine.Carts.Count < rates.CartCount)
            {
                mine.Carts.Add(new CartState
                {
                    Phase = CartPhase.Loading,
                    Timer = rates.DepartWait,
                    Load = 0d
                });
            }
        }

        static void AdvanceMine(GameState state, MineState mine, MineRates rates, double seconds)
        {
            double left = seconds;
            int guard = 0;
            while (left > Eps && guard++ < 400000)
            {
                bool moved = ResolveInstants(mine, rates);
                double dt = NextEvent(mine, rates);
                if (dt <= Eps)
                {
                    if (moved)
                        continue;
                    dt = Math.Min(left, 0.05d);
                }
                if (dt > left)
                    dt = left;
                Integrate(state, mine, rates, dt);
                left -= dt;
            }
            ClampMine(mine, rates);
        }

        static bool ResolveInstants(MineState mine, MineRates rates)
        {
            bool changed = false;
            for (int pass = 0; pass < 12; pass++)
            {
                bool any = false;
                CartState loader = FirstLoading(mine);
                if (loader != null && mine.OreAtFace >= rates.CartCapacity - Eps)
                {
                    Depart(mine, loader, rates);
                    any = true;
                }
                else if (loader != null && mine.OreAtFace > Eps && loader.Timer <= Eps)
                {
                    Depart(mine, loader, rates);
                    any = true;
                }

                for (int i = 0; i < mine.Carts.Count; i++)
                {
                    CartState cart = mine.Carts[i];
                    if (cart.Phase == CartPhase.Outbound && cart.Timer <= Eps)
                    {
                        cart.Phase = CartPhase.Unloading;
                        cart.Timer = 0d;
                        any = true;
                    }
                    else if (cart.Phase == CartPhase.Returning && cart.Timer <= Eps)
                    {
                        cart.Phase = CartPhase.Loading;
                        cart.Timer = rates.DepartWait;
                        cart.Load = 0d;
                        any = true;
                    }
                }

                if (PullIntoProcessor(mine, rates))
                    any = true;

                if (!any)
                    return changed;
                changed = true;
            }
            return changed;
        }

        static void Depart(MineState mine, CartState cart, MineRates rates)
        {
            double load = Math.Min(rates.CartCapacity, mine.OreAtFace);
            if (load <= Eps)
                return;
            mine.OreAtFace -= load;
            cart.Load = load;
            cart.Phase = CartPhase.Outbound;
            cart.Timer = rates.TripSeconds;
            if (mine.TripsStarted < int.MaxValue) mine.TripsStarted += 1;
        }

        static bool PullIntoProcessor(MineState mine, MineRates rates)
        {
            bool moved = false;
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                CartState cart = mine.Carts[i];
                if (cart.Phase != CartPhase.Unloading || cart.Load <= Eps)
                    continue;
                double space = rates.ProcessBuffer - mine.ProcessInput;
                if (space <= Eps)
                    break;
                double move = Math.Min(cart.Load, space);
                mine.ProcessInput += move;
                cart.Load -= move;
                moved = true;
                if (cart.Load <= Eps)
                    StartReturn(cart, rates, rates.TripSeconds);
            }
            return moved;
        }

        static void StartReturn(CartState cart, MineRates rates, double timer)
        {
            cart.Load = 0d;
            cart.Phase = CartPhase.Returning;
            cart.Timer = Math.Max(0d, timer);
        }

        static double NextEvent(MineState mine, MineRates rates)
        {
            double soon = MaxStep;
            CartState loader = FirstLoading(mine);
            if (loader != null)
            {
                if (mine.OreAtFace >= rates.CartCapacity - Eps)
                    return 0d;
                bool active = mine.OreAtFace > Eps || (rates.AutoUnlocked && rates.AutoRate > 0d);
                if (active && mine.OreAtFace < rates.CartCapacity - Eps)
                {
                    soon = Math.Min(soon, Math.Max(0d, loader.Timer));
                    if (rates.AutoUnlocked && rates.AutoRate > Eps)
                        soon = Math.Min(soon, (rates.CartCapacity - mine.OreAtFace) / rates.AutoRate);
                }
            }

            double unloading = 0d;
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                CartState cart = mine.Carts[i];
                if (cart.Phase == CartPhase.Outbound || cart.Phase == CartPhase.Returning)
                    soon = Math.Min(soon, Math.Max(0d, cart.Timer));
                else if (cart.Phase == CartPhase.Unloading)
                    unloading += Math.Max(0d, cart.Load);
            }

            if (unloading > Eps)
            {
                if (rates.ProcessBuffer - mine.ProcessInput > Eps)
                    return 0d;
                if (rates.ProcessRate > Eps && mine.Rough < rates.RoughBuffer - Eps)
                    soon = Math.Min(soon, unloading / rates.ProcessRate);
            }

            return soon;
        }

        static void Integrate(GameState state, MineState mine, MineRates rates, double dt)
        {
            ApplySortAndSell(state, mine, rates, dt);
            ApplyProcess(mine, rates, dt);
            ApplyMining(mine, rates, dt);
            AdvanceTimers(mine, rates, dt);
        }

        static void ApplySortAndSell(GameState state, MineState mine, MineRates rates, double dt)
        {
            double sortPotential = Math.Min(rates.SortRate * dt, Math.Max(0d, mine.Rough));
            double sellPotential = rates.SellRate * dt;
            double room = Math.Max(0d, rates.StorageCap - mine.Storage);
            double sorted = Math.Min(sortPotential, room + sellPotential);
            double sold = Math.Min(sellPotential, Math.Max(0d, mine.Storage) + sorted);
            sorted = Math.Min(sorted, room + sold);
            sold = Math.Min(sellPotential, Math.Max(0d, mine.Storage) + sorted);
            if (sorted < 0d) sorted = 0d;
            if (sold < 0d) sold = 0d;

            mine.Rough = Math.Max(0d, mine.Rough - sorted);
            mine.Storage = Math.Max(0d, mine.Storage + sorted - sold);
            if (mine.Storage > rates.StorageCap)
                mine.Storage = rates.StorageCap;
            mine.DiamondsSold += sold;
            Wallet.Credit(state, sold * rates.CoinValue);
        }

        static void ApplyProcess(MineState mine, MineRates rates, double dt)
        {
            double unloading = 0d;
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                if (mine.Carts[i].Phase == CartPhase.Unloading)
                    unloading += Math.Max(0d, mine.Carts[i].Load);
            }

            double roomDiamonds = Math.Max(0d, rates.RoughBuffer - mine.Rough);
            double roomOre = roomDiamonds / rates.Yield;
            double available = Math.Max(0d, mine.ProcessInput) + unloading;
            double processed = Math.Min(rates.ProcessRate * dt, Math.Min(available, roomOre));
            if (!NumericFormat.IsFinite(processed) || processed < 0d)
                processed = 0d;

            double remaining = available - processed;
            if (remaining < 0d && remaining > -1e-6)
                remaining = 0d;
            double inBuffer = Math.Min(rates.ProcessBuffer, Math.Max(0d, remaining));
            double leftOnCarts = Math.Max(0d, remaining - inBuffer);
            mine.ProcessInput = inBuffer;
            mine.OreProcessed += processed;
            mine.DiamondsProduced += processed * rates.Yield;
            mine.Rough += processed * rates.Yield;

            double slack = 0d;
            if (unloading > Eps && leftOnCarts <= Eps && rates.ProcessRate > Eps)
            {
                double clearTime = unloading / rates.ProcessRate;
                if (clearTime < dt)
                    slack = dt - clearTime;
            }

            bool assigned = false;
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                CartState cart = mine.Carts[i];
                if (cart.Phase != CartPhase.Unloading)
                    continue;
                if (!assigned && leftOnCarts > Eps)
                {
                    cart.Load = leftOnCarts;
                    assigned = true;
                    leftOnCarts = 0d;
                }
                else
                {
                    double timer = Math.Max(0d, rates.TripSeconds - slack) + dt;
                    StartReturn(cart, rates, timer);
                }
            }
        }

        static void ApplyMining(MineState mine, MineRates rates, double dt)
        {
            if (!rates.AutoUnlocked || rates.AutoRate <= 0d)
                return;
            double space = rates.MineBuffer - mine.OreAtFace;
            if (space <= Eps)
                return;
            double mined = Math.Min(rates.AutoRate * dt, space);
            mine.OreAtFace += mined;
            mine.OreMined += mined;
        }

        static void AdvanceTimers(MineState mine, MineRates rates, double dt)
        {
            bool loaderActive = mine.OreAtFace > Eps || (rates.AutoUnlocked && rates.AutoRate > 0d);
            bool loaderSeen = false;
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                CartState cart = mine.Carts[i];
                if (cart.Phase == CartPhase.Outbound || cart.Phase == CartPhase.Returning)
                    cart.Timer -= dt;
                else if (!loaderSeen && cart.Phase == CartPhase.Loading)
                {
                    loaderSeen = true;
                    if (loaderActive && mine.OreAtFace < rates.CartCapacity - Eps)
                        cart.Timer -= dt;
                }
            }
        }

        static CartState FirstLoading(MineState mine)
        {
            for (int i = 0; i < mine.Carts.Count; i++)
            {
                if (mine.Carts[i].Phase == CartPhase.Loading)
                    return mine.Carts[i];
            }
            return null;
        }

        static void ClampMine(MineState mine, MineRates rates)
        {
            mine.OreAtFace = ClampBuffer(mine.OreAtFace, rates.MineBuffer);
            mine.ProcessInput = ClampBuffer(mine.ProcessInput, rates.ProcessBuffer);
            mine.Rough = ClampBuffer(mine.Rough, rates.RoughBuffer);
            mine.Storage = ClampBuffer(mine.Storage, rates.StorageCap);
        }

        static double ClampBuffer(double value, double cap)
        {
            if (!NumericFormat.IsFinite(value) || value < 0d)
                return 0d;
            if (value > cap)
                return cap;
            return value;
        }
    }
}
