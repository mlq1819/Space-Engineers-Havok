//Havok Core programming information
//Core strategy; interface between the other cores. Makes decisions based on the current DroneStatus and ActiveTask and sends commands to the relevant cores 

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

public enum ActiveTask{
	//Nothing
	None = 0,
	//Scouting an area for ores, asteroids, scrap, etc.
	Scouting = 1,
	//Grinding down scrap
	Salvaging = 2,
	//Devouring small scrap
	Devouring = 3,
	//Delivering supplies from one unit to another
	Delivering = 4,
	//Refueling from another vessel
	Refueling = 5,
	//Docking with another vessel
	Docking = 6,
	//Assaulting an enemy vessel; holding a position and allowing turrets to do the work
	Attacking = 7,
	//Mining an asteroid
	Mining = 8,
	//Welding up a new (large) ship
	Building = 9,
	//Printing a new (small) ship
	Printing = 10,
	//Attempting to lock onto an enemy ship
	Tracking = 11,
	//Critical Task - Defending other units; holding a position and either allowing decoys to draw fire or allowing turrets to open fire
	Defending = 12,
	//Critical Task - Locked onto an enemy vessel and actively broadcasting its position to others
	Yelling = 13,
	//Critical Task - Armed explosives and bee-lining towards an enemy ship
	Kamikaze = 14
}

public enum DroneStatus{
	//Irreparable damage to ship; functionally idle
	Salvage = -1,
	//Freshly built/printed ship; needs to be initialized
	Fresh = 0,
	//No active tasks
	Idle = 1,
	//Waiting for information
	Waiting = 2,
	//Returning from an active task
	Returning = 3,
	//Moving to perform an active task
	Venturing = 4,
	//Performing an active task
	Performing = 5,
	//Escaping from a dangerous situation
	Escaping = 6,
	//Ship is performing a Critical Task; either Defending, Yelling, or Kamikaze
	CriticalTask = 7
}

public enum SystemStatus{
	//Functioning at full capacity/has sufficient resources to function
	Normal = 1,
	//Functioning at reduced capacity/running low on resources
	Warning = 2,
	//Functioning at limited capacity/running critically low on resources
	Critical = 3,
	//Dysfunctional and beyond repair; fully out of power
	Irreparable = 4
}

private DroneStatus Status = DroneStatus.Fresh;
private ActiveTask CurrentTask = ActiveTask.None;

private bool ConfirmCoreName(){
	return CoreName.ToLower().Equals(GetCoreName().ToLower());
}

private bool Changed_ID = false;

private string ShipName = "";
private string CoreIdentification = "";
private const string CoreName = "CoreStrategy";
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

private SystemStatus Systems = SystemStatus.Normal;
private SystemStatus Power = SystemStatus.Normal;
private SystemStatus Rockets = SystemStatus.Normal;
private SystemStatus Magazines = SystemStatus.Normal;
private SystemStatus Components = SystemStatus.Normal;
private bool Locked = false;
private bool Lockable = false;
private bool Docked = false;
private bool Dockable = false;
private Vector3D DockPort;
private List<Vector3D> Scouting = new List<Vector3D>();

private IMyProgrammableBlock FindClosestCoreWithName(string name){
	List<IMyProgrammableBlock> AllBlocks = new List<IMyProgrammableBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(AllBlocks);
	double min_distance = Double.MaxValue;
	bool found_a_core = false;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomName.Equals(name) && (Me.GetPosition() - AllBlocks[i].GetPosition()).Length() <= 10){
			found_a_core = true;
			if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
				return AllBlocks[i];
			}
			else {
				min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
			}
		}
	}
	if(found_a_core){
		for(int i=0; i<AllBlocks.Count; i++){
			if(AllBlocks[i].CustomName.Equals(name) && (Me.GetPosition() - AllBlocks[i].GetPosition()).Length() <= 10){
				double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
				if(distance < min_distance+0.5){
					AllBlocks[i].CustomData = CoreIdentification;
					return AllBlocks[i];
				}
			}
		}
	}
	return null;
}

