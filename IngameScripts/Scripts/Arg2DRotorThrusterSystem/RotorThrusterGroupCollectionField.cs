using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {

        /// <summary>
        /// A  field capable of holding a <see cref="Arg2DRotorThrusterMechanism.RotorThrusterGroup"/>.
        /// </summary>
        class RotorThrusterGroupCollectionField : ArgPersistenceSystem.Fields.BlockCollectionField, ArgPersistenceSystem.Fields.ICollectionField<Arg2DRotorThrusterMechanism.RotorThrusterGroup>
        {
            private string separator;
            /// <summary>
            /// The separator used to separate the property values of <see cref="Arg2DRotorThrusterMechanism.RotorThrusterGroup"/> from each other.
            /// </summary>
            public string Separator
            {
                get
                {
                    return separator;
                }
                set
                {
                    if (string.IsNullOrEmpty(value) || value == Delimiter || value==PropertyDelimiter)
                    {
                        throw new Exception("Separator can not be null or empty and can not be equal to the delimiter or property delimiter.");
                    }
                    separator = value;
                }
            }


            private string propertyDelimiter;
            /// <summary>
            /// The delimiter used to separate items of list properties.
            /// </summary>
            private string PropertyDelimiter
            {
                get
                {
                    return propertyDelimiter;
                }
                set
                {
                    if (string.IsNullOrEmpty(value) || value == Delimiter || value == Separator)
                    {
                        throw new Exception("Separator can not be null or empty and can not be equal to the delimiter or separator.");
                    }
                    propertyDelimiter = value;
                }
            }

            public override string Delimiter
            {
                get
                {
                    return delimiter;
                }
                set
                {
                    if (string.IsNullOrEmpty(value) || value == Separator || value == PropertyDelimiter)
                    {
                        throw new Exception("Delimiter can not be null or empty and can not be equal to the separator or property delimiter.");
                    }
                    delimiter = value;
                }
            }

            public override Type TypeName => typeof(List<Arg2DRotorThrusterMechanism.RotorThrusterGroup>);

            List<Arg2DRotorThrusterMechanism.RotorThrusterGroup> fieldValues;
            public List<Arg2DRotorThrusterMechanism.RotorThrusterGroup> FieldValues
            {
                get
                {
                    return fieldValues;
                }

                set
                {
                    if (value == null)
                    {
                        throw new Exception("Field values can not be null!");
                    }
                    fieldValues = value;
                }
            }

            private List<Arg2DRotorThrusterMechanism.RotorThrusterGroup> fieldDefaultValues;

            public List<Arg2DRotorThrusterMechanism.RotorThrusterGroup> FieldDefaultValues
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
                FieldDefaultValues.Clear();
                FieldValues.AddRange(FieldDefaultValues);
            }

            private string RTGToString(Arg2DRotorThrusterMechanism.RotorThrusterGroup rtg)
            {
                string r = "";
                r += $"{rtg.DefaultAngle}{Separator}{rtg.HeadingAt0:F}{Separator}{rtg.HeadingAt90:F}{Separator}{rtg.Stator.EntityId}{Separator}";
                if (rtg.Thrusters.Count>0)
                {
                    r += $"{rtg.Thrusters[0].EntityId}";
                }
                for (int i = 1; i < rtg.Thrusters.Count; i++)
                {
                    r += $"{PropertyDelimiter}{rtg.Thrusters[i].EntityId}";
                }
                return r;
            }

            private bool TryParseStringToRTG(string s, IMyGridTerminalSystem gridTerminalSystem, out Arg2DRotorThrusterMechanism.RotorThrusterGroup rtg)
            {
                bool success = true;

                var separators = new string[] { Separator };
                var propertyDelimiters = new string[] { PropertyDelimiter };
                string[] structParts=s.Split(separators,StringSplitOptions.RemoveEmptyEntries);
                string[] propertyParts;
                float f=0;
                long l=0;
                Base6Directions.Direction headingAt0=default(Base6Directions.Direction);
                Base6Directions.Direction headingAt90 = default(Base6Directions.Direction);

                success &= structParts.Length >= 5 &&
                    float.TryParse(structParts[0], out f) &&
                    Enum.TryParse(structParts[1], out headingAt0) &&
                    Enum.TryParse(structParts[2], out headingAt90) &&
                    long.TryParse(structParts[3], out l);

                rtg = new Arg2DRotorThrusterMechanism.RotorThrusterGroup();

                if (success)
                {
                    IMyMotorStator stator = gridTerminalSystem.GetBlockWithId(l) as IMyMotorStator;
                    success &= stator != null;
                    if (success)
                    {
                        rtg.Stator = stator;
                        rtg.HeadingAt0 = headingAt0;
                        rtg.HeadingAt90 = headingAt90;
                        rtg.DefaultAngle = f;
                        rtg.Thrusters = new List<IMyThrust>();
                        propertyParts = structParts[4].Split(propertyDelimiters, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < propertyParts.Length && success; i++)
                        {
                            success &= long.TryParse(propertyParts[i], out l);
                            if (success)
                            {
                                IMyThrust thruster = gridTerminalSystem.GetBlockWithId(l) as IMyThrust;
                                success &= thruster != null;
                                if (success)
                                {
                                    rtg.Thrusters.Add(thruster);
                                }
                            }
                        }
                    }
                }

                if (success)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }

            public override void SetToIni(MyIni ini, bool setComment = true)
            {
                string v = "";
                if (FieldValues.Count>0)
                {
                    v += $"{RTGToString(FieldValues[0])}";
                }
                for (int i = 1; i < FieldValues.Count; i++)
                {
                    v += $"{Delimiter}{FieldValues[i]}";
                }
                ini.Set(Key, v);
                if (setComment)
                {
                    ini.SetComment(Key, FieldComment);
                }
            }

            public override bool TryGetFromIni(MyIni ini, IMyGridTerminalSystem gridTerminalSystem)
            {
                string s;
                FieldValues.Clear();
                bool success = true;
                success &= ini.Get(Key).TryGetString(out s);

                if (success)
                {
                    string[] structStrings = s.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < structStrings.Length && success; i++)
                    {
                        Arg2DRotorThrusterMechanism.RotorThrusterGroup rtg;
                        success &= TryParseStringToRTG(structStrings[i], gridTerminalSystem, out rtg);
                        if (success)
                        {
                            FieldValues.Add(rtg);
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
            /// Instantiates a field capable of holding a <see cref="Arg2DRotorThrusterMechanism.RotorThrusterGroup"/>.
            /// </summary>
            /// <param name="fieldSection">The section this field belongs to.</param>
            /// <param name="fieldName">The name of this field.</param>
            /// <param name="fieldComment">The comment for this field.</param>
            /// <param name="fieldValues"><para>The values of this field.</para><para>This value can not be null!</para></param>
            /// <param name="fieldDefaultValues"><para>The default values of this field.</para><para>This value can not be null!</para></param>
            /// <param name="propertyDelimiter">The delimiter used to separate items of list properties.</param>
            /// <param name="separator">The separator used to separate the property values of <see cref="Arg2DRotorThrusterMechanism.RotorThrusterGroup"/> from each other.</param>
            /// <param name="delimiter">The delimeter used to separate the string equivalents of the values in the list.</param>
            /// <param name="actionOnGetFail">This action will be called when the reading of this field fails.</param>
            /// <param name="actionOnGetSuccess">This action will be called when the reading of this field succeeds.</param>
            public RotorThrusterGroupCollectionField(string fieldSection, string fieldName, string fieldComment, List<Arg2DRotorThrusterMechanism.RotorThrusterGroup> fieldValues, List<Arg2DRotorThrusterMechanism.RotorThrusterGroup> fieldDefaultValues, string propertyDelimiter=":" ,string separator = ",", string delimiter = ";", Action actionOnGetFail = null, Action actionOnGetSuccess = null)
                : base(fieldSection, fieldName, fieldComment, delimiter, actionOnGetFail, actionOnGetSuccess)
            {
                FieldValues = fieldValues;
                FieldDefaultValues = fieldDefaultValues;
                Separator = separator;
                PropertyDelimiter = propertyDelimiter;
            }
        }
    }
}
