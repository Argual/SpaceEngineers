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
    public static class ArgShipControllerExtensions
    {
        /// <summary>
        /// Gets the linear velocities of a ship controller relative to its orientation.
        /// </summary>
        public static Vector3 GetLocalLinearVelocities(this IMyShipController controller)
        {

            Vector3D velocity = controller.GetShipVelocities().LinearVelocity;

            MatrixD mat = controller.WorldMatrix.GetOrientation();

            Vector3D localVelocity = Vector3D.Transform(velocity, MatrixD.Transpose(mat));

            return localVelocity;
        }
    }
}
