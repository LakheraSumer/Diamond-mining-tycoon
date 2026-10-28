using System;
using System.Collections.Generic;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    [Serializable] public sealed class ReferenceUiElement
    {
        public string id;
        public float[] rect;
        public float fontSize;
        public float uncertaintyPixels;
    }
    [Serializable] public sealed class ReferenceUiScreen
    {
        public string id;
        public int width, height;
        public ReferenceUiElement[] elements;
    }
    [Serializable] public sealed class ReferenceUiManifest
    {
        public string source, sourceHash;
        public ReferenceUiScreen[] screens;
    }
    [Serializable] public sealed class ReferenceUiBound
    {
        public string id;
        public float[] expected, actual, error;
        public float measurementUncertaintyPixels;
        public bool present, withinTarget;
    }
    [Serializable] public sealed class ReferenceUiReport
    {
        public string screen, source, sourceHash;
        public int sourceWidth, sourceHeight, viewportWidth, viewportHeight;
        public float target = .005f;
        public bool referenceReview;
        public string scope = "RectTransform bounds only. Does not certify icons, font, 3D composition or visual identity. Source JPEG edges have recorded uncertainty.";
        public ReferenceUiBound[] elements;
    }

    /// <summary>Editable measured layout shared by live play and isolated visual review.</summary>
    public sealed partial class YardControls
    {
        ReferenceUiManifest referenceManifest;
        ReferenceUiScreen referenceScreen;
        RectTransform referenceSurface;
        readonly Dictionary<string, RectTransform> measured = new Dictionary<string, RectTransform>();
        bool referenceReview;
        string selectedRecipe = "ring";
        int recipePage, selectedMapPlot = 5;
        string selectedMachine = "crush";
        int selectedProgressGroup;
        Text machineDetailText;
        Rect lastSafeArea;
        int lastScreenWidth, lastScreenHeight;
        public bool IsReferenceReview { get { return referenceReview; } }
        public string CurrentReferenceScreen { get { return referenceScreen == null ? "" : referenceScreen.id; } }
        public Vector2Int ReferenceViewport { get { return referenceScreen == null ? new Vector2Int(988,1136) : new Vector2Int(referenceScreen.width*4,referenceScreen.height*4); } }

        public void BeginReferenceReview(string screen)
        {
            referenceReview = true;
            lastScreenWidth = 0;
            Show();
            Open(screen == "explore" ? "" : screen);
        }

        public void EndReferenceReview()
        {
            referenceReview = false;
            lastScreenWidth = 0;
            ClosePanel();
        }

        // These calls expose the same live screen actions to editor QA and accessibility tooling.
        public void OpenScreen(string screen) { Open(screen == "explore" ? "" : screen); }

        void BuildMeasuredSurface()
        {
            TextAsset data = Resources.Load<TextAsset>("Ui/reference-layout-runtime");
            if (data == null) throw new InvalidOperationException("Missing measured reference UI manifest.");
            referenceManifest = JsonUtility.FromJson<ReferenceUiManifest>(data.text);
            referenceSurface = UiFactory.Rect("MeasuredGameUi", root);
            UiFactory.Stretch(referenceSurface);
        }

        void RefreshMeasuredLive()
        {
            ApplyPresentationInsets();
            if (referenceReview || referenceScreen == null || mode.Length == 0) return;
            // Update displayed numbers in place: held pointers and keyboard focus survive simulation ticks.
            if (mode == "mine")
            {
                AdventureState a = session.State.Adventure;
                string[] ids = { JourneyCatalog.Stone, JourneyCatalog.DiamondOre, JourneyCatalog.SilverOre };
                for (int i=0;i<3;i++) SetMeasuredText("ore_count_"+i,Number(Count(ids[i])));
                SetMeasuredText("carry_count",Number(a.HandsAmount)+" / "+a.CarryCapacity);
                SetMeasuredBar("carry_bar",(float)(a.HandsAmount/Math.Max(1,a.CarryCapacity)));
            }
            else if (mode == "factory")
            {
                string[] ids={JourneyCatalog.DiamondOre,JourneyCatalog.RoughDiamond,JourneyCatalog.RefinedGold};
                for(int i=0;i<3;i++) SetMeasuredText("input_count_"+i,Number(Count(ids[i])));
                if(machineDetailText != null)
                    machineDetailText.text=selectedMachine.ToUpperInvariant()+"\n"+AdventureRules.MachineStatus(session.State.Adventure.FindMachine(selectedMachine));
            }
            else if (mode == "craft")
            {
                CraftJob first=null; foreach(CraftJob job in session.State.Adventure.Jobs) if(!job.OutputGranted){first=job;break;}
                SetMeasuredText("queue_time",first==null?"Idle":first.Remaining.ToString("0.0")+"s");
                SetMeasuredBar("queue_progress",first==null?0:1-(float)(first.Remaining/JourneyCatalog.FindRecipe(first.RecipeId).Seconds));
            }
        }
        void SetMeasuredText(string id,string value)
        { RectTransform rect; if(measured.TryGetValue(id,out rect)&&rect!=null){Text text=rect.GetComponent<Text>();if(text!=null)text.text=value;} }
        void SetMeasuredBar(string id,float value)
        { RectTransform rect; if(measured.TryGetValue(id,out rect)&&rect!=null){Transform fill=rect.Find("Fill");if(fill!=null)((RectTransform)fill).anchorMax=new Vector2(Mathf.Clamp01(value),1);} }

        void ApplyPresentationInsets()
        {
            if (referenceSurface == null) return;
            Rect safe = Screen.safeArea;
            if (!referenceReview && safe == lastSafeArea && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;
            lastSafeArea = safe; lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (referenceReview && referenceScreen != null)
            {
                scaler.referenceResolution = new Vector2(referenceScreen.width*4, referenceScreen.height*4);
                UiFactory.Stretch(referenceSurface);
            }
            else
            {
                scaler.referenceResolution = new Vector2(720,1280);
                Vector2 lo = new Vector2(safe.xMin/Mathf.Max(1,Screen.width), safe.yMin/Mathf.Max(1,Screen.height));
                Vector2 hi = new Vector2(safe.xMax/Mathf.Max(1,Screen.width), safe.yMax/Mathf.Max(1,Screen.height));
                // Navigation dock occupies a live-only band beneath the measured game composition.
                lo.y += (hi.y-lo.y)*.10f;
                referenceSurface.anchorMin = lo; referenceSurface.anchorMax = hi;
                referenceSurface.offsetMin = referenceSurface.offsetMax = Vector2.zero;
            }
            Transform dock = root.Find("BottomDock");
            if (dock != null) dock.gameObject.SetActive(!referenceReview);
            if (exploreControls != null) exploreControls.gameObject.SetActive(false);
        }

        bool BuildMeasuredScreen(string screen)
        {
            if (screen.Length == 0) screen = "explore";
            ReferenceUiScreen found = null;
            if (referenceManifest == null) return false;
            foreach (ReferenceUiScreen entry in referenceManifest.screens) if (entry.id == screen) found = entry;
            if (found == null) { referenceScreen = null; return false; }
            referenceScreen = found;
            referenceSurface.gameObject.SetActive(true);
            UiFactory.Clear(referenceSurface);
            measured.Clear(); coinPill = gemPill = interactLabel = null; stick = null;
            ApplyPresentationInsets();
            if(statusText!=null)statusText.gameObject.SetActive(!referenceReview);
            if(session==null||session.State==null||session.State.Adventure==null)
            {
                // A blocked/newer save is presented by GameUi. Do not dereference or rewrite it here.
                Text error=UiFactory.Label(referenceSurface,session==null?"Opening mine...":session.StatusMessage,24,Color.white,TextAnchor.MiddleCenter,true);
                UiFactory.Box(error.rectTransform,.08f,.35f,.92f,.65f);
                return true;
            }
            BuildMeasuredCurrency();
            switch (screen)
            {
                case "explore": MeasuredExplore(); break;
                case "mine": MeasuredMine(); break;
                case "factory": MeasuredFactory(); break;
                case "craft": MeasuredCraft(); break;
                case "shop": MeasuredShop(); break;
                case "map": MeasuredMap(); break;
                case "tasks": MeasuredTasks(); break;
                case "rank": MeasuredRank(); break;
            }
            if (referenceReview)
            {
                foreach (Button button in referenceSurface.GetComponentsInChildren<Button>())
                {
                    ColorBlock colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;
                    button.interactable=false;
                }
                Text demo=UiFactory.Label(referenceSurface,"DEMO · REFERENCE REVIEW",20,Color.white,TextAnchor.MiddleCenter,true);
                UiFactory.Box(demo.rectTransform,.02f,.002f,.98f,.037f);
                Shadow shadow=demo.gameObject.AddComponent<Shadow>();shadow.effectColor=Color.black;
            }
            else AddAccessibleHitAreas();
            return true;
        }

        void AddAccessibleHitAreas()
        {
            Canvas.ForceUpdateCanvases();
            float density=Screen.dpi>0 ? Mathf.Clamp(Screen.dpi/160f,1f,4f) : 1f;
            float minimum=44f*density/Mathf.Max(.01f,canvas.scaleFactor);
            foreach(Button button in referenceSurface.GetComponentsInChildren<Button>())
            {
                RectTransform visual=button.GetComponent<RectTransform>();
                if(visual.Find("AccessibleHitTarget")!=null)continue;
                if(visual.rect.width>=minimum&&visual.rect.height>=minimum)continue;
                RectTransform hit=UiFactory.Rect("AccessibleHitTarget",visual);
                hit.anchorMin=hit.anchorMax=new Vector2(.5f,.5f);
                hit.sizeDelta=new Vector2(Mathf.Max(minimum,visual.rect.width),Mathf.Max(minimum,visual.rect.height));
                Image image=hit.gameObject.AddComponent<Image>();image.color=new Color(1,1,1,.001f);image.raycastTarget=true;
                // Pointer events bubble to the existing parent Button; no duplicate game action listener.
            }
        }

        ReferenceUiElement Definition(string id)
        {
            foreach (ReferenceUiElement e in referenceScreen.elements) if (e.id == id) return e;
            return null;
        }
        RectTransform Place(string id, RectTransform rect)
        {
            ReferenceUiElement def = Definition(id);
            if (def == null) throw new InvalidOperationException("Unmeasured UI element: "+referenceScreen.id+"/"+id);
            float[] r = def.rect;
            UiFactory.Box(rect,r[0],1-r[1]-r[3],r[0]+r[2],1-r[1]);
            rect.name = "Reference."+id;
            measured[id] = rect;
            return rect;
        }
        RectTransform Card(string id, bool dark=false)
        { return Place(id,dark ? UiFactory.DarkPill(id,referenceSurface) : UiFactory.Card(id,referenceSurface)); }
        RectTransform Flat(string id, Color color)
        { return Place(id,UiFactory.Panel(id,referenceSurface,color)); }
        Image Icon(string id,string icon)
        { Image image=UiFactory.Icon(id,referenceSurface,icon); Place(id,image.rectTransform); return image; }
        ReferenceUiGlyph Glyph(string id,ReferenceUiGlyph.Symbol symbol,Color color)
        {RectTransform rect=Place(id,UiFactory.Rect(id,referenceSurface));return ChildGlyph(rect,symbol,color);}
        ReferenceUiGlyph ChildGlyph(RectTransform parent,ReferenceUiGlyph.Symbol symbol,Color color)
        {RectTransform rect=UiFactory.Rect("Glyph",parent);UiFactory.Stretch(rect);ReferenceUiGlyph glyph=rect.gameObject.AddComponent<ReferenceUiGlyph>();glyph.Shape=symbol;glyph.color=color;glyph.raycastTarget=false;return glyph;}
        int TypeSize(string id, float fallback=9)
        {
            ReferenceUiElement def=Definition(id);
            float native=def == null || def.fontSize<=0 ? fallback : def.fontSize;
            return Mathf.RoundToInt(native*(referenceReview ? 4 : 720f/referenceScreen.width));
        }
        Text TextAt(string id,string value,TextAnchor anchor=TextAnchor.MiddleLeft,Color? color=null,float fallback=9)
        {
            Text label=UiFactory.Label(referenceSurface,value,TypeSize(id,fallback),color ?? UiTheme.Ink,anchor,true);
            label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit=true;
            label.resizeTextMinSize=Mathf.RoundToInt(6*(referenceReview?4:720f/referenceScreen.width));label.resizeTextMaxSize=TypeSize(id,fallback);
            Place(id,label.rectTransform); return label;
        }
        Text ChildText(RectTransform parent,string value,float native=9,Color? color=null)
        {
            Text text=UiFactory.Label(parent,value,Mathf.RoundToInt(native*(referenceReview ? 4 : 720f/referenceScreen.width)),color ?? UiTheme.Ink,TextAnchor.MiddleCenter,true);
            UiFactory.Stretch(text.rectTransform); return text;
        }
        Button ActionAt(string id,string caption,UnityAction action,string icon=null,bool circle=false,bool blue=false)
        {
            Button button = circle ? UiFactory.CircleButton(id,referenceSurface,action,icon,caption) : blue ? UiFactory.ButtonBlue(id,referenceSurface,caption,action) : UiFactory.ButtonGreen(id,referenceSurface,caption,action,icon);
            Place(id,button.GetComponent<RectTransform>());
            foreach(Text text in button.GetComponentsInChildren<Text>()) text.fontSize=TypeSize(id,10);
            if(circle)
            {
                Text text=button.GetComponentInChildren<Text>();if(text!=null)UiFactory.Box(text.rectTransform,.1f,.02f,.9f,.28f);
                Transform image=button.transform.Find("Icon");if(image!=null)UiFactory.Box((RectTransform)image,.20f,.32f,.80f,.86f);
            }
            button.name="Action."+id;
            return button;
        }
        RectTransform Bar(string id,float fill)
        { return Place(id,UiFactory.ProgressBar(id,referenceSurface,fill,UiTheme.Green)); }
        double Count(string item) { return AdventureRules.Count(session.State.Adventure,item); }
        string Number(double value) { return value.ToString("N0"); }
        void Rebuild() { BuildMeasuredScreen(mode); Ping(); }

        void BuildMeasuredCurrency()
        {
            if(Definition("coin")==null) return;
            RectTransform coin=Card("coin",true),gem=Card("gem",true),gear=Card("settings",true);
            Image ci=UiFactory.Icon("Coin",coin,"icon_coin");UiFactory.Box(ci.rectTransform,.02f,.08f,.27f,.92f);
            coinPill=ChildText(coin,referenceReview ? "2,480" : NumericFormat.Format(session.State.Coins),9,Color.white);
            UiFactory.Box(coinPill.rectTransform,.26f,.02f,.96f,.98f);
            Image gi=UiFactory.Icon("Diamond",gem,"icon_diamond");UiFactory.Box(gi.rectTransform,.02f,.08f,.30f,.92f);
            gemPill=ChildText(gem,referenceReview ? "36" : Number(session.State.Adventure.LifetimeDiamondsRecovered),9,Color.white);
            UiFactory.Box(gemPill.rectTransform,.30f,.02f,.75f,.98f);
            Button plus=UiFactory.ButtonGreen("ResourceInfo",gem,"+",delegate { session.Note("Diamonds recovered: "+Number(session.State.Adventure.LifetimeDiamondsRecovered)+". Finished resources are in your backpack."); Open("bag"); });
            UiFactory.Box(plus.GetComponent<RectTransform>(),.78f,.08f,.98f,.92f);
            plus.GetComponentInChildren<Text>().fontSize=TypeSize("gem");
            Image icon=UiFactory.Icon("SettingsIcon",gear,"icon_gear");UiFactory.Box(icon.rectTransform,.12f,.12f,.88f,.88f);
            Button settings=gear.gameObject.AddComponent<Button>();settings.onClick.AddListener(delegate {if(ui!=null)ui.OpenSettings();});
        }

        void MeasuredExplore()
        {
            Image pad=Icon("joystick","joystick_base");pad.raycastTarget=true;
            stick=pad.gameObject.AddComponent<YardStick>();
            Image knob=Icon("joystick_knob","joystick_knob");
            // Reparent while preserving its measured rectangle; the stick moves from this local centre.
            Canvas.ForceUpdateCanvases();
            knob.rectTransform.SetParent(pad.transform,true);
            Vector2 size=knob.rectTransform.rect.size;
            knob.rectTransform.anchorMin=knob.rectTransform.anchorMax=new Vector2(.5f,.5f);
            knob.rectTransform.sizeDelta=size;
            knob.rectTransform.anchoredPosition=new Vector2((referenceReview?4:720f/referenceScreen.width)*.5f,0);
            stick.Knob=knob.rectTransform;stick.RestPosition=knob.rectTransform.anchoredPosition;
            RectTransform home=Card("home");
            Button h=home.gameObject.AddComponent<Button>();h.onClick.AddListener(delegate {session.GoHome();Ping();});
            Glyph("home_icon",ReferenceUiGlyph.Symbol.Home,UiTheme.Ink);
            TextAt("home_label","Return Home",TextAnchor.MiddleCenter);
            RectTransform action=Place("interact",UiFactory.Rect("Interact",referenceSurface));
            Image bg=action.gameObject.AddComponent<Image>();bg.sprite=UiFactory.GetSprite("btn_circle");
            Button b=action.gameObject.AddComponent<Button>();b.onClick.AddListener(OnInteract);b.name="Action.interact";
            Icon("interact_icon","icon_hand");
            interactLabel=TextAt("interact_label","Interact",TextAnchor.MiddleCenter,Color.white);
        }

        void MeasuredMine()
        {
            AdventureState a=session.State.Adventure;
            Card("inventory");TextAt("inventory_title","Ore Inventory",TextAnchor.MiddleCenter);
            string[] ids={JourneyCatalog.Stone,JourneyCatalog.DiamondOre,JourneyCatalog.SilverOre};
            string[] icons={"icon_ore","icon_diamond","icon_ore"}; int[] sample={18,27,6};
            for(int i=0;i<3;i++){Icon("ore_icon_"+i,icons[i]);TextAt("ore_count_"+i,referenceReview ? sample[i].ToString() : Number(Count(ids[i])),TextAnchor.MiddleCenter);}
            Card("carry");TextAt("carry_title","Carry");Icon("carry_icon","icon_sack");
            double carry=referenceReview ? 27 : a.HandsAmount;int capacity=referenceReview ? 40 : a.CarryCapacity;
            Bar("carry_bar",(float)(carry/Math.Max(1,capacity)));TextAt("carry_count",Number(carry)+" / "+capacity,TextAnchor.MiddleCenter);
            int plot=pendingPlot>=0 ? pendingPlot : WorldGenerator.CrystalPlot;
            if(!WorldGenerator.IsValid(plot)||!a.Owned.Get(plot))plot=0;pendingPlot=plot;
            PlotProfile profile=WorldGenerator.Describe(a.WorldSeed,plot,a.GeneratorVersion);
            string resource="";
            foreach(ReserveAmount reserve in profile.Reserves)
                if(AdventureRules.Remaining(a,plot,reserve.ItemId)>0&&(resource.Length==0||reserve.ItemId==JourneyCatalog.DiamondOre))resource=reserve.ItemId;
            string item=resource;int chosen=plot;
            ActionAt("mine_action","Mine",delegate {session.TryDig(chosen,item);Rebuild();},"icon_pickaxe",true).name="Action.Mine";
            if(!referenceReview)
            {
                Button cart=UiFactory.ButtonBlue("Action.SendCart",referenceSurface,"Send cart to mill",delegate {session.TrySendCart();Rebuild();});
                UiFactory.Box(cart.GetComponent<RectTransform>(),.035f,.025f,.44f,.105f);
                cart.GetComponentInChildren<Text>().fontSize=22;
            }
        }

        void MeasuredFactory()
        {
            string[] ids={"crush","wash","recover","cut","polish"},names={"Crush","Wash","Sort","Cut","Polish"};
            for(int i=0;i<5;i++)
            {
                string machine=ids[i];RectTransform stage=Card("stage_"+i);ChildText(stage,names[i],9);
                Button pick=stage.gameObject.AddComponent<Button>();pick.onClick.AddListener(delegate {selectedMachine=machine;ShowMachineDetails(machine);});
                RectTransform number=Flat("stage_number_"+i,new Color(.03f,.43f,.39f));ChildText(number,(i+1).ToString(),9,Color.white);
                if(i<4)Glyph("flow_arrow_"+i,ReferenceUiGlyph.Symbol.RightArrow,Color.white);
            }
            Card("inputs");TextAt("inputs_title","Inputs");
            string[] input={JourneyCatalog.DiamondOre,JourneyCatalog.RoughDiamond,JourneyCatalog.RefinedGold};string[] icons={"icon_ore","icon_diamond","icon_gold_bar"};int[] sample={120,86,4};
            for(int i=0;i<3;i++){Icon("input_icon_"+i,icons[i]);TextAt("input_count_"+i,referenceReview ? sample[i].ToString() : Number(Count(input[i])),TextAnchor.MiddleCenter);}
            Button upgrade=ActionAt("upgrade","Upgrade",delegate {session.TryImproveMachine(selectedMachine);Rebuild();});
            ReferenceUiGlyph up=ChildGlyph(upgrade.GetComponent<RectTransform>(),ReferenceUiGlyph.Symbol.UpArrow,Color.white);UiFactory.Box(up.rectTransform,.08f,.21f,.28f,.79f);
            UiFactory.Box(upgrade.GetComponentInChildren<Text>().rectTransform,.30f,.05f,.96f,.95f);
            if (!referenceReview)
            {
                Button refinery=UiFactory.ButtonBlue("Action.Refinery",referenceSurface,"Refinery",ShowRefineryDetails);
                UiFactory.Box(refinery.GetComponent<RectTransform>(),.025f,.93f,.29f,.99f);
            }
        }

        void ShowRefineryDetails()
        {
            if(referenceReview)return;
            selectedMachine="refine";
            RectTransform card=UiFactory.Card("RefineryDetails",referenceSurface);UiFactory.Box(card,.05f,.24f,.95f,.73f);
            machineDetailText=ChildText(card,"REFINERY\n"+AdventureRules.MachineStatus(session.State.Adventure.FindMachine("refine")),11);
            UiFactory.Box(machineDetailText.rectTransform,.06f,.70f,.94f,.96f);
            string[] ores={JourneyCatalog.CopperOre,JourneyCatalog.SilverOre,JourneyCatalog.GoldOre};
            string[] names={"Copper","Silver","Gold"};
            for(int i=0;i<ores.Length;i++)
            {
                string ore=ores[i];
                Button load=UiFactory.ButtonGreen("Action.Refine."+ore,card,"Refine "+names[i]+" ("+Number(Count(ore))+")",delegate{session.TryRefine(ore);Rebuild();});
                UiFactory.Box(load.GetComponent<RectTransform>(),.06f,.52f-i*.15f,.94f,.65f-i*.15f);
            }
            Button collect=UiFactory.ButtonBlue("Action.CollectRefinery",card,"Collect",delegate{session.TryCollectMachine("refine");Rebuild();});
            UiFactory.Box(collect.GetComponent<RectTransform>(),.06f,.04f,.65f,.16f);
            Button close=UiFactory.ButtonBlue("Action.CloseRefinery",card,"Close",Rebuild);UiFactory.Box(close.GetComponent<RectTransform>(),.69f,.04f,.94f,.16f);
            AddAccessibleHitAreas();
        }

        void ShowMachineDetails(string id)
        {
            if(referenceReview)return;
            MachineState m=session.State.Adventure.FindMachine(id);if(m==null)return;
            RectTransform card=UiFactory.Card("MachineDetails",referenceSurface);UiFactory.Box(card,.06f,.28f,.94f,.68f);
            Text detail=ChildText(card,id.ToUpperInvariant()+"\n"+AdventureRules.MachineStatus(m),11);UiFactory.Box(detail.rectTransform,.06f,.45f,.94f,.93f);machineDetailText=detail;
            Button load=UiFactory.ButtonGreen("Action.LoadMachine",card,"Load batch",delegate{session.TryStartBatch(id);Rebuild();});UiFactory.Box(load.GetComponent<RectTransform>(),.04f,.08f,.47f,.32f);
            Button collect=UiFactory.ButtonBlue("Action.CollectMachine",card,"Collect",delegate{session.TryCollectMachine(id);Rebuild();});UiFactory.Box(collect.GetComponent<RectTransform>(),.53f,.08f,.96f,.32f);
            Button close=UiFactory.ButtonBlue("CloseMachine",card,"×",Rebuild);UiFactory.Box(close.GetComponent<RectTransform>(),.88f,.85f,.99f,.99f);
            AddAccessibleHitAreas();
        }

        void MeasuredCraft()
        {
            string[] initial={"ring","necklace","earrings"};
            for(int i=0;i<3;i++)
            {
                JewelRecipe recipe=recipePage==0 ? JourneyCatalog.FindRecipe(initial[i]) : JourneyCatalog.AllRecipes[(recipePage+i)%JourneyCatalog.AllRecipes.Count];
                string recipeId=recipe.Id;
                RectTransform card=Card("recipe_"+i);
                if(recipeId==selectedRecipe){Outline line=card.gameObject.AddComponent<Outline>();line.effectColor=UiTheme.Diamond;line.effectDistance=new Vector2(2,-2);}
                Button select=card.gameObject.AddComponent<Button>();select.onClick.AddListener(delegate{selectedRecipe=recipeId;Rebuild();});
                TextAt("recipe_name_"+i,recipe.Id=="ring" ? "Ring" : recipe.Name,TextAnchor.MiddleCenter);
                Icon("recipe_icon_"+i,RecipeIcon(recipe.Id));
                RectTransform ingredients=Place("recipe_ingredients_"+i,UiFactory.Rect("Ingredients",referenceSurface));
                for(int j=0;j<Math.Min(2,recipe.Inputs.Count);j++)
                {
                    Ingredient ingredient=recipe.Inputs[j];
                    string sprite=referenceReview?(j==0?"icon_gold_bar":"icon_diamond"):ingredient.ItemId==JourneyCatalog.AnyDiamond?"icon_diamond":ingredient.ItemId==JourneyCatalog.RefinedGold?"icon_gold_bar":"icon_ore";
                    Image icon=UiFactory.Icon("Ingredient",ingredients,sprite);UiFactory.Box(icon.rectTransform,j*.5f,.05f,j*.5f+.24f,.95f);
                    int count=referenceReview?(j==0?(i==1?3:2):(i==0?1:2)):ingredient.Count;
                    Text number=ChildText(ingredients,count.ToString(),9);UiFactory.Box(number.rectTransform,j*.5f+.24f,.05f,j*.5f+.48f,.95f);
                }
            }
            RectTransform next=Card("next_recipe");ChildText(next,"›",15);Button n=next.gameObject.AddComponent<Button>();n.onClick.AddListener(delegate{recipePage=(recipePage+3)%JourneyCatalog.AllRecipes.Count;Rebuild();});
            Card("queue");TextAt("queue_title","Crafting Queue");
            List<CraftJob> jobs=new List<CraftJob>();foreach(CraftJob job in session.State.Adventure.Jobs)if(!job.OutputGranted)jobs.Add(job);
            for(int i=0;i<4;i++)
            {
                Flat("queue_slot_"+i,new Color(.86f,.82f,.72f,.5f));
                string label=referenceReview ? "" : (i<jobs.Count ? JourneyCatalog.FindRecipe(jobs[i].RecipeId).Name : "—");
                if((referenceReview&&i==0)||(!referenceReview&&i<jobs.Count))
                {Image icon=UiFactory.Icon("QueuedProduct",measured["queue_slot_"+i],RecipeIcon(referenceReview ? "ring" : jobs[i].RecipeId));UiFactory.Stretch(icon.rectTransform);}
                else if(referenceReview){ReferenceUiGlyph locked=ChildGlyph(measured["queue_slot_"+i],ReferenceUiGlyph.Symbol.Lock,UiTheme.Muted);UiFactory.Box(locked.rectTransform,.30f,.14f,.70f,.86f);}
                else ChildText(measured["queue_slot_"+i],label,11,UiTheme.Muted);
            }
            CraftJob first=jobs.Count>0?jobs[0]:null;double seconds=first==null?0:first.Remaining;float fill=first==null?0:1-(float)(seconds/JourneyCatalog.FindRecipe(first.RecipeId).Seconds);
            Bar("queue_progress",referenceReview?.62f:fill);TextAt("queue_time",referenceReview ? "12s" : seconds>0 ? seconds.ToString("0.0")+"s" : "Idle",TextAnchor.MiddleCenter);
            ActionAt("craft_action","Craft",delegate{session.TryMakeJewel(selectedRecipe);Rebuild();},"icon_pickaxe");
        }
        string RecipeIcon(string recipe) {return recipe=="necklace"?"icon_necklace":recipe=="earrings"?"icon_earrings":"icon_ring";}

        void MeasuredShop()
        {
            RectTransform request=Place("request",UiFactory.SpeechBubble("Request",referenceSurface));
            Icon("request_icon","icon_ring");
            Card("sale");Icon("product","icon_ring");TextAt("product_name","Diamond Ring");Icon("price_icon","icon_coin");
            TextAt("price",referenceReview ? "320" : JourneyCatalog.SellValue(JourneyCatalog.Ring).ToString());
            ActionAt("sell_action","Sell",delegate{session.TrySellGoods(JourneyCatalog.Ring,1);Rebuild();},"icon_coin");
            if(!referenceReview)
            {
                Button order=UiFactory.ButtonBlue("Action.FulfillOrder",referenceSurface,"Deliver current order",delegate{session.TryFulfillOrder();Rebuild();});UiFactory.Box(order.GetComponent<RectTransform>(),.08f,.02f,.92f,.085f);order.GetComponentInChildren<Text>().fontSize=22;
                string item=session.State.Adventure.OrderIndex<JourneyCatalog.AllOrders.Count ? JourneyCatalog.AllOrders[session.State.Adventure.OrderIndex].ItemId : JourneyCatalog.Ring;
                measured["request_icon"].GetComponent<Image>().sprite=UiFactory.GetSprite(item==JourneyCatalog.Ring?"icon_ring":item==JourneyCatalog.Stone?"icon_ore":"icon_gold_bar");
            }
        }

        void MeasuredMap()
        {
            AdventureState a=session.State.Adventure;
            selectedMapPlot=Mathf.Clamp(selectedMapPlot,0,WorldGenerator.PlotCount-1);
            int region=WorldGenerator.RegionOf(selectedMapPlot),page=(selectedMapPlot/12)*12;
            Card("region");ChildText(measured["region"],referenceReview ? "Sunridge Region" : WorldGenerator.ThemeName(region)+" Region",9);
            TextAt("region_previous","‹",TextAnchor.MiddleCenter);TextAt("region_next","›",TextAnchor.MiddleCenter);
            AddRectAction("region_previous",delegate{selectedMapPlot=Math.Max(0,selectedMapPlot-WorldGenerator.PlotsPerRegion);Rebuild();});
            AddRectAction("region_next",delegate{selectedMapPlot=Math.Min(WorldGenerator.PlotCount-1,selectedMapPlot+WorldGenerator.PlotsPerRegion);Rebuild();});
            Card("pin");ReferenceUiGlyph pin=ChildGlyph(measured["pin"],ReferenceUiGlyph.Symbol.Pin,UiTheme.Ink);UiFactory.Box(pin.rectTransform,.18f,.08f,.82f,.92f);
            Card("plot_total");ChildText(measured["plot_total"],WorldGenerator.PlotCount.ToString("N0")+" plots total",8);
            PlotProfile profile=WorldGenerator.Describe(a.WorldSeed,selectedMapPlot,a.GeneratorVersion);
            Card("purchase");TextAt("plot_title",referenceReview ? "Crystal Ridge Mine" : profile.Theme+" Mine "+(selectedMapPlot+1),TextAnchor.MiddleCenter);
            string[] icons={"icon_diamond","icon_ore","icon_gold_bar"};string[] sample={"40–90","20–60","5–15"};
            for(int i=0;i<3;i++)
            {
                Icon("reserve_icon_"+i,icons[i]);
                string count=i<profile.Reserves.Count ? AdventureRules.Remaining(a,selectedMapPlot,profile.Reserves[i].ItemId).ToString("0") : "—";
                TextAt("reserve_value_"+i,referenceReview ? sample[i] : count,TextAnchor.MiddleCenter);
            }
            TextAt("plot_price",referenceReview ? "500" : AdventureRules.PlotPrice(a,selectedMapPlot).ToString("0"),TextAnchor.MiddleCenter,UiTheme.Danger);
            bool owned=a.Owned.Get(selectedMapPlot);ActionAt("buy_action",!referenceReview&&owned?"Owned":"Buy",delegate{session.TryPurchasePlot(selectedMapPlot);Rebuild();}).interactable=referenceReview||!owned;
            Card("navigation");ChildText(measured["navigation"],(page+1)+" – "+Math.Min(WorldGenerator.PlotCount,page+12)+" / "+WorldGenerator.PlotCount.ToString("N0"),9);
            TextAt("page_previous","‹",TextAnchor.MiddleCenter);TextAt("page_next","›",TextAnchor.MiddleCenter);
            AddRectAction("page_previous",delegate{selectedMapPlot=Math.Max(0,selectedMapPlot-12);Rebuild();});AddRectAction("page_next",delegate{selectedMapPlot=Math.Min(WorldGenerator.PlotCount-1,selectedMapPlot+12);Rebuild();});
            for(int i=0;i<6;i++)
            {
                int plot=page+i;RectTransform marker=Flat("marker_"+i,new Color(.02f,.48f,.46f));ChildText(marker,(referenceReview?i+1:plot+1).ToString(),10,Color.white);
                AddRectAction("marker_"+i,delegate{selectedMapPlot=plot;Rebuild();});
            }
        }
        void AddRectAction(string id,UnityAction action)
        {
            RectTransform rect=measured[id];Graphic graphic=rect.GetComponent<Graphic>();if(graphic!=null)graphic.raycastTarget=true;
            Button button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(action);
        }

        void MeasuredTasks()
        {
            Card("panel");Card("tabs",true);
            string[] tabs={"Tasks","Upgrades","Milestones"};
            for(int i=0;i<3;i++){int index=i;bool active=referenceReview?i==0:i==selectedProgressGroup;RectTransform tab=Card("tab_"+i,!active);ChildText(tab,tabs[i],9,active?UiTheme.Ink:Color.white);AddRectAction("tab_"+i,delegate{selectedProgressGroup=index;Rebuild();});}
            AdventureState a=session.State.Adventure;
            string[] taskIds={"ore20","polish1","ring1"},demoTitle={"Collect ore","Polish diamond","Craft ring"},icons={"icon_ore","icon_diamond","icon_ring"};
            double[] current={a.OreCollected,a.PolishCount,a.RingsCrafted},targets={20,1,1};int[] demoCurrent={40,8,2},demoTarget={50,10,5},demoReward={50,60,80};
            if(!referenceReview&&selectedProgressGroup==2)
            {taskIds=new[]{"visit5","orders3","region2"};current=new double[]{a.VisitedStarters.Count,a.OrdersCompleted,a.DiscoveredRegions.Contains(1)?1:0};targets=new double[]{5,3,1};}
            for(int i=0;i<3;i++)
            {
                if(!referenceReview&&selectedProgressGroup==1)
                {
                    string machine=new[]{"recover","cut","polish"}[i];MachineState item=a.FindMachine(machine);int lv=item==null?0:item.Level;
                    Card("task_row_"+i);Icon("task_icon_"+i,"icon_gear");TextAt("task_title_"+i,machine=="recover"?"Sorter":machine=="cut"?"Cutter":"Polisher");
                    TextAt("task_progress_"+i,"Lv "+lv+" / 3");Bar("task_bar_"+i,lv/3f);Icon("task_reward_icon_"+i,"icon_coin");TextAt("task_reward_"+i,lv>=3?"Max":(100*(lv+1)).ToString(),TextAnchor.MiddleCenter);
                    ActionAt("task_action_"+i,"Upgrade",delegate{session.TryImproveMachine(machine);Rebuild();}).interactable=lv<3;
                    continue;
                }
                int index=i;string task=taskIds[i];YardTaskDef def=JourneyCatalog.FindTask(task);Card("task_row_"+i);Icon("task_icon_"+i,icons[i]);
                TextAt("task_title_"+i,referenceReview?demoTitle[i]:def.Title);
                double value=referenceReview?demoCurrent[i]:current[i],target=referenceReview?demoTarget[i]:targets[i];
                TextAt("task_progress_"+i,Number(value)+" / "+Number(target));Bar("task_bar_"+i,(float)(value/target));
                Icon("task_reward_icon_"+i,"icon_coin");TextAt("task_reward_"+i,(referenceReview?demoReward[i]:def.Reward).ToString(),TextAnchor.MiddleCenter);
                bool claimed=a.ClaimedTasks.Contains(task),ready=AdventureRules.TaskReady(session.State,task);
                ActionAt("task_action_"+i,referenceReview?(i<2?"Claim":"Go"):claimed?"Done":ready?"Claim":"Go",delegate{if(ready)session.TryClaimYardTask(task);else Open(index==0?"mine":index==1?"factory":"craft");Rebuild();},null,false,!referenceReview&&!ready||referenceReview&&i==2).interactable=!claimed;
            }
            TextAt("upgrades_title","Upgrades");
            // Simulation currently upgrades production machines. Live labels and costs state that honestly.
            string[] names=referenceReview?new[]{"Pickaxe","Mine Cart","Crusher"}:new[]{"Crusher","Wash tank","Sorter"};string[] machines={"crush","wash","recover"};
            for(int i=0;i<3;i++)
            {
                string machine=machines[i];MachineState state=a.FindMachine(machine);int level=state==null?0:state.Level;
                Card("upgrade_card_"+i);TextAt("upgrade_name_"+i,names[i],TextAnchor.MiddleCenter);
                Icon("upgrade_icon_"+i,i==0?"icon_pickaxe":i==1?"icon_cart_diamonds":"icon_crusher");
                TextAt("upgrade_level_"+i,referenceReview?(i==2?"Lv 3 > 4":"Lv 2 > 3"):"Lv "+level+" > "+Math.Min(3,level+1),TextAnchor.MiddleCenter);
                TextAt("upgrade_cost_"+i,referenceReview?(new[]{150,200,400}[i]).ToString():level>=3?"Max":(100*(level+1)).ToString(),TextAnchor.MiddleCenter,UiTheme.Coin);
                ActionAt("upgrade_action_"+i,"Upgrade",delegate{session.TryImproveMachine(machine);Rebuild();}).interactable=referenceReview||level<3;
            }
        }

        void MeasuredRank()
        {
            Card("tabs",true);Card("leaders");ChildText(measured["leaders"],referenceReview?"Diamond Leaders":"Your offline record",9);
            ChildText(Card("friends",true),referenceReview?"Friends":"Local only",9,Color.white);
            Card("table");TextAt("column_rank","#");TextAt("column_player","Player");TextAt("column_total","Diamonds Recovered",TextAnchor.MiddleRight,UiTheme.Ink,7);
            string[] aliases={"CrystalFox","MineMaven","JadeRaccoon","SunnyPick","BlueOtter","You","PebblePanda","GemWhale","MapleDigger","LuckyLynx"};
            int[] scores={12480,9320,7860,6410,5900,4820,4120,3700,3260,2940};
            // Aliases and totals are review-only examples; portraits come from actual imported crew models.
            string[] portraits={"tavi","mira","oren","zela","pavi","tavi","nilo","rumi","etta","daro"};
            string selected=session.State.Adventure.SelectedCharacter;
            if(string.IsNullOrEmpty(selected))selected="tavi";
            for(int i=0;i<10;i++)
            {
                Flat("row_"+i,i==5&&referenceReview?new Color(.48f,.84f,.95f):i%2==0?UiTheme.Paper:Color.white);
                bool visible=referenceReview||i==0;
                TextAt("row_rank_"+i,referenceReview?(i+1).ToString():i==0?"—":"");
                Image avatar=Icon("row_avatar_"+i,"crew_avatar_"+(referenceReview?portraits[i]:selected));avatar.enabled=visible;
                TextAt("row_alias_"+i,referenceReview?aliases[i]:i==0?"You":"");
                TextAt("row_total_"+i,referenceReview?scores[i].ToString("N0"):i==0?Number(session.State.Adventure.LifetimeDiamondsRecovered):"",TextAnchor.MiddleRight);
            }
            if(!referenceReview)
            {
                Text message=UiFactory.Label(referenceSurface,"No online rankings or Friends service is connected.\nYour record is stored on this device.",22,UiTheme.Muted,TextAnchor.MiddleCenter);
                UiFactory.Box(message.rectTransform,.12f,.40f,.88f,.69f);
            }
            Card("offline");Icon("offline_cart","icon_cart_diamonds");TextAt("offline_title",referenceReview?"Offline Earnings":"Offline coins settled");
            TextAt("offline_amount",referenceReview?"+320":"+"+Number(session.OfflineReport==null?0:session.OfflineReport.Coins),TextAnchor.MiddleCenter,UiTheme.Diamond,15);
            ActionAt("collect_action",referenceReview?"Collect":"Got it",delegate{session.DismissOfflineReport();session.Note("Offline coins were already credited once when this save loaded.");Rebuild();});
        }

        public ReferenceUiReport MeasureReferenceLayout()
        {
            if(referenceScreen==null)return null;
            Canvas.ForceUpdateCanvases();Vector3[] canvasCorners=new Vector3[4];referenceSurface.GetWorldCorners(canvasCorners);
            float width=canvasCorners[2].x-canvasCorners[0].x,height=canvasCorners[2].y-canvasCorners[0].y;
            var report=new ReferenceUiReport{screen=referenceScreen.id,source=referenceManifest.source,sourceHash=referenceManifest.sourceHash,sourceWidth=referenceScreen.width,sourceHeight=referenceScreen.height,viewportWidth=Screen.width,viewportHeight=Screen.height,referenceReview=referenceReview};
            var rows=new List<ReferenceUiBound>();
            foreach(ReferenceUiElement def in referenceScreen.elements)
            {
                var row=new ReferenceUiBound{id=def.id,expected=def.rect,measurementUncertaintyPixels=def.uncertaintyPixels};RectTransform actual;
                row.present=measured.TryGetValue(def.id,out actual)&&actual!=null;
                if(row.present)
                {
                    Vector3[] corners=new Vector3[4];actual.GetWorldCorners(corners);
                    row.actual=new[]{(corners[0].x-canvasCorners[0].x)/width,(canvasCorners[2].y-corners[2].y)/height,(corners[2].x-corners[0].x)/width,(corners[2].y-corners[0].y)/height};
                    row.error=new float[4];row.withinTarget=true;
                    for(int i=0;i<4;i++){row.error[i]=Mathf.Abs(row.actual[i]-row.expected[i]);row.withinTarget &= row.error[i]<=report.target;}
                }
                rows.Add(row);
            }
            report.elements=rows.ToArray();return report;
        }
    }
}
