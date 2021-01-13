using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// Provides easy string based save-load managing an internal ini.
        /// <para>Intended to be used as a storage manager for configuration fields, values, blocks. Can save and retrieve data from anything able to store (long) strings.</para>
        /// </summary>
        public class ArgPersistenceSystem
        {
            #region Subclasses

            public static class Fields
            {
                #region Abstract

                /// <summary>
                /// A field.
                /// </summary>
                public abstract class Field
                {
                    #region Private fields
                    private string fieldName;
                    private string fieldSection;
                    private string fieldComment;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The name of the field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null or white-space!
                    /// </remarks>
                    public string FieldName
                    {
                        get
                        {
                            return fieldName;
                        }
                        private set
                        {
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw new Exception("Field name can not be null or white-space!");
                            }
                            fieldName = value;
                        }
                    }

                    /// <summary>
                    /// The name of the section this field belongs to.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null or white-space!
                    /// </remarks>
                    public string FieldSection
                    {
                        get
                        {
                            return fieldSection;
                        }
                        private set
                        {
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw new Exception("Field section can not be null or white-space!");
                            }
                            fieldSection = value;
                        }
                    }

                    /// <summary>
                    /// The comment for this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public string FieldComment
                    {
                        get
                        {
                            return fieldComment;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field comment can not be null!");
                            }
                            fieldComment = value;
                        }
                    }

                    /// <summary>
                    /// This method will be called if the reading of this field is succesful.
                    /// </summary>
                    public Action ActionOnReadSuccess { get; set; }

                    /// <summary>
                    /// This method will be called if the reading of this field fails.
                    /// </summary>
                    public Action ActionOnReadFail { get; set; }

                    /// <summary>
                    /// The key for the field.
                    /// </summary>
                    public MyIniKey Key
                    {
                        get { return new MyIniKey(FieldSection, FieldName); }
                    }

                    /// <summary>
                    /// <para>The type of this field's value.</para>
                    /// <para>If this is a field capable of holding multiple values, the type returned will be a '<see cref="List{T}"/>' instead of '<see cref="T"/>'!</para>
                    /// </summary>
                    public abstract Type TypeName { get; }

                    /// <summary>
                    /// Fields with the highest pull priority will be pulled first, fields with lowest last.
                    /// <para><see cref="Fields.ValueField"/>s will always be pulled before <see cref="Fields.BlockField"/>s.</para>
                    /// </summary>
                    public int PullPriority { get; set; }
                    #endregion

                    #region Constructors
                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Field(string fieldSection, string fieldName, string fieldComment, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                    {
                        FieldSection = fieldSection;
                        FieldName = fieldName;
                        FieldComment = fieldComment;
                        ActionOnReadFail = actionOnReadFail;
                        ActionOnReadSuccess = actionOnReadSuccess;
                        PullPriority = 0;
                    }
                    #endregion

                    #region Delegates

                    /// <summary>
                    /// A parser function trying to parse T1 to T2.
                    /// </summary>
                    /// <typeparam name="T1">The type to parse from.</typeparam>
                    /// <typeparam name="T2">The type to parse to.</typeparam>
                    /// <param name="from">The variable with the value to parse from.</param>
                    /// <param name="to">The variable which will get the parsed value.</param>
                    /// <returns>Whether or not parsing was succesful.</returns>
                    public delegate TResult Parser<T1, T2, TResult>(T1 from, out T2 to);

                    #endregion

                    #region Public methods

                    /// <summary>
                    /// Sets the field's values to an ini.
                    /// </summary>
                    /// <param name="setComment">Whether or not to also set the comment.</param>
                    public abstract void SetToIni(MyIni ini, bool setComment = true);

                    /// <summary>
                    /// Sets values to default.
                    /// </summary>
                    public abstract void SetDefaults();

                    #endregion
                }


                /// <summary>
                /// Important: Setting and getting of blocks is done via 'EntityID'. ID changes between setting and getting can lead to unwanted results. IDs can change when merging/unmerging grids, pasting grids, etc.
                /// </summary>
                public abstract class BlockField : Field
                {
                    /// <summary>
                    /// Tries to get the value of this field from the given ini and returns whether or not it was succesful.
                    /// </summary>
                    /// <remarks>
                    /// Important: Setting and getting of blocks is done via 'EntityID'. ID changes between setting and getting can lead to unwanted results. IDs can change when merging/unmerging grids, pasting grids, etc.
                    /// </remarks>
                    public abstract bool TryGetFromIni(MyIni ini, IMyGridTerminalSystem gridTerminalSystem);

                    #region Constructors

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public BlockField(string fieldSection, string fieldName, string fieldComment, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess) { }

                    #endregion
                }

                public interface IField<T>
                {
                    /// <summary>
                    /// The name of the field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null or white-space!
                    /// </remarks>
                    string FieldName { get; }

                    /// <summary>
                    /// The name of the section this field belongs to.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null or white-space!
                    /// </remarks>
                    string FieldSection { get; }

                    /// <summary>
                    /// The comment for this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    string FieldComment { get; }

                    /// <summary>
                    /// The value of this field.
                    /// </summary>
                    T FieldValue { get; set; }

                    /// <summary>
                    /// The default value of this field.
                    /// </summary>
                    T FieldDefaultValue { get; set; }

                }

                public interface ICollectionField<T>
                {
                    /// <summary>
                    /// <para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para>
                    /// <para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para>
                    /// </summary>
                    string Delimiter { get; set; }

                    /// <summary>
                    /// The name of the field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null or white-space!
                    /// </remarks>
                    string FieldName { get; }

                    /// <summary>
                    /// The name of the section this field belongs to.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null or white-space!
                    /// </remarks>
                    string FieldSection { get; }

                    /// <summary>
                    /// The comment for this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    string FieldComment { get; }

                    /// <summary>
                    /// The values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    List<T> FieldValues { get; set; }

                    /// <summary>
                    /// The default values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    List<T> FieldDefaultValues { get; set; }

                }

                /// <summary>
                /// <inheritdoc/>
                /// </summary>
                public abstract class BlockCollectionField : BlockField
                {
                    #region Private fields
                    protected string delimiter;
                    #endregion

                    #region Public properties
                    /// <summary>
                    /// <para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para>
                    /// <para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para>
                    /// </summary>
                    public virtual string Delimiter
                    {
                        get
                        {
                            return delimiter;
                        }
                        set
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                throw new Exception("Delimiter can not be null or empty!");
                            }
                            delimiter = value;
                        }
                    }

                    #endregion

                    #region Constructors
                    /// <summary>
                    /// Instantiates a collection field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public BlockCollectionField(string fieldSection, string fieldName, string fieldComment, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess)
                    {
                        Delimiter = delimiter;
                    }
                    #endregion
                }

                /// <summary>
                /// <inheritdoc/>
                /// </summary>
                public abstract class ValueField : Field
                {
                    /// <summary>
                    /// Tries to get the value(s) of the field from an ini.
                    /// </summary>
                    public abstract bool TryGetFromIni(MyIni ini);

                    #region Constructors
                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public ValueField(string fieldSection, string fieldName, string fieldComment, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess) { }
                    #endregion
                }

                /// <summary>
                /// <inheritdoc/>
                /// </summary>
                public abstract class ValueCollectionField : ValueField
                {

                    #region Private fields
                    protected string delimiter;
                    #endregion

                    #region Public properties
                    /// <summary>
                    /// <para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para>
                    /// <para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para>
                    /// </summary>
                    public virtual string Delimiter
                    {
                        get
                        {
                            return delimiter;
                        }
                        set
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                throw new Exception("Delimiter can not be null or empty!");
                            }
                            delimiter = value;
                        }
                    }

                    #endregion

                    #region Constructors
                    /// <summary>
                    /// Instantiates a collection field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public ValueCollectionField(string fieldSection, string fieldName, string fieldComment, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess)
                    {
                        Delimiter = delimiter;
                    }
                    #endregion

                }

                #endregion

                /// <summary>
                /// <para>Represents a field.</para>
                /// <para>To store multiple values of the same type, use '<see cref="ArgPersistenceSystem.GenericCollectionField{T}"/>' instead.</para>
                /// </summary>
                /// <typeparam name="T">The type of the value of the field.</typeparam>
                /// <remarks>
                /// <para>If 'T' is type string, consider using '<see cref="ArgPersistenceSystem.Fields.StringField"/>' instead. It provides the same functionalities and is more performance-friendly.</para>
                /// </remarks>
                public class GenericValueField<T> : ValueField, IField<T>
                {
                    #region Private fields
                    private Parser<string, T, bool> fromStringParser;
                    private Func<T, string> toStringConverter;
                    private T fieldValue;
                    private T fieldDefaultValue;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The value of this field.
                    /// </summary>
                    public T FieldValue
                    {
                        get
                        {
                            return fieldValue;
                        }
                        set
                        {
                            fieldValue = value;
                        }
                    }

                    /// <summary>
                    /// The default value of this field.
                    /// </summary>
                    public T FieldDefaultValue
                    {
                        get
                        {
                            return fieldDefaultValue;
                        }
                        set
                        {
                            fieldDefaultValue = value;
                        }
                    }

                    /// <summary>
                    /// The function used to convert a string to the type of the value.
                    /// </summary>
                    public Parser<string, T, bool> FromStringParser
                    {
                        get
                        {
                            return fromStringParser;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Parser (from string) can not be null!");
                            }
                            fromStringParser = value;
                        }
                    }

                    /// <summary>
                    /// The function used to convert the value of this field to string.
                    /// </summary>
                    public Func<T, string> ToStringConverter
                    {
                        get
                        {
                            return toStringConverter;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Parser (to string) can not be null!");
                            }
                            toStringConverter = value;
                        }
                    }

                    /// <summary>
                    /// <inheritdoc/>
                    /// </summary>
                    public override Type TypeName
                    {
                        get
                        {
                            return typeof(T);
                        }
                    }
                    #endregion

                    #region Constructors

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="fromStringParser">The function used to parse a string to the type of the value.</param>
                    /// <param name="toStringConverter">The function used to parse the value of this field to string.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public GenericValueField(string fieldSection, string fieldName, string fieldComment, T fieldValue, T fieldDefaultValue, Parser<string, T, bool> fromStringParser, Func<T, string> toStringConverter, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess)
                    {
                        FieldValue = fieldValue;
                        FieldDefaultValue = fieldDefaultValue;
                        FromStringParser = fromStringParser;
                        ToStringConverter = toStringConverter;
                    }

                    #endregion

                    #region Public methods

                    /// <summary>
                    /// <para>Tries to get the value of this field from the given ini and returns whether or not it was succesful.</para>
                    /// <para>The field keeps its original value, if this fails.</para>
                    /// </summary>
                    /// <remarks>
                    /// This will invoke '<see cref="Field.ActionOnReadSuccess"/>' or '<see cref="Field.ActionOnReadFail"/>' depending on success.
                    /// </remarks>
                    public override bool TryGetFromIni(MyIni ini)
                    {
                        string s;
                        T value;
                        if (ini.Get(Key).TryGetString(out s) && FromStringParser(s, out value))
                        {
                            FieldValue = value;
                            ActionOnReadSuccess?.Invoke();
                            return true;
                        }
                        else
                        {
                            ActionOnReadFail?.Invoke();
                            return false;
                        }
                    }

                    /// <summary>
                    /// Sets this field's value to the given ini.
                    /// </summary>
                    /// <param name="setComment">Whether or not to also set the comment.</param>
                    public override void SetToIni(MyIni ini, bool setComment = true)
                    {
                        if (FieldValue == null)
                        {
                            ini.Set(Key, "");
                        }
                        else
                        {
                            ini.Set(Key, ToStringConverter(FieldValue));
                        }
                        if (setComment && !string.IsNullOrWhiteSpace(FieldComment))
                        {
                            ini.SetComment(Key, FieldComment);
                        }
                    }

                    /// <summary>
                    /// Resets the field's value to its default value.
                    /// </summary>
                    public override void SetDefaults()
                    {
                        FieldValue = FieldDefaultValue;
                    }

                    #endregion

                }

                /// <summary>
                /// Represents a field capable of holding multiple values of the same type.
                /// </summary>
                /// <typeparam name="T">The type of the value of the field.</typeparam>
                /// <remarks>
                /// <para>If 'T' is type string, consider using '<see cref="ArgPersistenceSystem.Fields.StringCollectionField"/>' instead. It provides the same functionalities and is more performance-friendly.</para>
                /// </remarks>
                public class GenericValueCollectionField<T> : ValueCollectionField, ICollectionField<T>
                {
                    #region Private fields
                    private Parser<string, T, bool> fromStringParser;
                    private Func<T, string> toStringConverter;
                    private List<T> fieldValues;
                    private List<T> fieldDefaultValues;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public List<T> FieldValues
                    {
                        get
                        {
                            return fieldValues;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field value can not be null!");
                            }
                            fieldValues = value;
                        }
                    }

                    /// <summary>
                    /// The default values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public List<T> FieldDefaultValues
                    {
                        get
                        {
                            return fieldDefaultValues;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field default value can not be null!");
                            }
                            fieldDefaultValues = value;
                        }
                    }


                    /// <summary>
                    /// The function used to parse a string to the type of the value.
                    /// </summary>
                    /// <remarks>
                    /// The function parameters must be 'string' and 'out T', and must return whether or not the parsing was successful.
                    /// </remarks>
                    public Parser<string, T, bool> FromStringParser
                    {
                        get
                        {
                            return fromStringParser;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Parser (from string) can not be null!");
                            }
                            fromStringParser = value;
                        }
                    }

                    /// <summary>
                    /// The function used to parse the value of this field to string.
                    /// </summary>
                    /// <remarks>
                    /// The function parameters must be 'T' and it has to return 'string'.
                    /// </remarks>
                    public Func<T, string> ToStringConverter
                    {
                        get
                        {
                            return toStringConverter;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Parser (to string) can not be null!");
                            }
                            toStringConverter = value;
                        }
                    }

                    /// <summary>
                    /// <inheritdoc/>
                    /// </summary>
                    public override Type TypeName
                    {
                        get
                        {
                            return typeof(List<T>);
                        }
                    }
                    #endregion

                    #region Constructors

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="fromStringParser">The function used to parse a string to the type of the value.</param>
                    /// <param name="toStringConverter">The function used to parse the value of this field to string.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public GenericValueCollectionField(string fieldSection, string fieldName, string fieldComment, List<T> fieldValues, List<T> fieldDefaultValues, Parser<string, T, bool> fromStringParser, Func<T, string> toStringConverter, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, delimiter, actionOnReadFail, actionOnReadSuccess)
                    {
                        FieldValues = fieldValues;
                        FieldDefaultValues = fieldDefaultValues;
                    }

                    #endregion

                    #region Public methods

                    /// <summary>
                    /// <para>Tries to get the values of this field from the given ini and returns whether or not it was succesful.</para>
                    /// <para>The field resets values to default if it fails.</para>
                    /// </summary>
                    /// <remarks>
                    /// This will invoke '<see cref="Field.ActionOnReadSuccess"/>' or '<see cref="Field.ActionOnReadFail"/>' depending on success.
                    /// </remarks>
                    public override bool TryGetFromIni(MyIni ini)
                    {
                        string s;
                        bool success = false;
                        if (ini.Get(Key).TryGetString(out s))
                        {
                            string[] valueStrings = s.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                            FieldValues.Clear();
                            success = true;
                            T value;
                            foreach (var valueString in valueStrings)
                            {
                                success &= FromStringParser(valueString, out value);
                                if (success)
                                {
                                    FieldValues.Add(value);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        if (success)
                        {
                            ActionOnReadSuccess?.Invoke();
                        }
                        else
                        {
                            FieldValues.Clear();
                            FieldValues.AddRange(FieldDefaultValues);
                            ActionOnReadFail?.Invoke();
                        }
                        return success;
                    }

                    /// <summary>
                    /// Sets this field's values to the given ini. Null values result in empty strings.
                    /// </summary>
                    /// <param name="setComment">Whether or not to also set the comment.</param>
                    public override void SetToIni(MyIni ini, bool setComment = true)
                    {
                        string valueStrings = "";
                        T v;
                        if (FieldValues.Count > 0)
                        {
                            v = FieldValues[0];
                            if (v != null)
                            {
                                valueStrings = ToStringConverter(FieldValues[0]);
                            }
                        }
                        for (int i = 1; i < FieldValues.Count; i++)
                        {
                            v = FieldValues[i];
                            if (v == null)
                            {
                                valueStrings += Delimiter;
                            }
                            else
                            {
                                valueStrings += Delimiter + ToStringConverter(v);
                            }
                        }
                        ini.Set(Key, valueStrings);
                        if (setComment && !string.IsNullOrWhiteSpace(FieldComment))
                        {
                            ini.SetComment(Key, FieldComment);
                        }
                    }

                    /// <summary>
                    /// Resets the field's value to its default value.
                    /// </summary>
                    public override void SetDefaults()
                    {
                        FieldValues.Clear();
                        FieldValues.AddRange(FieldDefaultValues);
                    }

                    #endregion

                }

                #region Block

                /// <summary>
                /// <inheritdoc/>
                /// </summary>
                public class BlockField<T> : BlockField, IField<T> where T : class, IMyTerminalBlock
                {

                    #region Private fields
                    private T fieldValue;
                    private T fieldDefaultValue;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The value of this field.
                    /// </summary>
                    public T FieldValue
                    {
                        get
                        {
                            return fieldValue;
                        }
                        set
                        {
                            fieldValue = value;
                        }
                    }

                    /// <summary>
                    /// The default value of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public T FieldDefaultValue
                    {
                        get
                        {
                            return fieldDefaultValue;
                        }
                        set
                        {
                            fieldDefaultValue = value;
                        }
                    }

                    /// <summary>
                    /// <inheritdoc/>
                    /// </summary>
                    public override Type TypeName
                    {
                        get
                        {
                            return typeof(T);
                        }
                    }
                    #endregion

                    #region Constructors

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public BlockField(string fieldSection, string fieldName, string fieldComment="", T fieldValue=default(T), T fieldDefaultValue=default(T), Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess)
                    {
                        FieldValue = fieldValue;
                        FieldDefaultValue = fieldDefaultValue;
                    }

                    #endregion

                    #region Public methods

                    /// <summary>
                    /// <para>Tries to get the value of this field from the given ini and returns whether or not it was succesful.</para>
                    /// <para>The field keeps its original value, if this fails.</para>
                    /// </summary>
                    /// <remarks>
                    /// This will invoke '<see cref="Field.ActionOnReadSuccess"/>' or '<see cref="Field.ActionOnReadFail"/>' depending on success.
                    /// </remarks>
                    public override bool TryGetFromIni(MyIni ini, IMyGridTerminalSystem gridTerminalSystem)
                    {
                        long id;
                        if (ini.Get(Key).TryGetInt64(out id))
                        {
                            T block = gridTerminalSystem.GetBlockWithId(id) as T;
                            if (block == default(IMyTerminalBlock))
                            {
                                ActionOnReadFail?.Invoke();
                                return false;
                            }
                            else
                            {
                                FieldValue = block;
                                ActionOnReadSuccess?.Invoke();
                                return true;
                            }
                        }
                        else
                        {
                            ActionOnReadFail?.Invoke();
                            return false;
                        }
                    }

                    /// <summary>
                    /// Sets this field's value to the given ini.
                    /// </summary>
                    /// <param name="setComment">Whether or not to also set the comment.</param>
                    public override void SetToIni(MyIni ini, bool setComment = true)
                    {
                        if (FieldValue == null)
                        {
                            ini.Set(Key, "");
                        }
                        else
                        {
                            ini.Set(Key, FieldValue.EntityId);
                        }
                        if (setComment && !string.IsNullOrWhiteSpace(FieldComment))
                        {
                            ini.SetComment(Key, FieldComment);
                        }
                    }

                    /// <summary>
                    /// Resets the field's value to its default value.
                    /// </summary>
                    public override void SetDefaults()
                    {
                        FieldValue = FieldDefaultValue;
                    }

                    #endregion

                }

                /// <summary>
                /// <inheritdoc/>
                /// </summary>
                public class BlockCollectionField<T> : BlockCollectionField, ICollectionField<T> where T : class, IMyTerminalBlock
                {
                    #region Private fields
                    private List<T> fieldValues;
                    private List<T> fieldDefaultValues;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public List<T> FieldValues
                    {
                        get
                        {
                            return fieldValues;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field value can not be null!");
                            }
                            fieldValues = value;
                        }
                    }

                    /// <summary>
                    /// The default values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public List<T> FieldDefaultValues
                    {
                        get
                        {
                            return fieldDefaultValues;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field default value can not be null!");
                            }
                            fieldDefaultValues = value;
                        }
                    }

                    /// <summary>
                    /// <inheritdoc/>
                    /// </summary>
                    public override Type TypeName
                    {
                        get
                        {
                            return typeof(List<T>);
                        }
                    }
                    #endregion

                    #region Constructors

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public BlockCollectionField(string fieldSection, string fieldName, string fieldComment, List<T> fieldValues, List<T> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, delimiter, actionOnReadFail, actionOnReadSuccess)
                    {
                        FieldValues = fieldValues;
                        FieldDefaultValues = fieldDefaultValues;
                    }

                    #endregion

                    #region Public methods

                    public override bool TryGetFromIni(MyIni ini, IMyGridTerminalSystem gridTerminalSystem)
                    {
                        string s;
                        bool success = false;
                        if (ini.Get(Key).TryGetString(out s))
                        {
                            string[] valueStrings = s.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                            FieldValues.Clear();
                            success = true;
                            Int64 entityID;
                            foreach (var idString in valueStrings)
                            {
                                success &= Int64.TryParse(idString, out entityID);
                                T block = gridTerminalSystem.GetBlockWithId(entityID) as T;
                                if (block != null)
                                {
                                    FieldValues.Add(block);
                                }
                                else
                                {
                                    success = false;
                                    break;
                                }
                            }
                        }
                        if (success)
                        {
                            ActionOnReadSuccess?.Invoke();
                        }
                        else
                        {
                            FieldValues.Clear();
                            FieldValues.AddRange(FieldDefaultValues);
                            ActionOnReadFail?.Invoke();
                        }
                        return success;
                    }

                    /// <summary>
                    /// Sets this field's values to the given ini. Null values result in empty strings.
                    /// </summary>
                    /// <param name="setComment">Whether or not to also set the comment.</param>
                    public override void SetToIni(MyIni ini, bool setComment = true)
                    {
                        string valueStrings = "";
                        T v;
                        if (FieldValues.Count > 0)
                        {
                            v = FieldValues[0];
                            if (v != null)
                            {
                                valueStrings = FieldValues[0].EntityId.ToString();
                            }
                        }
                        for (int i = 1; i < FieldValues.Count; i++)
                        {
                            v = FieldValues[i];
                            if (v == null)
                            {
                                valueStrings += Delimiter;
                            }
                            else
                            {
                                valueStrings += Delimiter + v.EntityId.ToString();
                            }
                        }
                        ini.Set(Key, valueStrings);
                        if (setComment && !string.IsNullOrWhiteSpace(FieldComment))
                        {
                            ini.SetComment(Key, FieldComment);
                        }
                    }

                    /// <summary>
                    /// Resets the field's value to its default value.
                    /// </summary>
                    public override void SetDefaults()
                    {
                        FieldValues.Clear();
                        FieldValues.AddRange(FieldDefaultValues);
                    }

                    #endregion
                }

                #endregion

                #region String

                public class StringField : ValueField, IField<string>
                {

                    #region Private fields
                    private string fieldValue;
                    private string fieldDefaultValue;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The value of this field.
                    /// </summary>
                    public string FieldValue
                    {
                        get
                        {
                            return fieldValue;
                        }
                        set
                        {
                            fieldValue = value;
                        }
                    }

                    /// <summary>
                    /// The default value of this field.
                    /// </summary>
                    public string FieldDefaultValue
                    {
                        get
                        {
                            return fieldDefaultValue;
                        }
                        set
                        {
                            fieldDefaultValue = value;
                        }
                    }

                    public override Type TypeName
                    {
                        get
                        {
                            return typeof(string);
                        }
                    }

                    #endregion

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public StringField(string fieldSection, string fieldName, string fieldComment, String fieldValue, String fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, actionOnReadFail, actionOnReadSuccess)
                    {
                        FieldValue = fieldValue;
                        FieldDefaultValue = fieldDefaultValue;
                    }

                    public override bool TryGetFromIni(MyIni ini)
                    {
                        string s;
                        if (ini.Get(Key).TryGetString(out s))
                        {
                            FieldValue = s;
                            ActionOnReadSuccess?.Invoke();
                            return true;
                        }
                        else
                        {
                            ActionOnReadFail?.Invoke();
                            return false;
                        }
                    }

                    public override void SetToIni(MyIni ini, bool setComment)
                    {
                        if (FieldValue==null)
                        {
                            ini.Set(Key, "");
                        }
                        else
                        {
                            ini.Set(Key, FieldValue);
                        }
                        if (setComment && !string.IsNullOrWhiteSpace(FieldComment))
                        {
                            ini.SetComment(Key, FieldComment);
                        }
                    }

                    public override void SetDefaults()
                    {
                        FieldValue = FieldDefaultValue;
                    }
                }

                public class StringCollectionField : ValueCollectionField, ICollectionField<string>
                {

                    #region Private fields
                    private List<string> fieldValues;
                    private List<string> fieldDefaultValues;
                    #endregion

                    #region Public properties

                    /// <summary>
                    /// The values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public List<string> FieldValues
                    {
                        get
                        {
                            return fieldValues;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field value can not be null!");
                            }
                            fieldValues = value;
                        }
                    }

                    /// <summary>
                    /// The default values of this field.
                    /// </summary>
                    /// <remarks>
                    /// This value can not be null!
                    /// </remarks>
                    public List<string> FieldDefaultValues
                    {
                        get
                        {
                            return fieldDefaultValues;
                        }
                        set
                        {
                            if (value == null)
                            {
                                throw new Exception("Field default value can not be null!");
                            }
                            fieldDefaultValues = value;
                        }
                    }

                    /// <summary>
                    /// <inheritdoc/>
                    /// </summary>
                    public override Type TypeName
                    {
                        get
                        {
                            return typeof(List<string>);
                        }
                    }
                    #endregion

                    #region Constructors
                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public StringCollectionField(string fieldSection, string fieldName, string fieldComment, List<String> fieldValues, List<String> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, delimiter, actionOnReadFail, actionOnReadSuccess)
                    {
                        FieldValues = fieldValues;
                        FieldDefaultValues = fieldDefaultValues;
                    }

                    #endregion

                    #region Public methods

                    /// <summary>
                    /// <para>Tries to get the values of this field from the given ini and returns whether or not it was succesful.</para>
                    /// <para>The field resets values to default if it fails.</para>
                    /// </summary>
                    /// <remarks>
                    /// This will invoke '<see cref="Field.ActionOnReadSuccess"/>' or '<see cref="Field.ActionOnReadFail"/>' depending on success.
                    /// </remarks>
                    public override bool TryGetFromIni(MyIni ini)
                    {
                        string s;
                        if (ini.Get(Key).TryGetString(out s))
                        {
                            string[] valueStrings = s.Split(new string[] { Delimiter }, StringSplitOptions.None);
                            FieldValues.Clear();
                            foreach (var valueString in valueStrings)
                            {
                                FieldValues.Add(valueString);
                            }
                            ActionOnReadSuccess?.Invoke();
                            return true;
                        }
                        else
                        {
                            FieldValues.Clear();
                            FieldValues.AddRange(FieldDefaultValues);
                            ActionOnReadFail?.Invoke();
                            return false;
                        }
                    }

                    /// <summary>
                    /// Sets this field's values to the given ini. Null values result in empty strings.
                    /// </summary>
                    /// <param name="setComment">Whether or not to also set the comment.</param>
                    public override void SetToIni(MyIni ini, bool setComment = true)
                    {
                        string valueStrings = "";
                        string v;
                        if (FieldValues.Count > 0)
                        {
                            v = FieldValues[0];
                            if (v != null)
                            {
                                valueStrings = FieldValues[0];
                            }
                        }
                        for (int i = 1; i < FieldValues.Count; i++)
                        {
                            v = FieldValues[i];
                            if (v == null)
                            {
                                valueStrings += Delimiter;
                            }
                            else
                            {
                                valueStrings += Delimiter + v;
                            }
                        }
                        ini.Set(Key, valueStrings);
                        if (setComment && !string.IsNullOrWhiteSpace(FieldComment))
                        {
                            ini.SetComment(Key, FieldComment);
                        }
                    }

                    /// <summary>
                    /// Resets the field's value to its default value.
                    /// </summary>
                    public override void SetDefaults()
                    {
                        FieldValues.Clear();
                        FieldValues.AddRange(FieldDefaultValues);
                    }

                    #endregion
                }

                #endregion

                #region Common types

                #region Boolean

                public class BooleanField : GenericValueField<Boolean>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public BooleanField(string fieldSection, string fieldName, string fieldComment, Boolean fieldValue, Boolean fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Boolean.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class BooleanCollectionField : GenericValueCollectionField<Boolean>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public BooleanCollectionField(string fieldSection, string fieldName, string fieldComment, List<Boolean> fieldValues, List<Boolean> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Boolean.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess)
                    {}

                }

                #endregion

                #region Char

                public class CharField : GenericValueField<Char>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public CharField(string fieldSection, string fieldName, string fieldComment, Char fieldValue, Char fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Char.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }


                }

                public class CharCollectionField : GenericValueCollectionField<Char>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public CharCollectionField(string fieldSection, string fieldName, string fieldComment, List<Char> fieldValues, List<Char> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Char.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region SByte

                public class SByteField : GenericValueField<SByte>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public SByteField(string fieldSection, string fieldName, string fieldComment, SByte fieldValue, SByte fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, SByte.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class SByteCollectionField : GenericValueCollectionField<SByte>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public SByteCollectionField(string fieldSection, string fieldName, string fieldComment, List<SByte> fieldValues, List<SByte> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, SByte.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }


                }

                #endregion

                #region Byte

                public class ByteField : GenericValueField<Byte>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public ByteField(string fieldSection, string fieldName, string fieldComment, Byte fieldValue, Byte fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Byte.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }


                }

                public class ByteCollectionField : GenericValueCollectionField<Byte>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public ByteCollectionField(string fieldSection, string fieldName, string fieldComment, List<Byte> fieldValues, List<Byte> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Byte.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }


                }

                #endregion

                #region UInt16

                public class UInt16Field : GenericValueField<UInt16>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public UInt16Field(string fieldSection, string fieldName, string fieldComment, UInt16 fieldValue, UInt16 fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, UInt16.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class UInt16CollectionField : GenericValueCollectionField<UInt16>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public UInt16CollectionField(string fieldSection, string fieldName, string fieldComment, List<UInt16> fieldValues, List<UInt16> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, UInt16.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region Int16

                public class Int16Field : GenericValueField<Int16>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Int16Field(string fieldSection, string fieldName, string fieldComment, Int16 fieldValue, Int16 fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Int16.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class Int16CollectionField : GenericValueCollectionField<Int16>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Int16CollectionField(string fieldSection, string fieldName, string fieldComment, List<Int16> fieldValues, List<Int16> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Int16.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region UInt32

                public class UInt32Field : GenericValueField<UInt32>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public UInt32Field(string fieldSection, string fieldName, string fieldComment, UInt32 fieldValue, UInt32 fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, UInt32.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class UInt32CollectionField : GenericValueCollectionField<UInt32>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public UInt32CollectionField(string fieldSection, string fieldName, string fieldComment, List<UInt32> fieldValues, List<UInt32> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, UInt32.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region Int32

                public class Int32Field : GenericValueField<Int32>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Int32Field(string fieldSection, string fieldName, string fieldComment, Int32 fieldValue, Int32 fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Int32.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class Int32CollectionField : GenericValueCollectionField<Int32>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Int32CollectionField(string fieldSection, string fieldName, string fieldComment, List<Int32> fieldValues, List<Int32> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Int32.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region UInt64

                public class UInt64Field : GenericValueField<UInt64>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public UInt64Field(string fieldSection, string fieldName, string fieldComment, UInt64 fieldValue, UInt64 fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, UInt64.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class UInt64CollectionField : GenericValueCollectionField<UInt64>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public UInt64CollectionField(string fieldSection, string fieldName, string fieldComment, List<UInt64> fieldValues, List<UInt64> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, UInt64.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region Int64

                public class Int64Field : GenericValueField<Int64>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Int64Field(string fieldSection, string fieldName, string fieldComment, Int64 fieldValue, Int64 fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Int64.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class Int64CollectionField : GenericValueCollectionField<Int64>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public Int64CollectionField(string fieldSection, string fieldName, string fieldComment, List<Int64> fieldValues, List<Int64> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Int64.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }


                }

                #endregion

                #region Single

                public class SingleField : GenericValueField<Single>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public SingleField(string fieldSection, string fieldName, string fieldComment, Single fieldValue, Single fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Single.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class SingleCollectionField : GenericValueCollectionField<Single>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public SingleCollectionField(string fieldSection, string fieldName, string fieldComment, List<Single> fieldValues, List<Single> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Single.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }


                }

                #endregion

                #region Double

                public class DoubleField : GenericValueField<Double>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public DoubleField(string fieldSection, string fieldName, string fieldComment, Double fieldValue, Double fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Double.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class DoubleCollectionField : GenericValueCollectionField<Double>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public DoubleCollectionField(string fieldSection, string fieldName, string fieldComment, List<Double> fieldValues, List<Double> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Double.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) { }

                }

                #endregion

                #region Decimal

                public class DecimalField : GenericValueField<Decimal>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValue">The value of this field.</param>
                    /// <param name="fieldDefaultValue">The default value of this field.</param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public DecimalField(string fieldSection, string fieldName, string fieldComment, Decimal fieldValue, Decimal fieldDefaultValue, Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValue, fieldDefaultValue, Decimal.TryParse, v => v.ToString(), actionOnReadFail, actionOnReadSuccess) { }

                }

                public class DecimalCollectionField : GenericValueCollectionField<Decimal>
                {

                    /// <summary>
                    /// Instantiates a field.
                    /// </summary>
                    /// <param name="fieldSection">The name of the section this field belongs to.</param>
                    /// <param name="fieldName">The name of the field.</param>
                    /// <param name="fieldComment">The comment for this field.</param>
                    /// <param name="fieldValues">The value of this field.</param>
                    /// <param name="fieldDefaultValues">The default value of this field.</param>
                    /// <param name="delimiter"><para>The delimiter used to separate the string equivalents of the value before parsing them to the type of the value.</para><para>Note: The string equivalent of the value must not contain this delimiter, as it would result in faulty splitting.</para></param>
                    /// <param name="actionOnReadSuccess">This method will be called if the reading of this field is succesful.</param>
                    /// <param name="actionOnReadFail">This method will be called if the reading of this field fails.</param>
                    public DecimalCollectionField(string fieldSection, string fieldName, string fieldComment, List<Decimal> fieldValues, List<Decimal> fieldDefaultValues, string delimiter = ",", Action actionOnReadFail = null, Action actionOnReadSuccess = null)
                        : base(fieldSection, fieldName, fieldComment, fieldValues, fieldDefaultValues, Decimal.TryParse, v => v.ToString(), delimiter, actionOnReadFail, actionOnReadSuccess) {}


                }

                #endregion

                #endregion
            }
            #endregion

            #region Private fields
            private readonly List<Fields.ValueField> valueFields;
            private readonly List<Fields.ValueField> testValueFields;

            private readonly List<Fields.BlockField> blockFields;
            private readonly List<Fields.BlockField> testBlockFields;

            private readonly MyIni ini;
            private readonly MyIni testIni;

            private Func<string> pullMethod;
            private Action<string> pushMethod;

            private readonly IMyGridTerminalSystem gridTerminalSystem;
            #endregion

            #region Public properties

            /// <summary>
            /// Iterates through all fields belonging to this persistence system.
            /// </summary>
            public IEnumerable<Fields.Field> AllFields
            {
                get
                {
                    foreach (var field in valueFields)
                    {
                        yield return field;
                    }
                    foreach (var field in blockFields)
                    {
                        yield return field;
                    }
                }
            }

            /// <summary>
            /// Iterates through all '<see cref="Fields.ValueField"/>s' belonging to this persistence system.
            /// </summary>
            public IEnumerable<Fields.ValueField> ValueFields
            {
                get
                {
                    foreach (var item in valueFields)
                    {
                        yield return item;
                    }
                }
            }

            /// <summary>
            /// Iterates through all '<see cref="Fields.BlockField"/>s' belonging to this persistence system.
            /// </summary>
            public IEnumerable<Fields.BlockField> BlockFields
            {
                get
                {
                    foreach (var field in blockFields)
                    {
                        yield return field;
                    }
                }
            }

            /// <summary>
            /// The method used to write the data to storage.
            /// </summary>
            public Action<string> PushMethod
            {
                get
                {
                    return pushMethod;
                }
                set
                {
                    if (value == null)
                    {
                        throw new Exception("Push method can not be null!");
                    }
                    pushMethod = value;
                }
            }

            /// <summary>
            /// The function used to retrieve the data from storage.
            /// </summary>
            public Func<string> PullMethod
            {
                get
                {
                    return pullMethod;
                }
                set
                {
                    if (value == null)
                    {
                        throw new Exception("Pull method can not be null!");
                    }
                    pullMethod = value;
                }
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Resets all fields to default values.
            /// </summary>
            public void SetDefaults()
            {
                foreach (var f in AllFields)
                {
                    f.SetDefaults();
                }
            }

            /// <summary>
            /// Adds a field to this persistence system.
            /// </summary>
            public void AddField(Fields.ValueField field)
            {
                if (AllFields.Any(f => f.Key == field.Key))
                {
                    throw new Exception($"Cannot add field! A field with this key ({field.Key.Section}/{field.Key.Name}) is already present in this persistence system!");
                }
                valueFields.Add(field);
            }

            /// <summary>
            /// Adds a field to this persistence system.
            /// </summary>
            public void AddField(Fields.BlockField field)
            {
                if (AllFields.Any(f => f.Key == field.Key))
                {
                    throw new Exception($"Cannot add field! A field with this key ({field.Key.Section}/{field.Key.Name}) is already present in this persistence system!");
                }
                blockFields.Add(field);
            }

            /// <summary>
            /// Removes the field with the given key from this persistence system.
            /// </summary>
            public void RemoveFieldWithKey(MyIniKey key)
            {
                int fieldInd = -1;
                for (int i = 0; i < valueFields.Count; i++)
                {
                    if (valueFields[i].Key == key)
                    {
                        fieldInd = i;
                        break;
                    }
                }
                if (fieldInd != -1)
                {
                    valueFields.RemoveAt(fieldInd);
                }
                else
                {
                    for (int i = 0; i < blockFields.Count; i++)
                    {
                        if (blockFields[i].Key == key)
                        {
                            fieldInd = i;
                            break;
                        }
                    }
                    if (fieldInd != -1)
                    {
                        blockFields.RemoveAt(fieldInd);
                    }
                }
            }

            /// <summary>
            /// Removes a field from this persistence system.
            /// </summary>
            public void RemoveField(Fields.ValueField field)
            {
                valueFields.Remove(field);
            }

            /// <summary>
            /// Removes a field from this persistence system.
            /// </summary>
            public void RemoveField(Fields.BlockField field)
            {
                blockFields.Remove(field);
            }

            /// <summary>
            /// Clears this persistence system. This removes all fields and clears the ini.
            /// </summary>
            public void Clear()
            {
                ClearIni();
                ClearFields();
            }

            /// <summary>
            /// Clears the internal ini but leaves fields unchanged.
            /// </summary>
            public void ClearIni()
            {
                ini.Clear();
                testIni.Clear();
            }

            /// <summary>
            /// Removes all fields but leaves internal ini unchanged.
            /// </summary>
            public void ClearFields()
            {
                valueFields.Clear();
                testValueFields.Clear();
                blockFields.Clear();
                testBlockFields.Clear();
            }

            /// <summary>
            /// Commits changes made to the given field to the internal ini.
            /// <para>If the commit fails, the internal ini remains unchanged.</para>
            /// </summary>
            /// <param name="field">The field with the changes to commit.</param>
            public bool CommitField(Fields.ValueField field)
            {
                if (!valueFields.Contains(field))
                {
                    return false;
                }
                else
                {
                    try
                    {
                        field.SetToIni(testIni, true);
                        field.SetToIni(ini, true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Commits changes made to the given field to the internal ini.
            /// <para>If the commit fails, the internal ini remains unchanged.</para>
            /// </summary>
            /// <param name="field">The field with the changes to commit.</param>
            public bool CommitField(Fields.BlockField field)
            {
                if (!blockFields.Contains(field))
                {
                    return false;
                }
                else
                {
                    try
                    {
                        field.SetToIni(testIni, true);
                        field.SetToIni(ini, true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Tries to commit changes to the internal ini and returns whether or not it was successful.
            /// </summary>
            /// <remarks>
            /// For a safer but slower method see '<see cref="Commit(out Exception)"/>'.
            /// </remarks>
            public bool Commit()
            {
                try
                {
                    foreach (var field in AllFields)
                    {
                        field.SetToIni(ini, true);
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            /// <summary>
            /// Tries to commit changes to the internal ini and returns whether or not it was successful.
            /// <para>
            /// If the commit fails, the internal ini remains unchanged and can safely be pushed.
            /// </para>
            /// </summary>
            /// <param name="exception">The exception the push failed with, otherwise null.</param>
            public bool Commit(out Exception exception)
            {
                bool success = true;
                exception = null;
                testIni.Clear();
                try
                {
                    foreach (var field in AllFields)
                    {
                        field.SetToIni(testIni, true);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                    success = false;
                }
                if (success)
                {
                    ini.TryParse(testIni.ToString());
                }
                return success;
            }

            /// <summary>
            /// Tries to pull data to the internal ini and returns whether or not it was successful.
            /// <para>Fields with the highest pull priority will be pulled first, fields with lowest last.</para>
            /// <para><see cref="Fields.ValueField"/>s will always be pulled before <see cref="Fields.BlockField"/>s.</para>
            /// </summary>
            /// <param name="stopAtFirstFail"><para>Whether or not to stop at the first failed attempt to parse data to a field.</para><para>Setting this to false can be useful when '<see cref="Field.ActionOnReadFail"/>' was set to a method that can rectify such a situation.</para></param>
            /// <remarks>
            /// For a safer but slower method see '<see cref="Pull(out Exception)"/>'.
            /// </remarks>
            public bool Pull(bool stopAtFirstFail = true)
            {
                if (ini.TryParse(PullMethod()))
                {
                    foreach (var field in valueFields.OrderByDescending(f => f.PullPriority))
                    {
                        if (!field.TryGetFromIni(ini) && stopAtFirstFail)
                        {
                            return false;
                        }
                    }
                    if (blockFields.Count>0 && gridTerminalSystem==null)
                    {
                        throw new Exception("Grid terminal system must be set to pull blocks!");
                    }
                    foreach (var field in blockFields.OrderByDescending(f => f.PullPriority))
                    {
                        if (!field.TryGetFromIni(ini, gridTerminalSystem) && stopAtFirstFail)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Tries to pull data to the internal ini and returns whether or not it was successful.
            /// <para>If the pull fails, the internal ini and the fields remain unchanged and they can be safely committed and/or pushed.</para>
            /// <para>Fields with the highest pull priority will be pulled first, fields with lowest last.</para>
            /// <para><see cref="Fields.ValueField"/>s will always be pulled before <see cref="Fields.BlockField"/>s.</para>
            /// </summary>
            /// <param name="exception">The exception the pull failed with, otherwise null.</param>
            public bool Pull(out Exception exception)
            {
                bool success = true;
                exception = null;
                testValueFields.Clear();
                testValueFields.AddRange(valueFields);
                testBlockFields.Clear();
                testBlockFields.AddRange(blockFields);
                try
                {
                    if (testIni.TryParse(PullMethod()))
                    {
                        foreach (var field in testValueFields.OrderByDescending(f=>f.PullPriority))
                        {
                            if (!field.TryGetFromIni(testIni))
                            {
                                throw new Exception($"Could not pull data for field with key '{field.FieldSection}/{field.FieldName}'.");
                            }
                        }
                        if (testBlockFields.Count > 0 && gridTerminalSystem == null)
                        {
                            throw new Exception("Grid terminal system must be set to pull blocks!");
                        }
                        foreach (var field in testBlockFields.OrderByDescending(f => f.PullPriority))
                        {
                            if (!field.TryGetFromIni(testIni, gridTerminalSystem))
                            {
                                throw new Exception($"Could not pull data for field with key '{field.FieldSection}/{field.FieldName}'.");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("The storage string could not be parsed.");
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                    success = false;
                }
                if (success)
                {
                    ini.TryParse(testIni.ToString());
                    valueFields.Clear();
                    valueFields.AddRange(testValueFields);
                    testValueFields.Clear();
                    blockFields.Clear();
                    blockFields.AddRange(testBlockFields);
                    testBlockFields.Clear();
                }
                return success;
            }

            /// <summary>
            /// Tries to push data from the internal ini and returns whether or not it was successful.
            /// </summary>
            /// <remarks>
            /// For a safer but slower method see '<see cref="Push(out Exception)"/>'.
            /// </remarks>
            public bool Push()
            {
                try
                {
                    PushMethod(ini.ToString());
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            /// <summary>
            /// Tries to push data from the internal ini and returns whether or not it was successful.
            /// </summary>
            /// <param name="exception">The exception the push failed with, otherwise null.</param>
            public bool Push(out Exception exception)
            {
                bool success = true;
                exception = null;
                try
                {
                    PushMethod(ini.ToString());
                }
                catch (Exception e)
                {
                    exception = e;
                    success = false;
                }
                return success;
            }

            /// <summary>
            /// Exposes the internal ini.
            /// </summary>
            /// <remarks>
            /// Use this if you want to have a more hands-on approach. Note: using the internal ini directly and making a mistake can make the persistence system corrupt.
            /// </remarks>
            public MyIni ExposeIni()
            {
                return ini;
            }

            #endregion

            /// <summary>
            /// Instantiates a persistence system.
            /// </summary>
            /// <param name="pullMethod">The method used to retrieve the data this persistence system will be handling.</param>
            /// <param name="pushMethod">The method used to push the data this persistence system is handling.</param>
            /// <param name="gridTerminalSystem">The grid terminal system with blocks to get/set from/to fields. <para>This is only required if the persistence system needs to be able to handle <see cref="Fields.BlockField"/>s.</para></param>
            public ArgPersistenceSystem(Func<string> pullMethod, Action<string> pushMethod, IMyGridTerminalSystem gridTerminalSystem=null)
            {
                ini = new MyIni();
                testIni = new MyIni();

                valueFields = new List<Fields.ValueField>();
                testValueFields = new List<Fields.ValueField>();

                blockFields = new List<Fields.BlockField>();
                testBlockFields = new List<Fields.BlockField>();

                PullMethod = pullMethod;
                PushMethod = pushMethod;

                this.gridTerminalSystem = gridTerminalSystem;
            }

        }
    }
}
