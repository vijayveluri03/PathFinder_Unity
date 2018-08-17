using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mr1
{
    public class MouseClickDemo : MonoBehaviour
    {
        public string pathName;
        public Camera camera;
        public float playerSpeed = 20.0f;
        public bool thoroughPathFinding = false;
        public bool useGroundSnap = false;

        public GameObject playerObj;


        void Update () 
        {
            if ( Input.GetMouseButtonUp(0))
            {
                OnMouseUp();
            }

        }
        void OnMouseUp()
        {
            LayerMask backgroundLayerMask = 1 << WaypointManager.instance.gameObject.layer;

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
                WaypointManager.instance.FindShortestPathBetweenPointsAsynchronous( playerObj.transform.position, hitPos,  WaypointManager.instance.graphData.lineType, 
                    thoroughPathFinding ? PathFinderUtility.SearchMode.Complex: PathFinderUtility.SearchMode.Simple,
                
                    delegate ( List<Vector3> wayPoints ) 
                    { 
                        if ( useGroundSnap )
                           PathFinderUtility.FollowPathWithGroundSnap ( playerObj.transform, wayPoints, playerSpeed, Vector3.down, 5, 0.1f, 200, LayerMask.NameToLayer("Default"));
                        else 
                            PathFinderUtility.FollowPath ( playerObj.transform, wayPoints, playerSpeed );

                    }
                 );
            }
        }

        

        
    }
}
