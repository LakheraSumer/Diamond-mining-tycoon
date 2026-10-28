using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DiamondMine.Runtime;
using DiamondMine.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiamondMine.Editor
{
    // This fixture runs only in the editor's private QA save. It is never a player component.
    public static class UiGameplayAssertions
    {
        public static void Run(GameSession session,YardControls controls,Action<string,bool> check)
        {
            string save=Environment.GetEnvironmentVariable("DIAMOND_REVIEW_SAVE_ROOT")??"";
            if(!save.Contains("qa-saves"))throw new InvalidOperationException("UI fixture requires an isolated QA save.");
            session.State.Adventure.Inventory.Clear();
            session.State.Adventure.Inventory[JourneyCatalog.DiamondOre]=4;
            session.State.Adventure.Inventory[JourneyCatalog.SilverOre]=2;
            string[] machines={"crush","wash","recover","cut","polish"};
            string[] inputs={JourneyCatalog.DiamondOre,JourneyCatalog.CrushedOre,JourneyCatalog.WashedOre,JourneyCatalog.RoughDiamond,JourneyCatalog.CutDiamond};
            string polished="";
            for(int i=0;i<machines.Length;i++)
            {
                OpenMachine(controls,i);double before=Count(session,inputs[i]);Click(controls,"Action.LoadMachine");
                MachineState machine=session.State.Adventure.FindMachine(machines[i]);double taken=machine.InputAmount;
                check(machines[i]+" UI starts a real batch and consumes exact inputs",machine.BusyUnits>0&&taken>0&&Count(session,inputs[i])==before-taken);
                OpenMachine(controls,i);Click(controls,"Action.LoadMachine");
                check(machines[i]+" repeated Load does not consume twice",Count(session,inputs[i])==before-taken);
                AdventureRules.Advance(session.State,session.State.Adventure.FindMachine(machines[i]).BusySeconds+.01);
                machine=session.State.Adventure.FindMachine(machines[i]);string output=machine.OutputItem;double amount=machine.OutputAmount,initial=Count(session,output);
                OpenMachine(controls,i);Click(controls,"Action.CollectMachine");
                check(machines[i]+" UI collects exact output",amount>0&&Count(session,output)==initial+amount);
                OpenMachine(controls,i);Click(controls,"Action.CollectMachine");
                check(machines[i]+" repeated Collect does not duplicate output",Count(session,output)==initial+amount);
                if(i==4)polished=output;
            }
            OpenRefinery(controls);Click(controls,"Action.Refine."+JourneyCatalog.SilverOre);
            check("Refinery UI consumes two real silver ore",Count(session,JourneyCatalog.SilverOre)==0&&session.State.Adventure.FindMachine("refine").BusyUnits==2);
            AdventureRules.Advance(session.State,session.State.Adventure.FindMachine("refine").BusySeconds+.01);
            OpenRefinery(controls);Click(controls,"Action.CollectRefinery");
            check("Refinery UI grants one silver bar",Count(session,JourneyCatalog.RefinedSilver)==1);
            OpenRefinery(controls);Click(controls,"Action.CollectRefinery");
            check("Repeated refinery collection does not duplicate silver",Count(session,JourneyCatalog.RefinedSilver)==1);
            controls.OpenScreen("craft");Click(controls,"Reference.recipe_0");Click(controls,"Action.craft_action");
            check("Craft UI consumes the diamond and silver once",Count(session,polished)==0&&Count(session,JourneyCatalog.RefinedSilver)==0&&session.State.Adventure.Jobs.Count(j=>!j.OutputGranted)==1);
            Click(controls,"Action.craft_action");
            check("Repeated Craft with empty materials does not enqueue twice",session.State.Adventure.Jobs.Count(j=>!j.OutputGranted)==1);
            AdventureRules.Advance(session.State,JourneyCatalog.FindRecipe("ring").Seconds+.01);
            AdventureRules.Advance(session.State,10);
            check("Craft completion grants one ring once",Count(session,JourneyCatalog.Ring)==1&&session.State.Adventure.RingsCrafted==1);
            controls.OpenScreen("shop");double coins=session.State.Coins;Click(controls,"Action.sell_action");
            check("Sell UI removes one ring and credits the catalogue price",Count(session,JourneyCatalog.Ring)==0&&session.State.Coins==coins+JourneyCatalog.SellValue(JourneyCatalog.Ring));
            coins=session.State.Coins;Click(controls,"Action.sell_action");
            check("Repeated Sell with no stock does not credit twice",session.State.Coins==coins);
            session.State.Adventure.OreCollected=20;
            controls.OpenScreen("tasks");coins=session.State.Coins;Click(controls,"Action.task_action_0");
            check("Task UI claims its authoritative reward once",session.State.Coins==coins+JourneyCatalog.FindTask("ore20").Reward&&session.State.Adventure.ClaimedTasks.Contains("ore20"));
            Button claimed=Find(controls,"Action.task_action_0");check("Claimed task button is disabled",!claimed.interactable);
            coins=session.State.Coins;claimed.onClick.Invoke();check("Replayed task callback cannot duplicate reward",session.State.Coins==coins);
            string path=(string)typeof(GameSession).GetField("savePath",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
            LoadOutcome restored=new SaveGameStore(new SystemSaveIO(),path).Load(session.Config);
            check("Actual QA save reload retains sale and claimed task",restored.State!=null&&restored.State.Coins==coins&&restored.State.Adventure.ClaimedTasks.Contains("ore20"));
            AdventureRules.Advance(restored.State,20);
            check("Reload cannot recreate the sold ring or refinery output",AdventureRules.Count(restored.State.Adventure,JourneyCatalog.Ring)==0&&AdventureRules.Count(restored.State.Adventure,JourneyCatalog.RefinedSilver)==0);
            controls.OpenScreen("explore");
        }
        static double Count(GameSession session,string id){return AdventureRules.Count(session.State.Adventure,id);}
        static void OpenMachine(YardControls controls,int index){controls.OpenScreen("factory");Click(controls,"Reference.stage_"+index);}
        static void OpenRefinery(YardControls controls){controls.OpenScreen("factory");Click(controls,"Action.Refinery");}
        static Button Find(YardControls controls,string name)
        {
            Button found=controls.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.name==name);
            if(found==null)throw new InvalidOperationException("Missing active UI action: "+name);
            return found;
        }
        public static void Click(YardControls controls,string name)
        {
            Button button=Find(controls,name);
            if(!button.interactable)throw new InvalidOperationException("UI action disabled: "+name);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
    }
}
