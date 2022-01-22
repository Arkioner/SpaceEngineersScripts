using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        Dictionary<MyItemType, ItemToRefine> OresInventory;
        private readonly List<IMyRefinery> Refineries = new List<IMyRefinery>();
        private readonly List<IMyCargoContainer> Containers = new List<IMyCargoContainer>();
        private IMyTextPanel TextPanel;
        private readonly float speedMultiplier = 8 * 5f;
        private readonly String TextPanelName = "Refinery CountDown Panel";

        IEnumerator<bool> _stateMachine;
        public Program()
        {
            _stateMachine = RunStuffOverTime();
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(Refineries);
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(Containers);
            TextPanel = GridTerminalSystem.GetBlockWithName(TextPanelName) as IMyTextPanel;
        }

        public IEnumerator<bool> RunStuffOverTime()
        {
            while (true)
            {
                InitOresInfo();
                yield return true;
                foreach (var refinery in Refineries)
                {
                    CountRefineryOre(refinery);
                }
                yield return true;
                foreach (var container in Containers)
                {
                    CountContainerOre(container);
                }
                yield return true;
                float secondsToRefine = 0;
                foreach (var itemToRefine in OresInventory)
                {
                    secondsToRefine += itemToRefine.Value.GetTotalAmountOfTime();
                }
                float bonifiedSecondsToRefine = secondsToRefine / (speedMultiplier * Refineries.Count);
                float speedBonifiedSecondsToRefine = secondsToRefine / (speedMultiplier * Refineries.Count * 7);
                var TotalTimeToRefine = TimeSpan.FromSeconds(bonifiedSecondsToRefine);
                var TotalTimeToRefineWithSpeedModules = TimeSpan.FromSeconds(speedBonifiedSecondsToRefine);
                var outputText = "Total time left:\nWith yield modules:\n" + TotalTimeToRefine.Days + " Days\n" + TotalTimeToRefine.Hours + " Hours\n" + TotalTimeToRefine.Minutes + " Minutes\n" + TotalTimeToRefine.Seconds + " Seconds";
                var outputTextWithSpeed = "With speed modules:\n" + TotalTimeToRefineWithSpeedModules.Days + " Days\n" + TotalTimeToRefineWithSpeedModules.Hours + " Hours\n" + TotalTimeToRefineWithSpeedModules.Minutes + " Minutes\n" + TotalTimeToRefineWithSpeedModules.Seconds + " Seconds";
                if (TextPanel != null)
                {
                    TextPanel.WriteText(outputText + "\n" + outputTextWithSpeed);
                } else
                {
                    Echo("Set up a lcd screen with name: " + TextPanelName);
                }
                Echo(outputText + "\n" + outputTextWithSpeed);
                yield return true;
            }
        }
        public void Main(string argument, UpdateType updateType)
        {
            if ((updateType & UpdateType.Once) == UpdateType.Once)
            {
                RunStateMachine();
            }
        }

        public void RunStateMachine()
        {
            if (_stateMachine != null)
            {
                bool hasMoreSteps = _stateMachine.MoveNext();

                if (hasMoreSteps)
                {
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
                else
                {
                    _stateMachine.Dispose();

                    _stateMachine = RunStuffOverTime();
                }
            }
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        private void CountRefineryOre(IMyRefinery refinery)
        {
            var inventoryItems = new List<MyInventoryItem>();
            refinery.InputInventory.GetItems(inventoryItems);
            CountOre(inventoryItems);
        }

        private void CountContainerOre(IMyCargoContainer container)
        {
            var inventoryItems = new List<MyInventoryItem>();
            container.GetInventory().GetItems(inventoryItems, item => OresInventory.ContainsKey(item.Type));
            CountOre(inventoryItems);
        }

        private void CountOre(List<MyInventoryItem> items)
        {
            foreach (var ore in items)
            {
                OresInventory[ore.Type].AddAmount(ore.Amount);
            }
        }

        private void InitOresInfo()
        {
            OresInventory = new Dictionary<MyItemType, ItemToRefine>()
            {
                 {new MyItemType("MyObjectBuilder_Ore","Cobalt"), new ItemToRefine("Cobalt", 2.308f)},
                 {new MyItemType("MyObjectBuilder_Ore","Gold"), new ItemToRefine("Gold", 0.308f)},
                 {new MyItemType("MyObjectBuilder_Ore","Iron"), new ItemToRefine("Iron", 0.038f)},
                 {new MyItemType("MyObjectBuilder_Ore","Magnesium"), new ItemToRefine("Magnesium", 0.385f)},
                 {new MyItemType("MyObjectBuilder_Ore","Nickel"), new ItemToRefine("Nickel", 0.508f)},
                 {new MyItemType("MyObjectBuilder_Ore","Platinum"), new ItemToRefine("Platinum", 2.308f)},
                 {new MyItemType("MyObjectBuilder_Ore","Silicon"), new ItemToRefine("Silicon", 0.462f)},
                 {new MyItemType("MyObjectBuilder_Ore","Silver"), new ItemToRefine("Silver", 0.769f)},
                 {new MyItemType("MyObjectBuilder_Ore","Stone"), new ItemToRefine("Stone", 0.008f)},
                 {new MyItemType("MyObjectBuilder_Ore","Uranium"), new ItemToRefine("Uranium", 3.077f)}
            };
        }
        class ItemToRefine
        {
            public readonly String name;
            private MyFixedPoint amount;
            private readonly float secondsToRefineKg;

            public ItemToRefine(String name, float secondsToRefineKg)
            {
                this.name = name;
                this.amount = MyFixedPoint.Zero;
                this.secondsToRefineKg = secondsToRefineKg;
            }

            public void AddAmount(MyFixedPoint amountToAdd)
            {
                amount += amountToAdd;
            }

            public float GetAmount()
            {
                return ((float)amount);
            }

            public float GetTotalAmountOfTime()
            {
                return ((float)amount) * secondsToRefineKg;
            }
        }
    }
}
