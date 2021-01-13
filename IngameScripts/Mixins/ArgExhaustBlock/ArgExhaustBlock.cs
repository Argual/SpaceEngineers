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
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// Exhaust Block wrap to provide easy control over the Exhaust Block introduced in the Wasteland DLC.
        /// <para>Only contains accessors to the effects which were present in the vanilla game in the version below.</para>
        /// <para>Can be used to select effects which are not in the list, but only using the 64 bit integer effect ID.</para>
        /// </summary>
        /// <remarks>
        /// Tested with game version v1.197.073.
        /// </remarks>
        public class ArgExhaustBlock
        {

            #region "Redirections"

            public bool Enabled
            {
                get
                {
                    return _block.Enabled;
                }
                set
                {
                    _block.Enabled = value;
                }
            }

            public string CustomName
            {
                get
                {
                    return _block.CustomName;
                }
                set
                {
                    _block.CustomName = value;
                }
            }


            public string CustomNameWithFaction => _block.CustomNameWithFaction;


            public string DetailedInfo => _block.DetailedInfo;


            public string CustomInfo => _block.CustomInfo;

            /// <summary>
            /// Gets or sets the Custom Data string. NOTE: Only use this for user input.
            /// For storing large mod configs, create your own MyModStorageComponent
            /// </summary>

            public string CustomData
            {
                get
                {
                    return _block.CustomData;
                }
                set
                {
                    _block.CustomData = value;
                }
            }

            public bool ShowOnHUD
            {
                get
                {
                    return _block.ShowOnHUD;
                }
                set
                {
                    _block.ShowOnHUD = value;
                }
            }

            public bool ShowInTerminal
            {
                get
                {
                    return _block.ShowInTerminal;
                }
                set
                {
                    _block.ShowInTerminal = value;
                }
            }

            public bool ShowInToolbarConfig
            {
                get
                {
                    return _block.ShowInToolbarConfig;
                }
                set
                {
                    _block.ShowInToolbarConfig = value;
                }
            }


            public bool ShowInInventory
            {
                get
                {
                    return _block.ShowInInventory;
                }
                set
                {
                    _block.ShowInInventory = value;
                }
            }

            /// <summary>
            /// Definition name.
            /// </summary>
            public string DefinitionDisplayNameText => _block.DefinitionDisplayNameText;

            /// <summary>
            /// Ratio at which is the block disassembled (grinding).
            /// </summary>
            public float DisassembleRatio => _block.DisassembleRatio;

            /// <summary>
            /// Translated block name.
            /// </summary>
            public string DisplayNameText => _block.DisplayNameText;

            /// <summary>
            /// Hacking of the block is in progress.
            /// </summary>
            public bool IsBeingHacked => _block.IsBeingHacked;

            /// <summary>
            /// True if integrity is above breaking threshold.
            /// </summary>
            public bool IsFunctional => _block.IsFunctional;

            /// <summary>
            /// True if block is able to do its work depening on block type (is functional, powered, enabled, etc...)
            /// </summary>
            public bool IsWorking => _block.IsWorking;

            public VRage.ObjectBuilders.SerializableDefinitionId BlockDefinition => _block.BlockDefinition;

            public bool CheckConnectionAllowed => _block.CheckConnectionAllowed;

            /// <summary>
            /// Grid in which the block is placed.
            /// </summary>
            public IMyCubeGrid CubeGrid => _block.CubeGrid;

            /// <summary>
            /// Maximum coordinates of grid cells occupied by this block.
            /// </summary>
            public Vector3I Max => _block.Max;

            /// <summary>
            /// Block mass.
            /// </summary>
            public float Mass => _block.Mass;

            /// <summary>
            /// Minimum coordinates of grid cells occupied by this block.
            /// </summary>
            public Vector3I Min => _block.Min;

            /// <summary>
            /// Order in which were the blocks of same type added to grid Used in default display name.
            /// </summary>
            public int NumberInGrid => _block.NumberInGrid;

            /// <summary>
            /// Returns block orientation in base 6 directions.
            /// </summary>
            public MyBlockOrientation Orientation => _block.Orientation;

            /// <summary>
            /// Id of player owning block (not steam Id).
            /// </summary>
            public long OwnerId => _block.OwnerId;

            /// <summary>
            /// Position in grid coordinates.
            /// </summary>
            public Vector3I Position => _block.Position;

            public long EntityId => _block.EntityId;

            public string Name => _block.Name;

            public string DisplayName => _block.DisplayName;

            /// <summary>
            /// Returns true if this entity has got at least one inventory.
            /// Note that one aggregate inventory can contain zero simple inventories => zero will be returned even if GetInventory() != null.
            /// </summary>
            public bool HasInventory => _block.HasInventory;

            /// <summary>
            /// Returns the count of the number of inventories this entity has.
            /// </summary>
            public int InventoryCount => _block.InventoryCount;

            public BoundingBoxD WorldAABB => _block.WorldAABB;

            public BoundingBoxD WorldAABBHr => _block.WorldAABBHr;

            public MatrixD WorldMatrix => _block.WorldMatrix;

            public BoundingSphereD WorldVolume => _block.WorldVolume;

            public BoundingSphereD WorldVolumeHr => _block.WorldVolumeHr;

            public bool HasLocalPlayerAccess()
            {
                return _block.HasLocalPlayerAccess();
            }

            public bool HasPlayerAccess(long playerId)
            {
                return _block.HasPlayerAccess(playerId);
            }

            public void GetActions(List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null)
            {
                _block.GetActions(resultList, collect);
            }

            public void SearchActionsOfName(string name, List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null)
            {
                _block.SearchActionsOfName(name, resultList, collect);
            }

            public ITerminalAction GetActionWithName(string name)
            {
                return _block.GetActionWithName(name);
            }

            public ITerminalProperty GetProperty(string id)
            {
                return _block.GetProperty(id);
            }

            public void GetProperties(List<ITerminalProperty> resultList, Func<ITerminalProperty, bool> collect = null)
            {
                _block.GetProperties(resultList, collect);
            }

            /// <summary>
            /// Determines whether this block is mechanically connected to the other.
            /// This is any block connected with rotors or pistons or other mechanical devices, but not things like connectors.
            /// This will in most cases constitute your complete construct.
            /// </summary>
            /// <remarks>
            /// Be aware that using merge blocks combines grids into one, so this function will not filter out grids connected that way.
            /// Also be aware that detaching the heads of pistons and rotors will cause this connection to change.
            /// </remarks>
            public bool IsSameConstructAs(IMyTerminalBlock other)
            {
                return _block.IsSameConstructAs(other);
            }

            /// <summary>
            /// Tag of faction owning block.
            /// </summary>
            public string GetOwnerFactionTag()
            {
                return _block.GetOwnerFactionTag();
            }

            public MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId)
            {
                return _block.GetUserRelationToOwner(playerId);
            }

            ///<summary>
            /// Simply get the MyInventoryBase component stored in this entity.
            /// </summary>
            public IMyInventory GetInventory()
            {
                return _block.GetInventory();
            }

            /// <summary>
            /// Search for inventory component with matching index.
            /// </summary>
            public IMyInventory GetInventory(int index)
            {
                return _block.GetInventory(index);
            }

            public Vector3D GetPosition()
            {
                return _block.GetPosition();
            }

            #endregion

            #region Subclasses

            public enum ExhaustEffect
            {
                CarSmoke,
                CarSmokeSmall,
                ElectricArc,
                ElectricArcSmall,
                Fire,
                FireSmall,
                FireAndSmoke,
                FireAndSmokeSmall,
                Smoke,
                SmokeSmall,
                SmokeElectric,
                SmokeElectricSmall,
                SmokeReactor,
                SmokeReactorSmall,
                SmokeWhite,
                SmokeWhiteSmall
            }

            #endregion

            #region Interfacing properties

            /// <summary>
            /// Power dependency determines the relationship between the "strength" of the exhaust effect and the power consumption on the grid.
            /// <para>
            /// If the power dependency is high then the power consumption also has to be high for the exhaust effect to play at full strength.
            /// </para>
            /// </summary>
            /// <value>
            /// <para>0.0 results in the exhaust effect always playing at full strength.</para>
            /// <para>1.0 results in the exhaust effect playing only when the power consumption is at maximum.</para>
            /// </value>
            public float PowerDependency
            {
                get
                {
                    return _block.GetValueFloat("PowerDependency");
                }
                set
                {
                    _block.SetValueFloat("PowerDependency", value);
                }
            }

            /// <summary>
            /// The 'long' value the exhaust block is using to describe the effect it is playing.
            /// </summary>
            public long EffectsCombo
            {
                get
                {
                    return _block.GetValue<long>("EffectsCombo");
                }
                set
                {
                    _block.SetValue<long>("EffectsCombo", value);
                }
            }

            #region Dictionaries
            private static readonly Dictionary<ExhaustEffect, long> ExhaustEffectToInt64Dictionary = new Dictionary<ExhaustEffect, long>
            {
                { ExhaustEffect.CarSmoke, 1945938088},
                { ExhaustEffect.CarSmokeSmall,1158671359},
                { ExhaustEffect.ElectricArc,-1667853719},
                { ExhaustEffect.ElectricArcSmall,997989196},
                { ExhaustEffect.Fire,147064682},
                { ExhaustEffect.FireSmall,45321317},
                { ExhaustEffect.FireAndSmoke,1268511193},
                { ExhaustEffect.FireAndSmokeSmall,-2055976012},
                { ExhaustEffect.Smoke,-479539197},
                { ExhaustEffect.SmokeSmall,689274528},
                { ExhaustEffect.SmokeElectric,-1401948218},
                { ExhaustEffect.SmokeElectricSmall,2033843515},
                { ExhaustEffect.SmokeReactor,-244481809},
                { ExhaustEffect.SmokeReactorSmall,-782755626},
                { ExhaustEffect.SmokeWhite,20799151},
                { ExhaustEffect.SmokeWhiteSmall,-206140778}
            };

            private static readonly Dictionary<long, ExhaustEffect> Int64ToExhaustEffectDictionary = new Dictionary<long, ExhaustEffect>
            {
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.CarSmoke],ExhaustEffect.CarSmoke},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.CarSmokeSmall],ExhaustEffect.CarSmokeSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.ElectricArc],ExhaustEffect.ElectricArc},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.ElectricArcSmall],ExhaustEffect.ElectricArcSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.Fire],ExhaustEffect.Fire},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.FireSmall],ExhaustEffect.FireSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.FireAndSmoke],ExhaustEffect.FireAndSmoke},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.FireAndSmokeSmall],ExhaustEffect.FireAndSmokeSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.Smoke],ExhaustEffect.Smoke},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeSmall],ExhaustEffect.SmokeSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeElectric],ExhaustEffect.SmokeElectric},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeElectricSmall],ExhaustEffect.SmokeElectricSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeReactor],ExhaustEffect.SmokeReactor},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeReactorSmall],ExhaustEffect.SmokeReactorSmall},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeWhite],ExhaustEffect.SmokeWhite},
                {ExhaustEffectToInt64Dictionary[ExhaustEffect.SmokeWhiteSmall],ExhaustEffect.SmokeWhiteSmall}
            };

            
            public static bool TryParseExhaustEffectToInt64(ExhaustEffect exhaustEffect, out long longInt)
            {
                if (ExhaustEffectToInt64Dictionary.ContainsKey(exhaustEffect))
                {
                    longInt = ExhaustEffectToInt64Dictionary[exhaustEffect];
                    return true;
                }
                else
                {
                    longInt = default(long);
                    return false;
                }
            }

            public static bool TryParseInt64ToExhaustEffect(long longInt, out ExhaustEffect exhaustEffect)
            {
                if (Int64ToExhaustEffectDictionary.ContainsKey(longInt))
                {
                    exhaustEffect = Int64ToExhaustEffectDictionary[longInt];
                    return true;
                }
                else
                {
                    exhaustEffect = default(ExhaustEffect);
                    return false;
                }
            }

            #endregion

            /// <summary>
            /// The exhaust effect the exhaust block is playing.
            /// </summary>
            public ExhaustEffect Effect
            {
                get
                {
                    return Int64ToExhaustEffectDictionary[_block.GetValue<long>("EffectsCombo")];
                }
                set
                {
                    _block.SetValue<long>("EffectsCombo", ExhaustEffectToInt64Dictionary[value]);
                }
            }


            #endregion

            #region Private fields

            private IMyFunctionalBlock _block;

            #endregion

            #region Public properties
            /// <summary>
            /// The actual exhaust block as '<see cref="IMyFunctionalBlock"/>'.
            /// </summary>
            public IMyFunctionalBlock ActualBlock
            {
                get
                {
                    return _block;
                }
            }
            #endregion

            /// <summary>
            /// Returns whether or not a block is an exhaust block.
            /// </summary>
            public static bool Verify(IMyFunctionalBlock block)
            {
                bool valid = true;
                valid &= (block.GetProperty("PowerDependency") != null);
                valid &= (block.GetProperty("EffectsCombo") != null);
                return valid;
            }

            /// <summary>
            /// Wraps the given '<see cref="IMyFunctionalBlock"/>' in an '<see cref="ArgExhaustBlock"/>' instance.
            /// </summary>
            /// <param name="exhaustBlock">The exhaust block as an '<see cref="IMyFunctionalBlock"/>'.</param>
            public ArgExhaustBlock(IMyFunctionalBlock exhaustBlock)
            {
                if (!Verify(exhaustBlock))
                {
                    throw new Exception($"{exhaustBlock.CustomName} is not an exhaust pipe!");
                }
                _block = exhaustBlock;
            }
        }

    }
}
