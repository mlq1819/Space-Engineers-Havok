//Havok Core Directive Information
//Semi-customizable program that mostly sets up information for the other core processors
//private List<string> Listener_Tags;
//private List<string> Critical_Components;


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

private string CoreIdentification = "";
private const string CoreName = "CoreDirective";
private IMyProgrammableBlock CoreStrategy = null;
private IMyProgrammableBlock CoreNavigation = null;
private IMyProgrammableBlock CoreDiagnostics = null;
private IMyProgrammableBlock CoreCommunications = null;
private IMyProgrammableBlock CoreDirective = null;
private string toEcho = "";
private char loadingChar = '|';
private List<string> message_history = new List<string>();
private List<string> argument_history = new List<string>();
private bool BlocksSet = false;
private long Cycle = 0;
private long Long_Cycle = 1;
private bool Started_Strategy = false;
private bool Started_Navigation = false;
private bool Started_Diagnostics = false;
private bool Started_Communications = false;
private int TryCount = 0;

private bool AllStarted(){
	return Started_Strategy && Started_Navigation && Started_Diagnostics && Started_Communications;
}

private void AddPrint(string message, bool AddToHistory){
	toEcho += message + '\n';
	if(AddToHistory)
		message_history.Add(message);
}

private void FinalPrint(){
	toEcho = "Cycle " + Long_Cycle + '-' + (++Cycle) + "\nCoreIdentification: " + CoreIdentification + '\n' + toEcho;
	if(argument_history.Count > 50){
		List<string> new_history = new List<string>();
		for(int i=25; i<argument_history.Count; i++){
			new_history.Add(argument_history[i]);
		}
		argument_history = new_history;
	}
	if(message_history.Count > 20){
		List<string> new_history = new List<string>();
		for(int i=10; i<message_history.Count; i++){
			new_history.Add(message_history[i]);
		}
		message_history = new_history;
	}
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
	if(argument_history.Count > 0){
		AddPrint("\n\n\nArgument History:", false);
		for(int i=argument_history.Count-1; i>=0; i++){
			AddPrint("\t" + argument_history[i] + "\n------------", false);
		}
	}
	AddPrint("\n\n\n", false);
	Echo(toEcho);
	toEcho = "";
}

