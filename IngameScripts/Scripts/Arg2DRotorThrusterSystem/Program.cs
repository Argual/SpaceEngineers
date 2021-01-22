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
        const string C_GeneralBlocks_Section = "GeneralBlocks";
        const string C_Arg2DRotorThrusterSystem_Section = "Arg2DRotorThrusterSystem";
        const string C_BlockConfig_TemplateRequest_Section = C_Arg2DRotorThrusterSystem_Section + "Template";

        const string C_RotorThrusterGroups_Section = "RotorThrusterGroups";
        const string C_RotorThrusterGroups_Name = "RotorThrusterGroups";
        const string C_RotorThrusterGroups_Comment = "Rotors and the thrusters they are rotating grouped together with other relevant values.";

        const string C_TextPanels_Section = C_GeneralBlocks_Section;
        const string C_TextPanels_Name = "TextPanels";
        const string C_TextPanels_Comment = "Text panels where the script's outputs are shown.";

        const string C_Controllers_Section = C_GeneralBlocks_Section;
        const string C_Controllers_Name = "Controllers";
        const string C_Controllers_Comment = "Ship controller blocks and their priorities.";

        const string C_LastUsedController_Section = C_GeneralBlocks_Section;
        const string C_LastUsedController_Name = "LastUsedController";
        const string C_LastUsedController_Comment = "The last ship controller which given non-zero movement indicator to the system.";

        const string C_SystemID_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_SystemID_Name = "SystemID";
        const string C_SystemID_Comment = "The ID of the rotor thruster system this belongs to.\nType: string";

        const string C_RotateToDefaultWhenUnused_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_RotateToDefaultWhenUnused_Name = "RotateToDefaultWhenUnused";
        const string C_RotateToDefaultWhenUnused_Comment = "Whether or not to rotate rotors to default angle when the thrusters it rotates are unused.\nValid values: 'True', 'False'";

        const string C_ForceEnableDampeners_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_ForceEnableDampeners_Name = "ForceEnableDampeners";
        const string C_ForceEnableDampeners_Comment = "If set to true, the thrusters will always use inertial dampeners.\nValid values: 'True', 'False'";

        const string C_ForceDisableDampeners_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_ForceDisableDampeners_Name = "ForceDisableDampeners";
        const string C_ForceDisableDampeners_Comment = "If set to true, the thrusters will never use inertial dampeners.\nValid values: 'True', 'False'";

        const string C_ShareInertiaTensor_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_ShareInertiaTensor_Name = "ShareInertiaTensor";
        const string C_ShareInertiaTensor_Comment = "Whether or not to share inertia tensor on rotors when they are not rotating. Inertia tensor sharing is always disabled on a rotor while it rotates for safety reasons.\nValid values: 'True', 'False'";

        const string C_DampenerActivationVelocityThreshold_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_DampenerActivationVelocityThreshold_Name = "DampenerActivationVelocityThreshold";
        const string C_DampenerActivationVelocityThreshold_Comment = "Dampeners will only be used while ship speed is above this value, even if '"+C_ForceEnableDampeners_Name+ "' is set to true.\nType: decimal number";

        const string C_ProportionalVelocityThreshold_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_ProportionalVelocityThreshold_Name = "ProportionalVelocityThreshold";
        const string C_ProportionalVelocityThreshold_Comment = "Thrusters strength will be proportional to (ship speed / this value) while ship speed is below this value.\nType: decimal number";

        const string C_ThrustStrengthMin_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_ThrustStrengthMin_Name = "ThrustStrengthMin";
        const string C_ThrustStrengthMin_Comment = "The minimum thrust override percentage (between 0 and 1). When thrusters are fired, they will be fired with strength equivalent to this value at minimum.\nType: decimal number";

        const string C_ThrustStrengthMultiplier_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_ThrustStrengthMultiplier_Name = "ThrustStrengthMultiplier";
        const string C_ThrustStrengthMultiplier_Comment = "Multiplies the thrust override percentages by this amount. Must be between 0 and 1.\nType: decimal number";

        const string C_RunUpdateFrequency_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_RunUpdateFrequency_Name = "UpdateFrequency";
        const string C_RunUpdateFrequency_Comment = "The frequency at which the thruster system checks for changes and makes appropriate corrections.\nValid values: 'Update1', 'Update10', 'Update100'";

        const string C_BlockConfig_Controller_Priority_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_BlockConfig_Controller_Priority_Name = "Priority";
        const string C_BlockConfig_Controller_Priority_Comment = "The proprity of this controller. The controller with the highest priority and non-zero indicator will be used as guide.\nType: integer";

        const string C_BlockConfig_RotorID_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_BlockConfig_RotorID_Name = "RotorID";
        const string C_BlockConfig_RotorID_Comment = "The rotor ID for this block. All thrusters must have the rotor ID of the rotor they are controlling. Each rotor in the system must have a different rotor ID.\nType: string";

        const string C_BlockConfig_RotorDefaultAngle_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_BlockConfig_RotorDefaultAngle_Name = "DefaultAngle";
        const string C_BlockConfig_RotorDefaultAngle_Comment = "The angle this rotor should rotate to when the thrusters are not in use.\nType: decimal number";

        const string C_BlockConfig_RotorHeading_Section = C_Arg2DRotorThrusterSystem_Section;
        const string C_BlockConfig_RotorHeading_0_Name = "HeadingAt0";
        const string C_BlockConfig_RotorHeading_0_Comment = "The direction the ship would be heading if the rotor angle is 0° and the thrusters it rotates are firing.\nValid values: 'Left', 'Right', 'Up', 'Down', 'Forward', 'Backward'";
        const string C_BlockConfig_RotorHeading_90_Name = "HeadingAt90";
        const string C_BlockConfig_RotorHeading_90_Comment = "The direction the ship would be heading if the rotor angle is 90° and the thrusters it rotates are firing.\nValid values: 'Left', 'Right', 'Up', 'Down', 'Forward', 'Backward'";
        #endregion

        #region Commands

        const string C_Cmd_Help = "Help";
        const string C_Cmd_Help_Arg1 = "Command_Name";

        const string C_Cmd_Reset = "Reset";

        const string C_Cmd_Set = "Set";
        const string C_Cmd_Set_Arg1 = "Property_Name";
        const string C_Cmd_Set_Arg2 = "New_Value";

        const string C_Cmd_Start = "Start";

        const string C_Cmd_ConfigureFromCustomData = "ConfigureFromCustomData";

        const string C_Cmd_AddTemplates = "AddTemplates";
        const string C_Cmd_AddTemplates_SwitchOverwriteExistingKeys = "OverwriteExistingKeys";
        const string C_Cmd_AddTemplates_SwitchOverwriteIfInvalid = "OverwriteIfInvalid";

        const string C_Cmd_GetBlocks = "GetBlocks";

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
            GridTerminalSystem.GetBlocksOfType(reusableTextPanels, b => MyIni.HasSection(b.CustomData, C_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableTextPanels)
            {
                AddTemplateToBlock(templateTextPanel, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion

            #region ShipControllers
            reusableShipControllers.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableShipControllers, b => MyIni.HasSection(b.CustomData, C_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableShipControllers)
            {
                AddTemplateToBlock(templateIniShipController, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion

            #region Thrusters
            reusableThrusters.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableThrusters, b => MyIni.HasSection(b.CustomData, C_BlockConfig_TemplateRequest_Section));
            foreach (var block in reusableThrusters)
            {
                AddTemplateToBlock(templateIniThruster, block, overwriteExistingKeys, overwriteIfInvalid);
            }
            #endregion

            #region Rotors
            reusableRotors.Clear();
            GridTerminalSystem.GetBlocksOfType(reusableRotors, b => MyIni.HasSection(b.CustomData, C_BlockConfig_TemplateRequest_Section));
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
                Log($"Set template on '{block.CustomName}'.");
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
                reusableIni.DeleteSection(C_BlockConfig_TemplateRequest_Section);
                block.CustomData = reusableIni.ToString();
                Log($"Added template to '{block.CustomName}'.");
            }
            else
            {
                Log($"Could not add template to '{block.CustomName}'.");
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
            if (templateTextPanel == null)
            {
                templateTextPanel = new MyIni();

                // System ID
                templateTextPanel.Set(C_SystemID_Section, C_SystemID_Name, "");
                templateTextPanel.SetComment(C_SystemID_Section, C_SystemID_Name, C_SystemID_Comment);
            }

            if (templateIniRotor == null)
            {
                templateIniRotor = new MyIni();

                // System ID
                templateIniRotor.Set(C_SystemID_Section, C_SystemID_Name, "");
                templateIniRotor.SetComment(C_SystemID_Section, C_SystemID_Name, C_SystemID_Comment);

                // Rotor ID
                templateIniRotor.Set(C_BlockConfig_RotorID_Section, C_BlockConfig_RotorID_Name, "");
                templateIniRotor.SetComment(C_BlockConfig_RotorID_Section, C_BlockConfig_RotorID_Name, C_BlockConfig_RotorID_Comment);

                // Heading at 0
                templateIniRotor.Set(C_BlockConfig_RotorHeading_Section, C_BlockConfig_RotorHeading_0_Name, "");
                templateIniRotor.SetComment(C_BlockConfig_RotorHeading_Section, C_BlockConfig_RotorHeading_0_Name, C_BlockConfig_RotorHeading_0_Comment);

                // Heading at 90
                templateIniRotor.Set(C_BlockConfig_RotorHeading_Section, C_BlockConfig_RotorHeading_90_Name, "");
                templateIniRotor.SetComment(C_BlockConfig_RotorHeading_Section, C_BlockConfig_RotorHeading_90_Name, C_BlockConfig_RotorHeading_90_Comment);

                // Default angle
                templateIniRotor.Set(C_BlockConfig_RotorDefaultAngle_Section, C_BlockConfig_RotorDefaultAngle_Name, "");
                templateIniRotor.SetComment(C_BlockConfig_RotorDefaultAngle_Section, C_BlockConfig_RotorDefaultAngle_Name, C_BlockConfig_RotorDefaultAngle_Comment);

            }

            if (templateIniThruster == null)
            {
                templateIniThruster = new MyIni();

                // System ID
                templateIniThruster.Set(C_SystemID_Section, C_SystemID_Name, "");
                templateIniThruster.SetComment(C_SystemID_Section, C_SystemID_Name, C_SystemID_Comment);

                // Rotor ID
                templateIniThruster.Set(C_BlockConfig_RotorID_Section, C_BlockConfig_RotorID_Name, "");
                templateIniThruster.SetComment(C_BlockConfig_RotorID_Section, C_BlockConfig_RotorID_Name, C_BlockConfig_RotorID_Comment);

            }

            if (templateIniShipController == null)
            {
                templateIniShipController = new MyIni();

                // System ID
                templateIniShipController.Set(C_SystemID_Section, C_SystemID_Name, "");
                templateIniShipController.SetComment(C_SystemID_Section, C_SystemID_Name, C_SystemID_Comment);

                // Priority
                templateIniShipController.Set(C_BlockConfig_Controller_Priority_Section, C_BlockConfig_Controller_Priority_Name, "");
                templateIniShipController.SetComment(C_BlockConfig_Controller_Priority_Section, C_BlockConfig_Controller_Priority_Name, C_BlockConfig_Controller_Priority_Comment);
            }

        }

        #endregion

        #endregion

        #region Persistence

        ArgPersistenceSystem persistenceSystem;
        RotorThrusterGroupCollectionField rotorThrusterGroups;
        ShipControllerCollectionField shipControllers;
        ArgPersistenceSystem.Fields.BlockCollectionField<IMyTextPanel> textPanels;
        ArgPersistenceSystem.Fields.BlockField<IMyShipController> lastUsedShipController;

        ArgPersistenceSystem.Fields.StringField systemID;
        ArgPersistenceSystem.Fields.BooleanField rotateToDefaultWhenUnused;
        ArgPersistenceSystem.Fields.BooleanField forceEnableDampeners;
        ArgPersistenceSystem.Fields.BooleanField forceDisableDampeners;
        ArgPersistenceSystem.Fields.BooleanField shareInertiaTensor;
        ArgPersistenceSystem.Fields.SingleField dampenerActivationVelocityThreshold;
        ArgPersistenceSystem.Fields.SingleField proportionalVelocityThreshold;
        ArgPersistenceSystem.Fields.SingleField thrustStrengthMin;
        ArgPersistenceSystem.Fields.SingleField thrustStrengthMultiplier;
        ArgPersistenceSystem.Fields.GenericValueField<UpdateFrequency> runUpdateFrequency;

        #region Push and Pull

        string PullPersistence()
        {
            return Storage;
        }

        void PushPersistence(string s)
        {
            Storage = s;
        }

        #endregion

        #region Initialization

        void SetDefaultsOnFail(ArgPersistenceSystem.Fields.Field f)
        {
            Log($"Pulling '{f.Key}' failed. It will be set to default value.");
            f.SetDefaults();
        }

        void InitializePersistence()
        {
            if (persistenceSystem == null)
            {
                persistenceSystem = new ArgPersistenceSystem(false, PullPersistence, PushPersistence, GridTerminalSystem);
            }
            else
            {
                persistenceSystem.Clear();
            }

            if (runUpdateFrequency==null)
            {
                runUpdateFrequency = new ArgPersistenceSystem.Fields.GenericValueField<UpdateFrequency>(C_RunUpdateFrequency_Section,
                    C_RunUpdateFrequency_Name, C_RunUpdateFrequency_Comment, UpdateFrequency.Update10, UpdateFrequency.Update10,
                    Enum.TryParse, u => u.ToString("F"), actionOnGetFail: () => SetDefaultsOnFail(runUpdateFrequency),
                    actionOnGetSuccess: () => {
                        Runtime.UpdateFrequency = runUpdateFrequency.FieldValue;
                    });
            }
            else
            {
                runUpdateFrequency.SetDefaults();
            }
            persistenceSystem.AddField(runUpdateFrequency);

            if (rotorThrusterGroups == null)
            {
                rotorThrusterGroups = new RotorThrusterGroupCollectionField(C_RotorThrusterGroups_Section,
                    C_RotorThrusterGroups_Name, C_RotorThrusterGroups_Comment,
                    new List<Arg2DRotorThrusterMechanism.RotorThrusterGroup>(), new List<Arg2DRotorThrusterMechanism.RotorThrusterGroup>(),
                    actionOnGetFail: ()=> {
                        Log($"Pulling '{rotorThrusterGroups.Key}' failed.");
                        GetRotorThrusterGroupsFromGrid();
                    },
                    actionOnGetSuccess: ()=> {
                        if (mechanism!=null)
                        {
                            mechanism.ClearRotorThrusterGroups();
                            foreach (var rtg in rotorThrusterGroups.FieldValues)
                            {
                                mechanism.AddRotorThrusterGroup(rtg);
                            }
                        }
                    });
            }
            else
            {
                rotorThrusterGroups.SetDefaults();
            }
            persistenceSystem.AddField(rotorThrusterGroups);

            if (shipControllers == null)
            {
                shipControllers = new ShipControllerCollectionField(C_Controllers_Section,
                    C_Controllers_Name, C_Controllers_Comment, new List<ShipControllerWithPriority>(),
                    new List<ShipControllerWithPriority>(),
                    actionOnGetFail: ()=>
                    {
                        Log($"Pulling '{shipControllers.Key}' failed.");
                        GetShipControllersFromGrid();
                    });
                shipControllers.PullPriority = 1;
            }
            else
            {
                shipControllers.SetDefaults();
            }
            persistenceSystem.AddField(shipControllers);

            if (textPanels == null)
            {
                textPanels = new ArgPersistenceSystem.Fields.BlockCollectionField<IMyTextPanel>(C_TextPanels_Section,
                    C_TextPanels_Name, C_TextPanels_Comment, new List<IMyTextPanel>(), new List<IMyTextPanel>(),
                    actionOnGetFail: ()=>
                    {
                        Log($"Pulling '{textPanels.Key}' failed.");
                        GetTextPanelsFromGrid();
                    });
                textPanels.PullPriority = 2;
            }
            else
            {
                textPanels.SetDefaults();
            }
            persistenceSystem.AddField(textPanels);

            if (systemID == null)
            {
                systemID = new ArgPersistenceSystem.Fields.StringField(C_Arg2DRotorThrusterSystem_Section,
                    C_SystemID_Name, C_SystemID_Comment, "", "",
                    actionOnGetFail: () => SetDefaultsOnFail(systemID),
                    actionOnGetSuccess: UpdateSystemIDsOnControlledBlocks);
                systemID.PullPriority = 1;
            }
            else
            {
                systemID.SetDefaults();
            }
            persistenceSystem.AddField(systemID);

            if (thrustStrengthMultiplier == null)
            {
                thrustStrengthMultiplier = new ArgPersistenceSystem.Fields.SingleField(C_ThrustStrengthMultiplier_Section,
                    C_ThrustStrengthMultiplier_Name, C_ThrustStrengthMultiplier_Comment, 1, 1,
                    actionOnGetFail: () => SetDefaultsOnFail(thrustStrengthMultiplier),
                    actionOnGetSuccess: () => {
                        if (mechanism!=null)
                        {
                            mechanism.ThrustStrengthMultiplier = thrustStrengthMultiplier.FieldValue;
                        }
                    });
            }
            else
            {
                thrustStrengthMultiplier.SetDefaults();
            }
            persistenceSystem.AddField(thrustStrengthMultiplier);

            if (thrustStrengthMin == null)
            {
                thrustStrengthMin = new ArgPersistenceSystem.Fields.SingleField(C_ThrustStrengthMin_Section,
                    C_ThrustStrengthMin_Name, C_ThrustStrengthMin_Comment, 0.1f, 0.1f,
                    actionOnGetFail: () => SetDefaultsOnFail(thrustStrengthMin),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.ThrustStrengthMin = thrustStrengthMin.FieldValue;
                        }
                    });
            }
            else
            {
                thrustStrengthMin.SetDefaults();
            }
            persistenceSystem.AddField(thrustStrengthMin);

            if (proportionalVelocityThreshold == null)
            {
                proportionalVelocityThreshold = new ArgPersistenceSystem.Fields.SingleField(C_ProportionalVelocityThreshold_Section,
                    C_ProportionalVelocityThreshold_Name, C_ProportionalVelocityThreshold_Comment, 0, 0,
                    actionOnGetFail: () => SetDefaultsOnFail(proportionalVelocityThreshold),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.ProportionalVelocityThreshold = proportionalVelocityThreshold.FieldValue;
                        }
                    });
            }
            else
            {
                proportionalVelocityThreshold.SetDefaults();
            }
            persistenceSystem.AddField(proportionalVelocityThreshold);

            if (dampenerActivationVelocityThreshold == null)
            {
                dampenerActivationVelocityThreshold = new ArgPersistenceSystem.Fields.SingleField(C_DampenerActivationVelocityThreshold_Section,
                    C_DampenerActivationVelocityThreshold_Name, C_DampenerActivationVelocityThreshold_Comment, 5, 5,
                    actionOnGetFail: () => SetDefaultsOnFail(dampenerActivationVelocityThreshold),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.DampenerActivationVelocityThreshold = dampenerActivationVelocityThreshold.FieldValue;
                        }
                    });
            }
            else
            {
                dampenerActivationVelocityThreshold.SetDefaults();
            }
            persistenceSystem.AddField(dampenerActivationVelocityThreshold);

            if (shareInertiaTensor == null)
            {
                shareInertiaTensor = new ArgPersistenceSystem.Fields.BooleanField(C_ShareInertiaTensor_Section,
                    C_ShareInertiaTensor_Name, C_ShareInertiaTensor_Comment, false, false,
                    actionOnGetFail: () => SetDefaultsOnFail(shareInertiaTensor),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.ShareInertiaTensor = shareInertiaTensor.FieldValue;
                        }
                    });
            }
            else
            {
                shareInertiaTensor.SetDefaults();
            }
            persistenceSystem.AddField(shareInertiaTensor);

            if (forceDisableDampeners == null)
            {
                forceDisableDampeners = new ArgPersistenceSystem.Fields.BooleanField(C_ForceDisableDampeners_Section,
                    C_ForceDisableDampeners_Name, C_ForceDisableDampeners_Comment, false, false,
                    actionOnGetFail: () => SetDefaultsOnFail(forceDisableDampeners),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.ForceDisableDampeners = forceDisableDampeners.FieldValue;
                        }
                    });
            }
            else
            {
                forceDisableDampeners.SetDefaults();
            }
            persistenceSystem.AddField(forceDisableDampeners);

            if (forceEnableDampeners == null)
            {
                forceEnableDampeners = new ArgPersistenceSystem.Fields.BooleanField(C_ForceEnableDampeners_Section,
                    C_ForceEnableDampeners_Name, C_ForceEnableDampeners_Comment, false, false,
                    actionOnGetFail: () => SetDefaultsOnFail(forceEnableDampeners),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.ForceEnableDampeners = forceEnableDampeners.FieldValue;
                        }
                    });
            }
            else
            {
                forceEnableDampeners.SetDefaults();
            }
            persistenceSystem.AddField(forceEnableDampeners);

            if (rotateToDefaultWhenUnused == null)
            {
                rotateToDefaultWhenUnused = new ArgPersistenceSystem.Fields.BooleanField(C_RotateToDefaultWhenUnused_Section,
                    C_RotateToDefaultWhenUnused_Name, C_RotateToDefaultWhenUnused_Comment, false, false,
                    actionOnGetFail: () => SetDefaultsOnFail(rotateToDefaultWhenUnused),
                    actionOnGetSuccess: () => {
                        if (mechanism != null)
                        {
                            mechanism.RotateToDefaultWhenUnused = rotateToDefaultWhenUnused.FieldValue;
                        }
                    });
            }
            else
            {
                rotateToDefaultWhenUnused.SetDefaults();
            }
            persistenceSystem.AddField(rotateToDefaultWhenUnused);

            if (lastUsedShipController==null)
            {
                lastUsedShipController = new ArgPersistenceSystem.Fields.BlockField<IMyShipController>(C_LastUsedController_Section,
                    C_LastUsedController_Name, C_LastUsedController_Comment, null, null,
                    actionOnGetFail: () => lastUsedShipController.SetDefaults());
            }
            else
            {
                lastUsedShipController.SetDefaults();
            }
            persistenceSystem.AddField(lastUsedShipController);
        }

        #endregion

        #endregion

        #region Configuration

        ArgPersistenceSystem configurationSystem;

        #region Push and Pull

        string PullConfiguration()
        {
            return Me.CustomData;
        }

        void PushConfiguration(string str)
        {
            Me.CustomData = str;
        }

        #endregion

        #region Initialization

        void InitializeConfiguration()
        {
            if (configurationSystem==null)
            {
                configurationSystem = new ArgPersistenceSystem(true, PullConfiguration, PushConfiguration);
            }
            else
            {
                configurationSystem.Clear();
            }

            configurationSystem.AddField(systemID);
            configurationSystem.AddField(runUpdateFrequency);
            configurationSystem.AddField(rotateToDefaultWhenUnused);
            configurationSystem.AddField(forceEnableDampeners);
            configurationSystem.AddField(forceDisableDampeners);
            configurationSystem.AddField(shareInertiaTensor);
            configurationSystem.AddField(dampenerActivationVelocityThreshold);
            configurationSystem.AddField(proportionalVelocityThreshold);
            configurationSystem.AddField(thrustStrengthMin);
            configurationSystem.AddField(thrustStrengthMultiplier);
        }

        #endregion

        #endregion

        #region Commands

        ArgCommandParser commandParser;

        #region Initialization
        void InitializeCommands()
        {
            if (commandParser == null)
            {
                commandParser = new ArgCommandParser(C_Cmd_Help, C_Cmd_Help_Arg1, Log);
            }
            else
            {
                commandParser.Clear(true, C_Cmd_Help, C_Cmd_Help_Arg1);
            }

            commandParser.AddCommand(new ArgCommandParser.Command(C_Cmd_Start, CmdStart));
            commandParser.AddCommand(new ArgCommandParser.Command(C_Cmd_Reset, CmdReset));
            commandParser.AddCommand(new ArgCommandParser.Command(C_Cmd_AddTemplates, CmdAddTemplates, null, new string[] { C_Cmd_AddTemplates_SwitchOverwriteExistingKeys, C_Cmd_AddTemplates_SwitchOverwriteIfInvalid }));
            commandParser.AddCommand(new ArgCommandParser.Command(C_Cmd_GetBlocks, CmdGetBlocks));
            commandParser.AddCommand(new ArgCommandParser.Command(C_Cmd_ConfigureFromCustomData, CmdConfigureFromCustomData));
            commandParser.AddCommand(new ArgCommandParser.Command(C_Cmd_Set, CmdSet, new string[] { C_Cmd_Set_Arg1, C_Cmd_Set_Arg2 }));
        }
        #endregion

        #region Commands

        void CmdReset(MyCommandLine commandLine, out ArgCommandParser.CommandInvalidArgumentsException e)
        {
            Log("Resetting system...");
            e = null;
            Storage = "";
            Me.CustomData = "";
            InitializePersistence();
            InitializeConfiguration();
            persistenceSystem.Commit();
            persistenceSystem.Push();
            configurationSystem.Commit();
            configurationSystem.Push();
            ResetMechanism();
            Log("System reset.");
        }

        void CmdSet(MyCommandLine commandLine, out ArgCommandParser.CommandInvalidArgumentsException e)
        {
            e = null;
            string propertyName = commandLine.Argument(1);
            string newValue = commandLine.Argument(2);

            bool b;
            float f;
            switch (propertyName)
            {
                case C_SystemID_Name:
                    if (!string.IsNullOrWhiteSpace(newValue))
                    {
                        systemID.FieldValue = newValue;
                        Log($"'{C_SystemID_Name}' was set to '{newValue}'.");
                        configurationSystem.CommitField(systemID);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for  {C_SystemID_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_RunUpdateFrequency_Name:
                    UpdateFrequency u;
                    if (Enum.TryParse(newValue, out u))
                    {
                        runUpdateFrequency.FieldValue = u;
                        Log($"'{C_RunUpdateFrequency_Name}' was set to '{u:F}'.");
                        configurationSystem.CommitField(runUpdateFrequency);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for  {C_RunUpdateFrequency_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_RotateToDefaultWhenUnused_Name:
                    if (Boolean.TryParse(newValue, out b))
                    {
                        rotateToDefaultWhenUnused.FieldValue = b;
                        Log($"'{C_RotateToDefaultWhenUnused_Name}' was set to '{b:F}'.");
                        configurationSystem.CommitField(rotateToDefaultWhenUnused);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_RunUpdateFrequency_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_ForceEnableDampeners_Name:
                    if (Boolean.TryParse(newValue, out b))
                    {
                        forceEnableDampeners.FieldValue = b;
                        Log($"'{C_ForceEnableDampeners_Name}' was set to '{b:F}'.");
                        configurationSystem.CommitField(forceEnableDampeners);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_ForceEnableDampeners_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_ForceDisableDampeners_Name:
                    if (Boolean.TryParse(newValue, out b))
                    {
                        forceDisableDampeners.FieldValue = b;
                        Log($"'{C_ForceDisableDampeners_Name}' was set to '{b:F}'.");
                        configurationSystem.CommitField(forceDisableDampeners);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_ForceDisableDampeners_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_ShareInertiaTensor_Name:
                    if (Boolean.TryParse(newValue, out b))
                    {
                        shareInertiaTensor.FieldValue = b;
                        Log($"'{C_ShareInertiaTensor_Name}' was set to '{b:F}'.");
                        configurationSystem.CommitField(shareInertiaTensor);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_ShareInertiaTensor_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_DampenerActivationVelocityThreshold_Name:
                    if (Single.TryParse(newValue, out f))
                    {
                        dampenerActivationVelocityThreshold.FieldValue = f;
                        Log($"'{C_DampenerActivationVelocityThreshold_Name}' was set to '{f}'.");
                        configurationSystem.CommitField(dampenerActivationVelocityThreshold);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_DampenerActivationVelocityThreshold_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_ProportionalVelocityThreshold_Name:
                    if (Single.TryParse(newValue, out f))
                    {
                        proportionalVelocityThreshold.FieldValue = f;
                        Log($"'{C_ProportionalVelocityThreshold_Name}' was set to '{f}'.");
                        configurationSystem.CommitField(proportionalVelocityThreshold);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_ProportionalVelocityThreshold_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_ThrustStrengthMin_Name:
                    if (Single.TryParse(newValue, out f))
                    {
                        thrustStrengthMin.FieldValue = f;
                        Log($"'{C_ThrustStrengthMin_Name}' was set to '{f}'.");
                        configurationSystem.CommitField(thrustStrengthMin);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_ThrustStrengthMin_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                case C_ThrustStrengthMultiplier_Name:
                    if (Single.TryParse(newValue, out f))
                    {
                        thrustStrengthMultiplier.FieldValue = f;
                        Log($"'{C_ThrustStrengthMultiplier_Name}' was set to '{f}'.");
                        configurationSystem.CommitField(thrustStrengthMultiplier);
                    }
                    else
                    {
                        string errMsg = $"'{newValue}' is not a valid value for {C_ThrustStrengthMultiplier_Name}!";
                        Log(errMsg);
                        e = new ArgCommandParser.CommandInvalidArgumentsException(errMsg);
                    }
                    break;
                default:
                    e = new ArgCommandParser.CommandInvalidArgumentsException($"Property with name '{propertyName}' does not exist!");
                    break;
            }

            if (e == null)
            {
                configurationSystem.Push();
            }
        }

        void CmdStart(MyCommandLine commandLine, out ArgCommandParser.CommandInvalidArgumentsException e)
        {
            e = null;
            Runtime.UpdateFrequency &= runUpdateFrequency.FieldValue;
            Log($"Update frequency was set to '{runUpdateFrequency.FieldValue:F}'.");
        }

        void CmdConfigureFromCustomData(MyCommandLine commandLine, out ArgCommandParser.CommandInvalidArgumentsException e)
        {
            e = null;
            Exception ee;
            Log("Configuring from custom data...");
            configurationSystem.Pull(out ee);
            configurationSystem.Commit();
            configurationSystem.Push();
            Log("Configured from custom data.");
        }

        void CmdGetBlocks(MyCommandLine commandLine, out ArgCommandParser.CommandInvalidArgumentsException e)
        {
            e = null;
            Log("Getting blocks from grid...");

            GetTextPanelsFromGrid();
            persistenceSystem.CommitField(textPanels);
            Log($"Found {textPanels.FieldValues.Count} text panels.");

            GetShipControllersFromGrid();
            persistenceSystem.CommitField(shipControllers);
            Log($"Found {shipControllers.FieldValues.Count} ship controllers.");

            GetRotorThrusterGroupsFromGrid();
            persistenceSystem.CommitField(rotorThrusterGroups);
            Log($"Found {rotorThrusterGroups.FieldValues.Count} rotor thruster groups.");

            ResetMechanism();

            Log("Blocks retrieved.");
        }

        void CmdAddTemplates(MyCommandLine commandLine, out ArgCommandParser.CommandInvalidArgumentsException e)
        {
            e = null;
            Log($"Adding templates to blocks with '{C_BlockConfig_TemplateRequest_Section}' section in their custom data...");
            AddTemplatesWhereRequested(commandLine.Switch(C_Cmd_AddTemplates_SwitchOverwriteExistingKeys), commandLine.Switch(C_Cmd_AddTemplates_SwitchOverwriteIfInvalid));
        }

        #endregion

        #endregion

        #region Private fields
        Arg2DRotorThrusterMechanism mechanism;
        #endregion

        public Program()
        {
            Initialize();
            if (!string.IsNullOrWhiteSpace(Storage))
            {
                persistenceSystem.Pull(false);
            }
            if (string.IsNullOrWhiteSpace(Me.CustomData))
            {
                configurationSystem.Commit();
                configurationSystem.Push();
            }
            ResetMechanism();
            Runtime.UpdateFrequency |= runUpdateFrequency.FieldValue;
        }

        public void Save()
        {
            persistenceSystem.Commit();
            persistenceSystem.Push();
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
            
            if (string.IsNullOrWhiteSpace(systemID.FieldValue))
            {
                // Stop if the system has no valid ID.
                Runtime.UpdateFrequency &= ~runUpdateFrequency.FieldValue;
                Log($"Invalid {C_SystemID_Name} detected: '{systemID.FieldValue}'! Shutting down system...");
                return;
            }

            if (shipControllers.FieldValues.Count <=0)
            {
                Runtime.UpdateFrequency &= ~runUpdateFrequency.FieldValue;
                Log($"There are no ship controllers present in the system! Shutting down system...");
                return;
            }

            // Select highest priority ship controller with movement indicator
            var shipControllersWithIndicators = shipControllers.FieldValues.Where(c => !Vector3.IsZero(c.ShipController.MoveIndicator));
            IMyShipController shipController = null;
            if (shipControllersWithIndicators.Count()>0)
            {
                shipController = shipControllersWithIndicators.MaxBy(c => c.Priority).ShipController;
            }
            if (shipController==null)
            {
                if (lastUsedShipController.FieldValue==null)
                {
                    lastUsedShipController.FieldValue = shipControllers.FieldValues.MaxBy(c => c.Priority).ShipController;
                }
            }
            else
            {
                lastUsedShipController.FieldValue = shipController;
            }
            mechanism.Thrust(lastUsedShipController.FieldValue);
        }

        void Initialize()
        {
            InitializeReusables();
            InitializeTemplates();
            InitializeCommands();
            InitializePersistence();
            InitializeConfiguration();
        }

        void ResetMechanism()
        {
            if (mechanism==null)
            {
                mechanism = new Arg2DRotorThrusterMechanism(rotorThrusterGroups.FieldValues, dampenerActivationVelocityThreshold.FieldValue, proportionalVelocityThreshold.FieldValue, thrustStrengthMin.FieldValue, thrustStrengthMultiplier.FieldValue, rotateToDefaultWhenUnused.FieldValue, shareInertiaTensor.FieldValue);
            }
            else
            {
                mechanism.ClearRotorThrusterGroups();
                mechanism.DampenerActivationVelocityThreshold = dampenerActivationVelocityThreshold.FieldValue;
                mechanism.ProportionalVelocityThreshold = proportionalVelocityThreshold.FieldValue;
                mechanism.ThrustStrengthMin = thrustStrengthMin.FieldValue;
                mechanism.ThrustStrengthMultiplier = thrustStrengthMultiplier.FieldValue;
                mechanism.RotateToDefaultWhenUnused = rotateToDefaultWhenUnused.FieldValue;
                mechanism.ShareInertiaTensor = shareInertiaTensor.FieldValue;
                foreach (var rtg in rotorThrusterGroups.FieldValues)
                {
                    mechanism.AddRotorThrusterGroup(rtg);
                }
            }
            
        }

        void Log(string message)
        {
            Echo(message.TrimEnd('\n'));
            foreach (var tp in textPanels.FieldValues)
            {
                tp.WriteText(message+"\n", true);
            }
        }

        void UpdateSystemIDsOnControlledBlocks()
        {
            #region Updating ID on text panels
            foreach (var block in textPanels.FieldValues)
            {
                if (reusableIni.TryParse(block.CustomData))
                {
                    systemID.SetToIni(reusableIni, true);
                    block.CustomData = reusableIni.ToString();
                    Log($"Updated '{C_SystemID_Name}' on '{block.CustomName}'.");
                }
                else
                {
                    Log($"Could not update '{C_SystemID_Name}' on '{block.CustomName}'.");
                }
            }
            #endregion

            #region Updating ID on RTGs
            foreach (var rtg in rotorThrusterGroups.FieldValues)
            {
                if (reusableIni.TryParse(rtg.Stator.CustomData))
                {
                    systemID.SetToIni(reusableIni, true);
                    rtg.Stator.CustomData = reusableIni.ToString();
                    Log($"Updated '{C_SystemID_Name}' on '{rtg.Stator.CustomName}'.");
                }
                else
                {
                    Log($"Could not update '{C_SystemID_Name}' on '{rtg.Stator.CustomName}'.");
                }
                foreach (var block in rtg.Thrusters)
                {
                    if (reusableIni.TryParse(block.CustomData))
                    {
                        systemID.SetToIni(reusableIni, true);
                        block.CustomData = reusableIni.ToString();
                        Log($"Updated '{C_SystemID_Name}' on '{block.CustomName}'.");
                    }
                    else
                    {
                        Log($"Could not update '{C_SystemID_Name}' on '{block.CustomName}'.");
                    }
                }
            }
            #endregion

            #region Updating ID on ship controllers
            foreach (var shipController in shipControllers.FieldValues)
            {
                var block = shipController.ShipController;
                if (reusableIni.TryParse(block.CustomData))
                {
                    systemID.SetToIni(reusableIni, true);
                    block.CustomData = reusableIni.ToString();
                    Log($"Updated '{C_SystemID_Name}' on '{block.CustomName}'.");
                }
                else
                {
                    Log($"Could not update '{C_SystemID_Name}' on '{block.CustomName}'.");
                }
            }
            #endregion

            #region Updating ID on Me
            if (reusableIni.TryParse(Me.CustomData))
            {
                systemID.SetToIni(reusableIni, true);
                Me.CustomData = reusableIni.ToString();
                Log($"Updated '{C_SystemID_Name}' on '{Me.CustomName}'.");
            }
            else
            {
                Log($"Could not update '{C_SystemID_Name}' on '{Me.CustomName}'.");
            }
            #endregion

        }

        #region Getting blocks

        void GetBlocksOfTypeWithSameSystemIDFromGrid<T>(List<T> list) where T : class, IMyTerminalBlock
        {
            list.Clear();
            GridTerminalSystem.GetBlocksOfType(list, b => {
                string systemID = "";
                bool collect =
                    MyIni.HasSection(b.CustomData, C_Arg2DRotorThrusterSystem_Section) &&
                    reusableIni.TryParse(b.CustomData) && reusableIni.Get(C_Arg2DRotorThrusterSystem_Section, C_SystemID_Name).TryGetString(out systemID) &&
                    systemID == this.systemID.FieldValue;
                return collect;
            });
        }

        void GetShipControllersFromGrid()
        {
            GetBlocksOfTypeWithSameSystemIDFromGrid(reusableShipControllers);
            shipControllers.SetDefaults();

            int priority;
            foreach (var controller in reusableShipControllers)
            {
                if (reusableIni.TryParse(controller.CustomData) && reusableIni.Get(C_BlockConfig_Controller_Priority_Section, C_BlockConfig_Controller_Priority_Name).TryGetInt32(out priority))
                {
                    shipControllers.FieldValues.Add(new ShipControllerWithPriority(controller, priority));
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
            rotorThrusterGroups.SetDefaults();

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
                    reusableIni.Get(C_BlockConfig_RotorID_Section, C_BlockConfig_RotorID_Name).TryGetString(out rotorID) &&
                    !string.IsNullOrWhiteSpace(rotorID) &&
                    reusableIni.Get(C_BlockConfig_RotorHeading_Section, C_BlockConfig_RotorHeading_0_Name).TryGetString(out s) &&
                    Enum.TryParse(s, out h0) &&
                    reusableIni.Get(C_BlockConfig_RotorHeading_Section, C_BlockConfig_RotorHeading_90_Name).TryGetString(out s) &&
                    Enum.TryParse(s, out h90) &&
                    reusableIni.Get(C_BlockConfig_RotorDefaultAngle_Section, C_BlockConfig_RotorDefaultAngle_Name).TryGetSingle(out defaultAngle)
                )
                {
                    var rtg = new Arg2DRotorThrusterMechanism.RotorThrusterGroup(rotor, new List<IMyThrust>(), defaultAngle, h0, h90);
                    rtg.Thrusters.AddRange(reusableThrusters.Where(t => reusableIni.TryParse(t.CustomData) && reusableIni.Get(C_BlockConfig_RotorID_Section, C_BlockConfig_RotorID_Name).TryGetString(out s) && s == rotorID));
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
