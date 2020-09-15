//Havok Core Communications Program
//Manage broadcasting and receiving messages

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
private const string CoreName = "CoreCommunications";
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

private List<string> listener_tags = new List<string>{"Havok Open Channel"};
private List<MyIGCMessage> history = new List<MyIGCMessage>();

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


//Runs the scanner and passes any new messages to Core Strategy
private void Scanner(string argument){
	for(int i=0; i<listener_tags.Count; i++){
		IGC.RegisterBroadcastListener(listener_tags[i]);
	}
	List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
	IGC.GetBroadcastListeners(listeners);
	if(listeners.Count > 0){
		AddPrint("Scanning on " + listeners.Count + " listeners...", false);
		for(int i=0; i<listeners.Count; i++){
			AddPrint("Listening on \"" + listeners[i].Tag + "\"", false);
			while(listeners[i].HasPendingMessage){
				MyIGCMessage message = new MyIGCMessage();
				message = listeners[i].AcceptMessage();
				history.Add(message);
				AddPrint("<" + message.Tag + ";" + message.Data.ToString() + ";" + message.Source + ">", true);
				CoreStrategy.TryRun(CoreName + ":Receive<" + message.Data.ToString() + ">");
			}
		}
	}
}

//Broadcasts a message from Core Strategy
private void Send(string Data){
	try{
		string Tag = "";
		string Message = "";
		int index = Data.IndexOf(';');
		Tag = Data.Substring(0,index);
		Message = Data.Substring(index+1);
		IGC.SendBroadcastMessage(Tag, Message, TransmissionDistance.TransmissionDistanceMax);
		toEcho+="Broadcast message on <" + Tag + ";" + Message + ">\n";
		message_history.Add("Send <" + Tag + ";" + Message + ">");
	} catch (Exception e){
		toEcho+="Invalid Data: " + e.Message + "\n";
		message_history.Add("Invalid Data: " + e.Message);
	}
}

