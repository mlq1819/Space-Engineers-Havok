//Havok Core programming information

public struct ProgRunTuple{
	public IMyProgrammableBlock Block;
	public string Command;
	public ProgRunTuple(IMyProgrammableBlock b, string c){
		Block = b;
		Command = c;
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
private const string CoreName = "CoreBase";
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
private List<ProgRunTuple> programs_to_run = new List<ProgRunTuple>();

private void RunOldCommands(){
	if(programs_to_run.Count > 0){
		List<ProgRunTuple> old_progs = programs_to_run;
		programs_to_run = new List<ProgRunTuple>();
		for(int i=0; i<old_progs.Count; i++){
			ProgRunTuple tuple = old_progs[i];
			TryRunCommand(tuple.Block, tuple.Command);
		}
	}
}

private bool TryRunCommand(IMyProgrammableBlock block, string command){
	if(block.IsRunning){
		programs_to_run.Add(new ProgRunTuple(block, command));
		return false;
	}
	block.TryRun(CoreName + ":" + command);
	return true;
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
		for(int i=argument_history.Count-1; i>=0; i--){
			AddPrint("\t" + argument_history[i] + "\n------------", false);
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
			int CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')+1));
			if(CoreIDNumber == 0){
				AddPrint("Currently in Factory Default Settings", true);
				FinalPrint();
				BlocksSet = false;
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
	try{
		ShipName = Me.CubeGrid.CustomName;
		if(ConfirmCoreName()){
			Initialize();
		} else {
			throw new Exception("Correct CoreName to \"" + GetCoreName() + "\" (currently \"" + CoreName + "\")");
		}
		Me.GetSurface(0).WriteText(CoreName, false);
	}
	catch(Exception e){
		BlocksSet = false;
		AddPrint("Exception:\n" + e.Message, true);
		FinalPrint();
	}
}

public void Save()
{
	Me.CustomData = CoreIdentification;
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
	AddPrint("Factory Reset Settings and Cleared Storage", true);
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
	AddPrint("Set(" + argument + ")", true);
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
	if(argument.Equals("CoreDirective:Stop")){
		Runtime.UpdateFrequency = UpdateFrequency.None;
		AddPrint("Stop Command received", true);
		FinalPrint();
		return;
	}
	else if(argument.Equals("CoreDirective:Reset")){
		Reset();
		FinalPrint();
		return;
	} else if(argument.Contains("CoreDirective:Set<")){
		Set(argument);
		FinalPrint();
		return;
	}
	if(!CheckValidID())
		return;
	if(argument.Equals("CoreDirective:Start")){
		if(!BlocksSet){
			SetBlocks();
			AddPrint("Started Program", true);
		}
		TryRunCommand(CoreDirective, "Started");
		AddPrint("Responded to Core Directive", true);
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
		// The main entry point of the script, invoked every time
		// one of the programmable block's Run actions are invoked,
		// or the script updates itself. The updateSource argument
		// describes where the update came from.
		// 
		// The method itself is required, but the arguments above
		// can be removed if not needed.
	} else {
		AddPrint("Cannot run program --- blocks not set!", false);
	}
	FinalPrint();
}

public void Main(string argument, UpdateType updateSource){
	if(argument.Length > 0)
		argument_history.Add(argument);
	try{
		RunOldCommands();
		Run(argument, updateSource);
	}
	catch(Exception e){
		AddPrint("Exception:\n" + e.ToString(), true);
		BlocksSet = false;
		Runtime.UpdateFrequency = UpdateFrequency.None;
		FinalPrint();
	}
}
