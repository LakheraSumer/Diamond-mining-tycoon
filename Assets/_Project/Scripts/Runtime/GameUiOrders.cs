using System.Collections.Generic;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    public sealed partial class GameUi
    {
        sealed class OrderRow
        {
            public OrderDefinition Definition;
            public Text Progress;
            public Button Claim;
        }

        readonly List<OrderRow> orderRows = new List<OrderRow>();

        void BuildOrders(RectTransform card)
        {
            Header(card, "Orders");
            RectTransform content;
            RectTransform scroll = UiFactory.Scroll(card, out content);
            Place(scroll, 0.02f, 0.02f, 0.98f, 0.86f);
            Text note = UiFactory.Label(content,
                "Your diamonds fill these gem boxes as they sell. Collect a bonus when a box is ready. Take as long as you like!",
                28, UiTheme.Paper, TextAnchor.MiddleLeft);
            Preferred(note, 150f);
            foreach (OrderDefinition order in session.Config.Orders)
            {
                Text title = UiFactory.Label(content, order.Title, 32, UiTheme.Diamond, TextAnchor.MiddleLeft);
                Preferred(title, 55f);
                var row = new OrderRow { Definition = order };
                row.Progress = UiFactory.Label(content, "", 28, UiTheme.Paper, TextAnchor.MiddleLeft);
                Preferred(row.Progress, 100f);
                string id = order.Id;
                row.Claim = UiFactory.Press(content, "Collect", delegate
                {
                    ShowToast(session.TryClaimOrder(id) ? "Gem box bonus collected!" : "This bonus is not ready yet.");
                    RefreshOrders();
                }, UiTheme.ButtonOn, 100f);
                orderRows.Add(row);
            }
            RefreshOrders();
        }

        void RefreshOrders()
        {
            foreach (OrderRow row in orderRows)
            {
                OrderDefinition order = row.Definition;
                MineState mine = session.State.FindMine(order.MineId);
                MineDefinition def = session.Config.FindMine(order.MineId);
                bool claimed = session.State.ClaimedOrders.Contains(order.Id);
                double progress = mine == null ? 0 : System.Math.Min(order.Diamonds, mine.DiamondsSold);
                row.Progress.text = def.Name + " · " + NumericFormat.Format(progress) + " / " + NumericFormat.Format(order.Diamonds) +
                    " diamonds\nBonus: " + NumericFormat.Format(order.Bonus) + " coins";
                row.Claim.interactable = OrderService.CanClaim(session.State, order);
                UiFactory.Caption(row.Claim).text = claimed ? "Collected" : mine == null || !mine.Unlocked ? "Open this mine first" : row.Claim.interactable ? "Collect bonus" : "Keep mining";
            }
        }
    }
}