public void SetBlocks(bool retry){
	try{
		if(Me.CubeGrid.CustomName.Contains('-')){
			CoreIdentification = Me.CubeGrid.CustomName;
			try{
				int CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')+1));
				if(CoreIDNumber == 0){
					AddPrint("Currently in Factory Default Settings", true);
					FinalPrint();
					BlocksSet = false;
					return;
				}
			}
			catch(FormatException){
				AddPrint("Invalid CoreIdentification: " + CoreIdentification + "\nWiping data...", true);
				Me.CustomData = "";
				CoreIdentification = "";
				Me.CubeGrid.CustomName = Me.CubeGrid.CustomName.Substring(0,Me.CubeGrid.CustomName.IndexOf('-'));
				BlocksSet = false;
			}
		}
		if(!retry || (Me.CustomData == "" && CoreIdentification.Equals(""))){ //fresh processor; because this is the directive program, it sets up the identification for all other processors
			retry = false;
			Random rnd = new Random();
			if(Me.CubeGrid.CustomName.Contains('-')){
				Me.CubeGrid.CustomName = Me.CubeGrid.CustomName.Substring(0,Me.CubeGrid.CustomName.IndexOf('-'));
			}
			CoreIdentification = Me.CubeGrid.CustomName + '-' + rnd.Next(1, Int32.MaxValue).ToString();
			Me.CubeGrid.CustomName = CoreIdentification;
			AddPrint("New CoreIdentification: \"" + CoreIdentification + "\"", true);
			CoreDirective = Me;
			CoreDirective.CustomData = CoreIdentification;
			CoreDirective.CustomName = "Core Directive Processor";
			List<IMyProgrammableBlock> AllProgBlocks = new List<IMyProgrammableBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(AllProgBlocks);
			for(int i=0; i<AllProgBlocks.Count; i++){
				double distance = (CoreDirective.GetPosition()-AllProgBlocks[i].GetPosition()).Length();
				if(distance < 1.5){
					if(AllProgBlocks[i].CustomName.Contains("Core Strategy Processor")){
						CoreStrategy = AllProgBlocks[i];
						CoreStrategy.CustomData = CoreIdentification;
						CoreStrategy.CustomName = "Core Strategy Processor";
						CoreStrategy.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
						AddPrint("Found " + CoreStrategy.CustomName + " at " + distance.ToString() + " meters from Core Directive\nSet CoreIdentification for " + CoreStrategy.CustomName, true);
					}
					else if(AllProgBlocks[i].CustomName.Contains("Core Navigation Processor")){
						CoreNavigation = AllProgBlocks[i];
						CoreNavigation.CustomData = CoreIdentification;
						CoreNavigation.CustomName = "Core Navigation Processor";
						CoreNavigation.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
						AddPrint("Found " + CoreNavigation.CustomName + " at " + distance.ToString() + " meters from Core Directive\nSet CoreIdentification for " + CoreNavigation.CustomName, true);
					}
					else if(AllProgBlocks[i].CustomName.Contains("Core Diagnostics Processor")){
						CoreDiagnostics = AllProgBlocks[i];
						CoreDiagnostics.CustomData = CoreIdentification;
						CoreDiagnostics.CustomName = "Core Diagnostics Processor";
						CoreDiagnostics.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
						AddPrint("Found " + CoreDiagnostics.CustomName + " at " + distance.ToString() + " meters from Core Directive\nSet CoreIdentification for " + CoreDiagnostics.CustomName, true);
					}
					else if(AllProgBlocks[i].CustomName.Contains("Core Communications Processor")){
						CoreCommunications = AllProgBlocks[i];
						CoreCommunications.CustomData = CoreIdentification;
						CoreCommunications.CustomName = "Core Communications Processor";
						CoreCommunications.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
						AddPrint("Found " + CoreCommunications.CustomName + " at " + distance.ToString() + " meters from Core Directive\nSet CoreIdentification for " + CoreCommunications.CustomName, true);
					}
				}
			}
			if(CoreStrategy!=null && CoreNavigation!=null && CoreDiagnostics!=null && CoreCommunications!=null){
				AddPrint("Set object references to all 5 Cores", true);
			}
			else {
				string message = "";
				if(CoreStrategy == null){
					message+="\nMissing Core Strategy Processor";
				}
				if(CoreNavigation == null){
					message+="\nMissing Core Navigation Processor";
				}
				if(CoreDiagnostics == null){
					message+="\nMissing Core Diagnostics Processor";
				}
				if(CoreCommunications == null){
					message+="\nMissing Core Communications Processor";
				}
				throw new Exception(message.Substring(1));
			}
		} 
		else { //has been run before
			if(!CoreIdentification.Equals(Me.CustomData)){
				CoreIdentification = Me.CustomData;
				AddPrint("Retrieved CoreIdentification: " + CoreIdentification, true);
			}
			int CoreIDNumber = 0;
			if(CoreIdentification.Contains('-'))
				CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')+1));
			else
				CoreIDNumber = Int32.Parse(CoreIdentification);
			if(CoreIDNumber == 0){
				AddPrint("Currently in Factory Default Settings", true);
				FinalPrint();
				BlocksSet = false;
				return;
			}
			else {
				AddPrint("Valid ID number: " + CoreIDNumber, true);
			}
			List<IMyProgrammableBlock> AllProgBlocks = new List<IMyProgrammableBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(AllProgBlocks);
			IMyProgrammableBlock CoreStrategy = null;
			IMyProgrammableBlock CoreNavigation = null;
			IMyProgrammableBlock CoreDiagnostics = null;
			IMyProgrammableBlock CoreCommunications = null;
			IMyProgrammableBlock CoreDirective = null;
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
			bool set_strategy = false;
			bool set_navigation = false;
			bool set_diagnostics = false;
			bool set_communications = false;
			if(CoreStrategy == null){
				CoreStrategy = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("New Core Strategy Processor");
				if(CoreStrategy != null){
					CoreStrategy.CustomName = "Core Strategy Processor";
					CoreStrategy.CustomData = CoreIdentification;
					set_strategy = true;
					core_count++;
				}
			}
			if(CoreNavigation == null){
				CoreNavigation = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("New Core Navigation Processor");
				if(CoreNavigation != null){
					CoreNavigation.CustomName = "Core Navigation Processor";
					CoreNavigation.CustomData = CoreIdentification;
					set_navigation = true;
					core_count++;
				}
			}
			if(CoreDiagnostics == null){
				CoreDiagnostics = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("New Core Diagnostics Processor");
				if(CoreDiagnostics != null){
					CoreDiagnostics.CustomName = "Core Diagnostics Processor";
					CoreDiagnostics.CustomData = CoreIdentification;
					set_diagnostics = true;
					core_count++;
				}
			}
			if(CoreCommunications == null){
				CoreCommunications = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("New Core Communications Processor");
				if(CoreCommunications != null){
					CoreCommunications.CustomName = "Core Communications Processor";
					CoreCommunications.CustomData = CoreIdentification;
					set_communications = true;
					core_count++;
				}
			}
			if(CoreDirective == null){
				CoreDirective = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName(Me.CustomName);
				if(CoreDirective != null){
					CoreDirective.CustomName = "Core Directive Processor";
					CoreDirective.CustomData = CoreIdentification;
					core_count++;
				}
			}
			if(set_strategy){
				CoreStrategy.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
			}
			if(set_navigation){
				CoreNavigation.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
			}
			if(set_diagnostics){
				CoreDiagnostics.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
			}
			if(set_communications){
				CoreCommunications.TryRun(CoreName + ":Set<" + CoreIdentification + '>');
			}
			string exception_message = "";
			bool exception = false;
			if(core_count > 5){
				exception = true;
				retry = false;
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
				throw new Exception(exception_message.Substring(1));
			}
			AddPrint("Set object references to all 5 Cores", true);
		}
		CoreStrategy.TryRun(CoreName + ":Start");
		CoreNavigation.TryRun(CoreName + ":Start");
		CoreDiagnostics.TryRun(CoreName + ":Start");
		CoreCommunications.TryRun(CoreName + ":Start");
		Started_Strategy = false;
		Started_Navigation = false;
		Started_Diagnostics = false;
		Started_Communications = false;
		TryCount = 0;
		AddPrint("Started Core Processors", true);
		if(!AllStarted())
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
	} catch (Exception e){
		AddPrint("Critical Core Failure: " + e.ToString(), true);
		if(retry){
			Me.CustomData = "";
			if(CoreStrategy != null && !CoreStrategy.CustomName.Contains("New ")){
				CoreStrategy.CustomName = "New " + CoreStrategy.CustomName;
			}
			if(CoreNavigation != null && !CoreNavigation.CustomName.Contains("New ")){
				CoreNavigation.CustomName = "New " + CoreNavigation.CustomName;
			}
			if(CoreDiagnostics != null && !CoreDiagnostics.CustomName.Contains("New ")){
				CoreDiagnostics.CustomName = "New " + CoreDiagnostics.CustomName;
			}
			if(CoreCommunications != null && !CoreCommunications.CustomName.Contains("New ")){
				CoreCommunications.CustomName = "New " + CoreCommunications.CustomName;
			}
			if(CoreDirective != null && !CoreDirective.CustomName.Contains("New ")){
				CoreDirective.CustomName = "New " + CoreDirective.CustomName;
			}
			CoreIdentification = "";
			Me.CustomData = "";
			AddPrint("Resetting CustomNames and CoreIdentification; attempting to retry from scratch", true);
			SetBlocks(false);
			return;
		}
	}
}

