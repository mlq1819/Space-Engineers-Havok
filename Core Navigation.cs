//Havok Core programming information

public struct VTuple{
	public Vector3D Item1;
	public Vector3D Item2;
	public VTuple(Vector3D v1, Vector3D v2){
		Item1 = v1;
		Item2 = v2;
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
private const string CoreName = "CoreNavigation";
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
private long Days = 0;
private long Seconds = 0;

private Vector3D forward_vector;
private Vector3D upward_vector;
private Vector3D left_vector;
private Vector3D backward_vector;
private Vector3D downward_vector;
private Vector3D right_vector;

private Vector3D current_position;
private bool In_Gravity = false;

private IMyWarhead CoreWarhead = null;
private IMyGyro CoreGyroscope = null;
private IMyRemoteControl CoreRemote = null;
private IMyBatteryBlock CoreBattery = null;
private IMyShipConnector CoreConnector = null;
private IMyTimerBlock CoreTimer = null;
private List<IMySensorBlock> Sensors = new List<IMySensorBlock>();
private List<IMyLandingGear> LandingGear = new List<IMyLandingGear>();

private List<VTuple> position_history = new List<VTuple>();
private List<MyDetectedEntityInfo> detected_entities = new List<MyDetectedEntityInfo>();
private uint allied = 0;
private uint friendly = 0;
private uint neutral = 0;
private uint hostile = 0;
private bool evasion = false;
private Vector3D follow_position;
private Vector3D follow_velocity;
private bool follow_collision = true;
private bool following = false;
private long tracking_ID = 0;
private bool tracking = false;
private bool did_follow = false;

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

private void AddPosition(VTuple toAdd){
	if(position_history.Count > 300){
		position_history = new List<VTuple>();
	}
	position_history.Add(toAdd);
}

public void SetWarhead(){
	List<IMyWarhead> AllBlocks = new List<IMyWarhead>();
	GridTerminalSystem.GetBlocksOfType<IMyWarhead>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreWarhead = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreWarhead = AllBlocks[i];
			CoreWarhead.CustomData = CoreIdentification;
			return;
		}
	}
}

public void SetGyroscope(){
	List<IMyGyro> AllBlocks = new List<IMyGyro>();
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreGyroscope = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreGyroscope = AllBlocks[i];
			CoreGyroscope.CustomData = CoreIdentification;
			return;
		}
	}
}

public void SetRemote(){
	List<IMyRemoteControl> AllBlocks = new List<IMyRemoteControl>();
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreRemote = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreRemote = AllBlocks[i];
			CoreRemote.CustomData = CoreIdentification;
			return;
		}
	}
}

public void SetBattery(){
	List<IMyBatteryBlock> AllBlocks = new List<IMyBatteryBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreBattery = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreBattery = AllBlocks[i];
			CoreBattery.CustomData = CoreIdentification;
			return;
		}
	}
}

public void SetConnector(){
	List<IMyShipConnector> AllBlocks = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreConnector = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreConnector = AllBlocks[i];
			CoreConnector.CustomData = CoreIdentification;
			return;
		}
	}
}

public void SetTimer(){
	List<IMyTimerBlock> AllBlocks = new List<IMyTimerBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(AllBlocks);
	double min_distance = Double.MaxValue;
	for(int i=0; i<AllBlocks.Count; i++){
		if(AllBlocks[i].CustomData.Equals(CoreIdentification)){
			CoreTimer = AllBlocks[i];
			return;
		}
		else {
			min_distance = Math.Min(min_distance, (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length());
		}
	}
	for(int i=0; i<AllBlocks.Count; i++){
		double distance = (AllBlocks[i].GetPosition() - CoreDiagnostics.GetPosition()).Length();
		if(distance < min_distance+0.5){
			CoreTimer = AllBlocks[i];
			CoreTimer.CustomData = CoreIdentification;
			return;
		}
	}
}

public void SetSensors(){
	Sensors.Clear();
	GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(Sensors);
}

public void SetLandingGear(){
	LandingGear.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(LandingGear);
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
				toEcho+="Successfully initialized 5 cores\n";
				message_history.Add("Successfully initialized 5 cores");
				SetWarhead();
				SetGyroscope();
				SetRemote();
				SetBattery();
				SetConnector();
				SetTimer();
				SetSensors();
				SetLandingGear();
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			}
		}
	} catch (Exception e){
		toEcho+="Critical Core Failure: " + e.Message + '\n';
		message_history.Add("Critical Core Failure: " + e.Message);
		Runtime.UpdateFrequency = UpdateFrequency.None;
	}
}

