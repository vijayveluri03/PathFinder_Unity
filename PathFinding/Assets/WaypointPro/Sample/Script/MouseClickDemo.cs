using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mr1
{
    public class MouseClickDemo : MonoBehaviour
    {
        public Transform target;
        public string pathName;
        public Camera camera;

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


			WaypointManager.instance.SelectPath ( 0 );
            //WaypointManager.instance.selected.Refresh();
            {
                WaypointManager.instance.CreatePath( playerObj.transform.position, hitPos,  
                    delegate ( List<Vector3> wayPoints ) 
                    { 
                       PathFinderUtility.FollowPathWithGroundSnap ( playerObj.transform, wayPoints, 20.0f, Vector3.down, 5, 0.1f, 200, LayerMask.NameToLayer("Default"));
                    },
                    PathLineType.CatmullRomCurve,
                    false
                 );
            }
        }

        

        
    }
}
