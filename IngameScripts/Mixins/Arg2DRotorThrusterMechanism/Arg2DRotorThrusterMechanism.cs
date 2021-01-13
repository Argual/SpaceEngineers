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
        /// <para>All controlled rotors must rotate their thrusters in the same directions and to the same angle to achieve a direction.</para>
        /// <para>At least two of these are required to build a system for full 2D rotor thrust control.</para>
        /// </summary>
        public class Arg2DRotorThrusterMechanism
        {
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

            /// <summary>
            /// Get the axis this rotor thruster is rotating on.
            /// </summary>
            public Base6Directions.Axis Axis { get { return axis; } }

            /// <summary>
            /// If set to true, the rotors will be rotated to their default angles, when the thrusters are unused.
            /// </summary>
            public bool RotateToDefaultWhenUnused { get; set; }

            /// <summary>
            /// Whether or not to share inertia tensor on controlled rotors between rotations.
            /// </summary>
            /// <remarks>
            /// Inertia tensor sharing is disabled on rotors during rotations for safety reasons. All hail the almighty Clang!
            /// </remarks>
            public bool ShareInertiaTensor { get; set; }

            List<RotorThrusterGroup> rotorThrusterGroups;

            Dictionary<Base6Directions.Direction, int> dirToAngleDict;

            Base6Directions.Axis axis;

            Vector3 movementIndicatorMask;

            /// <summary>
            /// Instantiates a rotor thruster 2D system.
            /// </summary>
            /// <param name="rotorThrusterGroups">The rotor and the thrusters it rotates.</param>
            /// <param name="headingAt0">Which direction is the ship moving while the rotor is at 0° and the thrusters are firing.</param>
            /// <param name="headingAt90">Which direction is the ship moving while the rotor is at 90° and the thrusters are firing.</param>
            /// <param name="rotateToDefaultWhenUnused">Whether or not to rotate thrusters to default angle if they are unused.</param>
            /// <param name="shareInertiaTensors">Whether or not to share inertia tensors on rotors while not rotating.<para>Sharing inertia tensor on a rotor is always disabled while the rotor is rotating for safety reasons.</para></param>
            public Arg2DRotorThrusterMechanism(List<RotorThrusterGroup> rotorThrusterGroups, Base6Directions.Direction headingAt0, Base6Directions.Direction headingAt90, bool rotateToDefaultWhenUnused=false, bool shareInertiaTensors = false)
            {
                ShareInertiaTensor = shareInertiaTensors;
                RotateToDefaultWhenUnused = rotateToDefaultWhenUnused;

                this.rotorThrusterGroups = rotorThrusterGroups;
                InitializeDictionaries(headingAt0, headingAt90);
                axis = GetAxis(headingAt0, headingAt90);
                switch (axis)
                {
                    case Base6Directions.Axis.ForwardBackward:
                        movementIndicatorMask = new Vector3(1,1,0);
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

            void InitializeDictionaries(Base6Directions.Direction dirAt0, Base6Directions.Direction dirAt90)
            {
                dirToAngleDict = new Dictionary<Base6Directions.Direction, int>()
                {
                    {dirAt0,0},
                    {dirAt90,90},
                    {Base6Directions.GetOppositeDirection(dirAt0),180},
                    {Base6Directions.GetOppositeDirection(dirAt90), 270}
                };
            }

            #region Helper functions
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

            Base6Directions.Axis GetAxis(Base6Directions.Direction dir1, Base6Directions.Direction dir2)
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
                if (Math.Abs(velocity.X)<threshold)
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

            /// <summary>
            /// Overrides the thrusters on the given rotor thruster group.
            /// </summary>
            /// <param name="rotorThrusterGroup">The rotor thruster group with the thrusters to override.</param>
            /// <param name="powerOverridePercentage">The value (at or between 0 and 1) the thrusters should be overridden with.</param>
            /// <param name="rotateToDefaultOnZero">If set to true, the rotor will be rotated to the default angle when the override percentage is 0.</param>
            public static void OverrideRotorThrusterGroupThrusters(RotorThrusterGroup rotorThrusterGroup,float powerOverridePercentage, bool rotateToDefaultOnZero = false)
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
                    rotorThrusterGroup.Stator.RotateToAngle(rotorThrusterGroup.DefaultAngle);
                }
            }

            /// <summary>
            /// Overrides all thrusters.
            /// </summary>
            /// <param name="powerOverridePercentage">The value (at or between 0 and 1) the thrusters should be overridden with.</param>
            /// <param name="rotateToDefaultOnZero">If set to true, the rotor will be rotated to the default angle when the override percentage is 0.</param>
            public void OverrideAllThrusters(float powerOverridePercentage, bool rotateToDefaultOnZero = false)
            {
                foreach (var RTG in rotorThrusterGroups)
                {
                    OverrideRotorThrusterGroupThrusters(RTG, powerOverridePercentage);
                    if (powerOverridePercentage == 0 && rotateToDefaultOnZero)
                    {
                        RTG.Stator.RotateToAngle(RTG.DefaultAngle);
                    }
                }
            }

            /// <summary>
            /// Rotates the thrusters to the appropriate direction and engages them.
            /// </summary>
            /// <param name="movementIndicator">The movement indicator vector.</param>
            /// <param name="thrustOverrideMultiplier">Multiplies the thrust override percentage by this amount. Must be between 0 and 1.</param>
            public void Thrust(Vector3 movementIndicator, float thrustOverrideMultiplier = 1)
            { 
                movementIndicator *= movementIndicatorMask;

                if (movementIndicator == Vector3.Zero)
                {
                    OverrideAllThrusters(0, RotateToDefaultWhenUnused);
                    return;
                }

                float targetAngle;
                if (IsBase6Direction(movementIndicator))
                {
                    Base6Directions.Direction targetDirection = Base6Directions.GetDirection(movementIndicator);
                    if (Base6Directions.GetAxis(targetDirection) == axis)
                    {
                        OverrideAllThrusters(0, RotateToDefaultWhenUnused);
                        return;
                    }
                    targetAngle = dirToAngleDict[targetDirection];
                    foreach (var RTG in rotorThrusterGroups)
                    {
                        if (ShareInertiaTensor)
                        {
                            RTG.Stator.RotateToAngle(true, targetAngle, false);
                        }
                        else
                        {
                            RTG.Stator.RotateToAngle(false, targetAngle, false);
                        }
                    }
                }
                else
                {
                    Base6Directions.Direction d1, d2;
                    if(!GetNeighbouringDirections(movementIndicator, out d1, out d2))
                    {
                        OverrideAllThrusters(0, RotateToDefaultWhenUnused);
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
                        if (ShareInertiaTensor)
                        {
                            RTG.Stator.RotateToAngle(true, targetAngle, false);
                        }
                        else
                        {
                            RTG.Stator.RotateToAngle(false, targetAngle, false);
                        }
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
                    OverrideRotorThrusterGroupThrusters(RTG, differencePercentage, RotateToDefaultWhenUnused);
                }
            }

            /// <summary>
            /// Rotates the thrusters to the appropriate weighted direction and engages them.
            /// </summary>
            /// <param name="movementIndicator">The movement indicator vector.</param>
            /// <param name="axisWeights">The weights for each axis. The more weight an indicated direction's axis has over another's, the more the thrusters will be rotated towards it rather than the other when both directions are indicated. Components are treated as absolute values.</param>
            /// <param name="thrustOverrideMultiplier">Multiplies the thrust override percentage by this amount. Must be between 0 and 1.</param>
            public void Thrust(Vector3 movementIndicator, Vector3 axisWeights, float thrustOverrideMultiplier = 1)
            {
                movementIndicator *= movementIndicatorMask;

                if (Vector3.IsZero(movementIndicator))
                {
                    OverrideAllThrusters(0, RotateToDefaultWhenUnused);
                    return;
                }

                float targetAngle;
                if (IsBase6Direction(movementIndicator))
                {
                    Base6Directions.Direction targetDirection = Base6Directions.GetDirection(movementIndicator);
                    if (Base6Directions.GetAxis(targetDirection) == axis)
                    {
                        OverrideAllThrusters(0, RotateToDefaultWhenUnused);
                        return;
                    }
                    targetAngle = dirToAngleDict[targetDirection];
                    foreach (var RTG in rotorThrusterGroups)
                    {
                        if (ShareInertiaTensor)
                        {
                            RTG.Stator.RotateToAngle(true, targetAngle, false);
                        }
                        else
                        {
                            RTG.Stator.RotateToAngle(false, targetAngle, false);
                        }
                    }
                }
                else
                {
                    Base6Directions.Direction d1, d2;

                    if (!GetNeighbouringDirections(movementIndicator, out d1, out d2))
                    {
                        OverrideAllThrusters(0, RotateToDefaultWhenUnused);
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
                    else if((desire1 > desire2))
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
                        if (ShareInertiaTensor)
                        {
                            RTG.Stator.RotateToAngle(true, targetAngle, false);
                        }
                        else
                        {
                            RTG.Stator.RotateToAngle(false, targetAngle, false);
                        }
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
                    OverrideRotorThrusterGroupThrusters(RTG, differencePercentage, RotateToDefaultWhenUnused);
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
            public void Thrust(IMyShipController shipController, float thrustOverrideMultiplier = 1, float minOverridePercentage = 0.1f, float gradualVelocityThreshold = 15f, bool useInertialDampeners = true, float dampenerVelocityThreshold = 5f)
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

                if (shipController.DampenersOverride && useInertialDampeners && shipSpeed>dampenerVelocityThreshold)
                {
                    Vector3 desiredVelocityIndicator = shipController.MoveIndicator;

                    Vector3 localVelocities = shipController.GetLocalLinearVelocities();
                    localVelocities = ApplyVelocityThreshold(localVelocities, dampenerVelocityThreshold);

                    Vector3 localVelocityIndicator = VelocityToIndicator(localVelocities);

                    Vector3 correctionIndicator = -(localVelocityIndicator - Vector3.Abs(desiredVelocityIndicator) * localVelocityIndicator);
                    Vector3 correctionWeight = Vector3.Abs((localVelocities / (shipSpeed+1)) * correctionIndicator);

                    Vector3 correctedDesiredIndicator = desiredVelocityIndicator + correctionIndicator;
                    Vector3 correctedDesiredWeight = desiredVelocityIndicator + correctionWeight;

                    Thrust(correctedDesiredIndicator, correctedDesiredWeight, thrustOverrideMultiplier*gradualMultiplier);

                }
                else
                {
                    Thrust(shipController.MoveIndicator, thrustOverrideMultiplier * gradualMultiplier);
                }
            }

        }
    }
}