private void AddPrint(string message, bool AddToHistory){
	toEcho += message + '\n';
	if(AddToHistory)
		message_history.Add(message);
}

private void FinalPrint(){
	toEcho = "Status:" + Status.ToString() + "\nCycle " + Long_Cycle + '-' + (++Cycle) + "\nCoreIdentification: " + CoreIdentification + '\n' + toEcho;
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
			long CoreIDNumber = Int32.Parse(CoreIdentification.IndexOf('-'));
			if(CoreIDNumber == 0){
				AddPrint("Currently in Factory Default Settings");
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
				Runtime.UpdateFrequency = UpdateFrequency.None;
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
		CoreIdentification = "";
		AddPrint("Waiting for Core Directive for CoreIdentification...", true);
	}
	FinalPrint();
}

public Program()
{
	ShipName = Me.CubeGrid.CustomName;
	if(this.Storage.Length>0){
		if(this.Storage.Contains("\n")){
			Status = (DroneStatus) Int32.Parse(this.Storage.Substring(0,this.Storage.IndexOf("/n")));
			CurrentTask = (ActiveTask) Int32.Parse(this.Storage.Substring(this.Storage.IndexOf("/n")+1));
		}
		Status = (DroneStatus) Int32.Parse(this.Storage);
	}
	if(ConfirmCoreName()){
		Initialize();
	} else {
		throw new Exception("Correct CoreName to \"" + GetCoreName() + "\" (currently \"" + CoreName + "\")");
	}
	Me.GetSurface(0).WriteText(CoreName, false);
}

public void Save()
{
	Me.CustomData = CoreIdentification;
	this.Storage = ((int)Status).ToString();
	if(Status == DroneStatus.Performing){
		this.Storage += "\n" + ((int)CurrentTask).ToString();
	}
}

public void SetLights(Color lightcolor){
	List<IMyInteriorLight> AllLights = new List<IMyInteriorLight>();
	GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(AllLights);
	foreach(IMyInteriorLight light in AllLights){
		double distance = (Me.GetPosition() - light.GetPosition()).Length();
		if(distance < 3 && light.CustomName.Contains("Core Light ")){
			light.SetValue("Color", lightcolor);
		}
	}
}

public void UpdateStatus(DroneStatus new_status){
	if(Status != DroneStatus.Salvage && Status != new_status){
		switch(new_status){
			case DroneStatus.Salvage:
				Send("Havok Open Channel", "Salvage", CoreIdentification);
				CoreDiagnostics.ApplyAction("OnOff_Off");
				SetLights(new Color(0,0,0,255));
				AddPrint("Broadcast Salvage Request; disabled Diagnostics", true);
				break;
			case DroneStatus.Fresh:
				SetLights(new Color(255, 255, 255, 255));
				break;;
			case DroneStatus.Idle:
				SetLights(new Color(125, 120, 60, 255));
				break;
			case DroneStatus.Waiting:
				SetLights(new Color(137, 239, 255, 255));
				break;
			case DroneStatus.Returning:
				SetLights(new Color(239, 255, 137, 255));
				break;
			case DroneStatus.Venturing:
				SetLights(new Color(255, 239, 137, 255));
				break;
			case DroneStatus.Performing:
				switch(CurrentTask){
					case ActiveTask.None:
						UpdateStatus(DroneStatus.Idle);
						break;
					case ActiveTask.Scouting:
						//TODO - Write scouting data
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Salvaging:
						//TODO - Write salvaging data (looking around, maybe some requests, grinding); definately uses an additional core
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Devouring:
						//TODO - Write devouring data (looking around, maybe some requests, grinding); definitely uses an additional core
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Attacking:
						//TODO - Write attacking data (mostly setting navigation to follow an enemy target and performing random vector thrusting to throw off targeting)
						if(Status != DroneStatus.Performing && Status != DroneStatus.Waiting && ((int) Status) < ((int)DroneStatus.Escaping)){
							Send(CoreIdentification, "Attack", "Request");
							UpdateStatus(DroneStatus.Waiting);
						}
						break;
					case ActiveTask.Mining:
						//TODO - Write mining data (scans, mining, etc); probably using an additional core
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Building:
						//TODO - Write building data (idk); definitely uses an additional core
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Printing:
						//TODO - Write printing data (idk); definitely uses an additional core
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Tracking:
						//TODO - Write tacking data (scans, locking, etc); uses a call to Navigation
						UpdateStatus(DroneStatus.Performing);
						break;
					case ActiveTask.Defending:
						//TODO - Write defending data; (mostly just setting navigation to follow an enemy target and performing random vector thrusting to throw off targeting);
						UpdateStatus(DroneStatus.CriticalTask);
						break;
					case ActiveTask.Yelling:
						//TODO - Write yelling data (mostly just a call to navigations)
						UpdateStatus(DroneStatus.CriticalTask);
						break;
					case ActiveTask.Kamikaze:
						//TODO - Write Kamikaze program
						UpdateStatus(DroneStatus.CriticalTask);
						break;
				}
				SetLights(new Color(255, 239, 50, 255));
				break;
			case DroneStatus.Escaping:
				SetLights(new Color(255, 137, 0, 255));
				break;
			case DroneStatus.CriticalTask:
				SetLights(new Color(255, 0, 0, 255));
				break;
		}
		AddPrint("Updated Ship Status to " + new_status.ToString() + " (was " + Status.ToString() + ")", true);
		Status = new_status;
		Send(CoreIdentification, "Status", new_status.ToString());
	}
	else {
		if(Status == DroneStatus.Salvage && new_status != Status)
			AddPrint("Ship in Salvage Mode; cannot cancel Salvage Request", true);
	}
}

