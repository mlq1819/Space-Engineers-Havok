//Havok Core programming information
//Takes note of ship status and updates the ship based on relevant information
public struct DTuple{
	public uint Num;
	public uint Full;
	public DTuple(uint n, uint f){
		Num = n;
		Full = f;
	}
	public float Percent(){
		if(Full==0)
			return 1.0f;
		return ((float)Num) / ((float)Full);
	}
}

private string GetCoreName(){
	string name = Me.CustomName;
	if(name.Contains("New ")){
		name = name.Substring(name.IndexOf("New ") + "New ".Length);
	}
	while(name.Contains(' ')){
		name = name.Substring(0, name.IndexOf(' ')) + name.Substring(name.IndexOf(' ')+1);
	}
	if(name.Contains("Processor")){
		name = name.Substring(0, name.IndexOf("Processor"));
	}
	return name;
}

private bool ConfirmCoreName(){
	return CoreName.ToLower().Equals(GetCoreName().ToLower());
}

private string ShipName = "";
private string CoreIdentification = "";
private const string CoreName = "CoreDiagnostics";
private IMyProgrammableBlock CoreStrategy = null;
private IMyProgrammableBlock CoreNavigation = null;
private IMyProgrammableBlock CoreDiagnostics = null;
private IMyProgrammableBlock CoreCommunications = null;
private IMyProgrammableBlock CoreDirective = null;
private string toEcho = "";
private char loadingChar = '|';
private List<string> message_history = new List<string>();
private bool BlocksSet = false;
private long Cycle = 0;
private long Long_Cycle = 1;

private IMyProjector CoreProjector = null;
private Dictionary<string, DTuple> Status = new Dictionary<string, DTuple>();
private Dictionary<string, DTuple> CargoStatus = new Dictionary<string, DTuple>();
bool found_update = false;
private List<double> PowerConsumption = new List<double>();

private void AddPrint(string message, bool AddToHistory){
	toEcho += message + '\n';
	if(AddToHistory)
		message_history.Add(message);
}

private void FinalPrint(){
	toEcho = "Cycle " + Long_Cycle + '-' + (++Cycle) + "\nCoreIdentification: " + CoreIdentification + '\n' + toEcho;
	if(Cycle >= Int64.MaxValue){
		Cycle = 0;
		Long_Cycle = (Long_Cycle+1)%Int64.MaxValue;
	}
	if(message_history.Count > 0){
		AddPrint("\n\n\nConsole History:", false);
		for(int i=Math.Min(message_history.Count-1, 10); i>=0; i--){
			AddPrint("\t" + message_history[i] + "\n------------", false);
		}
	}
	AddPrint("\n\n\n", false);
	Echo(toEcho);
	toEcho = "";
}

public void SetProjector(){
	List<IMyProjector> AllBlocks = new List<IMyProjector>();
	GridTerminalSystem.GetBlocksOfType<IMyProjector>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreProjector = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreProjector = AllBlocks[i];
			CoreProjector.CustomData = CoreIdentification;
			return;
		}
	}
}

private uint GetWorking(List<IMyTerminalBlock> list){
	uint output = 0;
	for(int i=0; i<list.Count; i++){
		if(list[i].IsFunctional)
			output++;
	}
	return output;
}

