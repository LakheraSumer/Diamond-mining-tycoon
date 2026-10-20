namespace DiamondMine.Simulation
{
    // Fixed delivery contracts count diamonds dispatched by the authoritative sale
    // stage. Claiming pays a one-time bonus; it never creates or resells diamonds.
    public static class OrderService
    {
        public static bool CanClaim(GameState state, OrderDefinition order)
        {
            if (order == null || state.ClaimedOrders.Contains(order.Id)) return false;
            MineState mine = state.FindMine(order.MineId);
            return mine != null && mine.Unlocked && mine.DiamondsSold + 1e-6 >= order.Diamonds;
        }

        public static bool TryClaim(GameState state, EconomyConfig config, string orderId)
        {
            foreach (OrderDefinition order in config.Orders)
            {
                if (order.Id != orderId || !CanClaim(state, order)) continue;
                state.ClaimedOrders.Add(order.Id);
                Wallet.Credit(state, order.Bonus);
                return true;
            }
            return false;
        }
    }
}
