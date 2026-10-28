using System;
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
        public RectTransform Knob;
        public Vector2 RestPosition;
        Vector2 origin;

        public void OnPointerDown(PointerEventData eventData)
        {
            origin = eventData.position;
            Value = Vector2.zero;
            UpdateKnob(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - origin;
            Value = Vector2.ClampMagnitude(delta / 65f, 1f);
            UpdateKnob(Value * 45f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            UpdateKnob(Vector2.zero);
        }

        void UpdateKnob(Vector2 pos)
        {
            if (Knob != null)
                Knob.anchoredPosition = RestPosition + pos;
        }
    }

    public sealed partial class YardControls : MonoBehaviour
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
        Text coinPill;
        Text gemPill;
        RectTransform exploreControls;
        int boardSignature = int.MinValue;
        YardStick stick;
        string mode = "";
        int pendingPlot = -1;
        bool shown;

        // Bottom dock buttons
        readonly Dictionary<string, Button> dockButtons = new Dictionary<string, Button>();
        RectTransform dockViewport, dockContent;
        float dockLayoutWidth = -1;
        Rect dockSafeArea;

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
            {
                UpdateDockLayout();
                bool covered = ui != null && ui.CoversYard;
                canvas.sortingOrder = referenceReview ? 50 : NeedsCharacter() ? 40 : (covered ? 4 : 12);
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            AdventureState adventure = session.State.Adventure;
            if (stick != null && stick.Value.sqrMagnitude > 0.04f && mode.Length == 0 && !referenceReview)
            {
                if (world != null)
                    world.ClearMove();
                float speed = stick.Value.magnitude > 0.82f ? 7f : 4.2f;
                session.MoveCrew(stick.Value.x * speed * dt, stick.Value.y * speed * dt);
                adventure.PoseHint = stick.Value.magnitude > 0.82f ? "run" : "move";
                if (world != null)
                    world.Facing = Mathf.Atan2(stick.Value.x, stick.Value.y) * Mathf.Rad2Deg;
            }
            else if (world != null && world.HasMove && mode.Length == 0 && !referenceReview)
            {
                Vector3 target = world.MoveTarget;
                float dx = target.x - (float)adventure.PlayerX;
                float dz = target.z - (float)adventure.PlayerZ;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                if (distance < 0.18f)
                {
                    world.ClearMove();
                }
                else
                {
                    float step = Mathf.Min(4.2f * dt, distance);
                    session.MoveCrew(dx / distance * step, dz / distance * step);
                    adventure.PoseHint = "move";
                    world.Facing = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
                }
            }
            if ((stick == null || stick.Value.sqrMagnitude <= 0.04f) && (world == null || !world.HasMove) &&
                (adventure.PoseHint == "move" || adventure.PoseHint == "run"))
                adventure.PoseHint = "";
            InteractSpot spot = YardLayout.Nearest(adventure);
            if (interactLabel != null && !referenceReview)
                interactLabel.text = spot.Kind == "none" ? "Look" : spot.Label;
            if (coinPill != null)
                coinPill.text = referenceReview ? "2,480" : NumericFormat.Format(session.State.Coins);
            if (gemPill != null)
                gemPill.text = referenceReview ? "36" : adventure.LifetimeDiamondsRecovered.ToString("0");
            if (statusText != null)
            {
                string hint = string.IsNullOrEmpty(session.ActionMessage) ? AdventureRules.NextHint(session.State) : session.ActionMessage;
                if (!string.IsNullOrEmpty(session.SaveError))
                    hint = session.SaveError;
                statusText.text = hint;
            }
            if (exploreControls != null)
                exploreControls.gameObject.SetActive(mode.Length == 0);
            if (world != null)
                world.Present(session);
            int signature = BoardSignature();
            if (!referenceReview && signature != boardSignature && (mode == "mine" || mode == "factory" || mode == "craft" || mode == "shop" || mode == "tasks"))
                Open(mode);
            RefreshMeasuredLive();
            if (NeedsCharacter() && mode != "characters" && !referenceReview)
                Open("characters");
        }

        bool NeedsCharacter()
        {
            return !referenceReview && session != null && session.Ready && session.State != null && string.IsNullOrEmpty(session.State.Adventure.SelectedCharacter);
        }

        void Build()
        {
            var canvasObject = new GameObject("YardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0f;
            root = canvasObject.GetComponent<RectTransform>();

            // Ground tap for tap-to-move
            var ground = UiFactory.Panel("GroundTap", root, new Color(1f, 1f, 1f, 0.001f));
            ground.GetComponent<Image>().raycastTarget = true;
            ground.anchorMin = new Vector2(0f, 0.12f);
            ground.anchorMax = new Vector2(1f, 0.90f);
            ground.offsetMin = Vector2.zero;
            ground.offsetMax = Vector2.zero;
            YardGroundClick tap = ground.gameObject.AddComponent<YardGroundClick>();
            tap.Owner = this;

            BuildBottomDock();

            // Explore controls (virtual joystick + interact button)
            exploreControls = UiFactory.Rect("ExploreControls", root);
            UiFactory.Stretch(exploreControls);
            // Reference-measured controls are built on their own surface.

            // Floating panel for screen content (transparent root, modular cards placed inside)
            panel = UiFactory.Rect("YardPanel", root);
            UiFactory.Stretch(panel);
            ScrollRect scrollRect = panel.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = panel;
            scrollRect.viewport = panel;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            panel.gameObject.SetActive(false);
            panelContent = panel;
            BuildMeasuredSurface();
            statusText = UiFactory.Label(root, "", 20, Color.white, TextAnchor.MiddleCenter, true);
            UiFactory.Box(statusText.rectTransform, .04f, .083f, .96f, .117f);
            Shadow feedbackShadow = statusText.gameObject.AddComponent<Shadow>();
            feedbackShadow.effectColor = new Color(0,0,0,.85f);
            BuildMeasuredScreen("explore");
        }

        void BuildTopHud()
        {
            // Coins Pill
            RectTransform coinCapsule = UiFactory.DarkPill("CoinPill", root);
            UiFactory.Box(coinCapsule, 0.04f, 0.932f, 0.38f, 0.985f);
            Image coinIcon = UiFactory.Icon("CoinIcon", coinCapsule, "icon_coin");
            UiFactory.Box(coinIcon.rectTransform, 0.06f, 0.15f, 0.28f, 0.85f);
            coinPill = UiFactory.Label(coinCapsule, "0", 24, Color.white, TextAnchor.MiddleCenter, true);
            UiFactory.Box(coinPill.rectTransform, 0.28f, 0.05f, 0.78f, 0.95f);
            Button coinPlus = UiFactory.CircleButton("CoinPlus", coinCapsule, delegate { Ping(); });
            UiFactory.Box(coinPlus.GetComponent<RectTransform>(), 0.78f, 0.15f, 0.96f, 0.85f);
            Text plusTxt = UiFactory.Label(coinPlus.transform, "+", 20, Color.white, TextAnchor.MiddleCenter, true);
            UiFactory.Stretch(plusTxt.rectTransform);

            // Diamonds Pill
            RectTransform gemCapsule = UiFactory.DarkPill("GemPill", root);
            UiFactory.Box(gemCapsule, 0.42f, 0.932f, 0.78f, 0.985f);
            Image gemIcon = UiFactory.Icon("GemIcon", gemCapsule, "icon_diamond");
            UiFactory.Box(gemIcon.rectTransform, 0.06f, 0.15f, 0.28f, 0.85f);
            gemPill = UiFactory.Label(gemCapsule, "0", 24, Color.white, TextAnchor.MiddleCenter, true);
            UiFactory.Box(gemPill.rectTransform, 0.28f, 0.05f, 0.78f, 0.95f);
            Button gemPlus = UiFactory.CircleButton("GemPlus", gemCapsule, delegate { Ping(); });
            UiFactory.Box(gemPlus.GetComponent<RectTransform>(), 0.78f, 0.15f, 0.96f, 0.85f);
            Text gemPlusTxt = UiFactory.Label(gemPlus.transform, "+", 20, Color.white, TextAnchor.MiddleCenter, true);
            UiFactory.Stretch(gemPlusTxt.rectTransform);

            // Settings Gear Button
            RectTransform gearBox = UiFactory.DarkPill("GearBox", root);
            UiFactory.Box(gearBox, 0.84f, 0.932f, 0.96f, 0.985f);
            Button gearBtn = gearBox.gameObject.AddComponent<Button>();
            gearBtn.onClick.AddListener(delegate { if (ui != null) ui.OpenSettings(); Ping(); });
            Image gearIcon = UiFactory.Icon("GearIcon", gearBox, "icon_gear");
            UiFactory.Box(gearIcon.rectTransform, 0.18f, 0.18f, 0.82f, 0.82f);
            Text gearTxt = UiFactory.Label(gearBox, "", 1, Color.clear, TextAnchor.MiddleCenter);

            // Subtle Status Hint text below top HUD
            statusText = UiFactory.Label(root, "", 20, Color.white, TextAnchor.MiddleCenter, false);
            UiFactory.Box(statusText.rectTransform, 0.05f, 0.89f, 0.95f, 0.925f);
        }

        void BuildBottomDock()
        {
            RectTransform dock = UiFactory.DarkPill("BottomDock", root);
            UiFactory.Box(dock, 0.02f, 0.012f, 0.98f, 0.096f);
            dockViewport = dock;
            dock.gameObject.AddComponent<RectMask2D>();
            dockContent = UiFactory.Rect("Tabs", dock);
            dockContent.anchorMin = new Vector2(0,0);dockContent.anchorMax = new Vector2(0,1);dockContent.pivot = new Vector2(0,.5f);
            dockContent.offsetMin = dockContent.offsetMax = Vector2.zero;
            ScrollRect dockScroll = dock.gameObject.AddComponent<ScrollRect>();
            dockScroll.viewport = dock;dockScroll.content = dockContent;dockScroll.horizontal = true;dockScroll.vertical = false;
            dockScroll.movementType = ScrollRect.MovementType.Clamped;dockScroll.scrollSensitivity = 50;

            string[] tabs = { "", "mine", "factory", "craft", "shop", "map", "tasks", "rank", "more" };
            string[] labels = { "Explore", "Mine", "Process", "Craft", "Sell", "Expand", "Progress", "Rank", "More" };
            string[] icons = { "icon_hand", "icon_pickaxe", "icon_crusher", "icon_ring", "icon_coin", "icon_sack", "trophy_gold", "trophy_silver", "icon_gear" };

            float count = tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                string tabMode = tabs[i];
                float xMin = (i + 0.05f) / count;
                float xMax = (i + 0.95f) / count;

                RectTransform item = UiFactory.Rect("Tab_" + labels[i], dockContent);
                UiFactory.Box(item, xMin, 0.05f, xMax, 0.95f);
                Button btn = item.gameObject.AddComponent<Button>();
                Image hit = item.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0.01f);
                btn.targetGraphic = hit;
                dockButtons[tabMode] = btn;

                Image icon = UiFactory.Icon("Icon", item, icons[i]);
                UiFactory.Box(icon.rectTransform, 0.15f, 0.40f, 0.85f, 0.92f);

                Text lbl = UiFactory.Label(item, labels[i], 24, Color.white, TextAnchor.LowerCenter, true);
                lbl.resizeTextForBestFit = true;lbl.resizeTextMinSize = 18;lbl.resizeTextMaxSize = 24;
                UiFactory.Box(lbl.rectTransform, 0f, 0.02f, 1f, 0.40f);

                btn.onClick.AddListener(delegate
                {
                    if (tabMode.Length == 0)
                        ClosePanel();
                    else if (tabMode == "mine")
                        OpenMine();
                    else
                        Open(tabMode);
                    Ping();
                });
            }
            UpdateDockLayout();
        }

        void UpdateDockLayout()
        {
            if(dockContent == null || dockViewport == null || canvas == null || dockButtons.Count == 0)return;
            Rect safe = Screen.safeArea;
            if(safe != dockSafeArea)
            {
                dockSafeArea = safe;
                Vector2 low = new Vector2(safe.xMin / Mathf.Max(1,Screen.width), safe.yMin / Mathf.Max(1,Screen.height));
                Vector2 span = new Vector2(safe.width / Mathf.Max(1,Screen.width), safe.height / Mathf.Max(1,Screen.height));
                dockViewport.anchorMin = low + new Vector2(.02f*span.x,.012f*span.y);
                dockViewport.anchorMax = low + new Vector2(.98f*span.x,.096f*span.y);
            }
            float density = Screen.dpi > 0 ? Mathf.Clamp(Screen.dpi / 160f, 1f, 4f) : 1f;
            float minimum = 56f * density / Mathf.Max(.01f, canvas.scaleFactor);
            float width = Mathf.Max(dockViewport.rect.width, dockButtons.Count * minimum / .90f);
            if(Mathf.Abs(width - dockLayoutWidth) < .5f)return;
            dockLayoutWidth = width;
            dockContent.sizeDelta = new Vector2(width,0);
        }

        void BuildExploreControls()
        {
            // Virtual Joystick
            RectTransform stickBox = UiFactory.Rect("StickBox", exploreControls);
            UiFactory.Box(stickBox, 0.06f, 0.12f, 0.32f, 0.26f);
            Image stickBase = stickBox.gameObject.AddComponent<Image>();
            stickBase.sprite = UiFactory.GetSprite("joystick_base");
            stickBase.color = Color.white;
            stickBase.raycastTarget = true;
            stick = stickBox.gameObject.AddComponent<YardStick>();

            RectTransform knob = UiFactory.Rect("Knob", stickBox);
            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(70f, 70f);
            knob.anchoredPosition = Vector2.zero;
            Image knobImg = knob.gameObject.AddComponent<Image>();
            knobImg.sprite = UiFactory.GetSprite("joystick_knob");
            knobImg.color = Color.white;
            knobImg.raycastTarget = false;
            stick.Knob = knob;

            // Circular Navy Action Button (Interact)
            Button interact = UiFactory.CircleButton("InteractBtn", exploreControls, OnInteract, "icon_hand", "Interact");
            UiFactory.Box(interact.GetComponent<RectTransform>(), 0.74f, 0.12f, 0.94f, 0.24f);
            interactLabel = interact.GetComponentInChildren<Text>();

            // Return Home Pill Button
            RectTransform homePill = UiFactory.DarkPill("HomePill", exploreControls);
            UiFactory.Box(homePill, 0.35f, 0.13f, 0.65f, 0.18f);
            Button homeBtn = homePill.gameObject.AddComponent<Button>();
            homeBtn.onClick.AddListener(delegate { session.GoHome(); Ping(); });
            Text homeTxt = UiFactory.Label(homePill, "Return Home", 22, Color.white, TextAnchor.MiddleCenter, true);
            UiFactory.Stretch(homeTxt.rectTransform);
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
                    Open("mine");
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
            if (world != null) world.ClearMove();
            if (stick != null) stick.Value = Vector2.zero;
            if (BuildMeasuredScreen(next))
            {
                panel.gameObject.SetActive(false);
                if (world != null) world.SetScreenMode(next);
                boardSignature = BoardSignature();
                return;
            }
            if (referenceSurface != null) referenceSurface.gameObject.SetActive(false);
            if (world != null)
                world.SetScreenMode(next);
            panel.gameObject.SetActive(true);
            UiFactory.Clear(panelContent);

            if (next == "characters")
                BuildCharacters();
            else if (next == "bag")
                BuildBag();
            else if (next == "more")
                BuildMore();
            else if (next == "bunker")
                BuildBunker();
            else if (next == "buy")
                BuildBuy();

            boardSignature = BoardSignature();
        }

        void OpenMine()
        {
            if (pendingPlot < 0)
                pendingPlot = 2;
            Open("mine");
        }

        int BoardSignature()
        {
            if (session == null || session.State == null || session.State.Adventure == null)
                return 0;
            AdventureState adventure = session.State.Adventure;
            int jobs = 0;
            for (int i = 0; i < adventure.Jobs.Count; i++)
            {
                if (!adventure.Jobs[i].OutputGranted)
                    jobs++;
            }
            return ((int)session.State.Coins * 17) ^ ((int)adventure.HandsAmount * 31) ^ ((int)adventure.CartLoad * 13) ^ adventure.RingsCrafted ^ (jobs << 8) ^ adventure.OrdersCompleted;
        }

        void ClosePanel()
        {
            if (NeedsCharacter())
                return;
            mode = "";
            if (world != null)
                world.SetScreenMode("");
            if (panel != null)
                panel.gameObject.SetActive(false);
            BuildMeasuredScreen("explore");
        }

        // ==========================================
        // SCREEN 9: CHARACTER SELECTION
        // ==========================================
        void BuildCharacters()
        {
            RectTransform card = UiFactory.Card("CrewSelectionCard", panelContent);
            UiFactory.Box(card, 0.04f, 0.10f, 0.96f, 0.92f);

            Texture2D crew = Resources.Load<Texture2D>("Ui/crew-lineup");
            if (crew != null)
            {
                RectTransform banner = UiFactory.Rect("CrewBanner", card);
                UiFactory.Box(banner, 0.04f, 0.74f, 0.96f, 0.96f);
                RawImage img = banner.gameObject.AddComponent<RawImage>();
                img.texture = crew;
                img.color = Color.white;
            }

            Text title = UiFactory.Label(card, "Choose Your Miner", 26, UiTheme.Ink, TextAnchor.MiddleCenter, true);
            UiFactory.Box(title.rectTransform, 0.04f, 0.67f, 0.96f, 0.73f);

            // 12 Characters Grid
            for (int i = 0; i < JourneyCatalog.AllCharacters.Count; i++)
            {
                CharacterDef person = JourneyCatalog.AllCharacters[i];
                int col = i % 3;
                int row = i / 3;

                float x0 = 0.04f + col * 0.31f;
                float x1 = x0 + 0.29f;
                float y1 = 0.65f - row * 0.13f;
                float y0 = y1 - 0.11f;

                string id = person.Id;
                RectTransform cBox = UiFactory.Panel("Char_" + id, card, UiTheme.Paper);
                UiFactory.Box(cBox, x0, y0, x1, y1);

                Text nm = UiFactory.Label(cBox, person.Name, 18, UiTheme.Ink, TextAnchor.UpperCenter, true);
                UiFactory.Box(nm.rectTransform, 0.02f, 0.45f, 0.98f, 0.95f);

                Text rl = UiFactory.Label(cBox, person.Role, 14, UiTheme.Muted, TextAnchor.LowerCenter, false);
                UiFactory.Box(rl.rectTransform, 0.02f, 0.05f, 0.98f, 0.45f);

                Button btn = cBox.gameObject.AddComponent<Button>();
                bool unlocked = session.State.Adventure.UnlockedCharacters.Contains(id);
                btn.interactable = unlocked;
                if (!unlocked) rl.text = person.UnlockText + " (locked)";
                btn.onClick.AddListener(delegate
                {
                    if (session.TryChooseCharacter(id))
                    {
                        if (world != null) world.Present(session);
                        ClosePanel();
                    }
                    Ping();
                });
            }
        }

        // ==========================================
        // OTHER EXISTING SCREENS
        // ==========================================
        void BuildBag()
        {
            RectTransform card = UiFactory.Card("BagCard", panelContent);
            UiFactory.Box(card, 0.06f, 0.15f, 0.94f, 0.88f);

            Text title = UiFactory.Label(card, "Backpack & Inventory", 28, UiTheme.Ink, TextAnchor.UpperLeft, true);
            UiFactory.Box(title.rectTransform, 0.08f, 0.88f, 0.92f, 0.96f);

            AdventureState adventure = session.State.Adventure;
            string hands = adventure.HandsAmount <= 0d ? "empty" : adventure.HandsAmount.ToString("0") + " " + JourneyCatalog.DisplayName(adventure.HandsItem);
            string cart = adventure.CartLoad > 0d && !adventure.CartGranted ? adventure.CartLoad.ToString("0") + " " + JourneyCatalog.DisplayName(adventure.CartItem) : "empty";

            Text info = UiFactory.Label(card, "Carrying: " + hands + "\nMine Cart: " + cart, 22, UiTheme.Ink, TextAnchor.MiddleLeft, false);
            UiFactory.Box(info.rectTransform, 0.08f, 0.72f, 0.92f, 0.86f);

            Button sendCart = UiFactory.ButtonBlue("SendCartBtn", card, "Send cart to mill", delegate { session.TrySendCart(); Open("bag"); Ping(); });
            UiFactory.Box(sendCart.GetComponent<RectTransform>(), 0.08f, 0.58f, 0.48f, 0.68f);

            Button pocket = UiFactory.ButtonGreen("PocketBtn", card, "Store hands at mill", delegate { session.TryPocket(); Open("bag"); Ping(); });
            UiFactory.Box(pocket.GetComponent<RectTransform>(), 0.52f, 0.58f, 0.92f, 0.68f);

            Button sweep = UiFactory.ButtonBlue("SweepBtn", card, "Sweep the path", delegate { session.TrySweepStone(); Open("bag"); Ping(); });
            UiFactory.Box(sweep.GetComponent<RectTransform>(), 0.08f, 0.44f, 0.92f, 0.54f);
            RectTransform inventory;
            RectTransform inventoryScroll = UiFactory.Scroll(card, out inventory);
            UiFactory.Box(inventoryScroll, .08f, .05f, .92f, .40f);
            bool any = false;
            foreach (ItemDef item in JourneyCatalog.AllItems)
            {
                double count = AdventureRules.Count(adventure, item.Id);
                if (count <= 0) continue;
                any = true;
                Text row = UiFactory.Label(inventory, item.Name + "   " + NumericFormat.Format(count), 22, UiTheme.Ink, TextAnchor.MiddleLeft);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
            }
            if (!any) UiFactory.Label(inventory, "Warehouse is empty. Mine and send a cart to the mill.", 22, UiTheme.Muted, TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
        }

        void BuildMore()
        {
            RectTransform card = UiFactory.Card("MoreCard", panelContent);
            UiFactory.Box(card, 0.08f, 0.20f, 0.92f, 0.85f);

            Text title = UiFactory.Label(card, "More Options", 28, UiTheme.Ink, TextAnchor.UpperLeft, true);
            UiFactory.Box(title.rectTransform, 0.08f, 0.86f, 0.92f, 0.96f);

            string[] opts = { "Choose miner", "Backpack & inventory", "Refinery", "Hire yard worker", "Assign worker to current bunker", "Progress & Tasks", "Expand Region", "Rankings", "Settings", "Works Mines" };
            for (int i = 0; i < opts.Length; i++)
            {
                float yTop = 0.81f - i * 0.078f;
                float yBottom = yTop - 0.068f;
                int idx = i;
                Button b = UiFactory.ButtonBlue("Opt_" + i, card, opts[i], delegate
                {
                    if (idx == 0) Open("characters");
                    else if (idx == 1) Open("bag");
                    else if (idx == 2) { Open("factory"); ShowRefineryDetails(); }
                    else if (idx == 3) { session.TryHireCrew(); Open("more"); }
                    else if (idx == 4) { session.TryPostCrew(Mathf.Max(0, session.State.Adventure.FocusPlotId)); Open("more"); }
                    else if (idx == 5) Open("tasks");
                    else if (idx == 6) Open("map");
                    else if (idx == 7) Open("rank");
                    else if (idx == 8) { if (ui != null) ui.OpenSettings(); }
                    else if (idx == 9) ShowWorks();
                    Ping();
                });
                UiFactory.Box(b.GetComponent<RectTransform>(), 0.08f, yBottom, 0.92f, yTop);
            }
        }

        void BuildBunker()
        {
            RectTransform card = UiFactory.Card("BunkerCard", panelContent);
            UiFactory.Box(card, 0.08f, 0.20f, 0.92f, 0.80f);

            int plot = pendingPlot;
            Text title = UiFactory.Label(card, "Mine Shaft " + (plot + 1), 28, UiTheme.Ink, TextAnchor.UpperLeft, true);
            UiFactory.Box(title.rectTransform, 0.08f, 0.84f, 0.92f, 0.96f);

            Text desc = UiFactory.Label(card, AdventureRules.PreviewPlot(session.State.Adventure, plot), 22, UiTheme.Muted, TextAnchor.MiddleLeft, false);
            UiFactory.Box(desc.rectTransform, 0.08f, 0.60f, 0.92f, 0.80f);

            Button mine = UiFactory.ButtonGreen("MineBtn", card, "Mine", delegate { session.TryDig(plot, JourneyCatalog.DiamondOre); Open("bunker"); Ping(); });
            UiFactory.Box(mine.GetComponent<RectTransform>(), 0.08f, 0.40f, 0.92f, 0.54f);

            Button sendCart = UiFactory.ButtonBlue("CartBtn", card, "Send cart to mill", delegate { session.TrySendCart(); Open("bunker"); Ping(); });
            UiFactory.Box(sendCart.GetComponent<RectTransform>(), 0.08f, 0.22f, 0.92f, 0.36f);
        }

        void BuildBuy()
        {
            RectTransform card = UiFactory.Card("BuyCard", panelContent);
            UiFactory.Box(card, 0.08f, 0.25f, 0.92f, 0.75f);

            int plot = pendingPlot;
            Text title = UiFactory.Label(card, "Purchase Mine Shaft " + (plot + 1), 28, UiTheme.Ink, TextAnchor.UpperLeft, true);
            UiFactory.Box(title.rectTransform, 0.08f, 0.80f, 0.92f, 0.95f);

            Text desc = UiFactory.Label(card, AdventureRules.PreviewPlot(session.State.Adventure, plot) + "\n\nPrice: " + AdventureRules.PlotPrice(session.State.Adventure, plot).ToString("0") + " Coins", 22, UiTheme.Muted, TextAnchor.MiddleLeft, false);
            UiFactory.Box(desc.rectTransform, 0.08f, 0.45f, 0.92f, 0.75f);

            Button buyBtn = UiFactory.ButtonGreen("ConfirmBuyBtn", card, "Buy Mine Shaft", delegate
            {
                if (session.TryPurchasePlot(plot))
                    ClosePanel();
                else
                    Open("buy");
                Ping();
            }, "icon_coin");
            UiFactory.Box(buyBtn.GetComponent<RectTransform>(), 0.08f, 0.15f, 0.92f, 0.35f);
        }

        static string MillLine(AdventureState adventure)
        {
            string line = AdventureRules.Bottleneck(adventure);
            string[] ids = { "crush", "wash", "recover", "cut", "polish" };
            for (int i = 0; i < ids.Length; i++)
            {
                MachineState machine = adventure.FindMachine(ids[i]);
                line += "\n" + ids[i] + " " + AdventureRules.MachineStatus(machine);
            }
            return line;
        }

        string CountLabel(string itemId)
        {
            return ((int)AdventureRules.Count(session.State.Adventure, itemId)).ToString();
        }

        public void TapMove(Vector2 screen)
        {
            if (world == null || world.View == null || !world.View.enabled || mode.Length != 0 || referenceReview || session == null || !session.Ready)
                return;
            Ray ray = world.View.ScreenPointToRay(screen);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (!plane.Raycast(ray, out distance))
                return;
            Vector3 point = ray.GetPoint(distance);
            world.SetMoveTarget(point.x, point.z);
        }

        void Ping()
        {
            if (cues != null && session != null)
                cues.PlayUi(session);
        }
    }

    public sealed class YardGroundClick : MonoBehaviour, IPointerDownHandler
    {
        public YardControls Owner;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Owner != null)
                Owner.TapMove(eventData.position);
        }
    }
}
