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
        /// <param name="stator">The rotor to rotate.</param>
        /// <param name="targetAngle">The target angle in degrees to rotate towards.</param>
        /// <param name="lockRotorOnTarget">Whether or not the rotor should be locked upon reaching the target angle.</param>
        /// <param name="tolerance">The maximum difference (in degrees) between two angles for them to be considered being equal.</param>
        /// <returns>Returns true if the rotation is finished (rotor is at target angle), false otherwise.</returns>
        public static bool RotateToAngle(this IMyMotorStator stator, float targetAngle, bool lockRotorOnTarget = true, float tolerance = 1f)
        {

            float currentAngle = ((stator).Angle * 180 / (float)Math.PI) % 360;

            if (AreAnglesEqual(targetAngle, currentAngle, tolerance))
            {
                stator.RotorLock |= lockRotorOnTarget;
                return true;
            }

            stator.RotorLock = false;

            float velocity = stator.TargetVelocityRPM;
            
            if (currentAngle < 0 && currentAngle > -360)
            {
                currentAngle += 360;
            }

            float angleDifference = targetAngle - currentAngle;

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
                stator.UpperLimitDeg = targetAngle;
                stator.LowerLimitDeg = -361;
            }
            else if (velocity < 0)
            {
                stator.UpperLimitDeg = 361;
                stator.LowerLimitDeg = targetAngle;
            }

            stator.TargetVelocityRPM = velocity;

            return false;
        }

        /// <summary>
        /// Rotates the rotor to the given angle. Sharing inertia tensor is disabled during rotation. Does not respect limits set on the rotor, and will overwrite them!
        /// </summary>
        /// <param name="stator">The rotor to rotate.</param>
        /// <param name="shareTensor">If set to true, sharing inertia tensor will be enabled on the rotor upon rotation success.</param>
        /// <param name="targetAngle">The target angle in degrees to rotate towards.</param>
        /// <param name="lockRotorOnTarget">Whether or not the rotor should be locked upon reaching the target angle.</param>
        /// <param name="tolerance">The maximum difference (in degrees) between two angles for them to be considered being equal.</param>
        /// <returns>Returns true if the rotation is finished (rotor is at target angle), false otherwise.</returns>
        /// <remarks>
        /// Note: This is a 3 step process for safety reasons, so in a sequence of calls for rotation calling this function results in an extra call before the rotation actually begins, and an extra call after the rotation ends.
        /// </remarks>
        public static bool RotateToAngle(this IMyMotorStator stator, bool shareTensor, float targetAngle, bool lockRotorOnTarget = true, float tolerance = 1f)
        {
            bool tensorShared = stator.GetValueBool("ShareInertiaTensor");
            if (tensorShared)
            {
                stator.SetValueBool("ShareInertiaTensor", false);
                return false;
            }
            bool rotationSuccess = RotateToAngle(stator, targetAngle, lockRotorOnTarget, tolerance);
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
        /// <returns>Whether or not the provided angles are equal.</returns>
        static bool AreAnglesEqual(float angle1, float angle2, float tolerance = 1f)
        {
            if (Math.Abs(angle1 - 360) < tolerance)
            {
                angle1 = 0;
            }
            if (Math.Abs(angle2 - 360) < tolerance)
            {
                angle2 = 0;
            }
            return Math.Abs(angle1 - angle2) < tolerance;
        }
    }

}