private void Wipe(){
	UpdateStatus(DroneStatus.Fresh);
	this.Storage = "";
	Me.CustomData = "";
	CoreIdentification = "0";
	UpdateStatus(DroneStatus.Fresh);
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
		Changed_ID = true;
	}
	Me.CustomData = CoreIdentification;
}

private void Send(string Tag, string Message){
	if(Tag.Contains(';'))
		throw new ArgumentException("Tag may not contain a semicolon (" + Tag + ')');
	CoreCommunications.TryRun(CoreName + ":Send<" + Tag + ';' + Message + '>');
}

private void Send(string Tag, string Command, string Data){
	if(Command.Contains(':'))
		throw new ArgumentException("Command may not contain a colon (" + Command + ')');
	Send(Tag, Command + ':' + Data);
}

private void HasArrived(Vector3D position){
	switch(CurrentTask){
		case ActiveTask.None:
			UpdateStatus(DroneStatus.Idle);
			break;
		case ActiveTask.Scouting:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Salvaging:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Devouring:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Attacking:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Mining:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Building:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Printing:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Tracking:
			UpdateStatus(DroneStatus.Performing);
			break;
		case ActiveTask.Defending:
			UpdateStatus(DroneStatus.CriticalTask);
			break;
		case ActiveTask.Yelling:
			UpdateStatus(DroneStatus.CriticalTask);
			break;
		case ActiveTask.Kamikaze:
			UpdateStatus(DroneStatus.CriticalTask);
			break;
	}
	Send(CoreIdentification, "Arrived", "(" + position.ToString() + ")");
	AddPrint("Alerted arrival at (" + position.ToString() + ")", true);
}

private void HasReturned(Vector3D position){
	switch(CurrentTask){
		case ActiveTask.Docking:
			if(DockPort != null && (DockPort - position).Length() < 5){
				Dock();
			}
			break;
		default:
			UpdateStatus(DroneStatus.Idle);
			Send(CoreIdentification, "Returned", "(" + position.ToString() + ")");
			AddPrint("Alerted return at (" + position.ToString() + ")", true);
			break;
	}
}

