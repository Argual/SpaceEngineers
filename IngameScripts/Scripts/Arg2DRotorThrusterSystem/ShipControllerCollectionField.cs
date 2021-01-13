using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program
    {
        struct ShipControllerWithPriority
        {
            public IMyShipController ShipController { get; set; }
            public int Priority { get; set; }
            public ShipControllerWithPriority(IMyShipController shipController, int priority = 0)
            {
                ShipController = shipController;
                Priority = priority;
            }
        }

        /// <summary>
        /// A  field capable of holding a <see cref="IMyShipController"/> and its priority.
        /// </summary>
        class ShipControllerCollectionField : ArgPersistenceSystem.Fields.BlockCollectionField, ArgPersistenceSystem.Fields.ICollectionField<ShipControllerWithPriority>
        {
            private string separator;
            /// <summary>
            /// The separator used to separate the entity ID and the priority.
            /// </summary>
            public string Separator {
                get
                {
                    return separator;
                }
                set
                {
                    if (string.IsNullOrEmpty(value) || value==Delimiter)
                    {
                        throw new Exception("Separator can not be null or empty and can not be equal to the delimiter.");
                    }
                    separator = value;
                }
            }

            public override string Delimiter {
                get
                {
                    return delimiter;
                }
                set
                {
                    if (string.IsNullOrEmpty(value) || value == Separator)
                    {
                        throw new Exception("Delimiter can not be null or empty and can not be equal to the separator.");
                    }
                    delimiter = value;
                }
            }

            public override Type TypeName => typeof(List<ShipControllerWithPriority>);

            private List<ShipControllerWithPriority> fieldValues;
            public List<ShipControllerWithPriority> FieldValues
            {
                get
                {
                    return fieldValues;
                }

                set
                {
                    if (value==null)
                    {
                        throw new Exception("Field values can not be null!");
                    }
                    fieldValues = value;
                }
            }

            private List<ShipControllerWithPriority> fieldDefaultValues;
            public List<ShipControllerWithPriority> FieldDefaultValues
            {
                get
                {
                    return fieldDefaultValues;
                }

                set
                {
                    if (value == null)
                    {
                        throw new Exception("Field default values can not be null!");
                    }
                    fieldDefaultValues = value;
                }
            }

            public override void SetDefaults()
            {
                FieldValues.Clear();
                FieldValues.AddRange(FieldDefaultValues);
            }

            public override void SetToIni(MyIni ini, bool setComment = true)
            {
                string v = "";
                if (FieldValues.Count>0)
                {
                    v = $"{FieldValues[0].ShipController.EntityId}{Separator}{FieldValues[0].Priority}";
                }
                for (int i = 1; i < FieldValues.Count; i++)
                {
                    v += $"{Delimiter}{FieldValues[0].ShipController.EntityId}{Separator}{FieldValues[0].Priority}";
                }
                ini.Set(Key, v);
                if (setComment)
                {
                    ini.SetComment(Key, FieldComment);
                }
            }

            public override bool TryGetFromIni(MyIni ini, IMyGridTerminalSystem gridTerminalSystem)
            {
                FieldValues.Clear();

                string s;
                int integer=0;
                long l=0;
                bool success = true;
                if (ini.Get(Key).TryGetString(out s))
                {
                    var pairStrings = s.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    string[] pairParts;
                    string[] separators = new string[] { Separator };
                    for (int i = 0; i < pairStrings.Length && success; i++)
                    {
                        pairParts = pairStrings[i].Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        success &= pairParts.Length >= 2 && long.TryParse(pairParts[0],out l) && int.TryParse(pairParts[1],out integer);
                        if (success)
                        {
                            IMyShipController c;
                            c =gridTerminalSystem.GetBlockWithId(l) as IMyShipController;
                            success &= c != null;
                            if (success)
                            {
                                FieldValues.Add(new ShipControllerWithPriority(c, integer));
                            }
                        }
                    }
                }
                if (success)
                {
                    ActionOnGetSuccess?.Invoke();
                    return true;
                }
                else
                {
                    SetDefaults();
                    ActionOnGetFail?.Invoke();
                    return false;
                }
            }

            /// <summary>
            /// Instantiates a field capable of holding a <see cref="IMyShipController"/> and its priority.
            /// </summary>
            /// <param name="fieldSection">The section this field belongs to.</param>
            /// <param name="fieldName">The name of this field.</param>
            /// <param name="fieldComment">The comment for this field.</param>
            /// <param name="fieldValues"><para>The values of this field.</para><para>This value can not be null!</para></param>
            /// <param name="fieldDefaultValues"><para>The default values of this field.</para><para>This value can not be null!</para></param>
            /// <param name="separator">The separator used to separate the entity ID and the priority.</param>
            /// <param name="delimiter">The delimeter used to separate the string equivalents of the values in the list.</param>
            /// <param name="actionOnGetFail">This action will be called when the reading of this field fails.</param>
            /// <param name="actionOnGetSuccess">This action will be called when the reading of this field succeeds.</param>
            public ShipControllerCollectionField(string fieldSection, string fieldName, string fieldComment, List<ShipControllerWithPriority> fieldValues, List<ShipControllerWithPriority> fieldDefaultValues, string separator=",", string delimiter=";", Action actionOnGetFail=null, Action actionOnGetSuccess=null)
                : base(fieldSection, fieldName, fieldComment, delimiter, actionOnGetFail, actionOnGetSuccess)
            {
                FieldValues = fieldValues;
                FieldDefaultValues = fieldDefaultValues;
                Separator = separator;
            }
        }
    }
}