public void InitializeCounts(){
	Status.Clear();
	Status = new Dictionary<string, DTuple>();
	List<IMyTerminalBlock> AllBlocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(AllBlocks);
	Status.Add("thruster", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(AllBlocks);
	Status.Add("gyroscope", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(AllBlocks);
	Status.Add("battery", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyReactor>(AllBlocks);
	Status.Add("reactor", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyDecoy>(AllBlocks);
	Status.Add("decoy", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(AllBlocks);
	Status.Add("antenna", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(AllBlocks);
	Status.Add("turret", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyWarhead>(AllBlocks);
	Status.Add("warhead", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(AllBlocks);
	Status.Add("welder", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(AllBlocks);
	Status.Add("grinder", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(AllBlocks);
	Status.Add("drill", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(AllBlocks);
	Status.Add("solarpanel", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyRefinery>(AllBlocks);
	Status.Add("refinery", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyAssembler>(AllBlocks);
	Status.Add("assembler", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyProjector>(AllBlocks);
	Status.Add("projector", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllBlocks);
	Status.Add("connector", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllBlocks);
	Status.Add("cargo", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyBeacon>(AllBlocks);
	Status.Add("beacon", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyCollector>(AllBlocks);
	Status.Add("collector", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(AllBlocks);
	Status.Add("landinggear", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(AllBlocks);
	Status.Add("laserantenna", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
	GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(AllBlocks);
	Status.Add("sensor", new DTuple(GetWorking(AllBlocks), GetWorking(AllBlocks)));
}

public void RetrievalHelper(string line){
	string[] parts = line.Split(';');
	if(parts.Length==3){
		string key = parts[0];
		uint count = (uint) Int32.Parse(parts[1]);
		uint full = (uint) Int32.Parse(parts[2]);
		if(Status.ContainsKey(key)){
			Status[key] = new DTuple(count,full);
		}
		else{
			Status.Add(key, new DTuple(count,full));
		}
	}
}

public void RetrieveCounts(){
	Status.Clear();
	Status = new Dictionary<string, DTuple>();
	string text = this.Storage;
	string[] lines = this.Storage.Split('\n');
	for(int i=0; i<lines.Length; i++){
		if(lines[i].Contains('<') && lines[i].Contains('>') && lines[i].IndexOf('<') < lines[i].IndexOf('>')){
			int start = lines[i].IndexOf('<') + 1;
			int end = lines[i].Substring(start).IndexOf('>');
			string line = lines[i].Substring(start, end);
			RetrievalHelper(line);
		}
	}
}

public void SetBlocks(){
	try{
		if(Me.CustomData.Equals("") || CoreIdentification.Equals("")){ //Not ready to run yet
			Runtime.UpdateFrequency = UpdateFrequency.None;
			throw new Exception("CoreIdentification not available, awaiting call from CoreDirective");
		} 
		else { //Ready-to-Run!
			if(Me.CustomData.Length>0 && !Me.CustomData.Equals(CoreIdentification)){
				CoreIdentification = Me.CustomData;
				BlocksSet = false;
				AddPrint("Retrieved CoreIdentification; set to \"" + CoreIdentification + "\"", true);
			}
			long CoreIDNumber = Int64.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')));
			if(CoreIDNumber == 0){
				AddPrint("Currently in Factory Default Settings", true);
				FinalPrint();
				exit(0);
				return;
			}
			List<IMyProgrammableBlock> AllProgBlocks = new List<IMyProgrammableBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(AllProgBlocks);
			int core_count = 0;
			for(int i=0; i<AllProgBlocks.Count; i++){
				if(AllProgBlocks[i].CustomName == "Core Strategy Processor" && AllProgBlocks[i].CustomData == CoreIdentification){
					CoreStrategy = AllProgBlocks[i];
					core_count++;
				}
				if(AllProgBlocks[i].CustomName == "Core Navigation Processor" && AllProgBlocks[i].CustomData == CoreIdentification){
					CoreNavigation = AllProgBlocks[i];
					core_count++;
				}
				if(AllProgBlocks[i].CustomName == "Core Diagnostics Processor" && AllProgBlocks[i].CustomData == CoreIdentification){
					CoreDiagnostics = AllProgBlocks[i];
					core_count++;
				}
				if(AllProgBlocks[i].CustomName == "Core Communications Processor" && AllProgBlocks[i].CustomData == CoreIdentification){
					CoreCommunications = AllProgBlocks[i];
					core_count++;
				}
				if(AllProgBlocks[i].CustomName == "Core Directive Processor" && AllProgBlocks[i].CustomData == CoreIdentification){
					CoreDirective = AllProgBlocks[i];
					core_count++;
				}
			}
			string exception_message = "";
			bool exception = false;
			if(core_count > 5){
				exception = true;
				exception_message+="\nExcessive cores with the CoreIdentification " + CoreIdentification;
			}
			if(CoreStrategy == null){
				exception = true;
				exception_message+="\nMissing Core Strategy Processor";
			}
			if(CoreNavigation == null){
				exception = true;
				exception_message+="\nMissing Core Navigation Processor";
			}
			if(CoreDiagnostics == null){
				exception = true;
				exception_message+="\nMissing Core Diagnostics Processor";
			}
			if(CoreCommunications == null){
				exception = true;
				exception_message+="\nMissing Core Communications Processor";
			}
			if(CoreDirective == null){
				exception = true;
				exception_message+="\nMissing Core Directive Processor";
			}
			if(exception){
				BlocksSet = false;
				Runtime.UpdateFrequency = UpdateFrequency.None;
				throw new Exception(exception_message.Substring(1));
			} 
			else {
				BlocksSet = true;
				AddPrint("Successfully initialized all 5 cores", true);
				SetProjector();
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			}
		}
	} catch (Exception e){
		AddPrint("Critical Core Failure: " + e.Message, true);
		Runtime.UpdateFrequency = UpdateFrequency.None;
	}
}

public void Initialize(){
	if(CoreIdentification == "" && Me.CustomData.Length>0){
		CoreIdentification = Me.CustomData;
		AddPrint("Retrieved CoreIdentification: \"" + CoreIdentification + "\"", true);
		AddPrint("Waiting for Core Directive to begin...", true);
	}
	else {
		AddPrint("Waiting for Core Directive for CoreIdentification...", true);
	}
	if(this.Storage.Length > 0 && this.Storage.IndexOf('<') >= 0 && this.Storage.IndexOf('>') > 0){
		RetrieveCounts();
	}
	else{
		InitializeCounts();
	}
	FinalPrint();
}

public Program()
{
	ShipName = Me.CubeGrid.CustomName;
	if(ConfirmCoreName()){
		Status.Clear();
		Initialize();
	} else {
		throw new Exception("Correct CoreName to \"" + GetCoreName() + "\" (currently \"" + CoreName + "\")");
	}
	Me.GetSurface(0).WriteText(CoreName, false);
}

public void Save()
{
    Me.CustomData = CoreIdentification;
	foreach(string key in Status.Keys){
		uint count = Status[key].Num;
		uint full = Status[key].Full;
		string output = "<" + key + ';' + count + ';' + full + '>';
		this.Storage += output + '\n';
	}
	Status.Clear();
	Status = new Dictionary<string, DTuple>();
	
	// Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

private void Wipe(){
	this.Storage = "";
	Me.CustomData = "";
	CoreIdentification = "0";
	BlocksSet = false;
	Cycle = 0;
	Long_Cycle = 1;
	message_history = new List<string>();
	Runtime.UpdateFrequency = UpdateFrequency.None;
	AddPrint("Reset Settings and Cleared Storage", true);
	Initialize();
}

private void Reset(){
	this.Storage = "";
	this.CoreIdentification = "";
	Me.CustomData = "";
	AddPrint("Reset CoreIdentification, Storage, and CustomData", true);
	BlocksSet = false;
}

private void Set(string argument){
	int index = argument.IndexOf('<')+1;
	int length = argument.Substring(index).IndexOf('>');
	ShipName = Me.CubeGrid.CustomName;
	if(!argument.Substring(index,length).Equals(CoreIdentification)){
		CoreIdentification = argument.Substring(index, length);
		BlocksSet = false;
		AddPrint("Set CoreIdentification to \"" + CoreIdentification + "\"", true);
	}
	Me.CustomData = CoreIdentification;
}

private double TotalRemainingPower(){
	List<IMyBatteryBlock> AllBatteries = new List<IMyBatteryBlock>();
	List<IMyReactor> AllReactors = new List<IMyReactor>();
	List<IMyCargoContainer> AllCargo = new List<IMyCargoContainer>();
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(AllBatteries);
	GridTerminalSystem.GetBlocksOfType<IMyReactor>(AllReactors);
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllCargo);
	
	double total_power = 0; //Measured in Megawatt/hours
	foreach(IMyBatteryBlock Battery in AllBatteries){
		total_power += Battery.CurrentStoredPower;
	}
	uint total_uranium = 0; //Measured in KG or Megawatt/hours
	foreach(IMyReactor Reactor in AllReactors){
		if(Reactor.HasInventory){
			IMyInventory Inventory = Reactor.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				if(type.ToLower().Equals("ingot") && subtype.ToLower().Equals("uranium")){
					total_uranium+=amount;
				}
			}
		}
	}
	foreach(IMyCargoContainer Cargo in AllCargo){
		if(Cargo.HasInventory){
			IMyInventory Inventory = Cargo.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				if(type.ToLower().Equals("ingot") && subtype.ToLower().Equals("uranium")){
					total_uranium+=amount;
				}
			}
		}
	}
	total_power += total_uranium;
	return total_power;
}

private bool average_time_low = false;
private bool average_time_critical = false;

private bool AveragePowerTime(){
	long total_base = 0;
	double total_sum = 0;
	for(int i=0; i<PowerConsumption.Count; i++){
		int weight = (i/10)+1;
		total_base += weight;
		total_sum += PowerConsumption[i] * weight;
	}
	double average_consumption = total_sum / total_base;
	double total_power = TotalRemainingPower();
	double time = total_power / average_consumption;
	AddPrint("Average Power Remaining: " + time + " hours\nAverage Power Consumption: " + average_consumption.ToString() + " mWh\nTotal Power: " + total_power + " mWh", false);
	if(!average_time_low && time < 2){
		found_update = true;
		CoreStrategy.TryRun(CoreName + ":PowerLow<" + time + ">");
		average_time_low = true;
	}
	else if(average_time_low && time >= 12){
		found_update = true;
		CoreStrategy.TryRun(CoreName + ":PowerAdequate<" + time + ">");
		average_time_low = false;
	}
	if(!average_time_critical && time < 0.5){
		found_update = true;
		CoreStrategy.TryRun(CoreName + ":PowerCritical<" + time + ">");
		average_time_critical = true;
	}
	else if(average_time_critical && time >= 0.5){
		found_update = true;
		average_time_critical = false;
	}
	return found_update;
}

private bool current_time_low = false;
private bool current_time_critical = false;

private bool CurrentPowerTime(){
	List<IMyBatteryBlock> AllBatteries = new List<IMyBatteryBlock>();
	List<IMyReactor> AllReactors = new List<IMyReactor>();
	List<IMyCargoContainer> AllCargo = new List<IMyCargoContainer>();
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(AllBatteries);
	GridTerminalSystem.GetBlocksOfType<IMyReactor>(AllReactors);
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllCargo);
	
	
	double total_time = 0;
	double power_consumption = 0;
	double total_power = 0; //Measured in Megawatt/hours
	foreach(IMyBatteryBlock Battery in AllBatteries){
		power_consumption += Battery.CurrentOutput;
		total_power += Battery.CurrentStoredPower;
	}
	foreach(IMyReactor Reactor in AllReactors){
		power_consumption += Reactor.CurrentOutput;
	}
	uint total_uranium = 0; //Measured in KG or Megawatt/hours
	double total_generation = 0;
	foreach(IMyReactor Reactor in AllReactors){
		if(Reactor.HasInventory){
			IMyInventory Inventory = Reactor.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				if(type.ToLower().Equals("ingot") && subtype.ToLower().Equals("uranium")){
					total_uranium+=amount;
				}
			}
		}
		total_generation += Reactor.MaxOutput;
	}
	foreach(IMyCargoContainer Cargo in AllCargo){
		if(Cargo.HasInventory){
			IMyInventory Inventory = Cargo.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				if(type.ToLower().Equals("ingot") && subtype.ToLower().Equals("uranium")){
					total_uranium+=amount;
				}
			}
		}
	}
	total_power += total_uranium;
	total_time = total_power / power_consumption;
	AddPrint("Current Power Remaining: " + total_time + " hours", false);
	PowerConsumption.Add(power_consumption);
	if(PowerConsumption.Count > 8192){
		List<double> Temp = new List<double>();
		for(int i=4096; i<PowerConsumption.Count(); i++){
			Temp.Add(PowerConsumption[i]);
			PowerConsumption[i] = 0;
		}
		PowerConsumption = Temp;
	}
	return found_update;
}

private bool MinimumPowerTime(){
	List<IMyBatteryBlock> AllBatteries = new List<IMyBatteryBlock>();
	List<IMyReactor> AllReactors = new List<IMyReactor>();
	List<IMyCargoContainer> AllCargo = new List<IMyCargoContainer>();
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(AllBatteries);
	GridTerminalSystem.GetBlocksOfType<IMyReactor>(AllReactors);
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllCargo);
	
	double max_time = 0; //measured in hours
	double power_consumption = 0;
	foreach(IMyBatteryBlock Battery in AllBatteries){
		power_consumption += Battery.MaxOutput;
	}
	foreach(IMyReactor Reactor in AllReactors){
		power_consumption += Reactor.MaxOutput;
	}
	
	
	foreach(IMyBatteryBlock Battery in AllBatteries){
		max_time = Math.Max(max_time, Battery.MaxOutput / Battery.CurrentStoredPower);
	}
	
	uint total_uranium = 0;
	double total_generation = 0;
	foreach(IMyReactor Reactor in AllReactors){
		if(Reactor.HasInventory){
			IMyInventory Inventory = Reactor.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				if(type.ToLower().Equals("ingot") && subtype.ToLower().Equals("uranium")){
					total_uranium+=amount;
				}
			}
		}
		total_generation += Reactor.MaxOutput;
	}
	foreach(IMyCargoContainer Cargo in AllCargo){
		if(Cargo.HasInventory){
			IMyInventory Inventory = Cargo.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				if(type.ToLower().Equals("ingot") && subtype.ToLower().Equals("uranium")){
					total_uranium+=amount;
				}
			}
		}
	}
	foreach(IMyReactor Reactor in AllReactors){
		double partial_generation = Reactor.MaxOutput;
		double partial_uranium = total_uranium * (partial_generation / total_generation);
		max_time = Math.Max(max_time, partial_generation / partial_uranium);
	}
	AddPrint("Minimum Power Remaining: " + max_time + " hours", false);
	return found_update;
}

private bool PowerDiagnostics(){
	MinimumPowerTime();
	CurrentPowerTime();
	if(PowerConsumption.Count > 20){
		AveragePowerTime();
	}
	return false;
}

private bool Low_Rockets = false;
private bool Low_Magazines = false;
private bool Critical_Rockets = false;
private bool Critical_Magazines = false;

private bool TurretDiagnostics(){
	List<IMyLargeGatlingTurret> AllGatling = new List<IMyLargeGatlingTurret>();
	List<IMyLargeMissileTurret> AllMissile = new List<IMyLargeMissileTurret>();
	GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(AllGatling);
	GridTerminalSystem.GetBlocksOfType<IMyLargeMissileTurret>(AllMissile);
	if(AllGatling.Count+AllMissile.Count>0){
		List<IMyCargoContainer> AllCargo = new List<IMyCargoContainer>();
		GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllCargo);
		uint Rocket_Ideal = (uint) (20 * AllMissile.Count);
		uint Magazine_Ideal = (uint) (30 * AllGatling.Count);
		uint Rocket_Count = (uint) 0;
		uint Magazine_Count = (uint) 0;
		foreach(IMyLargeGatlingTurret Turret in AllGatling){
			if(Turret.HasInventory){
				IMyInventory Inventory = Turret.GetInventory();
				List<MyInventoryItem> stacks = new List<MyInventoryItem>();
				Inventory.GetItems(stacks, null);
				foreach(MyInventoryItem Item in stacks){
					string type = Item.Type.TypeId;
					type = type.Substring(type.LastIndexOf('_') + 1);
					string subtype = Item.Type.SubtypeId;
					uint amount = (uint) Item.Amount.ToIntSafe();
					if(type.ToLower().Equals("ammomagazine")){
						if(subtype.ToLower().Equals("NATO_25x184mm".ToLower())){
							Magazine_Count+=amount;
						}
					}
				}
			}
		}
		foreach(IMyLargeMissileTurret Turret in AllMissile){
			if(Turret.HasInventory){
				IMyInventory Inventory = Turret.GetInventory();
				List<MyInventoryItem> stacks = new List<MyInventoryItem>();
				Inventory.GetItems(stacks, null);
				foreach(MyInventoryItem Item in stacks){
					string type = Item.Type.TypeId;
					type = type.Substring(type.LastIndexOf('_') + 1);
					string subtype = Item.Type.SubtypeId;
					uint amount = (uint) Item.Amount.ToIntSafe();
					if(type.ToLower().Equals("ammomagazine")){
						if(subtype.ToLower().Equals("Missile200mm".ToLower())){
							Rocket_Count+=amount;
						}
					}
				}
			}
		}
		foreach(IMyCargoContainer Cargo in AllCargo){
			if(Cargo.HasInventory){
				IMyInventory Inventory = Cargo.GetInventory();
				List<MyInventoryItem> stacks = new List<MyInventoryItem>();
				Inventory.GetItems(stacks, null);
				foreach(MyInventoryItem Item in stacks){
					string type = Item.Type.TypeId;
					type = type.Substring(type.LastIndexOf('_') + 1);
					string subtype = Item.Type.SubtypeId;
					uint amount = (uint) Item.Amount.ToIntSafe();
					if(type.ToLower().Equals("ammomagazine")){
						if(subtype.ToLower().Equals("NATO_25x184mm".ToLower())){
							Magazine_Count+=amount;
						}
						else if(subtype.ToLower().Equals("Missile200mm".ToLower())){
							Rocket_Count+=amount;
						}
					}
				}
			}
		}
		
		if(!Low_Rockets && Rocket_Count < Rocket_Ideal / 2){
			found_update = true;
			Low_Rockets = true;
			CoreStrategy.TryRun(CoreName + ":RequestLow<Missile200mm " + Rocket_Count + " of " + Rocket_Ideal + ">");
			AddPrint("Low Missile200mm " + Rocket_Count + " of " + Rocket_Ideal, true);
		}
		else if(Low_Rockets && Rocket_Count >= Rocket_Ideal){
			found_update = true;
			Low_Rockets = false;
			CoreStrategy.TryRun(CoreName + ":RequestAdequate<Missile200mm " + Rocket_Count + " of " + Rocket_Ideal + ">");
			AddPrint("Adequate Missile200mm " + Rocket_Count + " of " + Rocket_Ideal, true);
		}
		if(!Critical_Rockets && Rocket_Count < Rocket_Ideal / 5){
			found_update = true;
			Critical_Rockets = true;
			CoreStrategy.TryRun(CoreName + ":RequestCritical<Missile200mm " + Rocket_Count + " of " + Rocket_Ideal + ">");
			AddPrint("Critical Missile200mm " + Rocket_Count + " of " + Rocket_Ideal, true);
		}
		else if(Critical_Rockets && Rocket_Count >= Rocket_Ideal / 5){
			found_update = true;
			Critical_Rockets = false;
		}
		if(!Low_Magazines && Magazine_Count < Magazine_Ideal / 2){
			found_update = true;
			Low_Magazines = true;
			CoreStrategy.TryRun(CoreName + ":RequestLow<NATO_25x184mm " + Magazine_Count + " of " + Magazine_Ideal + ">");
			AddPrint("Low NATO_25x184mm " + Magazine_Count + " of " + Magazine_Ideal, true);
		}
		else if(Low_Magazines && Magazine_Count >= Magazine_Ideal){
			found_update = true;
			Low_Magazines = false;
			CoreStrategy.TryRun(CoreName + ":RequestCancel<NATO_25x184mm " + Magazine_Count + " of " + Magazine_Ideal + ">");
			AddPrint("Adequate NATO_25x184mm " + Magazine_Count + " of " + Magazine_Ideal, true);
		}
		if(!Critical_Magazines && Magazine_Count < Magazine_Ideal / 5){
			found_update = true;
			Critical_Magazines = true;
			CoreStrategy.TryRun(CoreName + ":RequestCritical<NATO_25x184mm " + Magazine_Count + " of " + Magazine_Ideal + ">");
			AddPrint("Critical NATO_25x184mm " + Magazine_Count + " of " + Magazine_Ideal, true);
		}
		else if(Critical_Magazines && Magazine_Count >= Magazine_Ideal / 5){
			found_update = true;
			Critical_Magazines = false;
		}
	}
	return found_update;
}

private bool Is_Docked = false;
private bool Can_Dock = false;

private bool DockedDiagnostics(){
	List<IMyShipConnector> AllConnectors = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllConnectors);
	if(AllConnectors.Count>0){
		bool found_docked = false;
		bool found_dockable = false;
		for(int i=0; i<AllConnectors.Count; i++){
			IMyShipConnector Connector = AllConnectors[i];
			if(Connector.Status == MyShipConnectorStatus.Connected){
				found_docked = true;
				if(!Is_Docked){
					found_update = true;
					CoreStrategy.TryRun(CoreName + ":Docking<Docked>");
					AddPrint(Connector.CustomName + ":Docked", true);
				}
			}
			if(Connector.Status == MyShipConnectorStatus.Connectable){
				found_dockable = true;
				if(!Can_Dock){
					found_update = true;
					CoreStrategy.TryRun(CoreName + ":Docking<Dockable>");
					AddPrint(Connector.CustomName + ":Dockable", true);
				}
			}
		}
		if(Is_Docked && !found_docked){
			found_update = true;
			Is_Docked = false;
			CoreStrategy.TryRun(CoreName + ":Docking<Undocked>");
			AddPrint("Connector:Undocked", true);
		}
		if(Can_Dock && !found_dockable){
			found_update = true;
			Can_Dock = false;
			CoreStrategy.TryRun(CoreName + ":Docking<Undockable>");
			AddPrint("Connector:Undockable", true);
		}
	}
	return found_update;
}

private bool Is_Locked = false;
private bool Can_Lock = false;

private bool LockedDiagnostics(){
	List<IMyLandingGear> AllLandingGear = new List<IMyLandingGear>();
	GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(AllLandingGear);
	if(AllLandingGear.Count>0){
		bool found_locked = false;
		bool found_lockable = false;
		foreach(IMyLandingGear LandingGear in AllLandingGear){
			if(LandingGear.LockMode == LandingGearMode.Locked){
				found_locked = true;
				if(!Is_Locked){
					found_update = true;
					CoreStrategy.TryRun(CoreName + ":Locking<Locked>");
					AddPrint(LandingGear.CustomName + ":Locked", true);
				}
			}
			if(LandingGear.LockMode == LandingGearMode.ReadyToLock){
				found_lockable = true;
				if(!Can_Lock){
					found_update = true;
					CoreStrategy.TryRun(CoreName + ":Locking<Lockable>");
					AddPrint(LandingGear.CustomName + ":Lockable", true);
				}
			}
		}
		if(Is_Locked && !found_locked){
			found_update = true;
			Is_Locked = false;
			CoreStrategy.TryRun(CoreName + ":Locking<Unlocked>");
			AddPrint("LandingGear:Unlocked", true);
		}
		if(Can_Lock && !found_lockable){
			found_update = true;
			Can_Lock = false;
			CoreStrategy.TryRun(CoreName + ":Locking<Unlockable>");
			AddPrint("LandingGear:Unlockable", true);
		}
	}
	return found_update;
}

//Performs cargo status diagnostics checks
private bool CargoDiagnostics(){
	List<IMyCargoContainer> AllCargo = new List<IMyCargoContainer>();
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllCargo);
	foreach(IMyCargoContainer Cargo in AllCargo){
		uint current = (uint) Cargo.GetInventory().CurrentVolume.ToIntSafe();
		uint full = (uint) Cargo.GetInventory().MaxVolume.ToIntSafe();
		if(CargoStatus.ContainsKey(Cargo.CustomName)){
			uint old = CargoStatus[Cargo.CustomName].Num;
			if(current != old){
				found_update = true;
				if(current < old){
					CoreStrategy.TryRun(Cargo.CustomName + ":Report<CargoUnloaded " + Cargo.CustomName + " at " + (new DTuple(current, full).Percent()).ToString() + ">");
					AddPrint(Cargo.CustomName + ":" + current.ToString() + " / " + full.ToString() + " (CargoUnloaded" + (new DTuple(current, full).Percent()).ToString() + ')', true);
				} else {
					CoreStrategy.TryRun(Cargo.CustomName + ":Report<CargoLoaded " + Cargo.CustomName + " at " + (new DTuple(current, full).Percent()).ToString() + ">");
					AddPrint(Cargo.CustomName + ":" + current.ToString() + " / " + full.ToString() + " (CargoLoaded" + (new DTuple(current, full).Percent()).ToString() + ')', true);
				}
				Status[Cargo.CustomName] = new DTuple(current, full);
			}
		}
		else {
			CargoStatus.Add(Cargo.CustomName, new DTuple(current, full));
		}
		if(Cargo.HasInventory){
			IMyInventory Inventory = Cargo.GetInventory();
			List<MyInventoryItem> stacks = new List<MyInventoryItem>();
			Inventory.GetItems(stacks, null);
			foreach(MyInventoryItem Item in stacks){
				string type = Item.Type.TypeId;
				type = type.Substring(type.LastIndexOf('_') + 1);
				string subtype = Item.Type.SubtypeId;
				uint amount = (uint) Item.Amount.ToIntSafe();
				AddPrint("type: " + type + " \tsubtype: " + subtype + " \tamount: " + amount.ToString(), false);
			}
		}
	}
	return found_update;
}

//Performs basic diagnostics status check on a type of component
private void DiagHelper(string key, uint count){
	if(Status.ContainsKey(key)){
		uint old_count = Status[key].Num;
		uint full = Status[key].Full;
		if(full>0){
			if(count != old_count){
				found_update = true;
				if(count < old_count){
					CoreStrategy.TryRun(CoreName + ":Report<Broke " + key + " at " + (new DTuple(count, full).Percent()).ToString() + ">");
					AddPrint(key + ":" + count.ToString() + " / " + full.ToString() + " (Broke" + (new DTuple(count, full).Percent()).ToString() + ')', true);
				} else {
					CoreStrategy.TryRun(CoreName + ":Report<Fixed " + key + " at " + (new DTuple(count, full).Percent()).ToString() + ">");
					AddPrint(key + ":" + count.ToString() + " / " + full.ToString() + " (Fixed" + (new DTuple(count, full).Percent()).ToString() + ')', true);
				}
				Status[key] = new DTuple(count, full);
			} else {
				AddPrint(key + ":" + count.ToString() + " / " + full.ToString(), false);
			}
		}
	} else {
		Status.Add(key, new DTuple(count, count));
	}
}

//Runs core diagnostic tests on critical systems
private bool CriticalDiagnostics(){
	List<IMyTerminalBlock> AllBlocks = new List<IMyTerminalBlock>();
	string key = "";
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(AllBlocks);
	key = "thruster";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(AllBlocks);
	key = "gyroscope";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(AllBlocks);
	key = "battery";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyReactor>(AllBlocks);
	key = "reactor";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyDecoy>(AllBlocks);
	key = "decoy";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(AllBlocks);
	key = "antenna";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(AllBlocks);
	key = "turret";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyWarhead>(AllBlocks);
	key = "warhead";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(AllBlocks);
	key = "welder";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(AllBlocks);
	key = "grinder";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(AllBlocks);
	key = "drill";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(AllBlocks);
	key = "solarpanel";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyRefinery>(AllBlocks);
	key = "refinery";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyAssembler>(AllBlocks);
	key = "assembler";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyProjector>(AllBlocks);
	key = "projector";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllBlocks);
	key = "connector";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(AllBlocks);
	key = "cargo";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyBeacon>(AllBlocks);
	key = "beacon";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyCollector>(AllBlocks);
	key = "collector";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(AllBlocks);
	key = "landinggear";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(AllBlocks);
	key = "laserantenna";
	DiagHelper(key, GetWorking(AllBlocks));
	GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(AllBlocks);
	key = "sensor";
	DiagHelper(key, GetWorking(AllBlocks));
	return found_update;
}

//Performs diagnostic tests
private void RunDiagnostics(string argument){
	found_update = false;
	CriticalDiagnostics();
	CargoDiagnostics();
	PowerDiagnostics();
	TurretDiagnostics();
	DockedDiagnostics();
	LockedDiagnostics();
	if(found_update){
		Runtime.UpdateFrequency = UpdateFrequency.Update10;
	}
	else{
		Runtime.UpdateFrequency = UpdateFrequency.Update100;
	}
}

public void Main(string argument, UpdateType updateSource)
{
	long CoreIDNumber = 0;
	if(CoreIdentification.Contains('-'))
		CoreIDNumber = Int32.Parse(CoreIdentification.IndexOf('-'));
	else
		CoreIDNumber = Int32.Parse(CoreIdentification);
	if(CoreIDNumber == 0){
		AddPrint("Currently in Factory Default Settings");
		FinalPrint();
		exit(0);
		return;
	}
	if(argument.Equals("CoreDirective:Reset")){
		Reset();
		FinalPrint();
		return;
	} else if(argument.Contains("CoreDirective:Set<")){
		Set(argument);
		FinalPrint();
		return;
	} else if(argument.Equals("CoreDirective:Start")){
		if(!BlocksSet)
			SetBlocks();
		AddPrint("Started Program", true);
		CoreDirective.TryRun(CoreName + ":Started");
		FinalPrint();
		return;
	} else if(argument.ToLower().Equals("terminal:reset")){
		Status.Clear();
		Status = new Dictionary<string, DTuple>();
		CargoStatus.Clear();
		CargoStatus = new Dictionary<string, DTuple>();
		Wipe();
		return;
	}
	switch(loadingChar){
		case '|':
			loadingChar='\\';
			break;
		case '\\':
			loadingChar='-';
			break;
		case '-':
			loadingChar='/';
			break;
		case '/':
			loadingChar='|';
			break;
	}
	if(BlocksSet){
		AddPrint("Running program... (" + loadingChar + ")\n", false);
		RunDiagnostics(argument);
	} else {
		AddPrint("Cannot run program --- blocks not set!", false);
	}
	FinalPrint();
}
