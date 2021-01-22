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

    public static class ArgRotorExtensions
    {

        /// <summary>
        /// Rotates the rotor to the given angle. Does not respect limits set on the rotor, and will overwrite them!
        /// </summary>
        /// <remarks>
        /// This method will not always rotate in the shortest direction, as that would introduce severe drawbacks due to limitations on '<see cref="IMyMotorStator"/>'.
        /// See '<see cref="RotateToAngleShortest(IMyMotorStator, float, bool, float)"/>', if you want to rotate using the shortest distance anyway.
        /// </remarks>
        /// <param name="stator">The rotor to rotate.</param>
        /// <param name="targetAngleDeg">The target angle in degrees to rotate towards.</param>
        /// <param name="lockRotorOnTarget">Whether or not the rotor should be locked upon reaching the target angle.</param>
        /// <param name="toleranceDeg">The maximum difference (in degrees) between two angles for them to be considered being equal.</param>
        /// <param name="toleranceNearZeroDeg">The target and actual angles will be considered equal if both are closer to 0° than this value.
        /// <para>Use this to avoid the rotor head doing unnecessary rotations f.e. when the target angle is 1° and the actual angle is  358.</para>
        /// </param>
        /// <returns>Returns true if the rotation is finished (rotor is at target angle), false otherwise.</returns>
        public static bool RotateToAngle(this IMyMotorStator stator, float targetAngleDeg, bool lockRotorOnTarget = true, float toleranceDeg = 1f, float toleranceNearZeroDeg = 1f)
        {
            float currentAngleDeg = stator.Angle * 180 / (float)Math.PI;

            targetAngleDeg %= 360f;

            if (AreAnglesSimilar(currentAngleDeg,targetAngleDeg,toleranceDeg,toleranceNearZeroDeg, true))
            {
                stator.RotorLock |= lockRotorOnTarget;
                return true;
            }

            stator.RotorLock = false;

            float velocityRPM = Math.Abs(stator.TargetVelocityRPM);

            if (currentAngleDeg < targetAngleDeg)
            {
                stator.LowerLimitDeg = -361f;
                stator.UpperLimitDeg = targetAngleDeg;
            }
            else
            {
                stator.LowerLimitDeg = targetAngleDeg;
                stator.UpperLimitDeg = 361f;
                velocityRPM *= -1;
            }

            stator.TargetVelocityRPM = velocityRPM;

            return false;
        }


        /// <summary>
        /// Rotates the rotor to the given angle. Sharing inertia tensor is disabled during rotation. Does not respect limits set on the rotor, and will overwrite them!
        /// </summary>
        /// <param name="stator">The rotor to rotate.</param>
        /// <param name="shareTensor">If set to true, sharing inertia tensor will be enabled on the rotor upon rotation success.</param>
        /// <param name="targetAngleDeg">The target angle in degrees to rotate towards.</param>
        /// <param name="lockRotorOnTarget">Whether or not the rotor should be locked upon reaching the target angle.</param>
        /// <param name="toleranceDeg">The maximum difference (in degrees) between two angles for them to be considered being equal.</param>
        /// <param name="toleranceNearZeroDeg">The target and actual angles will be considered equal if both are closer to 0° than this value.
        /// <para>Use this to avoid the rotor head doing unnecessary rotations f.e. when the target angle is 1° and the actual angle is  358.</para>
        /// </param>
        /// <returns>Returns true if the rotation is finished (rotor is at target angle), false otherwise.</returns>
        /// <remarks>
        /// This method will not always rotate in the shortest direction, as that would introduce severe drawbacks due to limitations on '<see cref="IMyMotorStator"/>'.
        /// See '<see cref="RotateToAngleShortest(IMyMotorStator,bool, float, bool, float)"/>', if you want to rotate using the shortest distance anyway.
        /// <para>Note: This is a 3 step process for safety reasons, so in a sequence of calls for rotation calling this function results in an extra call before the rotation actually begins, and an extra call after the rotation ends.</para>
        /// </remarks>
        public static bool RotateToAngle(this IMyMotorStator stator, bool shareTensor, float targetAngleDeg, bool lockRotorOnTarget = true, float toleranceDeg = 1f, float toleranceNearZeroDeg = 1f)
        {
            float currentAngleDeg = stator.Angle * 180 / (float)Math.PI;

            if (AreAnglesSimilar(targetAngleDeg,currentAngleDeg,toleranceDeg, toleranceNearZeroDeg, true))
            {
                stator.RotorLock |= lockRotorOnTarget;
                return true;
            }

            bool tensorShared = stator.GetValueBool("ShareInertiaTensor");
            if (tensorShared)
            {
                stator.SetValueBool("ShareInertiaTensor", false);
                return false;
            }

            bool rotationSuccess = RotateToAngle(stator, targetAngleDeg, lockRotorOnTarget, toleranceDeg, toleranceNearZeroDeg);

            if (rotationSuccess && tensorShared != shareTensor)
            {
                stator.SetValueBool("ShareInertiaTensor", shareTensor);
                return false;
            }

            return rotationSuccess;
        }


        /// <summary>
        /// Rotates the rotor to the given angle in the shortest way. Does not respect limits set on the rotor, and will overwrite them!
        /// </summary>
        /// <remarks>
        /// Warning: Using this method can often result in strong jerks due to limitations on '<see cref="IMyMotorStator"/>'. The game can make the rotor head
        /// do a full rotation in an extremely short time when setting new limits. Only use this method if you know what you are doing and/or
        /// the rotor is on a grid that can absorb the resulting forces, for example a static (station) grid.
        /// </remarks>
        /// <param name="stator">The rotor to rotate.</param>
        /// <param name="targetAngleDeg">The target angle in degrees to rotate towards.</param>
        /// <param name="lockRotorOnTarget">Whether or not the rotor should be locked upon reaching the target angle.</param>
        /// <param name="toleranceDeg">The maximum difference (in degrees) between two angles for them to be considered being equal.</param>
        /// <returns>Returns true if the rotation is finished (rotor is at target angle), false otherwise.</returns>
        public static bool RotateToAngleShortest(this IMyMotorStator stator, float targetAngleDeg, bool lockRotorOnTarget = true, float toleranceDeg = 1f)
        {

            float currentAngleDeg = ((stator).Angle * 180 / (float)Math.PI) % 360;

            if (AreAnglesSimilar(targetAngleDeg, currentAngleDeg, toleranceDeg,true))
            {
                stator.RotorLock |= lockRotorOnTarget;
                return true;
            }

            currentAngleDeg %= 360f;
            targetAngleDeg %= 360f;

            stator.RotorLock = false;

            float velocity = stator.TargetVelocityRPM;
            
            if (currentAngleDeg < 0 && currentAngleDeg > -360)
            {
                currentAngleDeg += 360;
            }

            float angleDifference = targetAngleDeg - currentAngleDeg;

            if ((angleDifference >= 0 && angleDifference < 180) || angleDifference <= -180)
            {
                velocity = Math.Abs(velocity);
            }
            else
            {
                velocity = Math.Abs(velocity) * -1;
            }

            if (velocity > 0)
            {
                stator.UpperLimitDeg = targetAngleDeg;
                stator.LowerLimitDeg = -361;
            }
            else if (velocity < 0)
            {
                stator.UpperLimitDeg = 361;
                stator.LowerLimitDeg = targetAngleDeg;
            }

            stator.TargetVelocityRPM = velocity;

            return false;
        }

        /// <summary>
        /// Rotates the rotor to the given angle. Sharing inertia tensor is disabled during rotation. Does not respect limits set on the rotor, and will overwrite them!
        /// </summary>
        /// <param name="stator">The rotor to rotate.</param>
        /// <param name="shareTensor">If set to true, sharing inertia tensor will be enabled on the rotor upon rotation success.</param>
        /// <param name="targetAngleDeg">The target angle in degrees to rotate towards.</param>
        /// <param name="lockRotorOnTarget">Whether or not the rotor should be locked upon reaching the target angle.</param>
        /// <param name="toleranceDeg">The maximum difference (in degrees) between two angles for them to be considered being equal.</param>
        /// <returns>Returns true if the rotation is finished (rotor is at target angle), false otherwise.</returns>
        /// <remarks>
        /// Warning: Using this method can often result in strong jerks due to limitations on '<see cref="IMyMotorStator"/>'. The game can make the rotor head
        /// do a full rotation in an extremely short time when setting new limits. Only use this method if you know what you are doing and/or
        /// the rotor is on a grid that can absorb the resulting forces, for example a static (station) grid.
        /// <para>Note: This is a 3 step process for safety reasons, so in a sequence of calls for rotation calling this function results in an extra call before the rotation actually begins, and an extra call after the rotation ends.</para>
        /// </remarks>
        public static bool RotateToAngleShortest(this IMyMotorStator stator, bool shareTensor, float targetAngleDeg, bool lockRotorOnTarget = true, float toleranceDeg = 1f)
        {
            float currentAngleDeg = stator.Angle * 180 / (float)Math.PI;

            if (AreAnglesSimilar(targetAngleDeg, currentAngleDeg,toleranceDeg,true))
            {
                stator.RotorLock |= lockRotorOnTarget;
                return true;
            }

            bool tensorShared = stator.GetValueBool("ShareInertiaTensor");
            if (tensorShared)
            {
                stator.SetValueBool("ShareInertiaTensor", false);
                return false;
            }

            bool rotationSuccess = RotateToAngleShortest(stator, targetAngleDeg, lockRotorOnTarget, toleranceDeg);

            if (rotationSuccess && tensorShared!=shareTensor)
            {
                stator.SetValueBool("ShareInertiaTensor", shareTensor);
                return false;
            }

            return rotationSuccess;
        }

        /// <summary>
        /// Returns whether or not the provided angles are equal.
        /// </summary>
        /// <param name="angle1">The first angle (in degrees) to compare.</param>
        /// <param name="angle2">The second angle (in degrees) to compare.</param>
        /// <param name="tolerance">The maximum difference (in degrees) between the angles for them to be considered equal.</param>
        /// <param name="normalizeAngles">Set this to true if either of the angles is less than 0° or eual to or greater than 360°.</param>
        /// <returns>Whether or not the provided angles are equal.</returns>
        public static bool AreAnglesSimilar(float angle1, float angle2, float tolerance = 1f, bool normalizeAngles = false)
        {
            #region Translating angles to angles between 0° and 360°
            if (normalizeAngles)
            {
                angle1 = GetNormalizedAngle(angle1);
                angle2 = GetNormalizedAngle(angle2);
            }
            #endregion

            float diff = Math.Abs(angle1 - angle2);

            return  diff < tolerance || Math.Abs(360f-diff) < tolerance;
        }

        /// <summary>
        /// Returns whether or not the provided angles are equal.
        /// </summary>
        /// <param name="angle1">The first angle (in degrees) to compare.</param>
        /// <param name="angle2">The second angle (in degrees) to compare.</param>
        /// <param name="tolerance">The maximum difference (in degrees) between the angles for them to be considered equal.</param>
        /// <param name="toleranceNearZero">If both angles are this close to 0°, they will be considered equal.</param>
        /// <param name="normalizeAngles">Set this to true if either of the angles is less than 0° or eual to or greater than 360°.</param>
        /// <returns>Whether or not the provided angles are equal.</returns>
        public static bool AreAnglesSimilar(float angle1, float angle2, float tolerance, float toleranceNearZero, bool normalizeAngles = false)
        {
            #region Translating angles to angles between 0° (inclusive) and 360° (exclusive)
            if (normalizeAngles)
            {
                angle1 = GetNormalizedAngle(angle1);
                angle2 = GetNormalizedAngle(angle2);
            }
            #endregion

            if ( ((angle1 < toleranceNearZero)||(360-angle1)<toleranceNearZero) && ((angle2 < toleranceNearZero) || (360 - angle2) < toleranceNearZero) )
            {
                return true;
            }

            float diff = Math.Abs(angle1 - angle2);

            return diff < tolerance || Math.Abs(360f - diff) < tolerance;
        }

        /// <summary>
        /// Returns the equivalent value between 0° (inclusive) and 360° (exclusive) of the given angle (in degrees).
        /// </summary>
        public static float GetNormalizedAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }
    }

}