public void SetBlocks(){
	SetBlocks(true);
}

public void Initialize()
{
	SetBlocks();
	FinalPrint();
}

public Program(){
	try{
		if(ConfirmCoreName()){
			Initialize();
		} else {
			throw new Exception("Correct CoreName to \"" + GetCoreName() + "\" (currently \"" + CoreName + "\")");
		}
		Me.GetSurface(0).WriteText(CoreName, false);
	}
	catch(Exception e){
		BlocksSet = false;
		AddPrint("Exception:\n" + e.ToString(), true);
		FinalPrint();
	}
}

public void Save()
{
    this.Storage = CoreIdentification + "\n" + this.Storage;
	
	// Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

private void Wipe(){
	if(CoreStrategy != null){
		CoreStrategy.TryRun("Terminal:Reset");
	}
	if(CoreNavigation != null){
		CoreNavigation.TryRun("Terminal:Reset");
	}
	if(CoreDiagnostics != null){
		CoreDiagnostics.TryRun("Terminal:Reset");
	}
	if(CoreCommunications != null){
		CoreCommunications.TryRun("Terminal:Reset");
	}
	this.Storage = "";
	Me.CustomData = "";
	CoreIdentification = "0";
	BlocksSet = false;
	Cycle = 0;
	Long_Cycle = 1;
	message_history = new List<string>();
	Runtime.UpdateFrequency = UpdateFrequency.None;
	AddPrint("Factory Reset Settings and Cleared Storage", true);
	Initialize();
}

public bool CheckValidID(){
	int CoreIDNumber = 0;
	try{
		if(CoreIdentification.Contains('-'))
			CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')+1));
		else
			CoreIDNumber = Int32.Parse(CoreIdentification);
		if(CoreIDNumber == 0){
			AddPrint("Currently in Factory Default Settings", true);
			FinalPrint();
			BlocksSet = false;
			return false;
		}
	}
	catch(FormatException e){
		AddPrint("Invalid ID:" + CoreIdentification + "\nWiping ID...", true);
		Wipe();
		FinalPrint();
		return false;
	}
	return true;
}

public void Run(string argument, UpdateType updateSource)
{
	if(!CheckValidID())
		return;
	if(argument.ToLower().Equals("wipe")){
		Wipe();
	}
	if(argument.ToLower().Equals("reset") || argument.ToLower().Equals("terminal:reset")){
		message_history = new List<string>();
		try{
			SetBlocks();
			if(CoreStrategy!=null){
				CoreStrategy.CustomData="";
				CoreStrategy.CustomName="New Core Strategy Processor";
				CoreStrategy.TryRun(CoreName + ":Reset");
			}
			if(CoreNavigation!=null){
				CoreNavigation.CustomData="";
				CoreNavigation.CustomName="New Core Navigation Processor";
				CoreNavigation.TryRun(CoreName + ":Reset");
			}
			if(CoreDiagnostics!=null){
				CoreDiagnostics.CustomData="";
				CoreDiagnostics.CustomName="New Core Diagnostics Processor";
				CoreDiagnostics.TryRun(CoreName + ":Reset");
			}
			if(CoreCommunications!=null){
				CoreCommunications.CustomData="";
				CoreCommunications.CustomName="New Core Communications Processor";
				CoreCommunications.TryRun(CoreName + ":Reset");
			}
			CoreDirective.CustomData="";
			CoreDirective.CustomName="New Core Directive Processor";
			if(Me.CubeGrid.CustomName.Contains('-')){
				Me.CubeGrid.CustomName = Me.CubeGrid.CustomName.Substring(0, Me.CubeGrid.CustomName.IndexOf('-'));
			}
			CoreIdentification = "";
			this.Storage = "";
			BlocksSet = false;
			Cycle = 0;
			Long_Cycle = 1;
			message_history = new List<string>();
			Runtime.UpdateFrequency = UpdateFrequency.None;
			AddPrint("Successfully reset data; now initializing...", true);
			Initialize();
		} catch (Exception e){
			AddPrint("Exception during Reset: " + e.ToString(), true);
		}
	}
	int CoreIDNumber = 0;
	if(CoreIdentification.Contains('-'))
		CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')+1));
	else
		CoreIDNumber = Int32.Parse(CoreIdentification);
	if(CoreIDNumber == 0){
		AddPrint("Currently in Factory Default Settings", true);
		FinalPrint();
		BlocksSet = false;
		return;
	}
	else if(argument.ToLower().Contains(":started")){
		AddPrint("Received \":started\" argument: " + argument, true);
		if(argument.ToLower().Contains("corestrategy:")){
			Started_Strategy = true;
			AddPrint("Core Strategy has Started", true);
		}
		if(argument.ToLower().Contains("corenavigation:")){
			Started_Navigation = true;
			AddPrint("Core Navigation has Started", true);
		}
		if(argument.ToLower().Contains("corediagnostics:")){
			Started_Diagnostics = true;
			AddPrint("Core Diagnostics has Started", true);
		}
		if(argument.ToLower().Contains("corecommunications:")){
			Started_Communications = true;
			AddPrint("Core Communications has Started", true);
		}
		if(AllStarted()){
			Runtime.UpdateFrequency = UpdateFrequency.None;
			AddPrint("All Processors Have Started", true);
		}
	}
	else if(argument.ToLower().Contains("set<")){
		int index = argument.IndexOf('<')+1;
		int length = argument.Substring(index).IndexOf('>');
		CoreIdentification = argument.Substring(index, length);
		AddPrint("Set CoreIdentification: " + CoreIdentification, true);
	} else if(argument.ToLower().Contains("stop")){
		Runtime.UpdateFrequency = UpdateFrequency.None;
		AddPrint("Stopped Updating", true);
	}
	else{
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
		AddPrint("Running program... (" + loadingChar + ")", false);
		if(TryCount++>3){
			Runtime.UpdateFrequency = UpdateFrequency.None;
			AddPrint("Stopped Updating", true);
		} else {
			if(!Started_Strategy){
				CoreStrategy.TryRun(CoreName + ":Start");
				AddPrint("Attempting to start Core Strategy...", false);
			} else{
				AddPrint("Core Strategy is Active", false);
			}
			if(!Started_Navigation){
				CoreNavigation.TryRun(CoreName + ":Start");
				AddPrint("Attempting to start Core Navigation...", false);
			} else{
				AddPrint("Core Navigation is Active", false);
			}
			if(!Started_Diagnostics){
				CoreDiagnostics.TryRun(CoreName + ":Start");
				AddPrint("Attempting to start Core Diagnostics...", false);
			} else{
				AddPrint("Core Diagnostics is Active", false);
			}
			if(!Started_Communications){
				CoreCommunications.TryRun(CoreName + ":Start");
				AddPrint("Attempting to start Core Communications...", false);
			} else{
				AddPrint("Core Communications is Active", false);
			}
		}
	}
	FinalPrint();
}

public void Main(string argument, UpdateType updateSource){
	argument_history.Add(argument);
	try{
		Run(argument, updateSource);
	}
	catch(Exception e){
		AddPrint("Exception:\n" + e.ToString(), true);
		BlocksSet = false;
		Runtime.UpdateFrequency = UpdateFrequency.None;
		FinalPrint();
	}
}