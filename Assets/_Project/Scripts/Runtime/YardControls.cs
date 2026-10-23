using System.Collections.Generic;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    public sealed class YardStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value;
        Vector2 origin;

        public void OnPointerDown(PointerEventData eventData)
        {
            origin = eventData.position;
            Value = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - origin;
            Value = Vector2.ClampMagnitude(delta / 90f, 1f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
        }
    }

    public sealed class YardControls : MonoBehaviour
    {
        GameSession session;
        YardWorld world;
        GameUi ui;
        GameAudio cues;
        Canvas canvas;
        RectTransform root;
        RectTransform panel;
        RectTransform panelContent;
        Text interactLabel;
        Text statusText;
        YardStick stick;
        string mode = "";
        int pendingPlot = -1;
        bool shown;

        public bool IsOpen { get { return shown; } }

        public void Bind(GameSession gameSession, YardWorld yardWorld, GameUi gameUi, GameAudio gameAudio)
        {
            session = gameSession;
            world = yardWorld;
            ui = gameUi;
            cues = gameAudio;
            Build();
            Show();
        }

        public void Show()
        {
            shown = true;
            if (root != null)
                root.gameObject.SetActive(true);
            if (world != null)
                world.SetShown(true);
            if (ui != null)
                ui.SetYardOpen(true);
        }

        public void ShowWorks()
        {
            shown = false;
            ClosePanel();
            if (root != null)
                root.gameObject.SetActive(false);
            if (world != null)
                world.SetShown(false);
            if (ui != null)
                ui.SetYardOpen(false);
        }

        public bool TryConsumeBack()
        {
            if (!shown)
                return false;
            if (NeedsCharacter())
                return true;
            if (mode.Length > 0)
            {
                ClosePanel();
                return true;
            }
            return false;
        }

        void Update()
        {
            if (!shown || session == null || !session.Ready || session.State == null || session.State.Adventure == null)
                return;
            if (canvas != null)
                canvas.sortingOrder = NeedsCharacter() ? 40 : 8;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (stick != null && stick.Value.sqrMagnitude > 0.04f && mode != "characters")
            {
                float speed = stick.Value.magnitude > 0.82f ? 7f : 4.2f;
                session.MoveCrew(stick.Value.x * speed * dt, stick.Value.y * speed * dt);
                session.State.Adventure.PoseHint = stick.Value.magnitude > 0.82f ? "run" : "move";
            }
            InteractSpot spot = YardLayout.Nearest(session.State.Adventure);
            if (interactLabel != null)
                interactLabel.text = spot.Kind == "none" ? "Look" : spot.Label;
            if (statusText != null)
                statusText.text = RankRules.ProductionEnabled ? "" : "Ranking: practice only";
            if (world != null)
                world.Present(session);
            if (NeedsCharacter() && mode != "characters")
                Open("characters");
        }

        bool NeedsCharacter()
        {
            return session != null && session.Ready && session.State != null && string.IsNullOrEmpty(session.State.Adventure.SelectedCharacter);
        }

        void Build()
        {
            var canvasObject = new GameObject("YardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0f;
            root = canvasObject.GetComponent<RectTransform>();

            statusText = UiFactory.Label(root, "", 24, UiTheme.Paper, TextAnchor.MiddleLeft);
            statusText.rectTransform.anchorMin = new Vector2(0.04f, 0.9f);
            statusText.rectTransform.anchorMax = new Vector2(0.7f, 0.95f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            RectTransform stickBase = UiFactory.Panel("Stick", root, new Color(0.1f, 0.12f, 0.1f, 0.45f));
            stickBase.anchorMin = new Vector2(0.04f, 0.16f);
            stickBase.anchorMax = new Vector2(0.34f, 0.34f);
            stickBase.offsetMin = Vector2.zero;
            stickBase.offsetMax = Vector2.zero;
            stick = stickBase.gameObject.AddComponent<YardStick>();
            Text stickText = UiFactory.Label(stickBase, "Move", 28, UiTheme.Paper, TextAnchor.MiddleCenter);
            UiFactory.Stretch(stickText.rectTransform);

            Button interact = MakeButton(root, "Look", OnInteract, new Vector2(0.58f, 0.16f), new Vector2(0.96f, 0.3f));
            interactLabel = UiFactory.Caption(interact);
            MakeButton(root, "Home", delegate { session.GoHome(); Ping(); }, new Vector2(0.36f, 0.16f), new Vector2(0.56f, 0.24f));
            MakeButton(root, "Works", ShowWorks, new Vector2(0.36f, 0.25f), new Vector2(0.56f, 0.33f));
            MakeButton(root, "Bag", delegate { Open("bag"); }, new Vector2(0.04f, 0.35f), new Vector2(0.27f, 0.43f));
            MakeButton(root, "Mill", delegate { Open("factory"); }, new Vector2(0.29f, 0.35f), new Vector2(0.52f, 0.43f));
            MakeButton(root, "Shop", delegate { Open("shop"); }, new Vector2(0.54f, 0.35f), new Vector2(0.77f, 0.43f));
            MakeButton(root, "Tasks", delegate { Open("tasks"); }, new Vector2(0.79f, 0.35f), new Vector2(0.96f, 0.43f));

            panel = UiFactory.Panel("YardPanel", root, new Color(0.08f, 0.07f, 0.06f, 0.94f));
            panel.anchorMin = new Vector2(0.04f, 0.45f);
            panel.anchorMax = new Vector2(0.96f, 0.9f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panel.gameObject.SetActive(false);
            RectTransform scroll = UiFactory.Scroll(panel, out panelContent);
            scroll.anchorMin = new Vector2(0f, 0.12f);
            scroll.anchorMax = Vector2.one;
            scroll.offsetMin = Vector2.zero;
            scroll.offsetMax = Vector2.zero;
            Button close = MakeButton(panel, "Close", ClosePanel, new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.11f));
            close.onClick.AddListener(delegate { });
        }

        void OnInteract()
        {
            if (session == null || !session.Ready)
                return;
            AdventureState adventure = session.State.Adventure;
            InteractSpot spot = YardLayout.Nearest(adventure);
            if (spot.Kind == "factory")
            {
                session.TryPocket();
                Open("factory");
            }
            else if (spot.Kind == "shop")
            {
                session.TryPocket();
                Open("shop");
            }
            else if (spot.Kind == "gate")
            {
                session.TryOpenRegion();
                Open("map");
            }
            else if (spot.Kind == "bunker")
            {
                if (adventure.Owned.Get(spot.PlotId))
                {
                    session.TryVisitBunker(spot.PlotId);
                    pendingPlot = spot.PlotId;
                    Open("bunker");
                }
                else
                {
                    pendingPlot = spot.PlotId;
                    Open("buy");
                }
            }
            else
            {
                session.Note("Walk up to a door, the mill, or the shop.");
            }
            Ping();
        }

        void Open(string next)
        {
            mode = next;
            panel.gameObject.SetActive(true);
            UiFactory.Clear(panelContent);
            if (next == "characters")
                BuildCharacters();
            else if (next == "bag")
                BuildBag();
            else if (next == "factory")
                BuildFactory();
            else if (next == "shop")
                BuildShop();
            else if (next == "tasks")
                BuildTasks();
            else if (next == "map")
                BuildMap();
            else if (next == "bunker")
                BuildBunker();
            else if (next == "buy")
                BuildBuy();
        }

        void ClosePanel()
        {
            if (NeedsCharacter())
                return;
            mode = "";
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        void BuildCharacters()
        {
            AddLine("Choose a miner. Clothes do not change pay.");
            for (int i = 0; i < JourneyCatalog.AllCharacters.Count; i++)
            {
                CharacterDef person = JourneyCatalog.AllCharacters[i];
                bool open = session.State.Adventure.UnlockedCharacters.Contains(person.Id);
                string label = person.Name + "\n" + person.Role + (open ? "" : "\n" + person.UnlockText);
                string id = person.Id;
                AddButton(label, delegate
                {
                    if (session.TryChooseCharacter(id))
                    {
                        world.Present(session);
                        ClosePanel();
                    }
                });
            }
        }

        void BuildBag()
        {
            AddLine("Hands: " + (session.State.Adventure.HandsAmount <= 0d ? "empty" : session.State.Adventure.HandsAmount.ToString("0") + " " + JourneyCatalog.DisplayName(session.State.Adventure.HandsItem)));
            AddButton("Store hands", delegate { session.TryPocket(); Open("bag"); });
            foreach (var pair in session.State.Adventure.Inventory)
            {
                string id = pair.Key;
                int count = (int)pair.Value;
                AddLine(JourneyCatalog.DisplayName(id) + "  " + count + "  ·  " + JourneyCatalog.FindItem(id).Route);
                AddButton("Sell 1", delegate { session.TrySellGoods(id, 1); Open("bag"); });
            }
            AddButton("Sweep the path", delegate { session.TrySweepStone(); Open("bag"); });
        }

        void BuildFactory()
        {
            AddLine(AdventureRules.Bottleneck(session.State.Adventure));
            AddMachine("crush", "Crush rock");
            AddMachine("wash", "Wash grit");
            AddMachine("recover", "Sort diamonds");
            AddMachine("cut", "Cut");
            AddMachine("polish", "Polish");
            AddButton("Refine silver", delegate { session.TryRefine(JourneyCatalog.SilverOre); Open("factory"); });
            AddButton("Refine gold", delegate { session.TryRefine(JourneyCatalog.GoldOre); Open("factory"); });
            AddButton("Refine copper", delegate { session.TryRefine(JourneyCatalog.CopperOre); Open("factory"); });
            AddButton("Refine iron", delegate { session.TryRefine(JourneyCatalog.IronOre); Open("factory"); });
            AddLine("Bench");
            for (int i = 0; i < JourneyCatalog.AllRecipes.Count; i++)
            {
                JewelRecipe recipe = JourneyCatalog.AllRecipes[i];
                string note;
                bool ready = AdventureRules.CanCraft(session.State, recipe, out note);
                AddLine(recipe.Name + " · " + recipe.Note + (ready ? "" : " · " + note));
                string id = recipe.Id;
                AddButton("Make " + recipe.Name, delegate { session.TryMakeJewel(id); Open("factory"); });
            }
        }

        void AddMachine(string id, string label)
        {
            MachineState machine = session.State.Adventure.FindMachine(id);
            AddLine(label + " · " + AdventureRules.MachineStatus(machine));
            AddButton("Start " + label, delegate { session.TryStartBatch(id); Open("factory"); });
            AddButton("Collect " + label, delegate { session.TryCollectMachine(id); Open("factory"); });
        }

        void BuildShop()
        {
            AddLine("The shop always buys. Prices stay steady.");
            if (session.State.Adventure.OrderIndex < JourneyCatalog.AllOrders.Count)
            {
                CustomerOrderDef order = JourneyCatalog.AllOrders[session.State.Adventure.OrderIndex];
                AddLine(order.Title + " · " + order.Amount + " " + JourneyCatalog.DisplayName(order.ItemId));
                AddButton("Give order", delegate { session.TryFulfillOrder(); Open("shop"); });
            }
            else
            {
                AddLine("No visitor is waiting.");
            }
            foreach (var pair in session.State.Adventure.Inventory)
            {
                string id = pair.Key;
                ItemDef item = JourneyCatalog.FindItem(id);
                if (item == null)
                    continue;
                AddButton("Sell " + item.Name + " (" + item.SellValue + ")", delegate { session.TrySellGoods(id, 1); Open("shop"); });
            }
        }

        void BuildTasks()
        {
            for (int i = 0; i < JourneyCatalog.AllTasks.Count; i++)
            {
                YardTaskDef task = JourneyCatalog.AllTasks[i];
                bool claimed = session.State.Adventure.ClaimedTasks.Contains(task.Id);
                bool ready = AdventureRules.TaskReady(session.State, task.Id);
                AddLine(task.Title + " · " + task.Detail + (claimed ? " · done" : ready ? " · ready" : ""));
                string id = task.Id;
                AddButton(claimed ? "Claimed" : "Claim " + task.Title, delegate { session.TryClaimYardTask(id); Open("tasks"); });
            }
        }

        void BuildMap()
        {
            AddLine(RankRules.PendingLabel);
            AddLine("My bunkers");
            var owned = new List<int>();
            session.State.Adventure.Owned.Collect(0, 12, owned);
            for (int i = 0; i < owned.Count; i++)
            {
                int plot = owned[i];
                AddButton("Go to bunker " + (plot + 1), delegate
                {
                    YardPoint door = YardLayout.Entrance(plot);
                    session.State.Adventure.PlayerX = door.X;
                    session.State.Adventure.PlayerZ = door.Z + 1.2d;
                    session.MarkYardDirty();
                    ClosePanel();
                });
            }
            AddLine("Region 1 · " + WorldGenerator.ThemeName(0));
            var page = new List<PlotProfile>();
            WorldGenerator.Page(session.State.Adventure.WorldSeed, 0, 0, 8, session.State.Adventure.GeneratorVersion, page);
            for (int i = 0; i < page.Count; i++)
                AddLine(AdventureRules.PreviewPlot(session.State.Adventure, page[i].PlotId));
        }

        void BuildBunker()
        {
            int plot = pendingPlot;
            AddLine(AdventureRules.PreviewPlot(session.State.Adventure, plot));
            PlotProfile profile = WorldGenerator.Describe(session.State.Adventure.WorldSeed, plot, session.State.Adventure.GeneratorVersion);
            for (int i = 0; i < profile.Reserves.Count; i++)
            {
                string item = profile.Reserves[i].ItemId;
                double left = AdventureRules.Remaining(session.State.Adventure, plot, item);
                AddButton("Mine " + JourneyCatalog.DisplayName(item) + " (" + left.ToString("0") + ")", delegate
                {
                    session.TryDig(plot, item);
                    Open("bunker");
                });
            }
            AddButton("Post crew here", delegate { session.TryPostCrew(plot); Open("bunker"); });
        }

        void BuildBuy()
        {
            int plot = pendingPlot;
            AddLine(AdventureRules.PreviewPlot(session.State.Adventure, plot));
            AddLine("Buying is only this button. Walking past does not spend coins.");
            AddButton("Buy this bunker", delegate
            {
                if (session.TryPurchasePlot(plot))
                    ClosePanel();
                else
                    Open("buy");
            });
        }

        void AddLine(string text)
        {
            Text line = UiFactory.Label(panelContent, text, 28, UiTheme.Paper, TextAnchor.MiddleLeft);
            LayoutElement layout = line.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 72f;
            layout.preferredHeight = 96f;
        }

        void AddButton(string label, UnityEngine.Events.UnityAction action)
        {
            UiFactory.Press(panelContent, label, action, UiTheme.ButtonOn, 108f);
        }

        static Button MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Vector2 min, Vector2 max)
        {
            Button button = UiFactory.Press(parent, label, action, UiTheme.ButtonOn, 108f);
            RectTransform rect = button.GetComponent<RectTransform>();
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null)
                Destroy(layout);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        void Ping()
        {
            if (cues != null && session != null)
                cues.PlayUi(session);
        }
    }
}