private void SourceNavigation(string Command, string Data){
	if(Command.ToLower().Equals("report")){
		Send(CoreIdentification, "Position", Data);
		AddPrint("Broadcast Position Report", true);
	}
	else if(Command.ToLower().Equals("gravity")){
		int start = Data.IndexOf('(')+1;
		int end = Data.Substring(start).IndexOf(',');
		double x = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(',');
		double y = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(')');
		double z = double.Parse(Data.Substring(start, end).Trim());
		Vector3D Gravity = new Vector3D(x,y,z);
		Vector3D Destination = -1*Gravity;
		Destination.Normalize();
		Destination = Me.GetPosition() + 5000 * Gravity;
		CoreNavigation.TryRun(CoreName + ":GoTo<(" + Destination.ToString() + ")>");
		Send("Havok Open Channel", "Gravity", "(" + Me.GetPosition().ToString() + ")");
		AddPrint("Set escape GPS to (" + Destination.ToString() + ")\nBroadcast edge of Gravity Well", true);
		UpdateStatus(DroneStatus.Escaping);
	}
	else if(Command.ToLower().Equals("reached")){
		int start = Data.IndexOf('(')+1;
		int end = Data.Substring(start).IndexOf(',');
		double x = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(',');
		double y = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(')');
		double z = double.Parse(Data.Substring(start, end).Trim());
		Vector3D Position = new Vector3D(x,y,z);
		if(Status == DroneStatus.Returning){
			HasReturned(Position);
		}
		else if(Status == DroneStatus.Venturing){
			HasArrived(Position);
		}
	}
	
}

private void SourceCommunications(string Command, string Data){
	if(Command.ToLower().Equals("receive")){
		int start = 0;
		int end = Data.IndexOf(' ');
		string Subcommand = Data.Substring(start, end);
		start += end+1;
		end=0;
		if(Subcommand.Equals("GoTo")){ //Data = "GoTo (x,y,z)"
			CoreNavigation.TryRun(CoreName + ":GoTo<" + Data.Substring(Data.IndexOf('(')) + '>');
			AddPrint("Directed CoreNavigation to GoTo " + Data.Substring(Data.IndexOf('(')), true);
		}
	}
	else if(Command.ToLower().Equals("follow")){
		int start = Data.IndexOf('(')+1;
		int end = Data.Substring(start).IndexOf(',');
		double x = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(',');
		double y = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(')');
		double z = double.Parse(Data.Substring(start, end).Trim());
		Vector3D position = new Vector3D(x,y,z);
		start = Data.Substring(start).IndexOf('(')+1;
		end = Data.Substring(start).IndexOf(',');
		x = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(',');
		y = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(')');
		z = double.Parse(Data.Substring(start, end).Trim());
		Vector3D velocity = new Vector3D(x,y,z);
		CoreNavigation.TryRun(CoreName + ":Follow<(" + position.ToString() + ");(" + velocity.ToString() + ")>");
		AddPrint("Commanding Navigation to follow position (" + position.ToString() + ") moving at (" + velocity.ToString() + ")", true);
	}
	else if(Command.ToLower().Equals("resupply")){
		int start = Data.IndexOf('(')+1;
		int end = Data.Substring(start).IndexOf(',');
		double x = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(',');
		double y = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(')');
		double z = double.Parse(Data.Substring(start, end).Trim());
		Vector3D flyto = new Vector3D(x,y,z);
		start = Data.Substring(start).IndexOf('(')+1;
		end = Data.Substring(start).IndexOf(',');
		x = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(',');
		y = double.Parse(Data.Substring(start, end).Trim());
		start += end+1;
		end = Data.Substring(start).IndexOf(')');
		z = double.Parse(Data.Substring(start, end).Trim());
		DockPort = new Vector3D(x,y,z);
		CoreNavigation.TryRun(CoreName + ":GoTo<(" + flyto.ToString() + ")>");
		AddPrint("Directed CoreNavigation to fly to (" + flyto.ToString() + ") to dock with (" + DockPort.ToString() + ")", true);
		CurrentTask = ActiveTask.Docking;
		Status = DroneStatus.Returning;
	}
	else if(Command.ToLower().Equals("attack")){
		if(Data.ToLower().Equals("request")){
			//TODO - perform attack request information using core logistics
		}
		else {
			if(CurrentTask == ActiveTask.None || CurrentTask == ActiveTask.Attacking){
				int start = Data.IndexOf('(')+1;
				int end = Data.Substring(start).IndexOf(',');
				double x = double.Parse(Data.Substring(start, end).Trim());
				start += end+1;
				end = Data.Substring(start).IndexOf(',');
				double y = double.Parse(Data.Substring(start, end).Trim());
				start += end+1;
				end = Data.Substring(start).IndexOf(')');
				double z = double.Parse(Data.Substring(start, end).Trim());
				Vector3D position = new Vector3D(x,y,z);
				start = Data.Substring(start).IndexOf('(')+1;
				end = Data.Substring(start).IndexOf(',');
				x = double.Parse(Data.Substring(start, end).Trim());
				start += end+1;
				end = Data.Substring(start).IndexOf(',');
				y = double.Parse(Data.Substring(start, end).Trim());
				start += end+1;
				end = Data.Substring(start).IndexOf(')');
				z = double.Parse(Data.Substring(start, end).Trim());
				Vector3D velocity = new Vector3D(x,y,z);
				CoreNavigation.TryRun(CoreName + ":Follow<(" + position.ToString() + ");(" + velocity.ToString() + ")>");
				CoreNavigation.TryRun(CoreName + ":Evasion<On>");
				AddPrint("Commanding Navigation to follow position (" + position.ToString() + ") moving at (" + velocity.ToString() + ")", true);
			}
		}
	}
	
}

