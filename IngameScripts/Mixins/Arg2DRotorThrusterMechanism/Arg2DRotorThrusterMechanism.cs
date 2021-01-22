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
        /// A rotor thruster mechanism able to control rotors and their thrusters using a move indicator or a ship controller as guide.
        /// </summary>
        public class Arg2DRotorThrusterMechanism
        {
            #region Subclasses

            /// <summary>
            /// A rotor (stator) and the thrusters it is intended to rotate to appropriate directions.
            /// </summary>
            public struct RotorThrusterGroup
            {
                /// <summary>
                /// The rotor (stator).
                /// </summary>
                public IMyMotorStator Stator { get; set; }

                /// <summary>
                /// The thrusters the rotor is intended to rotate to appropriate directions.
                /// </summary>
                public List<IMyThrust> Thrusters { get; set; }


                /// <summary>
                /// The angle the rotor should be at default state (for example when the engines are shut down).
                /// </summary>
                public float DefaultAngle { get; set; }

                /// <summary>
                /// Which direction is the ship moving while the rotor is at 0° and the thrusters are firing.
                /// </summary>
                public Base6Directions.Direction HeadingAt0 { get; set; }

                /// <summary>
                /// Which direction is the ship moving while the rotor is at 90° and the thrusters are firing.
                /// </summary>
                public Base6Directions.Direction HeadingAt90 { get; set; }

                /// <summary>
                /// Instantiates a rotor thruster group.
                /// </summary>
                /// <param name="stator">The rotor (stator).</param>
                /// <param name="thrusters">The thrusters the rotor is intended to rotate to appropriate directions.</param>
                /// <param name="defaultAngle">The angle the rotor should be at default state (for example when the engines are shut down).</param>
                /// <param name="headingAt0">Which direction is the ship moving while the rotor is at 0° and the thrusters are firing.</param>
                /// <param name="headingAt90">Which direction is the ship moving while the rotor is at 90° and the thrusters are firing.</param>
                public RotorThrusterGroup(IMyMotorStator stator, List<IMyThrust> thrusters, float defaultAngle, Base6Directions.Direction headingAt0, Base6Directions.Direction headingAt90)
                {
                    Stator = stator;
                    Thrusters = thrusters;
                    DefaultAngle = defaultAngle;
                    HeadingAt0 = headingAt0;
                    HeadingAt90 = headingAt90;
                }
            }

            public class RotorThrusterCollection
            {
                #region Private fields
                private List<RotorThrusterGroup> rotorThrusterGroups;
                private Dictionary<Base6Directions.Direction, int> dirToAngleDict;
                private Vector3 movementIndicatorMask;
                #endregion

                #region Public properties

                public Base6Directions.Direction HeadingAt0 { get; private set; }
                public Base6Directions.Direction HeadingAt90 { get; private set; }

                /// <summary>
                /// Get the axis these rotor thruster groups are rotating on.
                /// </summary>
                public Base6Directions.Axis Axis { get; private set; }
                #endregion

                #region Private methods
                bool GetNeighbouringDirections(Vector3 moveIndicator, out Base6Directions.Direction dir1, out Base6Directions.Direction dir2)
                {
                    bool found = false;
                    dir1 = default(Base6Directions.Direction);
                    dir2 = default(Base6Directions.Direction);
                    foreach (var d1 in dirToAngleDict.Keys)
                    {
                        foreach (var d2 in dirToAngleDict.Keys)
                        {
                            if (d1 == d2 || Base6Directions.GetOppositeDirection(d1) == d2)
                            {
                                continue;
                            }
                            var v1 = Base6Directions.GetVector(d1);
                            var v2 = Base6Directions.GetVector(d2);
                            var V = v1 + v2;

                            if (V == moveIndicator)
                            {
                                found = true;
                                dir1 = d1;
                                dir2 = d2;
                                break;
                            }

                        }
                    }
                    return found;
                }

                bool IsBase6Direction(Vector3 moveIndicator)
                {
                    return Vector3.Abs(moveIndicator).Sum == 1;
                }

                Vector3 VelocityToIndicator(Vector3 velocity)
                {
                    if (Math.Abs(velocity.X) > 0)
                    {
                        velocity.X /= Math.Abs(velocity.X);
                    }
                    else
                    {
                        velocity.X = 0;
                    }

                    if (Math.Abs(velocity.Y) > 0)
                    {
                        velocity.Y /= Math.Abs(velocity.Y);
                    }
                    else
                    {
                        velocity.Y = 0;
                    }

                    if (Math.Abs(velocity.Z) > 0)
                    {
                        velocity.Z /= Math.Abs(velocity.Z);
                    }
                    else
                    {
                        velocity.Z = 0;
                    }

                    return velocity;
                }

                Vector3 ApplyVelocityThreshold(Vector3 velocity, float threshold)
                {
                    if (Math.Abs(velocity.X) < threshold)
                    {
                        velocity.X = 0;
                    }
                    if (Math.Abs(velocity.Y) < threshold)
                    {
                        velocity.Y = 0;
                    }
                    if (Math.Abs(velocity.Z) < threshold)
                    {
                        velocity.Z = 0;
                    }
                    return velocity;
                }

                #endregion

                #region Public methods

                public bool Add(RotorThrusterGroup rtg)
                {
                    if (rtg.HeadingAt0 != HeadingAt0 || rtg.HeadingAt90 != HeadingAt90 || rotorThrusterGroups.Contains(rtg))
                    {
                        return false;
                    }
                    rotorThrusterGroups.Add(rtg);
                    return false;
                }

                public bool Remove(RotorThrusterGroup rtg)
                {
                    return rotorThrusterGroups.Remove(rtg);
                }

                public void Clear()
                {
                    rotorThrusterGroups.Clear();
                }

                void InitializeDictionary()
                {
                    dirToAngleDict = new Dictionary<Base6Directions.Direction, int>()
                    {
                        {HeadingAt0,0},
                        {HeadingAt90,90},
                        {Base6Directions.GetOppositeDirection(HeadingAt0), 180},
                        {Base6Directions.GetOppositeDirection(HeadingAt90), 270}
                    };
                }

                /// <summary>
                /// Overrides the thrusters on the given rotor thruster group.
                /// </summary>
                /// <param name="rotorThrusterGroup">The rotor thruster group with the thrusters to override.</param>
                /// <param name="powerOverridePercentage">The value (at or between 0 and 1) the thrusters should be overridden with.</param>
                /// <param name="rotateToDefaultOnZero">If set to true, the rotor will be rotated to the default angle when the override percentage is 0.</param>
                public static void OverrideRotorThrusterGroupThrusters(RotorThrusterGroup rotorThrusterGroup, float powerOverridePercentage, bool shareInertiaTensor, bool rotateToDefaultOnZero = false)
                {
                    if (powerOverridePercentage < 0)
                    {
                        powerOverridePercentage = 0;
                    }
                    else if (powerOverridePercentage > 1)
                    {
                        powerOverridePercentage = 1;
                    }

                    foreach (var thruster in rotorThrusterGroup.Thrusters)
                    {
                        thruster.ThrustOverridePercentage = powerOverridePercentage;
                    }

                    if (powerOverridePercentage == 0 && rotateToDefaultOnZero)
                    {
                        rotorThrusterGroup.Stator.RotateToAngle(shareInertiaTensor,rotorThrusterGroup.DefaultAngle);
                    }
                }

                /// <summary>
                /// Gets the axis the given 2 directions are not part of.
                /// </summary>
                public static Base6Directions.Axis GetAxis(Base6Directions.Direction dir1, Base6Directions.Direction dir2)
                {
                    Base6Directions.Axis axis1 = Base6Directions.GetAxis(dir1);
                    Base6Directions.Axis axis2 = Base6Directions.GetAxis(dir2);

                    if (axis1 == axis2)
                    {
                        throw new Exception("Can not get axis from 2 directions on the same axis.");
                    }

                    if (axis1 == Base6Directions.Axis.ForwardBackward)
                    {
                        if (axis2 == Base6Directions.Axis.LeftRight)
                        {
                            return Base6Directions.Axis.UpDown;
                        }
                        else
                        {
                            return Base6Directions.Axis.LeftRight;
                        }
                    }
                    else if (axis1 == Base6Directions.Axis.LeftRight)
                    {
                        if (axis2 == Base6Directions.Axis.ForwardBackward)
                        {
                            return Base6Directions.Axis.UpDown;
                        }
                        else
                        {
                            return Base6Directions.Axis.ForwardBackward;
                        }
                    }
                    else
                    {
                        if (axis2 == Base6Directions.Axis.ForwardBackward)
                        {
                            return Base6Directions.Axis.LeftRight;
                        }
                        else
                        {
                            return Base6Directions.Axis.ForwardBackward;
                        }
                    }
                }

                /// <summary>
                /// Overrides all thrusters.
                /// </summary>
                /// <param name="powerOverridePercentage">The value (at or between 0 and 1) the thrusters should be overridden with.</param>
                /// <param name="rotateToDefaultOnZero">If set to true, the rotor will be rotated to the default angle when the override percentage is 0.</param>
                /// <param name="shareInertiaTensors">Whether or not to share inertia tensors between rotations.</param>
                public void OverrideAllThrusters(float powerOverridePercentage, bool rotateToDefaultOnZero, bool shareInertiaTensors)
                {
                    foreach (var RTG in rotorThrusterGroups)
                    {
                        OverrideRotorThrusterGroupThrusters(RTG, powerOverridePercentage, shareInertiaTensors);
                        if (powerOverridePercentage == 0 && rotateToDefaultOnZero)
                        {
                            RTG.Stator.RotateToAngle(shareInertiaTensors, RTG.DefaultAngle);
                        }
                    }
                }

                /// <summary>
                /// Rotates the thrusters to the appropriate direction and engages them.
                /// </summary>
                /// <param name="movementIndicator">The movement indicator vector.</param>
                /// <param name="thrustOverrideMultiplier">Multiplies the thrust override percentage by this amount. Must be between 0 and 1.</param>
                /// <param name="rotateToDefaultWhenUnused">If set to true, the rotors will be rotated to their default angles, when the thrusters are unused.</param>
                /// <param name="shareInertiaTensor">Whether or not to share inertia tensor on controlled rotors between rotations.<para>Inertia tensor sharing is disabled on rotors during rotations for safety reasons.</para></param>
                public void Thrust(Vector3 movementIndicator, float thrustOverrideMultiplier = 1, bool rotateToDefaultWhenUnused = false, bool shareInertiaTensor = false)
                {
                    movementIndicator *= movementIndicatorMask;

                    if (movementIndicator == Vector3.Zero)
                    {
                        OverrideAllThrusters(0, rotateToDefaultWhenUnused, shareInertiaTensor);
                        return;
                    }

                    float targetAngle;
                    if (IsBase6Direction(movementIndicator))
                    {
                        Base6Directions.Direction targetDirection = Base6Directions.GetDirection(movementIndicator);
                        if (Base6Directions.GetAxis(targetDirection) == Axis)
                        {
                            OverrideAllThrusters(0, rotateToDefaultWhenUnused, shareInertiaTensor);
                            return;
                        }
                        targetAngle = dirToAngleDict[targetDirection];
                        foreach (var RTG in rotorThrusterGroups)
                        {
                            RTG.Stator.RotateToAngle(shareInertiaTensor, targetAngle, false);
                        }
                    }
                    else
                    {
                        Base6Directions.Direction d1, d2;
                        if (!GetNeighbouringDirections(movementIndicator, out d1, out d2))
                        {
                            OverrideAllThrusters(0, rotateToDefaultWhenUnused, shareInertiaTensor);
                            return;
                        }
                        int angle1 = dirToAngleDict[d1];
                        int angle2 = dirToAngleDict[d2];

                        if (angle1 == 270 && angle2 == 0)
                        {
                            angle2 = 360;
                        }
                        else if (angle2 == 270 && angle1 == 0)
                        {
                            angle1 = 360;
                        }


                        int lower;
                        if (angle2 > angle1)
                        {
                            lower = angle1;
                        }
                        else
                        {
                            lower = angle2;
                        }

                        targetAngle = lower + 45;

                        foreach (var RTG in rotorThrusterGroups)
                        {
                            RTG.Stator.RotateToAngle(shareInertiaTensor, targetAngle, false);
                        }
                    }

                    float differencePercentage;
                    float statorAngle;
                    foreach (var RTG in rotorThrusterGroups)
                    {
                        targetAngle %= 360;
                        statorAngle = ((RTG.Stator.Angle * 180 / (float)Math.PI) + 360) % 360;
                        differencePercentage = Math.Abs(1 - ((float)Math.Abs(targetAngle - statorAngle) / 180)); // Needs abs, because if rotor is at 359 and target is at 0, this results in negative value.
                        differencePercentage *= Math.Abs(thrustOverrideMultiplier);
                        OverrideRotorThrusterGroupThrusters(RTG, differencePercentage, rotateToDefaultWhenUnused);
                    }
                }

                /// <summary>
                /// Rotates the thrusters to the appropriate weighted direction and engages them.
                /// </summary>
                /// <param name="movementIndicator">The movement indicator vector.</param>
                /// <param name="axisWeights">The weights for each axis. The more weight an indicated direction's axis has over another's, the more the thrusters will be rotated towards it rather than the other when both directions are indicated. Components are treated as absolute values.</param>
                /// <param name="thrustOverrideMultiplier">Multiplies the thrust override percentage by this amount. Must be between 0 and 1.</param>
                /// <param name="rotateToDefaultWhenUnused">If set to true, the rotors will be rotated to their default angles, when the thrusters are unused.</param>
                /// <param name="shareInertiaTensor">Whether or not to share inertia tensor on controlled rotors between rotations.<para>Inertia tensor sharing is disabled on rotors during rotations for safety reasons.</para></param>
                public void Thrust(Vector3 movementIndicator, Vector3 axisWeights, float thrustOverrideMultiplier = 1, bool rotateToDefaultWhenUnused = false, bool shareInertiaTensor = false)
                {
                    movementIndicator *= movementIndicatorMask;

                    if (Vector3.IsZero(movementIndicator))
                    {
                        OverrideAllThrusters(0, rotateToDefaultWhenUnused, shareInertiaTensor);
                        return;
                    }

                    float targetAngle;
                    if (IsBase6Direction(movementIndicator))
                    {
                        Base6Directions.Direction targetDirection = Base6Directions.GetDirection(movementIndicator);

                        if (Base6Directions.GetAxis(targetDirection) == Axis)
                        {
                            OverrideAllThrusters(0, rotateToDefaultWhenUnused, shareInertiaTensor);
                            return;
                        }
                        targetAngle = dirToAngleDict[targetDirection];
                        foreach (var RTG in rotorThrusterGroups)
                        {
                            RTG.Stator.RotateToAngle(shareInertiaTensor, targetAngle, false, 5f,15f);
                        }
                    }
                    else
                    {
                        Base6Directions.Direction d1, d2;

                        if (!GetNeighbouringDirections(movementIndicator, out d1, out d2))
                        {
                            OverrideAllThrusters(0, rotateToDefaultWhenUnused, shareInertiaTensor);
                            return;
                        }

                        int angle1 = dirToAngleDict[d1];
                        int angle2 = dirToAngleDict[d2];

                        if (angle1 == 270 && angle2 == 0)
                        {
                            angle2 = 360;
                        }
                        else if (angle2 == 270 && angle1 == 0)
                        {
                            angle1 = 360;
                        }

                        float desire1, desire2;
                        desire1 = Math.Abs((Vector3.Abs(Base6Directions.GetVector(d1)) * axisWeights).Sum);
                        desire2 = Math.Abs((Vector3.Abs(Base6Directions.GetVector(d2)) * axisWeights).Sum);

                        Base6Directions.Direction desiredMoreDir, desiredLessDir;
                        float desiredMoreAngle, desiredLessAngle;
                        float R;

                        if (desire1 > desire2)
                        {
                            desiredMoreDir = d1;
                            desiredMoreAngle = angle1;

                            desiredLessDir = d2;
                            desiredLessAngle = angle2;

                            R = desire2 / desire1;
                        }
                        else if ((desire1 > desire2))
                        {
                            R = desire1 / desire2;

                            desiredMoreAngle = angle2;
                            desiredMoreDir = d2;

                            desiredLessAngle = angle1;
                            desiredLessDir = d1;
                        }
                        else
                        {
                            R = 0.5f;

                            desiredMoreDir = d2;
                            desiredMoreAngle = angle2;

                            desiredLessAngle = angle1;
                            desiredLessDir = d1;
                        }

                        if (desiredMoreAngle > desiredLessAngle)
                        {
                            targetAngle = desiredMoreAngle - (90 * R);
                        }
                        else
                        {
                            targetAngle = desiredMoreAngle + (90 * R);
                        }



                        foreach (var RTG in rotorThrusterGroups)
                        {
                            RTG.Stator.RotateToAngle(shareInertiaTensor, targetAngle, false, 5f, 15f);
                        }
                    }

                    float differencePercentage;
                    float statorAngle;
                    foreach (var RTG in rotorThrusterGroups)
                    {
                        targetAngle %= 360;
                        statorAngle = ((RTG.Stator.Angle * 180 / (float)Math.PI) + 360) % 360;
                        differencePercentage = Math.Abs(1 - ((float)Math.Abs(targetAngle - statorAngle) / 180)); // Needs abs, because if rotor is at 359 and target is at 0, this results in negative value.
                        differencePercentage *= Math.Abs(thrustOverrideMultiplier);
                        OverrideRotorThrusterGroupThrusters(RTG, differencePercentage, rotateToDefaultWhenUnused);
                    }
                }

                /// <summary>
                /// Rotates and fires the thrusters appropriately using the provided ship controller as a guide.
                /// </summary>
                /// <param name="shipController">The ship controller used as a guide.</param>
                /// <param name="minOverridePercentage">The minimum thrust override percentage (between 0 and 1). When thrusters are fired, they will be fired with strength equivalent to this value at minimum.</param>
                /// <param name="thrustOverrideMultiplier">Multiplies the thrust override percentage by this amount. Must be between 0 and 1.</param>
                /// <param name="gradualVelocityThreshold">Thrusters strength will be proportional to (ship speed / this value) while ship speed is below this value.</param>
                /// <param name="useInertialDampeners">If set to true, thrusters will use dampener functionality, if it is enabled on the controller. If set to false, thrusters will always thrust only in the desired direction.</param>
                /// <param name="dampenerVelocityThreshold">Dampeners will not be triggered under this velocity.</param>
                /// <param name="rotateToDefaultWhenUnused">If set to true, the rotors will be rotated to their default angles, when the thrusters are unused.</param>
                /// <param name="shareInertiaTensor">Whether or not to share inertia tensor on controlled rotors between rotations.<para>Inertia tensor sharing is disabled on rotors during rotations for safety reasons.</para></param>
                public void Thrust(IMyShipController shipController, float thrustOverrideMultiplier = 1, float minOverridePercentage = 0.1f, float gradualVelocityThreshold = 15f, bool useInertialDampeners = true, float dampenerVelocityThreshold = 5f, bool rotateToDefaultWhenUnused = false, bool shareInertiaTensor = false)
                {
                    float shipSpeed = (float)shipController.GetShipSpeed();

                    float gradualMultiplier = 1;
                    if (gradualVelocityThreshold > shipSpeed)
                    {
                        gradualMultiplier = shipSpeed / gradualVelocityThreshold;
                    }
                    if (gradualMultiplier < minOverridePercentage)
                    {
                        gradualMultiplier = minOverridePercentage;
                    }

                    if (shipController.DampenersOverride && useInertialDampeners && shipSpeed > dampenerVelocityThreshold)
                    {
                        Vector3 desiredVelocityIndicator = shipController.MoveIndicator;

                        Vector3 localVelocities = shipController.GetLocalLinearVelocities();
                        localVelocities = ApplyVelocityThreshold(localVelocities, dampenerVelocityThreshold);

                        Vector3 localVelocityIndicator = VelocityToIndicator(localVelocities);

                        Vector3 correctionIndicator = -(localVelocityIndicator - Vector3.Abs(desiredVelocityIndicator) * localVelocityIndicator);
                        Vector3 correctionWeight = Vector3.Abs((localVelocities / (shipSpeed + 1)) * correctionIndicator);

                        Vector3 correctedDesiredIndicator = desiredVelocityIndicator + correctionIndicator;
                        Vector3 correctedDesiredWeight = desiredVelocityIndicator + correctionWeight;

                        Thrust(correctedDesiredIndicator, correctedDesiredWeight, thrustOverrideMultiplier * gradualMultiplier, rotateToDefaultWhenUnused, shareInertiaTensor);

                    }
                    else
                    {
                        Thrust(shipController.MoveIndicator, thrustOverrideMultiplier * gradualMultiplier, rotateToDefaultWhenUnused, shareInertiaTensor);
                    }
                }

                #endregion

                public RotorThrusterCollection(Base6Directions.Direction headingAt0, Base6Directions.Direction headingAt90)
                {
                    rotorThrusterGroups = new List<RotorThrusterGroup>();

                    HeadingAt0 = headingAt0;
                    HeadingAt90 = headingAt90;

                    InitializeDictionary();

                    Axis = GetAxis(headingAt0, headingAt90);
                    switch (Axis)
                    {
                        case Base6Directions.Axis.ForwardBackward:
                            movementIndicatorMask = new Vector3(1, 1, 0);
                            break;
                        case Base6Directions.Axis.LeftRight:
                            movementIndicatorMask = new Vector3(0, 1, 1);
                            break;
                        case Base6Directions.Axis.UpDown:
                            movementIndicatorMask = new Vector3(1, 0, 1);
                            break;
                        default:
                            break;
                    }
                }
            }

            #endregion

            #region Private fields

            List<RotorThrusterCollection> rotorThrusterCollections;
            private bool forceEnableDampeners;
            private bool forceDisableDampeners;
            private float thrustStrengthMultiplier;
            private float thrustStrengthMin;

            #endregion

            /// <summary>
            /// If set to true, the rotors will be rotated to their default angles, when the thrusters are unused.
            /// </summary>
            public bool RotateToDefaultWhenUnused { get; set; }

            /// <summary>
            /// Whether or not to share inertia tensor on controlled rotors between rotations.
            /// </summary>
            /// <remarks>
            /// Inertia tensor sharing is disabled on rotors during rotations for safety reasons.
            /// </remarks>
            public bool ShareInertiaTensor { get; set; }

            /// <summary>
            /// Whether or not to always use inertia dampeners.
            /// </summary>
            public bool ForceEnableDampeners
            {
                get
                {
                    return forceEnableDampeners;
                }
                set
                {
                    forceEnableDampeners = value;
                    if (forceEnableDampeners)
                    {
                        forceDisableDampeners = false;
                    }
                }
            }

            /// <summary>
            /// Whether or not to never use inertia dampeners.
            /// </summary>
            public bool ForceDisableDampeners
            {
                get
                {
                    return forceDisableDampeners;
                }
                set
                {
                    forceDisableDampeners = value;
                    if (forceDisableDampeners)
                    {
                        forceEnableDampeners = false;
                    }
                }
            }

            /// <summary>
            /// Multiplies the thrust override percentages by this amount. Must be between 0 and 1.
            /// </summary>
            public float ThrustStrengthMultiplier
            {
                get
                {
                    return thrustStrengthMultiplier;
                }
                set
                {
                    if (value < 0)
                    {
                        value = 0;
                    }
                    if (value > 1)
                    {
                        value = 1;
                    }
                    thrustStrengthMultiplier = value;
                }
            }

            /// <summary>
            /// Thrusters strength will be proportional to (ship speed / this value) while ship speed is below this value.
            /// </summary>
            public float ProportionalVelocityThreshold { get; set; }

            /// <summary>
            /// The minimum thrust override percentage (between 0 and 1). When thrusters are fired, they will be fired with strength equivalent to this value at minimum.
            /// </summary>
            public float ThrustStrengthMin
            {
                get
                {
                    return thrustStrengthMin;
                }
                set
                {
                    if (value<0)
                    {
                        value = 0;
                    }
                    if (value>1)
                    {
                        value = 1;
                    }
                    thrustStrengthMin = value;
                }
            }

            /// <summary>
            /// Inertia dampeners will be disabled while ship velocity is below this value.
            /// </summary>
            public float DampenerActivationVelocityThreshold { get; set; }

            /// <summary>
            /// Instantiates a rotor thruster 2D system.
            /// </summary>
            /// <param name="rotorThrusterGroups"></param>
            /// <param name="dampenerActivationVelocityThreshold">Inertia dampeners will be disabled while ship velocity is below this value.</param>
            /// <param name="proportionalVelocityThreshold">Thrusters strength will be proportional to (ship speed / this value) while ship speed is below this value.</param>
            /// <param name="thrustStrengthPercentageMin">The minimum thrust override percentage (between 0 and 1). When thrusters are fired, they will be fired with strength equivalent to this value at minimum.</param>
            /// <param name="thrustStrengthMultiplier">Multiplies the thrust override percentages by this amount. Must be between 0 and 1.</param>
            /// <param name="rotateToDefaultWhenUnused">Whether or not to rotate thrusters to default angle if they are unused.</param>
            /// <param name="shareInertiaTensors">Whether or not to share inertia tensors on rotors while not rotating.<para>Sharing inertia tensor on a rotor is always disabled while the rotor is rotating for safety reasons.</para></param>
            public Arg2DRotorThrusterMechanism(List<RotorThrusterGroup> rotorThrusterGroups, float dampenerActivationVelocityThreshold=5, float proportionalVelocityThreshold = 0, float thrustStrengthPercentageMin=0.1f, float thrustStrengthMultiplier=1, bool rotateToDefaultWhenUnused = false, bool shareInertiaTensors = false)
            {
                DampenerActivationVelocityThreshold = dampenerActivationVelocityThreshold;
                ProportionalVelocityThreshold = proportionalVelocityThreshold;
                ThrustStrengthMin = thrustStrengthPercentageMin;
                ThrustStrengthMultiplier = thrustStrengthMultiplier;
                ShareInertiaTensor = shareInertiaTensors;
                RotateToDefaultWhenUnused = rotateToDefaultWhenUnused;
                rotorThrusterCollections = new List<RotorThrusterCollection>();
                foreach (var rtg in rotorThrusterGroups)
                {
                    AddRotorThrusterGroup(rtg);
                }
            }

            #region Public methods

            /// <summary>
            /// Attempts to add a rotor thruster group to this system and returns whether or not it could be added.
            /// </summary>
            public bool AddRotorThrusterGroup(RotorThrusterGroup rtg)
            {
                var rtgcWithSameDirections = rotorThrusterCollections.FirstOrDefault(r=>r.HeadingAt0==rtg.HeadingAt0 && r.HeadingAt90==rtg.HeadingAt90);
                if (rtgcWithSameDirections != default(RotorThrusterCollection))
                {
                    return rtgcWithSameDirections.Add(rtg);
                }
                else
                {
                    rtgcWithSameDirections = new RotorThrusterCollection(rtg.HeadingAt0, rtg.HeadingAt90);
                    rotorThrusterCollections.Add(rtgcWithSameDirections);
                    return rtgcWithSameDirections.Add(rtg);
                }
            }

            /// <summary>
            /// Attempts to remove a rotor thruster group from this system and returns whether or not it could be removed.
            /// </summary>
            /// <remarks>
            /// This also returns false when the rotor thruster group could not be found in this system.
            /// </remarks>
            public bool RemoveRotorThrusterGroup(RotorThrusterGroup rtg)
            {
                bool success = false;
                foreach (var rtgc in rotorThrusterCollections)
                {
                    success |= rtgc.Remove(rtg);
                    if (success)
                    {
                        break;
                    }
                }
                return success;
            }

            /// <summary>
            /// Removes all rotor thruster groups from this system.
            /// </summary>
            public void ClearRotorThrusterGroups()
            {
                foreach (var rtgc in rotorThrusterCollections)
                {
                    rtgc.Clear();
                }
                rotorThrusterCollections.Clear();
            }

            /// <summary>
            /// Overrides all thrusters.
            /// </summary>
            /// <param name="powerOverridePercentage">The value (at or between 0 and 1) the thrusters should be overridden with.</param>
            public void OverrideAllThrusters(float powerOverridePercentage)
            {
                foreach (var rt in rotorThrusterCollections)
                {
                    rt.OverrideAllThrusters(powerOverridePercentage * ThrustStrengthMultiplier, RotateToDefaultWhenUnused, ShareInertiaTensor);
                }
            }

            /// <summary>
            /// Rotates the thrusters to the appropriate direction and engages them.
            /// </summary>
            /// <param name="movementIndicator">The movement indicator vector.</param>
            public void Thrust(Vector3 movementIndicator)
            {
                foreach (var rt in rotorThrusterCollections)
                {
                    rt.Thrust(movementIndicator, ThrustStrengthMultiplier, RotateToDefaultWhenUnused, ShareInertiaTensor);
                }
            }

            /// <summary>
            /// Rotates the thrusters to the appropriate weighted direction and engages them.
            /// </summary>
            /// <param name="movementIndicator">The movement indicator vector.</param>
            /// <param name="axisWeights">The weights for each axis. The more weight an indicated direction's axis has over another's, the more the thrusters will be rotated towards it rather than the other when both directions are indicated. Components are treated as absolute values.</param>
            public void Thrust(Vector3 movementIndicator, Vector3 axisWeights)
            {
                foreach (var rt in rotorThrusterCollections)
                {
                    rt.Thrust(movementIndicator, axisWeights, ThrustStrengthMultiplier,RotateToDefaultWhenUnused, ShareInertiaTensor);
                }
            }

            /// <summary>
            /// Rotates and fires the thrusters appropriately using the provided ship controller as a guide.
            /// </summary>
            /// <param name="shipController">The ship controller used as a guide.</param>
            public void Thrust(IMyShipController shipController)
            {
                foreach (var rt in rotorThrusterCollections)
                {
                    rt.Thrust(shipController, ThrustStrengthMultiplier, ThrustStrengthMin, ProportionalVelocityThreshold, !ForceDisableDampeners && (shipController.DampenersOverride || ForceEnableDampeners), DampenerActivationVelocityThreshold,RotateToDefaultWhenUnused, ShareInertiaTensor);
                }
            }

            #endregion

        }
    }
}
