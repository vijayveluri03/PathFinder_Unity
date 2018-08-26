using System;
using System.Collections.Generic;
using UnityEngine;

namespace QPathFinder
{
    public class MouseClickDemo : MonoBehaviour
    {
        public Camera camera;   // Needed for mouse click to world position convertion. 
        public float playerSpeed = 20.0f;
                public GameObject playerObj;


        // For PathFollowerWithGroundSnap - This will snap the player to the ground while it follows the path. 
        public float playerFloatOffset;     // This is how high the player floats above the ground. 
        public float raycastOriginOffset;   // This is how high above the player u want to raycast to ground. 
        public string colliderLayer = "Default";    // the ground layout for the ray to hit. 
        public bool thoroughPathFinding = false;    // uses few extra steps in pathfinding to find accurate result. 


        public bool useGroundSnap = false;          // if snap to ground is not used, player goes only through nodes and doesnt project itself on the ground. 

        void Awake()
        {
            QPathFinder.Logger.SetLoggingLevel( QPathFinder.Logger.Level.Info );
        }
        void Update () 
        {
            if ( Input.GetMouseButtonUp(0)) 
            {
                MovePlayerToMousePosition();
            }

        }

        void MovePlayerToMousePosition()
        {
            LayerMask backgroundLayerMask = 1 << PathFinder.instance.gameObject.layer;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Vector3 hitPos = Vector3.zero;
            if (Physics.Raycast(ray, out hit, 1000f, backgroundLayerMask))
            {
                hitPos = hit.point;
            }
            else 
            {
                Debug.LogError("ERROR!");
                return;
            }

            {
                PathFinder.instance.FindShortestPathOfPoints( playerObj.transform.position, hitPos,  PathFinder.instance.graphData.lineType, 
                    Execution.Asynchronously,
                    thoroughPathFinding ? SearchMode.Complex: SearchMode.Simple,
                    delegate ( List<Vector3> points ) 
                    { 
                        PathFollowerUtility.StopFollowing( playerObj.transform );
                        if ( useGroundSnap )
                        {
                           FollowThePathWithGroundSnap ( points );
                        }
                        else 
                            FollowThePathNormally ( points );

                    }
                 );
            }
        }

        void FollowThePathWithGroundSnap ( List<Vector3> nodes )
        {
            PathFollowerUtility.FollowPathWithGroundSnap ( playerObj.transform, 
                                                        nodes, playerSpeed, Vector3.down, playerFloatOffset, LayerMask.NameToLayer( colliderLayer ),
                                                        raycastOriginOffset, 40 );
        }

        void FollowThePathNormally ( List<Vector3> nodes )
        {
            PathFollowerUtility.FollowPath ( playerObj.transform, nodes, playerSpeed );
        }
    }
}