private void SourceDiagnostics(string Command, string Data){
	if(Command.ToLower().Equals("report")){
		int start = 0;
		int end = Data.IndexOf(' ');
		string Subcommand = Data.Substring(start, end);
		start += end+1;
		end = Data.Substring(start).IndexOf(" at ");
		string Target = Data.Substring(start, end);
		start += end+1;
		float Percent = float.Parse(Data.Substring(start));
		if(Subcommand.ToLower().Equals("broke")){
			if(Percent <= 0.5){
				Systems = SystemStatus.Irreparable;
			} 
			else {
				if(Percent >= 0.75){
					Systems = SystemStatus.Warning;
				}
				else{
					Systems = SystemStatus.Critical;
				}
				Send(CoreIdentification, "Damaged", Target);
				AddPrint("Broadcast Damage Report", true);
			}
		}
		if(Subcommand.ToLower().Equals("fixed")){
			if(Percent >= 1.0){
				Send(CoreIdentification, "Fixed", Target);
				AddPrint("Broadcast Repair Report", true);
			}
		}
	}
	else if(Command.ToLower().IndexOf("power")==0){
		string Subcommand = Command.Substring("power".Length);
		if(Subcommand.ToLower().Equals("adequate")){
			if(Power != SystemStatus.Normal)
				Send(CoreIdentification, "Power", Subcommand);
			Power = SystemStatus.Normal;
		}
		else if(Subcommand.ToLower().Equals("low")){
			if(Power != SystemStatus.Warning)
				Send(CoreIdentification, "Power", Subcommand);
			Power = SystemStatus.Warning;
		}
		else if(Subcommand.ToLower().Equals("critical")){
			if(Power != SystemStatus.Critical)
				Send(CoreIdentification, "Power", Subcommand);
			Power = SystemStatus.Critical;
		}
	}
	else if(Command.ToLower().IndexOf("request")==0){
		string Subcommand = Command.Substring("request".Length);
		int index = 0;
		int length = Data.IndexOf(' ');
		string ComponentName = Data.Substring(index, length);
		index+=length+1;
		length = Data.Substring(index).IndexOf(" of ");
		string Count = Data.Substring(index, length);
		index+=length+" of ".Length;
		string Full = Data.Substring(index);
		if(ComponentName.ToLower().Equals("Missile200mm".ToLower())){
			if(Subcommand.ToLower().Equals("adequate")){
				if(Rockets != SystemStatus.Normal)
					Send(CoreIdentification, "ComponentAdequate", ComponentName);
				Rockets = SystemStatus.Normal;
			}
			else if(Subcommand.ToLower().Equals("low")){
				if(Rockets != SystemStatus.Warning)
					Send(CoreIdentification, "ComponentLow", ComponentName);
				Rockets = SystemStatus.Warning;
			}
			else if(Subcommand.ToLower().Equals("critical")){
				if(Rockets != SystemStatus.Critical)
					Send(CoreIdentification, "ComponentCritical", ComponentName);
				Rockets = SystemStatus.Critical;
			}
		}
		else if(ComponentName.ToLower().Equals("NATO_25x184mm".ToLower())){
			if(Subcommand.ToLower().Equals("adequate")){
				if(Magazines != SystemStatus.Normal)
					Send(CoreIdentification, "ComponentAdequate", ComponentName);
				Magazines = SystemStatus.Normal;
			}
			else if(Subcommand.ToLower().Equals("low")){
				if(Magazines != SystemStatus.Warning)
					Send(CoreIdentification, "ComponentLow", ComponentName);
				Magazines = SystemStatus.Warning;
			}
			else if(Subcommand.ToLower().Equals("critical")){
				if(Magazines != SystemStatus.Critical)
					Send(CoreIdentification, "ComponentCritical", ComponentName);
				Magazines = SystemStatus.Critical;
			}
		}
		else {
			if(Subcommand.ToLower().Equals("adequate")){
				if(Components != SystemStatus.Normal)
					Send(CoreIdentification, "ComponentAdequate", ComponentName);
				Components = SystemStatus.Normal;
			}
			else if(Subcommand.ToLower().Equals("low")){
				if(Components != SystemStatus.Warning)
					Send(CoreIdentification, "ComponentLow", ComponentName);
				Components = SystemStatus.Warning;
			}
			else if(Subcommand.ToLower().Equals("critical")){
				if(Components != SystemStatus.Critical)
					Send(CoreIdentification, "ComponentCritical", ComponentName);
				Components = SystemStatus.Critical;
			}
		}
	}
	else if(Command.ToLower().Equals("docking")){
		if(!Docked && Data.ToLower().Equals("docked")){
			Docked = true;
			Send(CoreIdentification, Command, Data);
			AddPrint("Docked Ship", true);
		}
		else if(Docked && Data.ToLower().Equals("undocked")){
			Docked = false;
			Send(CoreIdentification, Command, Data);
			AddPrint("Undocked Ship", true);
		}
		if(!Dockable && Data.ToLower().Equals("dockable")){
			Dockable = true;
			Send(CoreIdentification, Command, Data);
			AddPrint("Ship can Dock", true);
		}
		else if(Dockable && Data.ToLower().Equals("undockable")){
			Dockable = false;
			Send(CoreIdentification, Command, Data);
			AddPrint("Ship cannot Dock", true);
		}
	}
	else if(Command.ToLower().Equals("locking")){
		if(!Locked && Data.ToLower().Equals("locked")){
			Locked = true;
			Send(CoreIdentification, Command, Data);
			AddPrint("Locked Ship", true);
		}
		else if(Locked && Data.ToLower().Equals("unlocked")){
			Locked = false;
			Send(CoreIdentification, Command, Data);
			AddPrint("Unlocked Ship", true);
		}
		if(!Lockable && Data.ToLower().Equals("lockable")){
			Lockable = true;
			Send(CoreIdentification, Command, Data);
			AddPrint("Ship can Lock", true);
		}
		else if(Lockable && Data.ToLower().Equals("unlockable")){
			Lockable = false;
			Send(CoreIdentification, Command, Data);
			AddPrint("Ship cannot Lock", true);
		}
	}
}