public VTuple ParseVectorTuple(string line){
	try{
		int index = line.IndexOf('<')+1;
		int length = line.Substring(index).IndexOf(';');
		string v1 = line.Substring(index, length);
		index+=length + 1;
		length = line.Substring(index).IndexOf('>');
		string v2 = line.Substring(index, length);
		index = 0;
		length = v1.IndexOf(',');
		double x = Double.Parse(v1.Substring(index,length));
		index+=length+1;
		length = v1.Substring(index).IndexOf(',');
		double y = Double.Parse(v1.Substring(index, length));
		index+=length+1;
		double z = Double.Parse(v1.Substring(index));
		Vector3D vector1 = new Vector3D(x,y,z);
		index = 0;
		length = v2.IndexOf(',');
		x = Double.Parse(v2.Substring(index,length));
		index+=length+1;
		length = v2.Substring(index).IndexOf(',');
		y = Double.Parse(v2.Substring(index, length));
		index+=length+1;
		z = Double.Parse(v2.Substring(index));
		Vector3D vector2 = new Vector3D(x,y,z);
		return new VTuple(vector1, vector2);
	} catch (Exception e){
		toEcho += "Bad tuple: " + e.Message;
		message_history.Add("Bad tuple: " + e.Message);
		return new VTuple(new Vector3D(0,0,0), new Vector3D(0,0,0));
	}
}

public string GetTupleString(VTuple vectortuple){
	return "(" + vectortuple.Item1.ToString() + ';' + vectortuple.Item2.ToString() + ")";
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
	for(int i=0; i<position_history.Count; i++){
		string output = '<' + position_history[i].Item1.ToString() + ';' + position_history[i].Item2.ToString() + '>';
		this.Storage += output + '\n';
	}
}

/*
* Converts a string-formatted GPS string into a Vector3D for input to a remote
*/
public Vector3D ConvertGPS(string gps){
	//example input: GPS:Point 1:5.09:-0.5:-3.09:
	if(!gps.Contains("GPS:")){
		toEcho+="Conversion failed: input does not contain \"GPS:\"\n";
		return new Vector3D(0,0,0);
	}
	try{
		double x=0;
		double y=0;
		double z=0;
		string toConvert = gps.Substring(gps.IndexOf("GPS:")+4);
		toConvert = toConvert.Substring(toConvert.IndexOf(":")+1);
		x = Double.Parse(toConvert.Substring(0, toConvert.IndexOf(":")));
		toConvert = toConvert.Substring(toConvert.IndexOf(":")+1);
		y = Double.Parse(toConvert.Substring(0, toConvert.IndexOf(":")));
		toConvert = toConvert.Substring(toConvert.IndexOf(":")+1);
		z = Double.Parse(toConvert.Substring(0, toConvert.IndexOf(":")));
		return new Vector3D(x,y,z);
	}
	catch(Exception e){
		toEcho+="Conversion failed: " + e.Message + "\n";
		return new Vector3D(0,0,0);
	}
}

/*
* Generates a GPS-formatted GPS string from a string and three doubles
*/
public string GenerateGPS(string name, double x, double y, double z){
	return "GPS:" + name + " " + x + ":" + y + ":" + z + ":";
}

/*
* Generates a GPS-formatted GPS string from three doubles
*/
public string GenerateGPS(double x, double y, double z){
	return GenerateGPS("target", x, y, z);
}

/*
* Generates a GPS-formatted GPS string from a Vector
*/
public string GenerateGPS(Vector3D vect){
	return GenerateGPS("target", vect);
}

