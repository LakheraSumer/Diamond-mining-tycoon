using System;
using DiamondMine.Simulation;

namespace DiamondMine.Runtime
{
    public sealed partial class GameSession
    {
        public string ActionMessage { get; private set; }

        public void Note(string message)
        {
            ActionMessage = message ?? "";
        }

        public bool TryChooseCharacter(string id)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TrySelectCharacter(clone, id, out message);
            });
        }

        public bool TryDig(int plotId, string itemId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryDig(clone, plotId, itemId, out message);
            });
        }

        public bool TryPocket()
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryStoreHands(clone, out message);
            });
        }

        public bool TryStartBatch(string machineId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryLoadMachine(clone, machineId, out message);
            });
        }

        public bool TryRefine(string oreId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryLoadRefine(clone, oreId, out message);
            });
        }

        public bool TryCollectMachine(string machineId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryCollectMachine(clone, machineId, out message);
            });
        }

        public bool TryMakeJewel(string recipeId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryStartCraft(clone, recipeId, out message);
            });
        }

        public bool TrySellGoods(string itemId, int count)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TrySell(clone, itemId, count, out message);
            });
        }

        public bool TryPurchasePlot(int plotId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryBuyPlot(clone, plotId, out message);
            });
        }

        public bool TryHireCrew()
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryHire(clone, out message);
            });
        }

        public bool TryPostCrew(int plotId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryAssignWorker(clone, plotId, out message);
            });
        }

        public bool TryClaimYardTask(string taskId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryClaimTask(clone, taskId, out message);
            });
        }

        public bool TryFulfillOrder()
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryDeliverOrder(clone, out message);
            });
        }

        public bool TrySweepStone()
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TrySweepStone(clone, out message);
            });
        }

        public bool TryOpenRegion()
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryDiscoverGate(clone, out message);
            });
        }

        public bool TryVisitBunker(int plotId)
        {
            return Play(delegate (GameState clone, out string message)
            {
                return AdventureRules.TryVisit(clone, plotId, out message);
            });
        }

        public void DismissMigrationNote()
        {
            if (!CanPlay() || State.Adventure == null || string.IsNullOrEmpty(State.Adventure.MigrationNote))
                return;
            Commit(delegate (GameState clone) { clone.Adventure.MigrationNote = ""; });
        }

        public void MoveCrew(float dx, float dz)
        {
            if (!CanPlay() || State.Adventure == null)
                return;
            YardLayout.Move(State.Adventure, dx, dz);
        }

        public void GoHome()
        {
            if (!CanPlay() || State.Adventure == null)
                return;
            YardLayout.ReturnHome(State.Adventure);
            ActionMessage = "Back at the factory door.";
            dirtySave = true;
        }

        public void MarkYardDirty()
        {
            dirtySave = true;
        }

        bool Play(YardAction action)
        {
            if (!CanPlay())
                return false;
            string message = "";
            bool accepted = false;
            GameState trial = State.Clone();
            accepted = action(trial, out message);
            ActionMessage = message;
            if (!accepted)
                return false;
            bool saved = Commit(delegate (GameState clone)
            {
                string ignored;
                action(clone, out ignored);
            });
            if (saved && State.Settings != null)
                PlatformInfo.Pulse(State.Settings.Haptics);
            return saved;
        }

        delegate bool YardAction(GameState state, out string message);
    }
}