private void SourceUnknown(string Source, string Command, string Data){
	
}

private void RequestRefuel(){
	Send(CoreIdentification, "Request", "Refuel");
}

private void RequestRepair(){
	Send(CoreIdentification, "Request", "Repair");
}

private bool Dock(){
	List<IMyShipConnector> AllConnectors = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllConnectors);
	foreach(IMyShipConnector connector in AllConnectors){
		if(connector.Status == MyShipConnectorStatus.Connectable){
			connector.Connect();
			if(connector.Status == MyShipConnectorStatus.Connected){
				Docked = true;
				AddPrint("Docked " + connector.CustomName, true);
			}
		}
	}
	return Docked;
}

private bool Undock(){
	List<IMyShipConnector> AllConnectors = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllConnectors);
	foreach(IMyShipConnector connector in AllConnectors){
		if(connector.Status == MyShipConnectorStatus.Connected){
			connector.Disconnect();
			if(connector.Status != MyShipConnectorStatus.Connected){
				Docked = false;
				AddPrint("Undocked " + connector.CustomName, true);
			}
			else {
				Docked = true;
				return false;
			}
		}
	}
	return !Docked;
}

private void LowPriority(DroneStatus normal_status, DroneStatus refuel_status, DroneStatus damage_status){
	switch(WorstStatus){
		case SystemStatus.Normal:
			if((int) Status < (int) DroneStatus.Escaping){
				UpdateStatus(normal_status);
			}
			break;
		case SystemStatus.Warning:
			if((int) Status < (int) DroneStatus.Escaping){
				if(Systems != WorstStatus){
					RequestRefuel();
					UpdateStatus(refuel_status);
				}
				else{
					RequestRepair();
					UpdateStatus(damage_status);
				}
			}
			break;
		case SystemStatus.Critical:
			if((int) Status < (int) DroneStatus.Escaping){
				if(Systems != WorstStatus){
					RequestRefuel();
					UpdateStatus(refuel_status);
				}
				else{
					RequestRepair();
					UpdateStatus(damage_status);
				}
			}
			break;
		case SystemStatus.Irreparable:
			UpdateStatus(DroneStatus.Salvage);
			break;
	}
}

