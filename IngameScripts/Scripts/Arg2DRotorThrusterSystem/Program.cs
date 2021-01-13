using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
        #region Constants

        #region Ini
        const string StrConst_GeneralBlocks_Section = "GeneralBlocks";
        const string StrConst_Arg2DRotorThrusterSystem_Section_Name = "Arg2DRotorThrusterSystem";
        const string StrConst_BlockConfig_TemplateRequest_Section = StrConst_Arg2DRotorThrusterSystem_Section_Name + "Template";

        const string StrConst_RotorThrusterGroups_Section = "RotorThrusterGroups";
        const string StrConst_RotorThrusterGroup_Name = "RotorThrusterGroup";
        const string StrConst_RotorThrusterGroup_Comment = "A rotor and the thrusters it rotates grouped together with information about default angle and heading directions.";


        const string StrConst_TextPanels_Section = StrConst_GeneralBlocks_Section;
        const string StrConst_TextPanels_Name = "TextPanels";
        const string StrConst_TextPanels_Comment = "Text panels where the script's outputs are shown.";

        const string StrConst_Controllers_Section = StrConst_GeneralBlocks_Section;
        const string StrConst_Controllers_Name = "Controllers";
        const string StrConst_Controllers_Comment = "Ship controller blocks and their priorities.";

        const string StrConst_SystemID_Section = StrConst_Arg2DRotorThrusterSystem_Section_Name;
        const string StrConst_SystemID_Name = "SystemID";
        const string StrConst_SystemID_Comment = "The ID of the rotor thruster system this belongs to.";

        const string StrConst_BlockConfig_Controller_Priority_Section = StrConst_Arg2DRotorThrusterSystem_Section_Name;
        const string StrConst_BlockConfig_Controller_Priority_Name = "Priority";
        const string StrConst_BlockConfig_Controller_Priority_Comment = "The proprity of this controller. The controller with the highest priority and non-zero indicator will be used as guide.";

        const string StrConst_BlockConfig_RotorID_Section = StrConst_Arg2DRotorThrusterSystem_Section_Name;
        const string StrConst_BlockConfig_RotorID_Name = "RotorID";
        const string StrConst_BlockConfig_RotorID_Comment = "The rotor ID for this block. All thrusters must have the rotor ID of the rotor they are controlling. Each rotor in the system must have a different rotor ID.";

        const string StrConst_BlockConfig_RotorDefaultAngle_Section = StrConst_Arg2DRotorThrusterSystem_Section_Name;
        const string StrConst_BlockConfig_RotorDefaultAngle_Name = "DefaultAngle";
        const string StrConst_BlockConfig_RotorDefaultAngle_Comment = "The angle this rotor should rotate to when the thrusters are not in use.";

        const string StrConst_BlockConfig_RotorHeading_Section = StrConst_Arg2DRotorThrusterSystem_Section_Name;
        const string StrConst_BlockConfig_RotorHeading_0_Name = "HeadingAt0";
        const string StrConst_BlockConfig_RotorHeading_0_Comment = "The direction the ship would be heading if the rotor angle is 0° and the thrusters it rotates are firing. ";
        const string StrConst_BlockConfig_RotorHeading_90_Name = "HeadingAt90";
        const string StrConst_BlockConfig_RotorHeading_90_Comment = "The direction the ship would be heading if the rotor angle is 90° and the thrusters it rotates are firing. ";
        #endregion

        #endregion

        #region Reusable variables

        List<IMyTextPanel> reusableTextPanels;
        List<IMyShipController> reusableShipControllers;
        List<IMyMotorStator> reusableRotors;
        List<IMyThrust> reusableThrusters;
        List<MyIniKey> reusableMyIniKeys;
        List<string> reusableStrings;

        MyIni reusableIni;

        #region Initialize

        void InitializeReusables()
        {
            if (reusableTextPanels==null)
            {
                reusableTextPanels = new List<IMyTextPanel>();
            }
            else
            {
                reusableTextPanels.Clear();
            }

            if (reusableStrings == null)
            {
                reusableStrings = new List<string>();
            }
            else
            {
                reusableStrings.Clear();
            }

            if (reusableMyIniKeys == null)
            {
                reusableMyIniKeys = new List<MyIniKey>();
            }
            else
            {
                reusableMyIniKeys.Clear();
            }

            if (reusableShipControllers == null)
            {
                reusableShipControllers = new List<IMyShipController>();
            }
            else
            {
                reusableShipControllers.Clear();
            }

            if (reusableRotors == null)
            {
                reusableRotors = new List<IMyMotorStator>();
            }
            else
            {
                reusableRotors.Clear();
            }

            if (reusableThrusters == null)
            {
                reusableThrusters = new List<IMyThrust>();
            }
            else
            {
                reusableThrusters.Clear();
            }

            if (reusableIni == null)
            {
                reusableIni = new MyIni();
            }
            else
            {
                reusableIni.Clear();
            }

        }

        #endregion

        #endregion

        #region Persistence

        ArgPersistenceSystem persistenceSystem;
        RotorThrusterGroupCollectionField rotorThrusterGroups;
        ShipControllerCollectionField controllers;
        ArgPersistenceSystem.Fields.BlockCollectionField<IMyTextPanel> textPanels;
        ArgPersistenceSystem.Fields.StringField systemID;

        #region Push and Pull

        string Pull()
        {
            return Storage;
        }

        void Push(string s)
        {
            Storage = s;
        }

        #endregion

        #region Initialization

        void InitializePersistence()
        {
            if (persistenceSystem == null)
            {
                persistenceSystem = new ArgPersistenceSystem(Pull, Push, GridTerminalSystem);
            }
            else
            {
                persistenceSystem.Clear();
            }

            if (rotorThrusterGroups == null)
            {
                rotorThrusterGroups = new RotorThrusterGroupCollectionField(StrConst_RotorThrusterGroups_Section, StrConst_RotorThrusterGroup_Name, StrConst_RotorThrusterGroup_Comment, new List<Arg2DRotorThrusterMechanism.RotorThrusterGroup>(), new List<Arg2DRotorThrusterMechanism.RotorThrusterGroup>(), actionOnReadFail: GetRotorThrusterGroupsFromGrid);
            }
            else
            {
                rotorThrusterGroups.SetDefaults();
            }

            if (controllers == null)
            {
                controllers = new ShipControllerCollectionField(StrConst_Controllers_Section, StrConst_Controllers_Name, StrConst_Controllers_Comment, new List<ShipControllerWithPriority>(), new List<ShipControllerWithPriority>(), actionOnReadFail: GetControllersFromGrid);
            }
            else
            {
                controllers.SetDefaults();
            }

            if (textPanels == null)
            {
                textPanels = new ArgPersistenceSystem.Fields.BlockCollectionField<IMyTextPanel>(StrConst_TextPanels_Section, StrConst_TextPanels_Name, StrConst_TextPanels_Comment, new List<IMyTextPanel>(), new List<IMyTextPanel>(), actionOnReadFail: GetTextPanelsFromGrid);
                textPanels.PullPriority = 1;
            }
            else
            {
                textPanels.SetDefaults();
            }

            if (systemID == null)
            {
                systemID = new ArgPersistenceSystem.Fields.StringField(StrConst_Arg2DRotorThrusterSystem_Section_Name, StrConst_SystemID_Name, StrConst_SystemID_Comment, "", "");
                systemID.PullPriority = 1;
            }

        }

        #endregion

        #endregion


        #region Commands

        ArgCommandParser commandParser;

        void InitializeCommands()
        {
            if (commandParser == null)
            {
                commandParser = new ArgCommandParser("help", "command_name", Echo);
            }
            else
            {
                commandParser.Clear(true, "help", "command_name");
            }


        }

        #endregion


        Arg2DRotorThrusterMechanism mechanismTemp;
        IMyShipController lastUsedController;
        public Program()
        {
            Initialize();

            // TODO replace this with command
            AddTemplatesWhereRequested();

            //TODO replace this with command
            systemID.FieldValue = "test_thrust_system_id";

            //TODO replace with pull request
            GetTextPanelsFromGrid();
            GetControllersFromGrid();
            GetRotorThrusterGroupsFromGrid();

            //TODO
            foreach (var tp in textPanels.FieldValues)
            {
                tp.WriteText("");
            }

            mechanismTemp = new Arg2DRotorThrusterMechanism(rotorThrusterGroups.FieldValues,rotateToDefaultWhenUnused: true, shareInertiaTensors: false);
            lastUsedController = controllers.FieldValues.MaxBy(c => c.Priority).ShipController;

            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // Parse command
            if ((updateSource & (UpdateType.Terminal | UpdateType.Script)) != 0)
            {
                if (!string.IsNullOrWhiteSpace(argument))
                {
                    commandParser.Parse(argument);
                }
            }

            //Debug begin TODO REMOVE
            IMyShipController controller = controllers.FieldValues.Where(c => !Vector3.IsZero(c.ShipController.MoveIndicator)).OrderByDescending(c => c.Priority).FirstOrDefault().ShipController;
            if (controller!=null)
            {
                lastUsedController = controller;
            }
            mechanismTemp.Thrust(lastUsedController);
            //Debug end TODO REMOVE

            // Stop if the system has no valid ID
            if (string.IsNullOrWhiteSpace(systemID.FieldValue))
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            foreach (var tp in textPanels.FieldValues)
            {
                tp.WriteText("");
            }
            Log($"Chain depth: {Runtime.CurrentCallChainDepth}/{Runtime.MaxCallChainDepth} ({Math.Round(100*(float)Runtime.CurrentCallChainDepth/Runtime.MaxCallChainDepth,2)})\n");
            Log($"Instruction count: {Runtime.CurrentInstructionCount}/{Runtime.MaxInstructionCount} ({Math.Round(100 * (float)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount, 2)})\n");
        }

        void Initialize()
        {
            InitializeReusables();
            InitializeTemplates();
            InitializeCommands();
            InitializePersistence();
        }

        void Log(string message)
        {
            Echo(message.TrimEnd('\n'));
            foreach (var tp in textPanels.FieldValues)
            {
                tp.WriteText(message, true);
            }
        }

        #region Templating
        /// <summary>
        /// Adds templates to related blocks if the request section is present.
        /// </summary>
        /// <param name="overwriteExistingKeys">If set to true, the keys already present will have their value overwritten.</param>
        /// <param name="overwriteIfInvalid">If set to true, the custom data will be overwritten if the block's custom data can not be parsed as an ini configuration.</param>
        void AddTemplatesWhereRequested(bool overwriteExistingKeys = false, bool overwriteIfInvalid = true)
        {
            #region TextPanels
            reusableTextPanels.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableTextPanels, b => MyIni.HasSection(b.CustomData, StrConst_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableTextPanels)
            {
                AddTemplateToBlock(templateTextPanel, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion

            #region ShipControllers
            reusableShipControllers.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableShipControllers, b => MyIni.HasSection(b.CustomData, StrConst_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableShipControllers)
            {
                AddTemplateToBlock(templateIniShipController, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion

            #region Thrusters
            reusableThrusters.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableThrusters, b => MyIni.HasSection(b.CustomData, StrConst_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableThrusters)
            {
                AddTemplateToBlock(templateIniThruster, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion

            #region Rotors
            reusableRotors.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableRotors, b => MyIni.HasSection(b.CustomData, StrConst_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableRotors)
            {
                AddTemplateToBlock(templateIniRotor, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion
        }

        /// <summary>
        /// Adds the contents of the given template ini to the block's custom data.
        /// <para>This will simply concatenate the end comments and end contents.</para>
        /// </summary>
        /// <param name="overwriteExistingKeys">If set to true, the keys already present will have their value overwritten.</param>
        /// <param name="overwriteIfInvalid">If set to true, the custom data will be overwritten if the block's custom data can not be parsed as an ini configuration.</param>
        void AddTemplateToBlock(MyIni templateIni, IMyTerminalBlock block, bool overwriteExistingKeys = false, bool overwriteIfInvalid = true)
        {
            bool parseSuccess = reusableIni.TryParse(block.CustomData);
            if (string.IsNullOrWhiteSpace(block.CustomData) || (!parseSuccess && overwriteIfInvalid))
            {
                block.CustomData = templateIni.ToString();
            }
            else if (parseSuccess)
            {
                reusableMyIniKeys.Clear();
                templateIni.GetKeys(reusableMyIniKeys);
                string s;
                foreach (var key in reusableMyIniKeys)
                {
                    if (!reusableIni.ContainsKey(key) || overwriteExistingKeys)
                    {
                        if (!templateIni.Get(key).TryGetString(out s))
                        {
                            s = "";
                        }
                        reusableIni.Set(key, s);
                    }
                    s = templateIni.GetComment(key);
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        reusableIni.SetComment(key, s);
                    }
                }
                if (!string.IsNullOrWhiteSpace(templateIni.EndComment))
                {
                    reusableIni.EndComment += "\n" + templateIni.EndComment;
                }
                if (!string.IsNullOrWhiteSpace(templateIni.EndContent))
                {
                    reusableIni.EndContent += "\n" + templateIni.EndContent;
                }
                reusableStrings.Clear();
                templateIni.GetSections(reusableStrings);
                if (reusableStrings.Count > 0)
                {
                    foreach (var section in reusableStrings)
                    {
                        s = templateIni.GetSectionComment(section);
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            reusableIni.SetSectionComment(section, s);
                        }
                    }
                }
                reusableIni.DeleteSection(StrConst_BlockConfig_TemplateRequest_Section);
                block.CustomData = reusableIni.ToString();
            }
        }

        #region Fields

        MyIni templateIniRotor;
        MyIni templateIniThruster;
        MyIni templateIniShipController;
        MyIni templateTextPanel;

        #endregion

        #region Initialization

        void InitializeTemplates()
        {
            if (templateTextPanel==null)
            {
                templateTextPanel = new MyIni();

                // System ID
                templateTextPanel.Set(StrConst_SystemID_Section, StrConst_SystemID_Name, "");
                templateTextPanel.SetComment(StrConst_SystemID_Section, StrConst_SystemID_Name, StrConst_SystemID_Comment);
            }

            if (templateIniRotor == null)
            {
                templateIniRotor = new MyIni();

                // System ID
                templateIniRotor.Set(StrConst_SystemID_Section, StrConst_SystemID_Name, "");
                templateIniRotor.SetComment(StrConst_SystemID_Section, StrConst_SystemID_Name, StrConst_SystemID_Comment);

                // Rotor ID
                templateIniRotor.Set(StrConst_BlockConfig_RotorID_Section, StrConst_BlockConfig_RotorID_Name, "");
                templateIniRotor.SetComment(StrConst_BlockConfig_RotorID_Section, StrConst_BlockConfig_RotorID_Name, StrConst_BlockConfig_RotorID_Comment);

                // Heading at 0
                templateIniRotor.Set(StrConst_BlockConfig_RotorHeading_Section, StrConst_BlockConfig_RotorHeading_0_Name, "");
                templateIniRotor.SetComment(StrConst_BlockConfig_RotorHeading_Section, StrConst_BlockConfig_RotorHeading_0_Name, StrConst_BlockConfig_RotorHeading_0_Comment);

                // Heading at 90
                templateIniRotor.Set(StrConst_BlockConfig_RotorHeading_Section, StrConst_BlockConfig_RotorHeading_90_Name, "");
                templateIniRotor.SetComment(StrConst_BlockConfig_RotorHeading_Section, StrConst_BlockConfig_RotorHeading_90_Name, StrConst_BlockConfig_RotorHeading_90_Comment);

                // Default angle
                templateIniRotor.Set(StrConst_BlockConfig_RotorDefaultAngle_Section, StrConst_BlockConfig_RotorDefaultAngle_Name, "");
                templateIniRotor.SetComment(StrConst_BlockConfig_RotorDefaultAngle_Section, StrConst_BlockConfig_RotorDefaultAngle_Name, StrConst_BlockConfig_RotorDefaultAngle_Comment);

            }

            if (templateIniThruster == null)
            {
                templateIniThruster = new MyIni();

                // System ID
                templateIniThruster.Set(StrConst_SystemID_Section, StrConst_SystemID_Name, "");
                templateIniThruster.SetComment(StrConst_SystemID_Section, StrConst_SystemID_Name, StrConst_SystemID_Comment);

                // Rotor ID
                templateIniThruster.Set(StrConst_BlockConfig_RotorID_Section, StrConst_BlockConfig_RotorID_Name, "");
                templateIniThruster.SetComment(StrConst_BlockConfig_RotorID_Section, StrConst_BlockConfig_RotorID_Name, StrConst_BlockConfig_RotorID_Comment);

            }

            if (templateIniShipController == null)
            {
                templateIniShipController = new MyIni();

                // System ID
                templateIniShipController.Set(StrConst_SystemID_Section, StrConst_SystemID_Name, "");
                templateIniShipController.SetComment(StrConst_SystemID_Section, StrConst_SystemID_Name, StrConst_SystemID_Comment);

                // Priority
                templateIniShipController.Set(StrConst_BlockConfig_Controller_Priority_Section, StrConst_BlockConfig_Controller_Priority_Name, "");
                templateIniShipController.SetComment(StrConst_BlockConfig_Controller_Priority_Section, StrConst_BlockConfig_Controller_Priority_Name, StrConst_BlockConfig_Controller_Priority_Comment);
            }

        }

        #endregion

        #endregion

        #region Getting blocks

        void GetBlocksOfTypeWithSameSystemIDFromGrid<T>(List<T> list) where T : class, IMyTerminalBlock
        {
            list.Clear();
            GridTerminalSystem.GetBlocksOfType(list, b => {
                string systemID = "";
                bool collect =
                    MyIni.HasSection(b.CustomData, StrConst_Arg2DRotorThrusterSystem_Section_Name) &&
                    reusableIni.TryParse(b.CustomData) && reusableIni.Get(StrConst_Arg2DRotorThrusterSystem_Section_Name, StrConst_SystemID_Name).TryGetString(out systemID) &&
                    systemID == this.systemID.FieldValue;
                return collect;
            });
        }

        void GetControllersFromGrid()
        {
            GetBlocksOfTypeWithSameSystemIDFromGrid(reusableShipControllers);
            controllers.SetDefaults();

            int priority;
            foreach (var controller in reusableShipControllers)
            {
                if (reusableIni.TryParse(controller.CustomData) && reusableIni.Get(StrConst_BlockConfig_Controller_Priority_Section, StrConst_BlockConfig_Controller_Priority_Name).TryGetInt32(out priority))
                {
                    controllers.FieldValues.Add(new ShipControllerWithPriority(controller, priority));
                }
            }
        }

        void GetTextPanelsFromGrid()
        {
            GetBlocksOfTypeWithSameSystemIDFromGrid(textPanels.FieldValues);
        }

        void GetRotorThrusterGroupsFromGrid()
        {
            GetBlocksOfTypeWithSameSystemIDFromGrid(reusableThrusters);
            GetBlocksOfTypeWithSameSystemIDFromGrid(reusableRotors);

            string rotorID;
            string s;
            float defaultAngle;
            Base6Directions.Direction h0 = default(Base6Directions.Direction);
            Base6Directions.Direction h90 = default(Base6Directions.Direction);
            while (reusableRotors.Count > 0)
            {
                IMyMotorStator rotor = reusableRotors[0];
                reusableRotors.Remove(rotor);
                if (
                    reusableIni.TryParse(rotor.CustomData) &&
                    reusableIni.Get(StrConst_BlockConfig_RotorID_Section, StrConst_BlockConfig_RotorID_Name).TryGetString(out rotorID) &&
                    !string.IsNullOrWhiteSpace(rotorID) &&
                    reusableIni.Get(StrConst_BlockConfig_RotorHeading_Section, StrConst_BlockConfig_RotorHeading_0_Name).TryGetString(out s) &&
                    Enum.TryParse(s, out h0) &&
                    reusableIni.Get(StrConst_BlockConfig_RotorHeading_Section, StrConst_BlockConfig_RotorHeading_90_Name).TryGetString(out s) &&
                    Enum.TryParse(s, out h90) &&
                    reusableIni.Get(StrConst_BlockConfig_RotorDefaultAngle_Section, StrConst_BlockConfig_RotorDefaultAngle_Name).TryGetSingle(out defaultAngle)
                )
                {
                    var rtg = new Arg2DRotorThrusterMechanism.RotorThrusterGroup(rotor, new List<IMyThrust>(), defaultAngle, h0, h90);
                    rtg.Thrusters.AddRange(reusableThrusters.Where(t => reusableIni.TryParse(t.CustomData) && reusableIni.Get(StrConst_BlockConfig_RotorID_Section, StrConst_BlockConfig_RotorID_Name).TryGetString(out s) && s == rotorID));
                    foreach (var t in rtg.Thrusters)
                    {
                        reusableThrusters.Remove(t);
                    }
                    if (rtg.Thrusters.Count > 0)
                    {
                        rotorThrusterGroups.FieldValues.Add(rtg);
                    }
                }
            }
        }

        #endregion
    }
}
