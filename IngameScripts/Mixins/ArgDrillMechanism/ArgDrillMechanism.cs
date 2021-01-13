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
        /// A mechanism designed to be able to extend in one direction while drilling.
        /// </summary>
        public class ArgDrillMechanism
        {
            #region Subclasses

            enum DrillMechanismState
            {
                Stopped,
                Drilling,
                Retracting
            }

            #endregion

            #region Public fields

            public List<IMyPistonBase> PistonsExtendingOnAxis { get; set; }


            public List<IMyPistonBase> PistonsRetractingOnAxis { get; set; }


            public List<IMyShipDrill> Drills { get; set; }


            public IMyMotorStator Rotor { get; set; }


            public Action<string> Log { get; set; }


            public float HighestPosition { get; private set; }

            /// <summary>
            /// Determines the tolerable margin for error on position checks.
            /// </summary>
            public float PositionErrorMargin { get; set; }

            public float ExtensionRatio
            {
                get
                {
                    return CurrentPosition / HighestPosition;
                }
            }

            #endregion

            /// <summary>
            /// Creates a new drill mechanism instance.
            /// </summary>
            /// <param name="pistonsExtendingOnAxis">The pistons in the mechanism which extend on the axis to extend the drill.</param>
            /// <param name="pistonsRetractingOnAxis">The pistons in the mechanism which retract on the axis to extend the drill.</param>
            /// <param name="drills">The drills.</param>
            /// <param name="rotor">The rotor that rotates the drill.</param>
            /// <param name="log">Optional method to call with basic status messages.</param>
            public ArgDrillMechanism(List<IMyPistonBase> pistonsExtendingOnAxis, List<IMyPistonBase> pistonsRetractingOnAxis, List<IMyShipDrill> drills, IMyMotorStator rotor, Action<string> log = null)
            {
                Initialize(pistonsExtendingOnAxis, pistonsRetractingOnAxis, drills, rotor, log);
            }

            private void Initialize(List<IMyPistonBase> pistonsExtendingOnAxis, List<IMyPistonBase> pistonsRetractingOnAxis, List<IMyShipDrill> drills, IMyMotorStator rotor, Action<string> log = null)
            {
                Log = log;
                log?.Invoke("Initializing drill mechanism...");

                PistonsExtendingOnAxis = pistonsExtendingOnAxis;
                PistonsRetractingOnAxis = pistonsRetractingOnAxis;
                Drills = drills;
                Rotor = rotor;
                PistonsAxisStepDistance = 0.2f;
                PositionErrorMargin = 0.1f;
                Reinitialize();
            }

            /// <summary>
            /// Resets values to their default state. This should not be used, but it may be helpful for some.
            /// </summary>
            public void Reinitialize()
            {
                lastAngle = Rotor.Angle;

                RecalculatePositions();

                extensionPositionTarget = CurrentPosition;

                state = DrillMechanismState.Stopped;
            }

            void RecalculatePositions()
            {
                float hp = 0;
                float cp = 0;
                foreach (var pistonEx in PistonsExtendingOnAxis)
                {
                    hp += pistonEx.HighestPosition;
                    cp += pistonEx.CurrentPosition;
                }
                foreach (var pistonRe in PistonsRetractingOnAxis)
                {
                    hp += pistonRe.LowestPosition;
                    cp += pistonRe.HighestPosition - pistonRe.CurrentPosition;
                }

                HighestPosition = hp;
                CurrentPosition = cp;
            }

            /// <summary>
            /// Enumerates through all pistons. Do not use this to retrieve count, summarize the counts of the piston lists instead for better performance!
            /// </summary>
            IEnumerable<IMyPistonBase> Pistons
            {
                get
                {
                    foreach (var piston in PistonsExtendingOnAxis)
                    {
                        yield return piston;
                    }
                    foreach (var piston in PistonsRetractingOnAxis)
                    {
                        yield return piston;
                    }
                }
            }


            float lastAngle = 0; // In radians!
            /// <summary>
            /// Returns whether or not the rotor did a full rotation since last called.
            /// </summary>
            bool RotorDidFullRotation()
            {
                float currentAngle = Rotor.Angle;
                bool result = currentAngle < lastAngle;
                lastAngle = currentAngle;
                if (result)
                {
                    Log?.Invoke("Full rotation complete.");
                }
                return result;
            }

            /// <summary>
            /// How many meters should the mechanism pistons extend at each rotation.
            /// </summary>
            public float PistonsAxisStepDistance { get; set; }

            /// <summary>
            /// The minimum step distance per piston.
            /// </summary>
            public float MinStepDistancePerPiston { get; private set; } = 0.0001f;

            public bool IsFullyExtended
            {
                get
                {
                    return HighestPosition - CurrentPosition < PositionErrorMargin;
                }
            }

            float extensionPositionTarget;
            /// <summary>
            /// Extends up to given meters and returns whether or the mechanism is extended fully.
            /// </summary>
            /// <param name="meters">How many meters should the pistons extend.</param>
            /// <returns>Whether or the mechanism is extended fully.</returns>
            public bool ExtendMeters(float meters)
            {
                RecalculatePositions();

                if (IsFullyExtended)
                {
                    Log?.Invoke("Drill is fully extended.");
                    return true;
                }

                if (extensionPositionTarget - CurrentPosition > PositionErrorMargin)
                {
                    Log?.Invoke($"Previous extension target ({extensionPositionTarget}m)has not been reached yet. Current position: {CurrentPosition}m. Skipping extension cycle.");
                    return false;
                }else if(extensionPositionTarget - CurrentPosition < -PositionErrorMargin)
                {
                    extensionPositionTarget = CurrentPosition;
                }
                else
                {
                    extensionPositionTarget += meters;
                }

                int pistonsReCount = PistonsRetractingOnAxis.Count;
                int pistonsExCount = PistonsExtendingOnAxis.Count;
                int pistonsCount = pistonsExCount + pistonsReCount;

                float extDistPerPistonEx = (extensionPositionTarget * ((float)pistonsExCount / pistonsCount)) / pistonsExCount;
                float extDistPerPistonRe = (extensionPositionTarget * ((float)pistonsReCount / pistonsCount)) / pistonsReCount;


                if (extDistPerPistonEx < MinStepDistancePerPiston)
                {
                    extDistPerPistonEx = MinStepDistancePerPiston;
                }

                if (extDistPerPistonRe < MinStepDistancePerPiston)
                {
                    extDistPerPistonRe = MinStepDistancePerPiston;
                }

                bool reachedMax = true;
                float currentExtensionAmount = 0f;
                foreach (var piston in PistonsExtendingOnAxis)
                {
                    float cp = piston.CurrentPosition;
                    piston.MaxLimit = extDistPerPistonEx;
                    piston.MinLimit = extDistPerPistonEx;
                    piston.Velocity = Math.Abs(piston.Velocity);
                    
                    currentExtensionAmount += cp;

                    reachedMax &= cp == piston.HighestPosition;
                }
                foreach (var piston in PistonsRetractingOnAxis)
                {
                    float hp = piston.HighestPosition;
                    piston.MinLimit = hp - extDistPerPistonRe;
                    piston.MaxLimit = hp - extDistPerPistonRe;
                    piston.Velocity = -Math.Abs(piston.Velocity);

                    float cp = piston.CurrentPosition;
                    currentExtensionAmount += piston.HighestPosition - cp;

                    reachedMax &= cp == piston.LowestPosition;
                }

                Log?.Invoke($"Extending drill mechanism by {meters} meters. Current extension ratio: {Math.Round(ExtensionRatio * 100, 2)}%");
                if (extensionPositionTarget > HighestPosition)
                {
                    extensionPositionTarget = HighestPosition;
                }
                return reachedMax;
            }

            public bool IsFullyRetracted
            {
                get
                {
                    return CurrentPosition < PositionErrorMargin;
                }
            }

            /// <summary>
            /// Retracts the drill mechanism and returns whether or not it is retracted fully.
            /// </summary>
            public bool Retract()
            {
                extensionPositionTarget = 0;
                if (state == DrillMechanismState.Stopped)
                {
                    foreach (var piston in Pistons)
                    {
                        piston.Enabled = true;
                    }
                }

                if (IsFullyRetracted)
                {
                    Log?.Invoke("Drill is fully retracted.");
                    CurrentPosition = 0;
                    return true;
                }

                state = DrillMechanismState.Retracting;

                bool reachedMin = true;
                float currentExtensionAmount = 0f;
                foreach (var piston in PistonsExtendingOnAxis)
                {
                    float lp = piston.LowestPosition;
                    piston.MinLimit = lp;
                    piston.MaxLimit = lp;
                    piston.Velocity = -Math.Abs(piston.Velocity);

                    float cp = piston.CurrentPosition;
                    currentExtensionAmount += cp;

                    reachedMin &= cp == lp;
                }
                foreach (var piston in PistonsRetractingOnAxis)
                {
                    float hp = piston.HighestPosition;
                    piston.MinLimit = hp;
                    piston.MaxLimit = hp;
                    piston.Velocity = Math.Abs(piston.Velocity);

                    float cp = piston.CurrentPosition;
                    currentExtensionAmount += hp - cp;

                    reachedMin &= cp == hp;
                }
                CurrentPosition = currentExtensionAmount;

                Log?.Invoke($"Retracting drill mechanism.  Current extension ratio: {Math.Round(ExtensionRatio, 4) * 100}%");
                return reachedMin;
            }

            public void Stop()
            {
                foreach (var piston in Pistons)
                {
                    piston.Enabled = false;
                }
                Rotor.Enabled = false;
                foreach (var drill in Drills)
                {
                    drill.Enabled = false;
                }
                state = DrillMechanismState.Stopped;
            }

            /// <summary>
            /// Starts the mechanism. Warning: This will not check if pistons are correcty initialied. Use '<see cref="ArgDrillMechanism.StartUp"/>' for full startup.
            /// </summary>
            public void Start()
            {
                foreach (var piston in Pistons)
                {
                    piston.Enabled = true;
                }
                Rotor.Enabled = true;
                foreach (var drill in Drills)
                {
                    drill.Enabled = true;
                }
                state = DrillMechanismState.Drilling;
            }

            /// <summary>
            /// Starts up the drill mechanism. Ensure that the mechanism is properly shut down and ready for startup before this is called.
            /// </summary>
            public void StartUp()
            {
                foreach (var piston in PistonsExtendingOnAxis)
                {
                    piston.MaxLimit = piston.LowestPosition;
                    piston.MinLimit = piston.LowestPosition;
                    piston.Velocity = Math.Abs(piston.Velocity);
                }
                foreach (var piston in PistonsRetractingOnAxis)
                {
                    piston.MaxLimit = piston.HighestPosition;
                    piston.MinLimit = piston.HighestPosition;
                    piston.Velocity = -Math.Abs(piston.Velocity);
                }
                Start();
            }

            DrillMechanismState state;
            /// <summary>
            /// Does a cycle according to the mechanism's current state. Call this as often as you want the mechanism to update.
            /// </summary>
            public void DoCycle()
            {
                if (state == DrillMechanismState.Drilling && RotorDidFullRotation())
                {
                    if (ExtendMeters(PistonsAxisStepDistance))
                    {
                        Stop();
                    }
                }
                else if (state == DrillMechanismState.Retracting)
                {
                    if (Retract())
                    {
                        Stop();
                    }
                }
                else if (state == DrillMechanismState.Stopped)
                {
                    Log?.Invoke("Drill mechanism is currently stopped.");
                }
            }



            /// <summary>
            /// Use this sparingly, as this is not performace friendly!
            /// </summary>
            public float DrillInventoryFillRatio
            {
                get
                {
                    int allDrillVolumeMax = Drills.Sum(d => d.GetInventory(0).MaxVolume.ToIntSafe());
                    int allDrillVolumeCurrent = Drills.Sum(d => d.GetInventory(0).CurrentVolume.ToIntSafe());
                    return allDrillVolumeCurrent / (float)allDrillVolumeMax;
                }
            }

            /// <summary>
            /// Returns the last calculated position of the drill mechanism.
            /// </summary>
            /// <remarks>
            /// This is less accurate than using '<see cref="GetAccurateCurrentPosition"/>', but this is also faster and results in lower instruction count.
            /// </remarks>
            public float CurrentPosition
            {
                get; private set;
            }

            /// <summary>
            /// Returns the accurate position of the drill mechanism.
            /// </summary>
            /// <remarks>
            /// This is more accurate than using '<see cref="CurrentPosition"/>', but this is also slower increases the instruction count more.
            /// </remarks>
            public float GetAccurateCurrentPosition()
            {
                float cp = 0;
                foreach (var piston in PistonsExtendingOnAxis)
                {
                    cp += piston.CurrentPosition;
                }
                foreach (var piston in PistonsRetractingOnAxis)
                {
                    cp += (piston.HighestPosition-piston.CurrentPosition);
                }
                CurrentPosition = cp;
                return cp;
            }

        }

    }
}