private void MedPriority(DroneStatus normal_status, DroneStatus refuel_status, DroneStatus damage_status){
	switch(WorstStatus){
		case SystemStatus.Normal:
			if((int) Status < (int) DroneStatus.Escaping){
				UpdateStatus(normal_status);
			}
			break;
		case SystemStatus.Warning:
			if((int) Status < (int) DroneStatus.Escaping){
				UpdateStatus(normal_status);
			}
			break;
		case SystemStatus.Critical:
			if(Systems != WorstStatus){
				RequestRefuel();
				UpdateStatus(refuel_status);
			}
			else{
				RequestRepair();
				UpdateStatus(damage_status);
			}
			break;
		case SystemStatus.Irreparable:
			UpdateStatus(DroneStatus.Salvage);
			break;
	}
}

private void HighPriority(DroneStatus normal_status, DroneStatus refuel_status, DroneStatus damage_status){
	switch(WorstStatus){
		case SystemStatus.Normal:
			if((int) Status < (int) DroneStatus.Escaping){
				UpdateStatus(normal_status);
			}
			break;
		case SystemStatus.Warning:
			UpdateStatus(normal_status);
			break;
		case SystemStatus.Critical:
			if(Systems != WorstStatus){
				RequestRefuel();
				UpdateStatus(refuel_status);
			}
			else{
				RequestRepair();
				UpdateStatus(damage_status);
			}
			break;
		case SystemStatus.Irreparable:
			UpdateStatus(DroneStatus.Salvage);
			break;
	}
}

private SystemStatus WorstStatus = SystemStatus.Normal;