/*
* Generates a GPS-formatted GPS string from a string and a Vector
*/
public string GenerateGPS(string name, Vector3D vect){
	return GenerateGPS(name, vect.X, vect.Y, vect.Z);
}

/*
* Creates and returns a normalized vector pointing in the direction from Vector3D src to Vector3D dest
*/
public Vector3D GetDirection(Vector3D src, Vector3D dest){
	Vector3D resultVector = dest - src;
	resultVector.Normalize();
	return resultVector;
}


/*
* Creates and returns a normalized vector pointing in the direction from Terminal Block src to Terminal Block dest
*/
public Vector3D GetDirection(IMyTerminalBlock src, IMyTerminalBlock dest){
	return GetDirection(src.GetPosition(), dest.GetPosition());
}

/*
* Creates and returns a normalized vector pointing in the direction from the Terminal Block with custom name src to custom name dest
*/
public Vector3D GetDirection(string src, string dest){
	List<IMyTerminalBlock> AllTerminals = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(AllTerminals);
	IMyTerminalBlock source = AllTerminals[0];
	IMyTerminalBlock destination = AllTerminals[0];
	for(int i=0; i<AllTerminals.Count; i++){
		if(AllTerminals[i].CustomName.ToLower().Contains(src.ToLower())){
			source = AllTerminals[i];
		}
		if(AllTerminals[i].CustomName.ToLower().Contains(dest.ToLower())){
			destination = AllTerminals[i];
		}
	}
	return GetDirection(source, destination);
}

/*
* Generates the angle between two vectors in degrees, rounded to 5 decimal places
*/
public double GetAngle(Vector3D v1, Vector3D v2){
	return Math.Round(Math.Acos(v1.X*v2.X + v1.Y*v2.Y + v1.Z*v2.Z) * 57.295755, 5);
}

/*
* Sets the 6 directional vectors, as well as references for 4 important objects
*/
public void SetDirectionalVectors(){
	forward_vector = GetDirection(CoreWarhead, CoreRemote);
	backward_vector = GetDirection(CoreRemote, CoreWarhead);
	upward_vector = GetDirection(CoreBattery, CoreWarhead);
	downward_vector = GetDirection(CoreWarhead, CoreBattery);
	left_vector = GetDirection(CoreWarhead, CoreCommunications);
	right_vector = GetDirection(CoreWarhead, CoreStrategy);
	
	current_position = CoreRemote.GetPosition();
}

public bool HasConnector(){
	if(CoreConnector!=null)
		return true;
	List<IMyShipConnector> AllConnectors = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllConnectors);
	return AllConnectors.Count > 0;
}

