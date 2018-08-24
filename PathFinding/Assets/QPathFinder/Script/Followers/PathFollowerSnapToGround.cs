using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPathFinder
{
    
    public class PathFollowerSnapToGround : PathFollower
    {
        Vector3 directionOfRayCast;
        float offsetDistanceFromPoint;
        int maxDistanceForRayCast;
        LayerMask backgroundLayerMask;
        float offsetDistanceToFloatFromGround;

        public void Init ( Vector3 directionOfRayCast, float offsetDistanceFromPoint, float offsetDistanceToFloatFromGround, int maxDistanceForRayCast, int groundLayer )
        {
            this.directionOfRayCast = directionOfRayCast.normalized;
            this.offsetDistanceFromPoint = offsetDistanceFromPoint;
            this.maxDistanceForRayCast = maxDistanceForRayCast;
            this.offsetDistanceToFloatFromGround = offsetDistanceToFloatFromGround;

            backgroundLayerMask = 1 << groundLayer;
        }

        public override Vector3 ConvertPointIfNeeded ( Vector3 point )
        {
            RaycastHit hitInfo;
            if (Physics.Raycast ( point + offsetDistanceFromPoint * (-directionOfRayCast), directionOfRayCast,out hitInfo, maxDistanceForRayCast, backgroundLayerMask.value ) )
            {
                Vector3 hitPos = hitInfo.point;
                return hitPos + offsetDistanceToFloatFromGround * (-directionOfRayCast);
            }

            if ( QPathFinder.Logger.CanLogError ) QPathFinder.Logger.LogError("Ground not found at " + point + ". Could not snap to ground properly!");
            return point;
            
        }

		protected override bool IsOnPoint(int pointIndex) 
		{ 
			Vector3 finalPoint = ConvertPointIfNeeded(pointsToFollow[pointIndex]);
			float mag = (_transform.position - finalPoint).sqrMagnitude; 
			return mag < 0.1f;
		}

    }
}
