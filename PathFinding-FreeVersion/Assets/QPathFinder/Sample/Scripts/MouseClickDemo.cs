using System;
using System.Collections.Generic;
using UnityEngine;

namespace QPathFinder
{
    public class MouseClickDemo : MonoBehaviour
    {
        public string pathName;
        public Camera camera;
        public float playerSpeed = 20.0f;
        public float playerFloatOffset;
        public float raycastOriginOffset;
        public string colliderLayer = "Default";

        public bool thoroughPathFinding = false;
        public bool useGroundSnap = false;

        public GameObject playerObj;


        void Awake()
        {
            QPathFinder.Logger.SetLoggingLevel( QPathFinder.Logger.Level.Warnings );
        }
        void Update () 
        {
            if ( Input.GetMouseButtonUp(0))
            {
                OnMouseUp();
            }

        }
        void OnMouseUp()
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


            //WaypointManager.instance.selected.Refresh();
            {
                PathFinder.instance.FindShortestPathBetweenPointsAsynchronous( playerObj.transform.position, hitPos,  PathFinder.instance.graphData.lineType, 
                    thoroughPathFinding ? SearchMode.Complex: SearchMode.Simple,
                
                    delegate ( List<Vector3> wayPoints ) 
                    { 
                        if ( useGroundSnap )
                        {
                           PathFollowerUtility.FollowPathWithGroundSnap ( playerObj.transform, 
                                                                            wayPoints, playerSpeed, Vector3.down, playerFloatOffset, LayerMask.NameToLayer( colliderLayer ),
                                                                            raycastOriginOffset, 40 );
                        }
                        else 
                            PathFollowerUtility.FollowPath ( playerObj.transform, wayPoints, playerSpeed );

                    }
                 );
            }
        }

        

        
    }
}