public IMyShipConnector GetClosestConnector(){
	List<IMyShipConnector> AllConnectors = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(AllConnectors);
	if(AllConnectors.Count > 0){
		double min_distance = double.MaxValue;
		foreach(IMyShipConnector connector in AllConnectors){
			double distance = (CoreRemote.GetPosition()-connector.GetPosition()).Length();
			min_distance = Math.Min(min_distance, distance);
		}
		foreach(IMyShipConnector connector in AllConnectors){
			double distance = (CoreRemote.GetPosition()-connector.GetPosition()).Length();
			if(distance <= min_distance + 0.5){
				return connector;
			}
		}
	}
	return null;
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

private bool HasDetectedEntity(long ID){
	foreach(MyDetectedEntityInfo Entity in detected_entities){
		if(Entity.EntityId == ID)
			return true;
	}
	return false;
}

private bool HasDetectedEntity(MyDetectedEntityInfo Entity){
	return HasDetectedEntity(Entity.EntityId);
}

private MyDetectedEntityInfo GetDetectedEntity(long ID){
	foreach(MyDetectedEntityInfo Entity in detected_entities){
		if(Entity.EntityId == ID)
			return Entity;
	}
	return new MyDetectedEntityInfo(-1, "bad", MyDetectedEntityType.None, new Vector3(0,0,0), new MatrixD(), new Vector3(0,0,0), MyRelationsBetweenPlayerAndBlock.NoOwnership, new BoundingBoxD(), -1);
}

private void FillDetectedEntities(){
	detected_entities.Clear();
	allied = 0;
	friendly = 0;
	neutral = 0;
	hostile = 0;
	foreach(IMySensorBlock Sensor in Sensors){
		List<MyDetectedEntityInfo> sensor_data = new List<MyDetectedEntityInfo>();
		Sensor.DetectedEntities(sensor_data);
		foreach(MyDetectedEntityInfo Entity in sensor_data){
			if(!HasDetectedEntity(Entity)){
				detected_entities.Add(Entity);
				switch(Entity.Relationship){
					case MyRelationsBetweenPlayerAndBlock.Owner:
						allied++;
						break;
					case MyRelationsBetweenPlayerAndBlock.FactionShare:
						allied++;
						break;
					case MyRelationsBetweenPlayerAndBlock.Friends:
						friendly++;
						break;
					case MyRelationsBetweenPlayerAndBlock.Neutral:
						neutral++;
						break;
					case MyRelationsBetweenPlayerAndBlock.NoOwnership:
						neutral++;
						break;
					case MyRelationsBetweenPlayerAndBlock.Enemies:
						hostile++;
						break;
				}
			}
		}
	}
	AddPrint(detected_entities.Count.ToString() + " entities detected:\n" + allied + " allies\n" + friendly + " friendlies\n" + neutral + " neutrals\n" + hostile + " hostiles", false);
}

private void PerformEvasion(){
	Random rnd = new Random();
	List<IMyThrust> AllThrust = new List<IMyThrust>();
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(AllThrust);
	List<int> RandomThrusts = new List<int>();
	for(int i=0; i<Math.Min(Math.Max(3, AllThrust.Count/10), AllThrust.Count); i++){
		int random_index = rnd.Next(0, AllThrust.Count);
		if(!RandomThrusts.Contains(random_index))
			RandomThrusts.Add(random_index);
	}
	for(int i=0; i<AllThrust.Count; i++){
		if(RandomThrusts.Contains(i)){
			if(evasion){
				AllThrust[i].CustomData = "evading";
				AllThrust[i].ThrustOverridePercentage  = 1.0f;
			}
		}
		else{
			if(AllThrust[i].CustomData.Equals("evading")){
				AllThrust[i].ThrustOverride = 0;
				AllThrust[i].CustomData = "";
			}
		}
	}
}

private void PerformFollowing(){
	if(did_follow)
		return;
	Vector3D target_position = 2 * follow_velocity * CoreTimer.TriggerDelay + follow_position;
	Vector3D expected_position = follow_position + (follow_velocity * CoreTimer.TriggerDelay);
	double speed = follow_velocity.Length();
	Vector3D movement_direction = follow_velocity;
	movement_direction.Normalize();
	double distance = (CoreRemote.GetPosition() - follow_position).Length();
	bool catching_up = (CoreRemote.GetPosition() + 5*movement_direction - follow_position).Length() < distance;
	if(distance > 200){
		if(catching_up){
			speed = Math.Min(100.0, speed + (distance / 200.0));
		}
		else {
			speed = Math.Max(1.0, speed - (distance / 200.0));
		}
	}
	CoreRemote.SpeedLimit = (float) speed;
	follow_position = expected_position;
	CoreRemote.FlightMode = FlightMode.OneWay;
	CoreRemote.ClearWaypoints();
	CoreRemote.AddWaypoint(target_position, "Chase");
	CoreRemote.SetCollisionAvoidance(follow_collision);
	CoreRemote.SetDockingMode(false);
	CoreRemote.SetAutoPilotEnabled(true);
	did_follow = true;
}

private void PerformTracking(){
	double distance = double.MaxValue;
	foreach(IMyLandingGear Gear in LandingGear){
		distance = Math.Min(distance, (Gear.GetPosition() - follow_position).Length());
	}
	following = true;
	if(distance > 5){
		follow_collision = true;
		foreach(IMyLandingGear Gear in LandingGear){
			Gear.AutoLock = false;
		}
	}
	else {
		bool connected = false;
		follow_collision = false;
		foreach(IMyLandingGear Gear in LandingGear){
			Gear.AutoLock = true;
			Gear.Lock();
			if(Gear.IsLocked){
				connected = true;
				break;
			}
		}
		if(connected){
			following = false;
			tracking = false;
			CoreRemote.ClearWaypoints();
			CoreRemote.SetAutoPilotEnabled(false);
		}
	}
}

public bool CheckValidID(){
	int CoreIDNumber = 0;
	try{
		if(CoreIdentification.Contains('-'))
			CoreIDNumber = Int32.Parse(CoreIdentification.Substring(CoreIdentification.IndexOf('-')));
		else
			CoreIDNumber = Int32.Parse(CoreIdentification);
		if(CoreIDNumber == 0){
			AddPrint("Currently in Factory Default Settings", true);
			FinalPrint();
			BlocksSet = false;
		}
	}
	catch(FormatException e){
		AddPrint("Invalid ID:" + CoreIdentification + "\nWiping ID...", true);
		Wipe();
		FinalPrint();
	}
}

public void Run(string argument, UpdateType updateSource)
{
	if(!CheckValidID())
		return;
	did_follow = false;
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
		SetDirectionalVectors();
		if(argument.Length > 0 && argument.IndexOf(':') > 0 && argument.IndexOf(':') < argument.IndexOf('<') && argument.IndexOf('<') < argument.IndexOf('>')){
			int start = 0;
			int end = argument.Substring(start).IndexOf(':') - start;
			string Source = argument.Substring(start, end).Trim();
			start += end+1;
			end = argument.Substring(start).IndexOf('<');
			string Command = argument.Substring(start, end).Trim();
			start += end+1;
			end = argument.Substring(start).IndexOf('>');
			string Data = argument.Substring(start, end).Trim();
			if(Source.Equals("CoreStrategy")){
				if(Command.ToLower().Equals("goto")){
					following=false;
					start = Data.IndexOf('(')+1;
					end = Data.Substring(start).IndexOf(',');
					double x = double.Parse(Data.Substring(start,end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(',');
					double y = double.Parse(Data.Substring(start,end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(')');
					double z = double.Parse(Data.Substring(start,end).Trim());
					Vector3D target_pos = new Vector3D(x,y,z);
					CoreRemote.ClearWaypoints();
					CoreRemote.AddWaypoint(target_pos, Command);
					CoreRemote.SpeedLimit = 80.0f;
					CoreRemote.FlightMode = FlightMode.OneWay;
					CoreRemote.ApplyAction("Forward");
					CoreRemote.SetCollisionAvoidance(true);
					CoreRemote.SetDockingMode(false);
					CoreRemote.SetAutoPilotEnabled(true);
					AddPrint("Activated Autopilot to " + Data.Substring(start), true);
				}
				else if(Command.ToLower().Equals("dock")){
					if(HasConnector()){
						following=false;
						start = Data.IndexOf('(')+1;
						end = Data.Substring(start).IndexOf(',');
						double x = double.Parse(Data.Substring(start,end).Trim());
						start += end+1;
						end = Data.Substring(start).IndexOf(',');
						double y = double.Parse(Data.Substring(start,end).Trim());
						start += end+1;
						end = Data.Substring(start).IndexOf(')');
						double z = double.Parse(Data.Substring(start,end).Trim());
						Vector3D target_pos = new Vector3D(x,y,z);
						Vector3D target_direction = GetDirection(CoreRemote, CoreConnector);
						double min_angle = double.MaxValue;
						min_angle = Math.Min(min_angle, GetAngle(target_direction, forward_vector));
						min_angle = Math.Min(min_angle, GetAngle(target_direction, upward_vector));
						min_angle = Math.Min(min_angle, GetAngle(target_direction, left_vector));
						min_angle = Math.Min(min_angle, GetAngle(target_direction, backward_vector));
						min_angle = Math.Min(min_angle, GetAngle(target_direction, downward_vector));
						min_angle = Math.Min(min_angle, GetAngle(target_direction, right_vector));
						bool valid_direction = true;
						if(min_angle + 1 > GetAngle(target_direction, forward_vector)){
							CoreRemote.ApplyAction("Forward");
						}
						else if(min_angle + 1 > GetAngle(target_direction, upward_vector)){
							CoreRemote.ApplyAction("Up");
						}
						else if(min_angle + 1 > GetAngle(target_direction, left_vector)){
							CoreRemote.ApplyAction("Left");
						}
						else if(min_angle + 1 > GetAngle(target_direction, backward_vector)){
							CoreRemote.ApplyAction("Backward");
						}
						else if(min_angle + 1 > GetAngle(target_direction, downward_vector)){
							CoreRemote.ApplyAction("Down");
						}
						else if(min_angle + 1 > GetAngle(target_direction, right_vector)){
							CoreRemote.ApplyAction("Right");
						}
						else{
							valid_direction = false;
							AddPrint("Unable to dock; connector invalid", true);
							CoreStrategy.TryRun(CoreName + ":Invalid<Connector>");
						}
						if(valid_direction){
							double connector_distance = (CoreRemote.GetPosition() - CoreConnector.GetPosition()).Length();
							double target_distance = (CoreRemote.GetPosition() - target_pos).Length();
							target_pos.Normalize();
							target_pos *= (target_distance - connector_distance);
							CoreRemote.ClearWaypoints();
							CoreRemote.AddWaypoint(target_pos, Command);
							CoreRemote.SpeedLimit = 10.0f;
							CoreRemote.FlightMode = FlightMode.OneWay;
							CoreRemote.SetCollisionAvoidance(true);
							CoreRemote.SetDockingMode(true);
							CoreRemote.SetAutoPilotEnabled(true);
							AddPrint("Autopilot Docking initiated at " + Data.Substring(start), true);
						}
					}
					else {
						CoreStrategy.TryRun(CoreName + ":Missing<Connector>");
						AddPrint("Unable to dock; connector missing", true);
					}
				}
				else if(Command.ToLower().Equals("kamikaze")){
					start = Data.IndexOf('(')+1;
					end = Data.Substring(start).IndexOf(',');
					double x = double.Parse(Data.Substring(start,end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(',');
					double y = double.Parse(Data.Substring(start,end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(')');
					double z = double.Parse(Data.Substring(start,end).Trim());
					Vector3D target_pos = new Vector3D(x,y,z);
					Vector3D original_pos = new Vector3D(target_pos);
					Vector3D current_pos = CoreRemote.GetPosition();
					double target_length = (target_pos - current_pos).Length();
					target_pos -= current_pos;
					target_pos.Normalize();
					target_pos = current_pos + (target_pos * (target_length + 1000));
					CoreRemote.ClearWaypoints();
					CoreRemote.AddWaypoint(target_pos, Command + " - virtual");
					CoreRemote.AddWaypoint(original_pos, Command + " - actual");
					CoreRemote.SpeedLimit = 100.0f;
					CoreRemote.FlightMode = FlightMode.OneWay;
					CoreRemote.ApplyAction("Forward");
					CoreRemote.SetCollisionAvoidance(false);
					CoreRemote.SetDockingMode(false);
					CoreRemote.SetAutoPilotEnabled(true);
					AddPrint("Autopilot Kamikaze initiated towards " + Data.Substring(start), true);
				}
				else if(Command.ToLower().Equals("docked")){
					following=false;
					CoreRemote.ClearWaypoints();
					CoreRemote.SetAutoPilotEnabled(false);
					AddPrint("Ship has Docked; disabling autopilot", true);
				}
				else if(Command.ToLower().Equals("clear")){
					following=false;
					CoreRemote.ClearWaypoints();
					CoreRemote.SetAutoPilotEnabled(false);
					AddPrint("Cleared waypoints; disabling autopilot", true);
				}
				else if(Command.ToLower().Equals("evasion")){
					if(Data.ToLower().Equals("on")){
						evasion = true;
						AddPrint("Initiating evasive maneuvers", true);
					}
					else if(Data.ToLower().Equals("off")){
						evasion = false;
						PerformEvasion();
						AddPrint("Ending evasive maneuvers", true);
					}
				}
				else if(Command.ToLower().Equals("follow")){
					start = Data.IndexOf('(')+1;
					end = Data.Substring(start).IndexOf(',');
					double x = double.Parse(Data.Substring(start, end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(',');
					double y = double.Parse(Data.Substring(start, end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(')');
					double z = double.Parse(Data.Substring(start, end).Trim());
					follow_position = new Vector3D(x,y,z);
					start = Data.Substring(start).IndexOf('(')+1;
					end = Data.Substring(start).IndexOf(',');
					x = double.Parse(Data.Substring(start, end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(',');
					y = double.Parse(Data.Substring(start, end).Trim());
					start += end+1;
					end = Data.Substring(start).IndexOf(')');
					z = double.Parse(Data.Substring(start, end).Trim());
					follow_velocity = new Vector3D(x,y,z);
					following = true;
					follow_collision = true;
					PerformFollowing();
				}
				else if(Command.ToLower().Equals("tracking")){
					if(Sensors.Count > 1 && LandingGear.Count > 0){
						start = 0;
						end = Data.Substring(start).IndexOf(';');
						tracking_ID = Int64.Parse(Data.Substring(start,end).Trim());
						start+=end+1;
						start = Data.Substring(start).IndexOf('(')+1;
						end = Data.Substring(start).IndexOf(',');
						double x = double.Parse(Data.Substring(start, end).Trim());
						start += end+1;
						end = Data.Substring(start).IndexOf(',');
						double y = double.Parse(Data.Substring(start, end).Trim());
						start += end+1;
						end = Data.Substring(start).IndexOf(')');
						double z = double.Parse(Data.Substring(start, end).Trim());
						follow_position = new Vector3D(x,y,z);
						start = Data.Substring(start).IndexOf('(')+1;
						end = Data.Substring(start).IndexOf(',');
						x = double.Parse(Data.Substring(start, end).Trim());
						start += end+1;
						end = Data.Substring(start).IndexOf(',');
						y = double.Parse(Data.Substring(start, end).Trim());
						start += end+1;
						end = Data.Substring(start).IndexOf(')');
						z = double.Parse(Data.Substring(start, end).Trim());
						follow_velocity = new Vector3D(x,y,z);
						following = true;
						tracking = true;
						PerformTracking();
						PerformFollowing();
					}
					else{
						if(Sensors.Count < 2){
							AddPrint("Cannot perform tracking; no valid sensors", true);
							CoreStrategy.TryRun(CoreName + ":Missing<Sensor>");
						}
						else{
							AddPrint("Cannot perform tracking; no landing gear", true);
							CoreStrategy.TryRun(CoreName + ":Missing<LandingGear>");
						}
						
					}
				}
			}
			else if(Source.Equals("Timer")){
				long old_time = Seconds;
				Seconds += (long) CoreTimer.TriggerDelay;
				Days += (Seconds / 86400);
				Seconds = Seconds % 86400;
				if(evasion && (old_time % 10) != ((long)(old_time+CoreTimer.TriggerDelay) % 10)){
					PerformEvasion();
				}
				if(tracking)
					PerformTracking();
				if(following)
					PerformFollowing();
			}
		}
		else{
			AddPosition(new VTuple(current_position, forward_vector));
			bool found_update = false;
			FillDetectedEntities();
			foreach(MyDetectedEntityInfo entity in detected_entities){
				if(tracking){
					if(tracking_ID != 0 && tracking_ID == entity.EntityId){
						if(entity.HitPosition != null)
							follow_position = (Vector3D) entity.HitPosition;
						else
							follow_position = entity.Position;
						follow_velocity = entity.Velocity;
						PerformTracking();
					}
				}
				if(entity.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
					switch(entity.Type){
						case MyDetectedEntityType.SmallGrid:
							CoreStrategy.TryRun(CoreName + ":HostileFighter<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
							AddPrint("HostileFighter<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							break;
						case MyDetectedEntityType.LargeGrid:
							CoreStrategy.TryRun(CoreName + ":HostileFrigate<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
							AddPrint("HostileFrigate<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							break;
						case MyDetectedEntityType.CharacterHuman:
							CoreStrategy.TryRun(CoreName + ":HostileOrganic<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
							AddPrint("HostileOrganic<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							break;
						case MyDetectedEntityType.CharacterOther:
							CoreStrategy.TryRun(CoreName + ":HostileOrganic<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
							AddPrint("HostileOrganic<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							break;
					}
				}
				else{
					switch(entity.Type){
						case MyDetectedEntityType.SmallGrid:
							if(entity.Relationship != MyRelationsBetweenPlayerAndBlock.Owner && entity.Relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && entity.Relationship != MyRelationsBetweenPlayerAndBlock.Friends){
								CoreStrategy.TryRun(CoreName + ":ScrapFighter<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
								AddPrint("ScrapFighter<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							}
							break;
						case MyDetectedEntityType.LargeGrid:
							if(entity.Relationship != MyRelationsBetweenPlayerAndBlock.Owner && entity.Relationship != MyRelationsBetweenPlayerAndBlock.FactionShare && entity.Relationship != MyRelationsBetweenPlayerAndBlock.Friends){
								CoreStrategy.TryRun(CoreName + ":ScrapFrigate<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
								AddPrint("ScrapFrigate<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							}
							break;
						case MyDetectedEntityType.FloatingObject:
							CoreStrategy.TryRun(CoreName + ":ScrapObject<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>");
							AddPrint("ScrapObject<" + entity.EntityId + ";(" + entity.Position.ToString() + ");(" + entity.Velocity.ToString() + ")>", false);
							break;
						case MyDetectedEntityType.Asteroid:
							CoreStrategy.TryRun(CoreName + ":Asteroid<" + entity.EntityId + ";(" + entity.Position.ToString() + ")>");
							AddPrint("Asteroid<" + entity.EntityId + ";(" + entity.Position.ToString() + ")>", false);
							break;
					}
				}
			}
			
			if(position_history.Count > 1){
				Vector3D last_position = position_history[position_history.Count-2].Item1;
				Vector3D last_direction = position_history[position_history.Count-2].Item2;
				if((current_position-last_position).Length() > 1 || GetAngle(forward_vector, last_direction) > 5){
					AddPrint("Significantly moved: " + GetTupleString(new VTuple(current_position, forward_vector)), true);
					CoreStrategy.TryRun(CoreName + ":Report<" + GetTupleString(new VTuple(current_position, forward_vector)) + '>');
					found_update = true;
				}
				if(!CoreRemote.CurrentWaypoint.IsEmpty()){
					Vector3D current_target = CoreRemote.CurrentWaypoint.Coords;
					if((current_target - CoreRemote.GetPosition()).Length() < 0.5 && (current_target - CoreRemote.GetPosition()).Length() < (current_target - position_history.Last().Item1).Length()){
						AddPrint("Reached target coordinates: " + CoreRemote.GetPosition(), true);
						found_update = true;
						CoreStrategy.TryRun(CoreName + ":Reached<(" + CoreConnector.GetPosition() + ");" + (CoreConnector.GetPosition() - CoreRemote.GetPosition()).Length() + ">");
					}
				}
				if(found_update){
					Runtime.UpdateFrequency = UpdateFrequency.Update10;
				}
				else{
					Runtime.UpdateFrequency = UpdateFrequency.Update100;
				}
			}
			if(!In_Gravity && CoreRemote.GetNaturalGravity().Length() > 0.01){
				In_Gravity = true;
				Vector3D Gravity = CoreRemote.GetNaturalGravity();
				AddPrint("Caught in Gravity Well: (" + Gravity.ToString() + ')', true);
				CoreStrategy.TryRun(CoreName + ":Gravity<(" + Gravity.ToString() + ")>");
			}
			else if(In_Gravity && CoreRemote.GetNaturalGravity().Length() <= 0.01){
				In_Gravity = false;
				AddPrint("Escaped Gravity Well", true);
			}
		}
	} else {
		AddPrint("Cannot run program --- blocks not set!", false);
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