private void Strategize(){
	WorstStatus = SystemStatus.Normal;
	WorstStatus = (SystemStatus) Math.Max((int) WorstStatus, (int) Systems);
	WorstStatus = (SystemStatus) Math.Max((int) WorstStatus, (int) Power);
	WorstStatus = (SystemStatus) Math.Max((int) WorstStatus, (int) Rockets);
	WorstStatus = (SystemStatus) Math.Max((int) WorstStatus, (int) Magazines);
	//SystemStatus
	//Normal = 1,
	//Warning = 2,
	//Critical = 3,
	//Irreparable = 4
	
	//DroneStatus - Status
	//Salvage = -1,
	//Fresh = 0,
	//Idle = 1,
	//Waiting = 2,
	//Returning = 3,
	//Venturing = 4,
	//Performing = 5,
	//Escaping = 6,
	//CriticalTask = 7
	
	
	//None = 0,
	//Scouting = 1,
	//Salvaging = 2,
	//Devouring = 3,
	//Delivering = 4,
	//Refueling = 5,
	//Docking = 6,
	//Attacking = 7,
	//Mining = 8,
	//Building = 9,
	//Printing = 10,
	//Tracking = 11,
	//Defending = 12,
	//Yelling = 13,
	//Kamikaze = 14
	switch(CurrentTask){
		case ActiveTask.None:
			LowPriority(DroneStatus.Idle, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Scouting:
			LowPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Salvaging:
			LowPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Devouring:
			LowPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;	
		case ActiveTask.Delivering:
			LowPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Refueling:
			//TODO: perform refueling process here; probably a call to diagnostics
			break;
		case ActiveTask.Docking:
			if(DockPort==null){
				UpdateStatus(DroneStatus.Waiting);
				if(Systems != WorstStatus){
					RequestRefuel();
				}
				else{
					RequestRepair();
				}
			}
			else{
				if(Dockable){
					Docked = Docked || Dock();
				}
				if(Docked){
					CoreNavigation.TryRun(CoreName + ":Docked<>");
					CurrentTask = ActiveTask.Refueling;
					UpdateStatus(DroneStatus.Waiting);
					Strategize();
					return;
				}
			}
			break;
		case ActiveTask.Attacking:
			MedPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Mining:
			MedPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Building:
			MedPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Printing:
			MedPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.Escaping);
			break;
		case ActiveTask.Tracking:
			//TODO: perform tracking process here
			break;
		case ActiveTask.Defending:
			HighPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.CriticalTask);
			break;
		case ActiveTask.Yelling:
			HighPriority(DroneStatus.Performing, DroneStatus.Waiting, DroneStatus.CriticalTask);
			break;
		case ActiveTask.Kamikaze:
			HighPriority(DroneStatus.Performing, DroneStatus.CriticalTask, DroneStatus.CriticalTask);
			break;
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
		if(!BlocksSet){
			SetBlocks();
			AddPrint("Started Program", true);
		}
		CoreDirective.TryRun(CoreName + ":Started");
		AddPrint("Responded to Core Directive", true);
		if(Changed_ID){
			string ShipClass = ShipName;
			if(ShipClass.Contains('-')){
				ShipClass = ShipClass.Substring(0, ShipClass.IndexOf('-'));
			}
			Send(ShipClass, "NewID", CoreIdentification);
			AddPrint("Sent out notification of new CoreIdentification", true);
			Changed_ID = false;
		}
		FinalPrint();
		return;
	} else if(argument.ToLower().Equals("terminal:reset")){
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
		if(0 < argument.IndexOf(':') && argument.IndexOf(':') < argument.IndexOf('<') && argument.IndexOf('<') < argument.IndexOf('>')){
			int start = 0;
			int end = argument.Substring(start).IndexOf(':') - start;
			string Source = argument.Substring(start, end).Trim();
			start += end+1;
			end = argument.Substring(start).IndexOf('<');
			string Command = argument.Substring(start, end).Trim();
			start += end+1;
			end = argument.Substring(start).IndexOf('>');
			string Data = argument.Substring(start, end).Trim();
			//AddPrint("Received Data...\nSource=" + Source + "\nCommand=" + Command + "\nData=" + Data, true);
			if(Source.Equals("CoreNavigation")){
				SourceNavigation(Command, Data);
			}
			else if(Source.Equals("CoreCommunications")){
				SourceCommunications(Command, Data);
			}
			else if(Source.Equals("CoreDiagnostics")){
				SourceDiagnostics(Command, Data);
			}
			Strategize();
		}
	} else {
		AddPrint("Cannot run program --- blocks not set!", false);
	}
	FinalPrint();
}
