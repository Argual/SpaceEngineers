using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        #region Constants

        #region INI files

        #region Configuration
        const string Const_IniAll_SectionName_Mechanism = "ArgDrillMechanism";

        const string Const_IniCustomData_Name_ExtensionStepDistance = "ExtensionStepDistance";
        const string Const_IniAll_Name_MechanismID = "DrillMechanismID";
        const string Const_IniCustomData_Name_CycleUpdateFrequency = "CycleUpdateFrequency";
        const string Const_IniCustomData_Name_InventoryCheckIntervalInSec = "InventoryCheckIntervalInSec";
        const string Const_IniCustomData_Name_MessageHistorySize = "MessageHistorySize";
        const string Const_IniCustomData_Name_TolerableFillRatio = "TolerableFillRatio";
        const string Const_IniCustomData_Name_SafeExtensionDistance = "SafeExtensionDistance";
        const string Const_IniCustomData_Name_CurrentMechanismState = "CurrentMechanismState";
        const string Const_IniCustomData_Name_MessageHistory = "MessageHistory";
        const string Const_IniPiston_Name_PistonExtendsWhileDrilling = "ExtendsWhileDrilling";
        const string Const_IniClient_Name_IsClient = "IsClient";
        #endregion

        #region Info

        const string Const_IniInfo_SectionName_GeneralInfo = "General";
        const string Const_IniInfo_SectionName_MechanismInfo = "MechanismInfo";

        // const string Const_IniName_MessageHistory; // This has already been declared.
        const string Const_IniInfo_Name_Activity = "Activity";
        const string Const_IniInfo_Name_CurrentPosition = "CurrentPosition";
        const string Const_IniInfo_Name_HighestPosition = "HighestPosition";
        const string Const_IniInfo_Name_DrillInventoryFillRatio = "DrillInventoryFillRatio";

        #endregion

        #region Storage
        const string Const_IniSave_SectionName_drills = "drills";
        const string Const_IniSave_SectionName_pistonsExtendingOnAxis = "pistonsExtendingOnAxis";
        const string Const_IniSave_SectionName_pistonsRetractingOnAxis = "pistonsRetractingOnAxis";
        const string Const_IniSave_SectionName_textPanels = "textPanels";
        const string Const_IniSave_SectionName_clientPBs = "clientPBs";

        const string Const_IniSave_Name_rotor = "rotor";
        const string Const_IniSave_Name_elapsedTimeSinceLastInventoryCheckInSec = "elapsedTimeSinceLastInventoryCheckInSec";
        #endregion

        #endregion

        #region Commands

        const string Const_Cmd_Stop = "Stop";
        const string Const_Cmd_Startup = "Start";
        const string Const_Cmd_ResetToDefaults = "ResetToDefaults";
        const string Const_Cmd_ConfigureFromCustomData = "ConfigureFromCustomData";
        const string Const_Cmd_ShutDown = "ShutDown";
        const string Const_Cmd_Configure = "Configure";
        const string Const_Cmd_GetBlocks = "GetBlocks";
        const string Const_Cmd_UpdateIDs = "UpdateIDs";

        #endregion

        #endregion

        #region Private fields

        private string _drillMechanismID;


        private UpdateFrequency _cycleUpdateFrequency;


        private float _inventoryCheckIntervalInSec;

        private float _tolerableFillRatio;

        private float _extensionStepDistance;

        private float _safeExtensionDistance;


        private int _messageHistorySize;

        MyCommandLine commandLine;

        Dictionary<string, Action> commandDictionary;

        ArgDrillMechanism mechanism;
        List<IMyTextPanel> textPanels;

        float elapsedTimeSinceLastInventoryCheckInSec;

        MyIni ini;

        MyIni iniTemplatePiston;
        MyIni iniTemplateOnlyID;
        MyIni iniTemplatePB;

        MyIni iniPB;

        MyIni iniInfo;

        MyIni saveIni;

        MechanismState _currentMechanismState;
        List<IMyPistonBase> pistonsExtendingOnAxis;
        List<IMyPistonBase> pistonsRetractingOnAxis;
        IMyMotorStator rotor;
        List<IMyShipDrill> drills;
        List<IMyProgrammableBlock> clientPBs;

        #endregion

        #region Subclasses
        #region mdk preserve
        /// <summary>
        /// Describes the state of a drill mechanism from the perspective of the controller script.
        /// </summary>
        public enum MechanismState
        {
            /// <summary>
            /// The mechanism hasn't been assigned a valid state yet.
            /// </summary>
            None,
            /// <summary>
            /// The mechanism is drilling.
            /// </summary>
            Drilling,
            /// <summary>
            /// The mechanism is stopped.
            /// </summary>
            Stopped,
            /// <summary>
            /// The mechanism is shutting down and retracting.
            /// </summary>
            Retracting
        }
        #endregion
        #endregion

        #region Public fields

        /// <summary>
        /// How many meters should the mechanism extend each full rotation.
        /// </summary>
        public float ExtensionStepDistance
        {
            get
            {
                return _extensionStepDistance;
            }
            set
            {
                if (_extensionStepDistance != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_ExtensionStepDistance, value);
                    UpdateCustomData();
                    if (mechanism!=null)
                    {
                        mechanism.PistonsAxisStepDistance = value;
                    }
                }
                _extensionStepDistance = value;
            }
        }

        /// <summary>
        /// The current state of the mechanism.
        /// </summary>
        public MechanismState CurrentMechanismState
        {
            get
            {
                return _currentMechanismState;
            }
            private set
            {
                _currentMechanismState = value;
            }
        }

        /// <summary>
        /// The ID of the drill mechanism this script is controlling.
        /// </summary>
        public string DrillMechanismID
        {
            get
            {
                return _drillMechanismID;
            }

            set
            {
                if (_drillMechanismID != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, value);
                    UpdateCustomData();
                }
                _drillMechanismID = value;
            }
        }

        /// <summary>
        /// The tick frequency this script is run while drilling or retracting.
        /// </summary>
        public UpdateFrequency CycleUpdateFrequency
        {
            get
            {
                return _cycleUpdateFrequency;
            }

            set
            {
                if (_cycleUpdateFrequency != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_CycleUpdateFrequency, value.ToString("F"));
                    UpdateCustomData();
                }
                _cycleUpdateFrequency = value;
            }
        }

        /// <summary>
        /// The time between checking drill inventory fill ratios.
        /// </summary>
        public float InventoryCheckIntervalInSec
        {
            get
            {
                return _inventoryCheckIntervalInSec;
            }

            set
            {
                if (_inventoryCheckIntervalInSec != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_InventoryCheckIntervalInSec, value);
                    UpdateCustomData();
                }
                _inventoryCheckIntervalInSec = value;
            }
        }

        /// <summary>
        /// The ratio of the drill inventory fill rate above which the drill mechanism is stopped.
        /// 0f means that the drills must be empty. Do not use 0f, as it will regularly stop the drills.
        /// 1f means that the drill can be full, and the mechanism will still continue drilling.
        /// </summary>
        public float TolerableFillRatio
        {
            get
            {
                return _tolerableFillRatio;
            }

            set
            {
                if (_tolerableFillRatio != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_TolerableFillRatio, value);
                    UpdateCustomData();
                }
                _tolerableFillRatio = value;
            }
        }

        /// <summary>
        /// The distance the drill mechanism can extend without drilling.
        /// Use this to make the drill reach the ground faster.
        /// </summary>
        public float SafeExtensionDistance
        {
            get
            {
                return _safeExtensionDistance;
            }

            set
            {
                if (_safeExtensionDistance != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_SafeExtensionDistance, value);
                    UpdateCustomData();
                }
                _safeExtensionDistance = value;
            }
        }

        /// <summary>
        /// The amount of messages to show and save.
        /// </summary>
        public int MessageHistorySize
        {
            get
            {
                return _messageHistorySize;
            }

            set
            {
                if (_messageHistorySize != value)
                {
                    iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_MessageHistorySize, value);
                    UpdateCustomData();
                }
                _messageHistorySize = value;
            }
        }

        /// <summary>
        /// The most recent status messages.
        /// </summary>
        public List<string> MessageHistory { get; private set; }

        #endregion

        public Program()
        {
            Initialize(true);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal | UpdateType.Mod | UpdateType.Script)) != 0)
            {
                ParseArgument(argument);
            }
            if (mechanism==null)
            {
                return;
            }
            switch (CurrentMechanismState)
            {
                case MechanismState.None:
                    break;
                case MechanismState.Drilling:
                    mechanism.DoCycle();
                    elapsedTimeSinceLastInventoryCheckInSec += (float)Runtime.TimeSinceLastRun.TotalSeconds;
                    if (elapsedTimeSinceLastInventoryCheckInSec > InventoryCheckIntervalInSec)
                    {
                        elapsedTimeSinceLastInventoryCheckInSec = 0f;
                        if (GetInventoryFillRatio() > TolerableFillRatio)
                        {
                            MyEcho("Drill inventory fill ratio is above tolerable levels.");
                            Stop();
                        }
                    }
                    if (mechanism.IsFullyExtended)
                    {
                        Stop();
                    }

                    float cp = mechanism.GetAccurateCurrentPosition();
                    if (SafeExtensionDistance < cp)
                    {
                        SafeExtensionDistance = cp;
                    }
                    else if (cp < SafeExtensionDistance)
                    {
                        mechanism.ExtendMeters(SafeExtensionDistance - cp);
                    }
                    break;
                case MechanismState.Stopped:
                    break;
                case MechanismState.Retracting:
                    mechanism.DoCycle();
                    if (mechanism.IsFullyRetracted)
                    {
                        Stop();
                        MyEcho("Drill mechanism retraction complete.");
                    }
                    break;
                default:
                    break;
            }
            UpdateInfo();
            SendInfoToClientPBs();
        }

        float GetInventoryFillRatio()
        {
            float f = mechanism.DrillInventoryFillRatio;
            iniInfo.Set(Const_IniInfo_SectionName_MechanismInfo, Const_IniInfo_Name_DrillInventoryFillRatio, f);
            return f;
        }

        #region Persistence

        /// <summary>
        /// Saves the necessary data to the storage string. It is not advised to call this manually.
        /// </summary>
        public void Save()
        {
            saveIni.Set("Config", Const_IniAll_Name_MechanismID, DrillMechanismID);

            if (mechanism == null)
            {
                Storage = saveIni.ToString();
                return;
            }

            saveIni.Clear();

            #region Save configuration

            saveIni.Set("Config", Const_IniAll_Name_MechanismID, DrillMechanismID);
            saveIni.Set("Config", Const_IniCustomData_Name_SafeExtensionDistance, SafeExtensionDistance);
            saveIni.Set("Config", Const_IniCustomData_Name_TolerableFillRatio, TolerableFillRatio);
            saveIni.Set("Config", Const_IniCustomData_Name_CycleUpdateFrequency, CycleUpdateFrequency.ToString("F"));
            saveIni.Set("Config", Const_IniCustomData_Name_InventoryCheckIntervalInSec, InventoryCheckIntervalInSec);
            saveIni.Set("Config", Const_IniCustomData_Name_MessageHistorySize, MessageHistorySize);
            saveIni.Set("Config", Const_IniCustomData_Name_ExtensionStepDistance, ExtensionStepDistance);

            #endregion

            #region Save client PBs

            int count = clientPBs.Count;
            saveIni.Set(Const_IniSave_SectionName_clientPBs, "Count", count);
            for (int i = 0; i < count; i++)
            {
                saveIni.Set(Const_IniSave_SectionName_clientPBs, i.ToString(), clientPBs[i].EntityId);
            }

            #endregion

            #region Save drills

            count = mechanism.Drills.Count;
            saveIni.Set(Const_IniSave_SectionName_drills, "Count", count);
            for (int i = 0; i < count; i++)
            {
                saveIni.Set(Const_IniSave_SectionName_drills, i.ToString(), mechanism.Drills[i].EntityId);
            }

            #endregion

            #region Save pistons

            count = mechanism.PistonsExtendingOnAxis.Count;
            saveIni.Set(Const_IniSave_SectionName_pistonsExtendingOnAxis, "Count", count);
            for (int i = 0; i < count; i++)
            {
                saveIni.Set(Const_IniSave_SectionName_pistonsExtendingOnAxis, i.ToString(), mechanism.PistonsExtendingOnAxis[i].EntityId);
            }

            count = mechanism.PistonsRetractingOnAxis.Count;
            saveIni.Set(Const_IniSave_SectionName_pistonsRetractingOnAxis, "Count", count);
            for (int i = 0; i < count; i++)
            {
                saveIni.Set(Const_IniSave_SectionName_pistonsRetractingOnAxis, i.ToString(), mechanism.PistonsRetractingOnAxis[i].EntityId);
            }

            #endregion

            #region Save rotor

            saveIni.Set("OtherParts", Const_IniSave_Name_rotor, mechanism.Rotor.EntityId);

            #endregion

            #region Save text panels

            count = textPanels.Count;
            saveIni.Set(Const_IniSave_SectionName_textPanels, "Count", count);
            for (int i = 0; i < count; i++)
            {
                saveIni.Set(Const_IniSave_SectionName_textPanels, i.ToString(), textPanels[i].EntityId);
            }

            #endregion

            #region Save state

            saveIni.Set("State", Const_IniCustomData_Name_CurrentMechanismState, CurrentMechanismState.ToString("F"));
            saveIni.Set("State", Const_IniSave_Name_elapsedTimeSinceLastInventoryCheckInSec, InventoryCheckIntervalInSec);

            string messageHistoryString = "";
            foreach (var line in MessageHistory)
            {
                messageHistoryString += line + "\n";
            }
            messageHistoryString.Trim('\n');
            saveIni.Set("State", Const_IniCustomData_Name_MessageHistory, messageHistoryString);

            #endregion

            Storage = saveIni.ToString();
        }

        /// <summary>
        /// Tries to load the necessary data from the storage string. It is not advised to call this manually.
        /// </summary>
        /// <returns>Whether or not there was a critical failure during loading which makes the initialization of a working drill mechanism improbable.</returns>
        bool Load()
        {
            if (saveIni.TryParse(Storage))
            {
                float f;
                string s;
                int integer;

                #region Load configuration

                if (saveIni.Get("Config", Const_IniAll_Name_MechanismID).TryGetString(out s))
                {
                    _drillMechanismID = s;
                }
                else
                {
                    return false;
                }

                if (saveIni.Get("Config", Const_IniCustomData_Name_SafeExtensionDistance).TryGetSingle(out f))
                {
                    _safeExtensionDistance = f;
                }

                if (saveIni.Get("Config", Const_IniCustomData_Name_TolerableFillRatio).TryGetSingle(out f))
                {
                    _tolerableFillRatio = f;
                }

                UpdateFrequency u;
                if (saveIni.Get("Config", Const_IniCustomData_Name_CycleUpdateFrequency).TryGetString(out s) && UpdateFrequency.TryParse(s, true, out u))
                {
                    _cycleUpdateFrequency = u;
                }

                if (saveIni.Get("Config", Const_IniCustomData_Name_InventoryCheckIntervalInSec).TryGetSingle(out f))
                {
                    _inventoryCheckIntervalInSec = f;
                }

                if (saveIni.Get("Config", Const_IniCustomData_Name_ExtensionStepDistance).TryGetSingle(out f))
                {
                    _extensionStepDistance = f;
                }

                if (saveIni.Get("Config", Const_IniCustomData_Name_MessageHistorySize).TryGetInt32(out integer))
                {
                    _messageHistorySize = integer;
                }

                #endregion

                #region Load client PBs

                int count;
                bool loadBlocksSuccess = true;
                if (!saveIni.Get(Const_IniSave_SectionName_clientPBs, "Count").TryGetInt32(out count))
                {
                    loadBlocksSuccess = false;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        long entityID;
                        if (saveIni.Get(Const_IniSave_SectionName_clientPBs, i.ToString()).TryGetInt64(out entityID))
                        {
                            IMyProgrammableBlock block = GridTerminalSystem.GetBlockWithId(entityID) as IMyProgrammableBlock;
                            if (block == null)
                            {
                                loadBlocksSuccess = false;
                                break;
                            }
                            clientPBs.Add(block);
                        }
                        else
                        {
                            loadBlocksSuccess = false;
                            break;
                        }
                    }
                }
                if (!loadBlocksSuccess)
                {
                    GetClientProgrammableBlocksFromGrid();
                }

                #endregion

                #region Load drills

                loadBlocksSuccess = true;
                if (!saveIni.Get(Const_IniSave_SectionName_drills, "Count").TryGetInt32(out count))
                {
                    loadBlocksSuccess = false;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        long entityID;
                        if (saveIni.Get(Const_IniSave_SectionName_drills, i.ToString()).TryGetInt64(out entityID))
                        {
                            IMyShipDrill block = GridTerminalSystem.GetBlockWithId(entityID) as IMyShipDrill;
                            if (block == null)
                            {
                                loadBlocksSuccess = false;
                                break;
                            }
                            drills.Add(block);
                        }
                        else
                        {
                            loadBlocksSuccess = false;
                            break;
                        }
                    }
                }
                if (!loadBlocksSuccess)
                {
                    GetDrillsFromGrid();
                }

                #endregion

                #region Load pistons

                loadBlocksSuccess = true;
                if (!saveIni.Get(Const_IniSave_SectionName_pistonsExtendingOnAxis, "Count").TryGetInt32(out count))
                {
                    loadBlocksSuccess = false;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        long entityID;
                        if (saveIni.Get(Const_IniSave_SectionName_pistonsExtendingOnAxis, i.ToString()).TryGetInt64(out entityID))
                        {
                            IMyPistonBase block = GridTerminalSystem.GetBlockWithId(entityID) as IMyPistonBase;
                            if (block == null)
                            {
                                loadBlocksSuccess = false;
                                break;
                            }
                            pistonsExtendingOnAxis.Add(block);
                        }
                        else
                        {
                            loadBlocksSuccess = false;
                            break;
                        }
                    }
                }

                if (!saveIni.Get(Const_IniSave_SectionName_pistonsRetractingOnAxis, "Count").TryGetInt32(out count))
                {
                    loadBlocksSuccess = false;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        long entityID;
                        if (saveIni.Get(Const_IniSave_SectionName_pistonsRetractingOnAxis, i.ToString()).TryGetInt64(out entityID))
                        {
                            IMyPistonBase block = GridTerminalSystem.GetBlockWithId(entityID) as IMyPistonBase;
                            if (block == null)
                            {
                                loadBlocksSuccess = false;
                                break;
                            }
                            pistonsRetractingOnAxis.Add(block);
                        }
                        else
                        {
                            loadBlocksSuccess = false;
                            break;
                        }
                    }
                }

                if (!loadBlocksSuccess)
                {
                    GetPistonsFromGrid();
                }

                #endregion

                #region Load rotor

                long rotorEntityID;
                if (saveIni.Get("OtherParts", Const_IniSave_Name_rotor).TryGetInt64(out rotorEntityID))
                {
                    rotor = GridTerminalSystem.GetBlockWithId(rotorEntityID) as IMyMotorStator;
                    if (rotor == null)
                    {
                        GetRotorFromGrid();
                    }
                }
                else
                {
                    GetRotorFromGrid();
                }

                #endregion

                #region Load text panels

                loadBlocksSuccess = true;
                if (!saveIni.Get(Const_IniSave_SectionName_textPanels, "Count").TryGetInt32(out count))
                {
                    loadBlocksSuccess = false;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        long entityID;
                        if (saveIni.Get(Const_IniSave_SectionName_textPanels, i.ToString()).TryGetInt64(out entityID))
                        {
                            IMyTextPanel block = GridTerminalSystem.GetBlockWithId(entityID) as IMyTextPanel;
                            if (block == null)
                            {
                                loadBlocksSuccess = false;
                                break;
                            }
                            textPanels.Add(block);
                        }
                        else
                        {
                            loadBlocksSuccess = false;
                        }
                    }
                }
                if (!loadBlocksSuccess)
                {
                    GetTextPanelsFromGrid();
                }

                #endregion

                #region Load state

                MechanismState ms;
                if (saveIni.Get("State", Const_IniCustomData_Name_CurrentMechanismState).TryGetString(out s) && Enum.TryParse(s, true, out ms))
                {
                    _currentMechanismState = ms;
                }
                else
                {
                    return false;
                }

                if (saveIni.Get("State", Const_IniSave_Name_elapsedTimeSinceLastInventoryCheckInSec).TryGetSingle(out f))
                {
                    elapsedTimeSinceLastInventoryCheckInSec = f;
                }
                else
                {
                    elapsedTimeSinceLastInventoryCheckInSec = 0;
                }

                string messageHistoryString;
                if (saveIni.Get("State", Const_IniCustomData_Name_MessageHistory).TryGetString(out messageHistoryString))
                {
                    MessageHistory.Clear();
                    MessageHistory.AddRange(messageHistoryString.Split('\n'));
                }
                else
                {
                    MessageHistory.Clear();
                }

                #endregion

                return true;
            }
            else
            {
                saveIni.Clear();
                return false;
            }
        }

        #endregion

        #region Initialization

        void Initialize(bool tryLoad = true, bool tryReadCustomDataOnSkippedLoad = true)
        {
            InitializeDefaults();
            InitializeTemplates();
            InitializeCommands();

            if (tryLoad && Load())
            {
                InitializeMechanismFromExisting();
                if (mechanism==null)
                {
                    MyEcho("Initialization failed.");
                    return;
                }
                switch (CurrentMechanismState)
                {
                    case MechanismState.Retracting:
                        Runtime.UpdateFrequency |= CycleUpdateFrequency;
                        mechanism.Retract();
                        break;
                    case MechanismState.Drilling:
                        Runtime.UpdateFrequency |= CycleUpdateFrequency;
                        mechanism.Start();
                        break;
                    default:
                        break;
                }
                UpdateCustomData(true);
                MyEcho($"Loading complete! Continuing with state: '{CurrentMechanismState}'.");
            }
            else
            {
                InitializeMechanismFromScratch();
                if (mechanism==null)
                {
                    MyEcho("Initialization failed.");
                    return;
                }
                CurrentMechanismState = MechanismState.None;
                if (tryReadCustomDataOnSkippedLoad)
                {
                    try
                    {
                        ConfigureFromCustomData();
                    }
                    catch (Exception)
                    {

                        UpdateCustomData(true);
                    }
                }
            }
        }

        #region Sub-initializers
        void InitializeDefaults()
        {

            if (MessageHistory == null)
            {
                MessageHistory = new List<string>();
            }
            else
            {
                MessageHistory.Clear();
            }

            _currentMechanismState = MechanismState.None;

            if (pistonsExtendingOnAxis == null)
            {
                pistonsExtendingOnAxis = new List<IMyPistonBase>();
            }
            else
            {
                pistonsExtendingOnAxis.Clear();
            }

            if (pistonsRetractingOnAxis == null)
            {
                pistonsRetractingOnAxis = new List<IMyPistonBase>();
            }
            else
            {
                pistonsRetractingOnAxis.Clear();
            }

            rotor = null;

            if (drills == null)
            {
                drills = new List<IMyShipDrill>();
            }
            else
            {
                drills.Clear();
            }

            if (clientPBs == null)
            {
                clientPBs = new List<IMyProgrammableBlock>();
            }
            else
            {
                clientPBs.Clear();
            }

            if (saveIni == null)
            {
                saveIni = new MyIni();
            }
            else
            {
                saveIni.Clear();
            }

            if (ini == null)
            {
                ini = new MyIni();
            }
            else
            {
                ini.Clear();
            }

            if (iniInfo == null)
            {
                iniInfo = new MyIni();
            }
            else
            {
                iniInfo.Clear();
            }

            elapsedTimeSinceLastInventoryCheckInSec = 0;

            if (textPanels == null)
            {
                textPanels = new List<IMyTextPanel>();
            }
            else
            {
                textPanels.Clear();
            }

            if (commandLine == null)
            {
                commandLine = new MyCommandLine();
            }
            else
            {
                commandLine.Clear();
            }

            _cycleUpdateFrequency = UpdateFrequency.Update100;


            _inventoryCheckIntervalInSec = 60f;


            _tolerableFillRatio = 0.5f;


            _safeExtensionDistance = 0f;


            _messageHistorySize = 10;

            _extensionStepDistance = 0.2f;

            _drillMechanismID = "";

            if (iniPB == null)
            {
                iniPB = new MyIni();
            }
            else
            {
                iniPB.Clear();
            }
        }

        void InitializeCommands()
        {
            if (commandDictionary == null)
            {
                commandDictionary = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
                {
                    {Const_Cmd_Stop, Stop },
                    {Const_Cmd_ShutDown, ShutDown },
                    {Const_Cmd_Startup, StartUp },
                    {Const_Cmd_ResetToDefaults, ResetToDefaults },
                    {Const_Cmd_ConfigureFromCustomData, ConfigureFromCustomData },
                    {Const_Cmd_GetBlocks, GetBlocks },
                    {Const_Cmd_UpdateIDs, UpdateIDs }
                };
            } // No need to set them otherwise, as these values are never modified.
        }

        void InitializeTemplates()
        {
            #region Piston
            if (iniTemplatePiston == null)
            {
                iniTemplatePiston = new MyIni();

                iniTemplatePiston.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, "");
                iniTemplatePiston.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, $"The {Const_IniAll_Name_MechanismID} of the drill mechanism this belongs to.");

                iniTemplatePiston.Set(Const_IniAll_SectionName_Mechanism, Const_IniPiston_Name_PistonExtendsWhileDrilling, "");
                iniTemplatePiston.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniPiston_Name_PistonExtendsWhileDrilling, $"Whether or not this piston needs to extend while the mechanism is drilling."
                                                                                + "\n\tTrue  - The mechanism will extend this piston to drill further."
                                                                                + "\n\tFalse - The mechanism will retract this piston to drill further.");
            } // No need to set them otherwise, as these values are never modified.
            #endregion

            #region OnlyID
            if (iniTemplateOnlyID == null)
            {
                iniTemplateOnlyID = new MyIni();

                iniTemplateOnlyID.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, "");
                iniTemplateOnlyID.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, $"The {Const_IniAll_Name_MechanismID} of the drill mechanism this belongs to.");
            } // No need to set them otherwise, as these values are never modified.
            #endregion

            #region PB
            if (iniTemplatePB == null)
            {
                iniTemplatePB = new MyIni();

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, "");
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, $"The {Const_IniAll_Name_MechanismID} of the drill mechanism this programmable block is controlling.");

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_InventoryCheckIntervalInSec, 60f);
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_InventoryCheckIntervalInSec, "The time between checking drill inventory fill rates.");

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_CycleUpdateFrequency, UpdateFrequency.Update100.ToString("F"));
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_CycleUpdateFrequency, $"The tick frequency this script is run while drilling or retracting.\nValid values are: '{UpdateFrequency.Update1:F}','{UpdateFrequency.Update10:F}','{UpdateFrequency.Update100:F}'.");

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_TolerableFillRatio, 0.5f);
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_TolerableFillRatio, "The ratio of the drill inventory fill rate above which the drill mechanism is stopped.\n\t0.0 means that the drills must be empty. Do not use 0.0, as it will regularly stop the drills.\n\t1.0 means that the drill can be full, and the mechanism will still continue drilling.");

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_SafeExtensionDistance, 0f);
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_SafeExtensionDistance, "The distance the drill mechanism can extend without drilling.\nUse this to make the drill reach the ground faster.");

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_ExtensionStepDistance, 0.2f);
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_ExtensionStepDistance, "How many meters should the mechanism extend each full rotation.");

                iniTemplatePB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_MessageHistorySize, 10);
                iniTemplatePB.SetComment(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_MessageHistorySize, "The amount of messages to show and save.");
            } // No need to set them otherwise, as these values are never modified.
            #endregion
        }

        /// <summary>
        /// Gets the necessary blocks from the grid and then initializes the drill mechanism.
        /// </summary>
        void InitializeMechanismFromScratch()
        {
            GetBlocks();
            InitializeMechanismFromExisting();
        }

        /// <summary>
        /// Initializes the drill mechanism assuming that the necessary blocks are collected in their lists.
        /// </summary>
        void InitializeMechanismFromExisting()
        {
            if ((pistonsExtendingOnAxis.Count <= 0 && pistonsRetractingOnAxis.Count <= 0) || rotor == null || drills.Count <= 0)
            {
                MyEcho("Mechanism is missing blocks required to work properly!");
                return;
            }

            MyEcho($"Initializing drill mechanism with ID: '{DrillMechanismID}'...");
            mechanism = new ArgDrillMechanism(pistonsExtendingOnAxis, pistonsRetractingOnAxis, drills, rotor, MyEcho);
            mechanism.PistonsAxisStepDistance = ExtensionStepDistance;
        }

        #endregion

        #endregion

        #region Getting blocks

        void GetTextPanelsFromGrid()
        {
            textPanels.Clear();
            MyEcho("Searching for text panels...");
            List<IMyTextPanel> allTextPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(allTextPanels, t => MyIni.HasSection(t.CustomData, Const_IniAll_SectionName_Mechanism));
            foreach (var t in allTextPanels)
            {
                ini.Clear();
                string id;
                if (ini.TryParse(t.CustomData) && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID).TryGetString(out id) && !string.IsNullOrEmpty(id))
                {
                    if (id == DrillMechanismID)
                    {
                        textPanels.Add(t);
                        t.WriteText("");
                    }
                }
                else
                {
                    MyEcho($"Reading custom data failed on text panel: {t.CustomName}");
                    t.CustomData = iniTemplateOnlyID.ToString();
                }
            }
            MyEcho($"Found {textPanels.Count} text panels.");
        }

        void GetPistonsFromGrid()
        {
            pistonsExtendingOnAxis.Clear();
            pistonsRetractingOnAxis.Clear();
            MyEcho("Searching for pistons...");
            List<IMyPistonBase> pistons = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlocksOfType(pistons, p => MyIni.HasSection(p.CustomData, Const_IniAll_SectionName_Mechanism));

            foreach (var piston in pistons)
            {
                ini.Clear();
                string id;
                bool doesExtendWhileDrilling;
                if (ini.TryParse(piston.CustomData) && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID).TryGetString(out id) && !string.IsNullOrEmpty(id) && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniPiston_Name_PistonExtendsWhileDrilling).TryGetBoolean(out doesExtendWhileDrilling))
                {
                    if (id == DrillMechanismID)
                    {
                        if (doesExtendWhileDrilling)
                        {
                            pistonsExtendingOnAxis.Add(piston);
                        }
                        else
                        {
                            pistonsRetractingOnAxis.Add(piston);
                        }
                    }
                }
                else
                {
                    MyEcho($"Reading custom data failed on piston: {piston.CustomName}");
                    piston.CustomData = iniTemplatePiston.ToString();
                    continue;
                }
            }
            MyEcho($"Found {pistonsExtendingOnAxis.Count + pistonsRetractingOnAxis.Count} pistons.");
        }

        void GetRotorFromGrid()
        {
            rotor = null;
            try
            {
                MyEcho("Searching for rotor...");
                List<IMyMotorStator> allRotors = new List<IMyMotorStator>();
                List<IMyMotorStator> rotors = new List<IMyMotorStator>();
                GridTerminalSystem.GetBlocksOfType(allRotors, r => MyIni.HasSection(r.CustomData, Const_IniAll_SectionName_Mechanism));


                foreach (var r in allRotors)
                {
                    ini.Clear();
                    string id;
                    if (ini.TryParse(r.CustomData) && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID).TryGetString(out id) && !string.IsNullOrEmpty(id))
                    {
                        if (id == DrillMechanismID)
                        {
                            rotors.Add(r);
                        }
                    }
                    else
                    {
                        MyEcho($"Reading custom data failed on rotor: {r.CustomName}");
                        r.CustomData = iniTemplateOnlyID.ToString();
                    }

                }
                if (rotors.Count < 0)
                {
                    MyEcho("No rotor found!");
                }
                else if (rotors.Count > 1)
                {
                    MyEcho("Multiple rotors found! There can only be one!");
                }
                else
                {
                    rotor = rotors[0];
                    MyEcho("Rotor found.");
                }
            }
            catch (Exception)
            {
                MyEcho("Rotor not found!");
                rotor = null;
            }
            
        }

        void GetDrillsFromGrid()
        {
            drills.Clear();
            MyEcho("Searching for drills...");
            List<IMyShipDrill> allDrills = new List<IMyShipDrill>();
            GridTerminalSystem.GetBlocksOfType(allDrills, d => MyIni.HasSection(d.CustomData, Const_IniAll_SectionName_Mechanism));
            foreach (var d in allDrills)
            {
                ini.Clear();
                string id;
                if (ini.TryParse(d.CustomData) && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID).TryGetString(out id) && !string.IsNullOrEmpty(id))
                {
                    if (id == DrillMechanismID)
                    {
                        drills.Add(d);
                    }
                }
                else
                {
                    MyEcho($"Reading custom data failed on drill: {d.CustomName}");
                    d.CustomData = iniTemplateOnlyID.ToString();
                }
            }
            MyEcho($"Found {drills.Count} drills.");
        }

        void GetClientProgrammableBlocksFromGrid()
        {
            clientPBs.Clear();
            MyEcho("Searching for client programmable blocks...");
            List<IMyProgrammableBlock> allPBs = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(allPBs, pb => MyIni.HasSection(pb.CustomData, Const_IniAll_SectionName_Mechanism));
            foreach (var pb in allPBs)
            {
                ini.Clear();
                string id;
                if (ini.TryParse(pb.CustomData) && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID).TryGetString(out id) && !string.IsNullOrEmpty(id))
                {
                    bool b;
                    if (id == DrillMechanismID && ini.Get(Const_IniAll_SectionName_Mechanism, Const_IniClient_Name_IsClient).TryGetBoolean(out b) && b && (Me.EntityId != pb.EntityId))
                    {
                        clientPBs.Add(pb);
                    }
                }
                else
                {
                    MyEcho($"Reading custom data failed on programmable block: {pb.CustomName}. It will be skipped.");
                }
            }
            MyEcho($"Found {clientPBs.Count} client programmable blocks.");
        }

        #endregion

        void ParseArgument(string argument)
        {
            try
            {
                commandLine.TryParse(argument);
                if (commandLine.ArgumentCount == 1)
                {
                    commandDictionary[commandLine.Argument(0)]();
                }
                else
                {
                    string arg1 = commandLine.Argument(0).ToLower();
                    string arg2 = commandLine.Argument(1);
                    string arg3 = commandLine.Argument(2);

                    if (arg1 == Const_Cmd_Configure.ToLower())
                    {
                        Configure(arg2, arg3);
                    }

                }

            }
            catch (Exception)
            {
                string commandList = "";
                foreach (var cmd in commandDictionary.Keys)
                {
                    if (commandList != "")
                    {
                        commandList += ", ";
                    }
                    commandList += $"'{cmd}'";
                }
                commandList += $", '{Const_Cmd_Configure}'";
                MyEcho($"Invalid command! Valid commands: {commandList}.");
            }
        }

        void MyEcho(string message)
        {
            MessageHistory.Add(message);
            message.Trim('\n');
            message.Replace('\n', ' ');
            if (MessageHistory.Count > MessageHistorySize)
            {
                int diff = MessageHistory.Count - MessageHistorySize;
                MessageHistory.RemoveRange(0, diff);
            }


            Echo(message);
            if (textPanels == null)
            {
                return;
            }
            foreach (var textSurface in textPanels)
            {
                textSurface.WriteText("");
                foreach (var line in MessageHistory)
                {
                    textSurface.WriteText($"[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}]  {line}\n", true);
                }
            }
        }

        void UpdateCustomData(bool clearIni = false)
        {
            if (clearIni)
            {
                // Set comments in the template initializer.
                if (!iniPB.TryParse(iniTemplatePB.ToString()))
                {
                    throw new Exception("Template is invalid for the programmable block!");
                }

                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_InventoryCheckIntervalInSec, InventoryCheckIntervalInSec);
                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_CycleUpdateFrequency, CycleUpdateFrequency.ToString("F"));
                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_TolerableFillRatio, TolerableFillRatio);
                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_SafeExtensionDistance, SafeExtensionDistance);
                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_MessageHistorySize, MessageHistorySize);
                iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_ExtensionStepDistance, ExtensionStepDistance);
            }
            Me.CustomData = iniPB.ToString();
        }

        void UpdateInfo()
        {
            string s;
            #region MechanismInfo

            switch (CurrentMechanismState)
            {
                case MechanismState.Drilling:
                    s = "Drilling";
                    break;
                case MechanismState.Stopped:
                    s = "Stopped";
                    break;
                case MechanismState.Retracting:
                    s = "Retracting";
                    break;
                default:
                    s = "None";
                    break;
            }
            iniInfo.Set(Const_IniInfo_SectionName_GeneralInfo, Const_IniInfo_Name_Activity, s);

            iniInfo.Set(Const_IniInfo_SectionName_MechanismInfo, Const_IniInfo_Name_CurrentPosition, mechanism.CurrentPosition);
            iniInfo.Set(Const_IniInfo_SectionName_MechanismInfo, Const_IniInfo_Name_HighestPosition, mechanism.HighestPosition);
            // Drill inventory fill ratio is updated each time it is checked.

            #endregion

            #region MessageHistory

            s = "";
            foreach (var line in MessageHistory)
            {
                s += $"{line}\n";
            }
            s.TrimEnd('\n');
            iniInfo.Set(Const_IniInfo_SectionName_GeneralInfo, Const_IniCustomData_Name_MessageHistory, s);

            #endregion
        }

        void SendInfoToClientPBs()
        {
            string info = iniInfo.ToString();
            foreach (var pb in clientPBs)
            {
                pb.TryRun(info);
            }
        }

        #region Commands

        void StartUp()
        {
            Runtime.UpdateFrequency |= CycleUpdateFrequency;
            switch (CurrentMechanismState)
            {
                case MechanismState.None:
                    MyEcho("Starting up drill mechanism.");
                    mechanism.StartUp();
                    CurrentMechanismState = MechanismState.Drilling;
                    break;
                case MechanismState.Stopped:
                    if (mechanism.IsFullyRetracted)
                    {
                        MyEcho("Starting up drill mechanism.");
                        mechanism.StartUp();
                    }
                    else
                    {
                        MyEcho("Starting drill mechanism.");
                        mechanism.Start();
                    }
                    CurrentMechanismState = MechanismState.Drilling;
                    if (SafeExtensionDistance > mechanism.CurrentPosition)
                    {
                        mechanism.ExtendMeters(SafeExtensionDistance - mechanism.CurrentPosition);
                    }
                    break;
                case MechanismState.Drilling:
                    MyEcho("Can not start up drill mechanism, it is already drilling!");
                    break;
                case MechanismState.Retracting:
                    MyEcho("Can not start up drill mechanism while it is retracting!");
                    break;
                default:
                    break;
            }
        }

        void Stop()
        {
            Runtime.UpdateFrequency &= ~CycleUpdateFrequency;
            switch (CurrentMechanismState)
            {
                case MechanismState.Stopped:
                case MechanismState.None:
                    MyEcho("Can not stop drill mechanism, it is already stopped!");
                    break;
                case MechanismState.Drilling:
                case MechanismState.Retracting:
                    MyEcho("Stopping drill mechanism.");
                    mechanism.Stop();
                    CurrentMechanismState = MechanismState.Stopped;
                    break;
                default:
                    break;
            }
        }

        void ShutDown()
        {
            Runtime.UpdateFrequency |= CycleUpdateFrequency;
            switch (CurrentMechanismState)
            {
                case MechanismState.Stopped:
                    mechanism.Retract();
                    CurrentMechanismState = MechanismState.Retracting;
                    break;
                case MechanismState.None:
                case MechanismState.Drilling:
                    mechanism.Stop();
                    mechanism.Retract();
                    CurrentMechanismState = MechanismState.Retracting;
                    break;
                case MechanismState.Retracting:
                    MyEcho("The mechanism is in the process of shutting down.");
                    break;
                default:
                    break;
            }
        }

        void Configure(string fieldName, string newValue)
        {
            MyEcho($"Configuring field '{fieldName}' with value '{newValue}'...");

            int i;
            float f;
            UpdateFrequency u;
            switch (fieldName)
            {
                case Const_IniAll_Name_MechanismID:
                    DrillMechanismID = newValue;
                    MyEcho($"{Const_IniAll_Name_MechanismID} configured succesfully.");
                    break;
                case Const_IniCustomData_Name_CycleUpdateFrequency:
                    if (UpdateFrequency.TryParse(newValue, true, out u))
                    {
                        CycleUpdateFrequency = u;
                        MyEcho($"{Const_IniCustomData_Name_CycleUpdateFrequency} configured succesfully.");
                    }
                    break;
                case Const_IniCustomData_Name_InventoryCheckIntervalInSec:
                    if (float.TryParse(newValue, out f))
                    {
                        InventoryCheckIntervalInSec = f;
                        MyEcho($"{Const_IniCustomData_Name_InventoryCheckIntervalInSec} configured succesfully.");
                    }
                    break;
                case Const_IniCustomData_Name_TolerableFillRatio:
                    if (float.TryParse(newValue, out f))
                    {
                        TolerableFillRatio = f;
                        MyEcho($"{Const_IniCustomData_Name_TolerableFillRatio} configured succesfully.");
                    }
                    break;
                case Const_IniCustomData_Name_SafeExtensionDistance:
                    if (float.TryParse(newValue, out f))
                    {
                        SafeExtensionDistance = f;
                        MyEcho($"{Const_IniCustomData_Name_SafeExtensionDistance} configured succesfully.");
                    }
                    break;
                case Const_IniCustomData_Name_ExtensionStepDistance:
                    if (float.TryParse(newValue, out f))
                    {
                        ExtensionStepDistance = f;
                        MyEcho($"{Const_IniCustomData_Name_ExtensionStepDistance} configured succesfully.");
                    }
                    break;
                case Const_IniCustomData_Name_MessageHistorySize:
                    if (int.TryParse(newValue, out i))
                    {
                        MessageHistorySize = i;
                        MyEcho($"{Const_IniCustomData_Name_MessageHistorySize} configured succesfully.");
                    }
                    break;
                default:
                    MyEcho($"Could not configure field '{fieldName}', because it does not exist!");
                    break;
            }
        }

        void ResetToDefaults()
        {
            Storage = "";
            Me.CustomData = "";
            Initialize(false, false);
            UpdateCustomData(true);
        }

        void ConfigureFromCustomData()
        {
            MyEcho("Configuring from custom data...");
            if (iniPB.TryParse(Me.CustomData))
            {
                string s;
                float f;
                int i;

                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID).TryGetString(out s))
                {
                    _drillMechanismID = s;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniAll_Name_MechanismID}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniAll_Name_MechanismID}' from custom data.");
                }

                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_InventoryCheckIntervalInSec).TryGetSingle(out f))
                {
                    _inventoryCheckIntervalInSec = f;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniCustomData_Name_InventoryCheckIntervalInSec}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniCustomData_Name_InventoryCheckIntervalInSec}' from custom data.");
                }

                UpdateFrequency u;
                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_CycleUpdateFrequency).TryGetString(out s) && UpdateFrequency.TryParse(s, out u))
                {
                    _cycleUpdateFrequency = u;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniCustomData_Name_CycleUpdateFrequency}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniCustomData_Name_CycleUpdateFrequency}' from custom data.");
                }

                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_TolerableFillRatio).TryGetSingle(out f))
                {
                    _tolerableFillRatio = f;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniCustomData_Name_TolerableFillRatio}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniCustomData_Name_TolerableFillRatio}' from custom data.");
                }

                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_SafeExtensionDistance).TryGetSingle(out f))
                {
                    _safeExtensionDistance = f;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniCustomData_Name_SafeExtensionDistance}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniCustomData_Name_SafeExtensionDistance}' from custom data.");
                }

                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_ExtensionStepDistance).TryGetSingle(out f))
                {
                    _extensionStepDistance = f;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniCustomData_Name_ExtensionStepDistance}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniCustomData_Name_ExtensionStepDistance}' from custom data.");
                }

                if (iniPB.Get(Const_IniAll_SectionName_Mechanism, Const_IniCustomData_Name_MessageHistorySize).TryGetInt32(out i))
                {
                    _messageHistorySize = i;
                }
                else
                {
                    Me.CustomData = iniTemplatePB.ToString();
                    MyEcho($"Error while trying to configure '{Const_IniCustomData_Name_MessageHistorySize}' from custom data.");
                    throw new Exception($"Error while trying to configure '{Const_IniCustomData_Name_MessageHistorySize}' from custom data.");
                }
                MyEcho("Configuring from custom data was succesful.");
            }
            else
            {
                Me.CustomData = iniTemplatePB.ToString();
                MyEcho("Couldn't configure from custom data, because custom data could not be parsed!");
                throw new Exception("Couldn't configure from custom data, because custom data could not be parsed!");
            }
        }

        void GetBlocks()
        {
            GetTextPanelsFromGrid();
            GetClientProgrammableBlocksFromGrid();
            GetPistonsFromGrid();
            GetRotorFromGrid();
            GetDrillsFromGrid();
        }

        void UpdateIDs()
        {
            MyEcho($"Updating {Const_IniAll_Name_MechanismID} on controlled blocks...");

            MyIni blockIni = new MyIni();

            #region Update textPanels
            foreach (var b in textPanels)
            {
                blockIni.Clear();
                if (blockIni.TryParse(b.CustomData))
                {
                    blockIni.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                    b.CustomData = blockIni.ToString();
                }
                else
                {
                    MyEcho($"Could not update {Const_IniAll_Name_MechanismID} on {b.CustomName}.");
                }
            }
            #endregion

            #region Update client PBs
            foreach (var b in clientPBs)
            {
                blockIni.Clear();
                if (blockIni.TryParse(b.CustomData))
                {
                    blockIni.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                    b.CustomData = blockIni.ToString();
                }
                else
                {
                    MyEcho($"Could not update {Const_IniAll_Name_MechanismID} on {b.CustomName}.");
                }
            }
            #endregion

            #region Update self PB

            iniPB.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
            UpdateCustomData();

            #endregion

            #region Update drills
            foreach (var b in drills)
            {
                blockIni.Clear();
                if (blockIni.TryParse(b.CustomData))
                {
                    blockIni.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                    b.CustomData = blockIni.ToString();
                }
                else
                {
                    MyEcho($"Could not update {Const_IniAll_Name_MechanismID} on {b.CustomName}.");
                }
            }
            #endregion

            #region Update rotor
            blockIni.Clear();
            if (blockIni.TryParse(rotor.CustomData))
            {
                blockIni.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                rotor.CustomData = blockIni.ToString();
            }
            else
            {
                MyEcho($"Could not update {Const_IniAll_Name_MechanismID} on {rotor.CustomName}.");
            }
            #endregion

            #region Update pistons
            foreach (var b in pistonsExtendingOnAxis)
            {
                blockIni.Clear();
                if (blockIni.TryParse(b.CustomData))
                {
                    blockIni.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                    b.CustomData = blockIni.ToString();
                }
                else
                {
                    MyEcho($"Could not update {Const_IniAll_Name_MechanismID} on {b.CustomName}.");
                }
            }
            foreach (var b in pistonsRetractingOnAxis)
            {
                blockIni.Clear();
                if (blockIni.TryParse(b.CustomData))
                {
                    blockIni.Set(Const_IniAll_SectionName_Mechanism, Const_IniAll_Name_MechanismID, DrillMechanismID);
                    b.CustomData = blockIni.ToString();
                }
                else
                {
                    MyEcho($"Could not update {Const_IniAll_Name_MechanismID} on {b.CustomName}.");
                }
            }
            #endregion

            MyEcho($"{Const_IniAll_Name_MechanismID} updated on controlled blocks.");
        }

        #endregion
    }
}