//Parses the argument to determine a course of action
private void UnknownScriptCommand(string argument){
	try{
		string SourceName = "";
		string CommandName = "";
		string Data = "";
		
		int index = 0;
		int length = argument.IndexOf(':'); 
		SourceName = argument.Substring(index, length);
		index += length + 1;
		length = argument.Substring(index).IndexOf("<");
		CommandName = argument.Substring(index, length);
		index += length + 1;
		length = argument.Substring(index).IndexOf(">");
		Data = argument.Substring(index, length);
		history.Add(new MyIGCMessage(Data, CommandName, -1 * SourceName.GetHashCode()));
		AddPrint("Parsed Command: \nCommand:" + CommandName + "\nData:" + Data + "\nSource:" + SourceName, false);
		if(CommandName.ToLower().Equals("send")){
			Send(Data);
		}
		else if(CommandName.ToLower().Equals("addtag")){
			if(!listener_tags.Contains(Data)){
				listener_tags.Add(Data);
				AddPrint("Now listening on tag: \"" + Data + "\"", true);
			} else {
				AddPrint("Already listening on tag: \"" + Data + "\"", true);
			}
		}
		else if(CommandName.ToLower().Equals("clear")){
			history = new List<MyIGCMessage>();
			this.Storage = "";
			AddPrint("Cleared history and storage", true);
		}
	} catch (Exception e){
		AddPrint("Invalid Data: " + e.Message, true);
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
			int CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')));
			if(CoreIDNumber == 0){
				AddPrint("Currently in Factory Default Settings", true);
				FinalPrint();
				BlocksSet = false;
				return;
			}
			if(!listener_tags.Contains(CoreIdentification))
				listener_tags.Add(CoreIdentification);
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
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			}
			listener_tags = new List<string>{"Havok Open Channel"};
			if(ShipName.Contains('-')){
				if(!listener_tags.Contains(ShipName.Substring(0, ShipName.IndexOf('-'))))
					listener_tags.Add(ShipName.Substring(0, ShipName.IndexOf('-')));
			}
			if(!listener_tags.Contains(CoreIdentification))
				listener_tags.Add(CoreIdentification);
			
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

public void GetTags(){
	string text = this.Storage;
	int index = text.IndexOf("Tag<");
	if(index>=0){
		int length = text.Substring(index).IndexOf(">\n");
		while(index >=0 && length > 0 && index+length < text.Length){
			string Tag = text.Substring(index, length);
			Tag = Tag.Substring("Tag<".Length);
			if(!listener_tags.Contains(Tag))
				listener_tags.Add(Tag);
			index += length+1;
			index += text.Substring(index).IndexOf("Tag<");
			length = text.Substring(index).IndexOf(">\n");
		}
	}
	this.Storage = "";
}

public Program()
{
	try{
		ShipName = Me.CubeGrid.CustomName;
		if(ConfirmCoreName()){
			GetTags();
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
	this.Storage = "";
	for(int i=0; i<listener_tags.Count; i++){
		string output = "Tag<" + listener_tags[i] + ">";
		this.Storage += output + '\n';
	}
    for(int i=0; i<history.Count; i++){
		string output = "<" + history[i].Tag + ";" + history[i].Data.ToString() + ";" + history[i].Source.ToString() + ">";
		this.Storage = this.Storage + output + "\n";
	}
}

private void Wipe(){
	this.Storage = "";
	Me.CustomData = "";
	CoreIdentification = "0";
	BlocksSet = false;
	Cycle = 0;
	Long_Cycle = 1;
	message_history = new List<string>();
	listener_tags = new List<string>{"Havok Open Channel"};
	List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
	IGC.GetBroadcastListeners(listeners);
	foreach(IMyBroadcastListener listener in listeners){
		IGC.DisableBroadcastListener(listener);
	}
	Runtime.UpdateFrequency = UpdateFrequency.None;
	AddPrint("Reset Settings and Cleared Storage", true);
	Initialize();
}

private void Reset(){
	listener_tags.Remove(CoreIdentification);
	List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
	IGC.GetBroadcastListeners(listeners);
	foreach(IMyBroadcastListener listener in listeners){
		IGC.DisableBroadcastListener(listener);
	}
	this.Storage = "";
	CoreIdentification = "";
	Me.CustomData = "";
	AddPrint("Reset CoreIdentification, Storage, and CustomData", true);
	BlocksSet = false;
}

private void Set(string argument){
	ShipName = Me.CubeGrid.CustomName;
	int index = argument.IndexOf('<')+1;
	int length = argument.Substring(index).IndexOf('>');
	if(!argument.Substring(index,length).Equals(CoreIdentification)){
		CoreIdentification = argument.Substring(index, length);
		BlocksSet = false;
		AddPrint("Set CoreIdentification to \"" + CoreIdentification + "\"", true);
	}
	Me.CustomData = CoreIdentification;
}

public void Run(string argument, UpdateType updateSource)
{
	int CoreIDNumber = 0;
	if(CoreIdentification.Contains('-'))
		CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')));
	else
		CoreIDNumber = Int32.Parse(CoreIdentification);
	if(CoreIDNumber == 0){
		AddPrint("Currently in Factory Default Settings", true);
		FinalPrint();
		BlocksSet = false;
		return;
	}
	try{
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
			switch(updateSource){
				case UpdateType.Terminal:
					UnknownScriptCommand(argument);
					break;
				case UpdateType.Script:
					UnknownScriptCommand(argument);
					break;
				case UpdateType.Update1:
					Scanner(argument);
					break;
				case UpdateType.Update10:
					Scanner(argument);
					break;
				case UpdateType.Update100:
					Scanner(argument);
					break;
				default:
					break;
			}
		} else {
			AddPrint("Cannot run program --- blocks not set!", false);
		}
	} catch (Exception e){
		FinalPrint();
		throw e;
	}
	FinalPrint();
}

public void Main(string argument, UpdateType updateSource){
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