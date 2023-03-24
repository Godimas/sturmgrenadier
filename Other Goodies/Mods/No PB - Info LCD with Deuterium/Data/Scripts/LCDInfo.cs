using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace EconomySurvival.LCDInfo
{
    class cargoItemType
    {
        public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        public int amount;
    }

    [MyTextSurfaceScript("LCDInfoScreen", "ES Info LCD w/ Deuterium")]
    public class LCDInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        List<IMyPowerProducer> hydroenEngines = new List<IMyPowerProducer>();
		List<IMyPowerProducer> fuelCells = new List<IMyPowerProducer>();
        List<IMyPowerProducer> fusionReactors = new List<IMyPowerProducer>();
        List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();

        List<IMyInventory> inventorys = new List<IMyInventory>();
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        Dictionary<string, cargoItemType> cargoOres = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoIngots = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoComponents = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoAmmos = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoHandWeaponAmmos = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoBottles = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoWeapons = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoConsumables = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoFoods = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoTools = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoItems = new Dictionary<string, cargoItemType>();


        Vector2 right;
        Vector2 newLine;
        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        float textSize = 1.0f;

        bool ConfigCheck = false;

        public LCDInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Dispose()
        {

        }

        public override void Run()
        {
            if (myTerminalBlock.CustomData.Length <= 0)
                CreateConfig();

            LoadConfig();

            if (!ConfigCheck)
                return;

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);

            batteryBlocks.Clear();
            windTurbines.Clear();
            hydroenEngines.Clear();
			fuelCells.Clear();
            fusionReactors.Clear();
            solarPanels.Clear();
            reactors.Clear();
            inventorys.Clear();
            tanks.Clear();

            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock is IMyBatteryBlock)
                {
                    batteryBlocks.Add((IMyBatteryBlock)myBlock);
                }
                else if (myBlock is IMyPowerProducer)
                {
                    if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                    {
                        windTurbines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                    {
                        hydroenEngines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Fuelcell"))
                    {
                        fuelCells.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Fusion"))
                    {
                        fusionReactors.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock is IMyReactor)
                    {
                        reactors.Add((IMyReactor)myBlock);
                    }
                    else if (myBlock is IMySolarPanel)
                    {
                        solarPanels.Add((IMySolarPanel)myBlock);
                    }
                }
                else if (myBlock is IMyGasTank)
                {
                    tanks.Add((IMyGasTank)myBlock);
                }

                if (myBlock.HasInventory)
                {
                    for (int i = 0; i < myBlock.InventoryCount; i++)
                    {
                        inventorys.Add(myBlock.GetInventory(i));
                    }
                }
            }

            cargoOres.Clear();
            cargoIngots.Clear();
            cargoComponents.Clear();
            cargoAmmos.Clear();
	    cargoHandWeaponAmmos.Clear();
            cargoBottles.Clear();
            cargoWeapons.Clear();
            cargoConsumables.Clear();
            cargoFoods.Clear();
            cargoTools.Clear();
            cargoItems.Clear();

            foreach (var inventory in inventorys)
            {
                if (inventory.ItemCount == 0)
                    continue;

                inventoryItems.Clear();
                inventory.GetItems(inventoryItems);

                foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                {
                    var type = item.Type.TypeId.Split('_')[1];
					var subtypename = item.Type.SubtypeId.Split('_')[0];
                    var name = item.Type.SubtypeId;
                    var amount = item.Amount.ToIntSafe();

                    var myType = new cargoItemType { item=item, amount=0 };

                    if (subtypename.Contains("Meat") ^ subtypename.Contains("Apple") ^ subtypename.Contains("Soup") ^ subtypename.Contains("Chips") ^ subtypename == "Bits's" ^ subtypename == "Bread" ^ subtypename == "Burger" ^ subtypename == "Cabbage" ^ subtypename == "ClangCola" ^ subtypename == "CosmicCoffee" ^ subtypename == "Emergency_Ration" ^ subtypename == "EuropaTea" ^ subtypename == "Fendom_Fries" ^ subtypename == "Feines_Essen" ^ subtypename == "Herbs" ^ subtypename == "InterBeer" ^ subtypename == "Kosmit_Kola" ^ subtypename == "Medik_Vodka" ^ subtypename == "Mushrooms" ^ subtypename == "N1roos" ^ subtypename == "Pickled_FatFlies" ^ subtypename == "Potato" ^ subtypename == "Pumpkin" ^ subtypename == "Rabenswild" ^ subtypename == "Rembrau" ^ subtypename == "Sektans_Jednosladová" ^ subtypename == "Sixdiced_Stew" ^ subtypename == "Soya" ^ subtypename == "SparklingWater" ^ subtypename == "ShroomSteak" ^ subtypename == "Tofu" ^ subtypename == "Wheat") 
                    {
                        if (!cargoFoods.ContainsKey(name))
                            cargoFoods.Add(name, myType);

                        cargoFoods[name].amount += amount;
                    }
					else if (type == "Ore")
                    {
                        if (!cargoOres.ContainsKey(name))
                            cargoOres.Add(name, myType);

                        cargoOres[name].amount += amount;
                    }
                    else if (type == "Ingot")
                    {
                        if (!cargoIngots.ContainsKey(name))
                            cargoIngots.Add(name, myType);

                        cargoIngots[name].amount += amount;
                    }
                    else if (type == "Component")
                    {
                        if (!cargoComponents.ContainsKey(name))
                            cargoComponents.Add(name, myType);

                        cargoComponents[name].amount += amount;
                    }
                    else if (subtypename.Contains("SHELLS") ^ subtypename == "DAKKA")
                    {
                        if (!cargoHandWeaponAmmos.ContainsKey(name))
                            cargoHandWeaponAmmos.Add(name, myType);

                        cargoHandWeaponAmmos[name].amount += amount;
                    }
                    else if (type == "AmmoMagazine")
                    {
                        if (!cargoAmmos.ContainsKey(name))
                            cargoAmmos.Add(name, myType);

                        cargoAmmos[name].amount += amount;
                    }
                    else if (type == "GasContainerObject" ^ type == "OxygenContainerObject")
                    {
                        if (!cargoBottles.ContainsKey(name))
                            cargoBottles.Add(name, myType);

                        cargoBottles[name].amount += amount;
                    }
                    else if (subtypename == "BinocularsItem" ^ subtypename == "PhysicalPaintGun" ^ subtypename.Contains("HandDrill") ^ subtypename.Contains("Welder") ^ subtypename.Contains("Grinder")) 
                    {
                        if (!cargoTools.ContainsKey(name))
                            cargoTools.Add(name, myType);

                        cargoTools[name].amount += amount;
                    }
                    else if (type == "PhysicalGunObject")
                    {
                        if (!cargoWeapons.ContainsKey(name))
                            cargoWeapons.Add(name, myType);

                        cargoWeapons[name].amount += amount;
                    }
                    else if (type == "ConsumableItem")
                    {
                        if (!cargoConsumables.ContainsKey(name))
                            cargoConsumables.Add(name, myType);

                        cargoConsumables[name].amount += amount;
                    }
                    else
                    {
                        if (!cargoItems.ContainsKey(name))
                            cargoItems.Add(name, myType);

                        cargoItems[name].amount += amount;
                    }
                }
            }

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(5, 5) + myViewport.Position;

            textSize = config.Get("Settings", "TextSize").ToSingle(defaultValue: 1.0f);
            right = new Vector2(mySurface.SurfaceSize.X - 10, 0);
            newLine = new Vector2(0, 30 * textSize);
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (config.Get("Settings", "Battery").ToBoolean())
                DrawBatterySprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "WindTurbine").ToBoolean())
                DrawWindTurbineSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "HydrogenEngine").ToBoolean())
                DrawHydrogenEngineSprite(ref myFrame, ref myPosition, mySurface);
				
            if (config.Get("Settings", "FuelCell").ToBoolean())
                DrawFuelCellSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get("Settings", "FusionReactor").ToBoolean())
                DrawFusionReactorSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Tanks").ToBoolean())
                DrawTanksSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Solar").ToBoolean())
                DrawSolarPanelSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "NuclearReactor").ToBoolean())
                DrawReactorSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Ore").ToBoolean())
                DrawOreSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Ingot").ToBoolean())
                DrawIngotSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Component").ToBoolean())
                DrawComponentSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "VehicleAmmo").ToBoolean())
                DrawAmmoSprite(ref myFrame, ref myPosition, mySurface);
		
		if (config.Get("Settings", "HandWeaponAmmo").ToBoolean())
                DrawHandWeaponAmmoSprite(ref myFrame, ref myPosition, mySurface);
				
            if (config.Get("Settings", "Bottles").ToBoolean())
                DrawBottleSprite(ref myFrame, ref myPosition, mySurface);
				
            if (config.Get("Settings", "Hand Weapons").ToBoolean())
                DrawWeaponSprite(ref myFrame, ref myPosition, mySurface);
				
            if (config.Get("Settings", "Consumables").ToBoolean())
                DrawConsumableSprite(ref myFrame, ref myPosition, mySurface);
				
            if (config.Get("Settings", "Food").ToBoolean())
                DrawFoodSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get("Settings", "Tools").ToBoolean())
                DrawToolSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Miscellaneous Items").ToBoolean())
                DrawItemsSprite(ref myFrame, ref myPosition, mySurface);

            myFrame.Dispose();
        }

        void DrawBatterySprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = batteryBlocks.Sum(block => block.CurrentStoredPower);
            var total = batteryBlocks.Sum(block => block.MaxStoredPower);
            var input = batteryBlocks.Sum(block => block.CurrentInput);
            var output = batteryBlocks.Sum(block => block.CurrentOutput);

            WriteTextSprite(ref frame, "[ BATTERIES ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Stored Power:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MWh", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Stored Power:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MWh", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Input:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, input.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, output.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawWindTurbineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = windTurbines.Sum(block => block.CurrentOutput);
            var currentMax = windTurbines.Sum(block => block.MaxOutput);
            var total = windTurbines.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

            WriteTextSprite(ref frame, "[ WIND TURBINES ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawHydrogenEngineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = hydroenEngines.Sum(block => block.CurrentOutput);
            var total = hydroenEngines.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ HYDROGEN ENGINES ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }
		
        void DrawFuelCellSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = fuelCells.Sum(block => block.CurrentOutput);
            var total = fuelCells.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ HYDROGEN FUEL CELLS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }
		
        void DrawFusionReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = fusionReactors.Sum(block => block.CurrentOutput);
            var total = fusionReactors.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ FUSION REACTORS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var hydrogenTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
            var oxygenTanks = tanks.Where(block => ((!block.BlockDefinition.SubtypeName.Contains("Hydrogen")) && (!block.BlockDefinition.SubtypeName.Contains("Deuterium"))));
            var deuteriumTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Deuterium"));

            var currentHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Average(block => block.FilledRatio * 100);
            var totalHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Sum(block => block.Capacity);

            var currentOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Average(block => block.FilledRatio * 100);
            var totalOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Sum(block => block.Capacity);
            
            var currentDeuterium = deuteriumTanks.Count() == 0 ? 0 : deuteriumTanks.Average(block => block.FilledRatio * 100);
            var totalDeuterium = deuteriumTanks.Count() == 0 ? 0 : deuteriumTanks.Sum(block => block.Capacity);

            WriteTextSprite(ref frame, "[ HYDROGEN TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentHydrogen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalHydrogen), position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "[ OXYGEN TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentOxygen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalOxygen), position + right, TextAlignment.RIGHT);

            position += newLine;
			
            WriteTextSprite(ref frame, "[ DEUTERIUM TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentDeuterium.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalDeuterium), position + right, TextAlignment.RIGHT);

            position += newLine + newLine;;
        }

        void DrawSolarPanelSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = solarPanels.Sum(block => block.CurrentOutput);
            var currentMax = solarPanels.Sum(block => block.MaxOutput);
            var total = solarPanels.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

            WriteTextSprite(ref frame, "[ SOLAR PANELS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = reactors.Sum(block => block.CurrentOutput);
            var total = reactors.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ NUCLEAR REACTORS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawOreSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ ORES ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoOres)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }

        void DrawIngotSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ INGOTS ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoIngots)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }

        void DrawComponentSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ COMPONENTS ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoComponents)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }

        void DrawAmmoSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ VEHICLE AMMUNITION ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoAmmos)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }
	
        void DrawHandWeaponAmmoSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ HAND WEAPON AMMUNITION ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoHandWeaponAmmos)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }
		
        void DrawBottleSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ BOTTLES ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoBottles)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }
		
        void DrawWeaponSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ HAND WEAPONS ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoWeapons)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }
		
        void DrawConsumableSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ CONSUMABLES ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoConsumables)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }
		
        void DrawFoodSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ FOOD AND DRINK ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoFoods)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }
		
        void DrawToolSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {			
            WriteTextSprite(ref frame, "[ TOOLS ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoTools)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }

        void DrawItemsSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ MISCELLANEOUS ITEMS ]", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoItems)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine + newLine;;
            }
        }

        static string KiloFormat(int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + " M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0 K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + " K";

            return num.ToString("#,0");
        }

        void WriteTextSprite(ref MySpriteDrawFrame frame, string text, Vector2 position, TextAlignment alignment)
        {
            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = textSize,
                Color = mySurface.ScriptForegroundColor,
                Alignment = alignment,
                FontId = "White"
            };

            frame.Add(sprite);
        }

        private void CreateConfig()
        {
            config.AddSection("Settings");

            config.Set("Settings", "TextSize", "1.0");
            config.Set("Settings", "Battery", "false");
            config.Set("Settings", "WindTurbine", "false");
            config.Set("Settings", "HydrogenEngine", "false");
            config.Set("Settings", "FuelCell", "false");
            config.Set("Settings", "FusionReactor", "false");
            config.Set("Settings", "Tanks", "false");
            config.Set("Settings", "Solar", "false");
            config.Set("Settings", "NuclearReactor", "false");
            config.Set("Settings", "Ore", "false");
            config.Set("Settings", "Ingot", "false");
            config.Set("Settings", "Component", "false");
			config.Set("Settings", "VehicleAmmo", "false");
			config.Set("Settings", "HandWeaponAmmo", "false");
			config.Set("Settings", "Bottles", "false");
			config.Set("Settings", "Hand Weapons", "false");
			config.Set("Settings", "Consumables", "false");
			config.Set("Settings", "Food", "false");
			config.Set("Settings", "Tools", "false");
            config.Set("Settings", "Miscellaneous Items", "false");

            config.Invalidate();
            myTerminalBlock.CustomData = config.ToString();
        }

        private void LoadConfig()
        {
            ConfigCheck = false;

            if (config.TryParse(myTerminalBlock.CustomData))
            {
                if (config.ContainsSection("Settings"))
                {
                    ConfigCheck = true;
                }
                else
                {
                    MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Value error");
                }
            }
            else
            {
                MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Syntax error");
            }
        }
    }
}
