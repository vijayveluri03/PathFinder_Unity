using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mr1
{
    public static class PathFinderUtility 
    {
            public static PathFollower FollowPath( Transform transform, List<Vector3> points, float moveSpeed )
            {
                var pathFollower = Create(transform);
                if (points != null) pathFollower.Follow(points, moveSpeed);
                else Debug.LogError(string.Format("[WaypointManager] couldn't find path"));
                return pathFollower;
            }
            public static PathFollower FollowPathWithGroundSnap( Transform transform, List<Vector3> points, float moveSpeed, Vector3 directionOfRayCast, 
                                                                    float offsetDistanceFromPoint, float offsetDistanceToFloatFromGround, int maxDistanceForRayCast, int groundLayer )
            {
                var pathFollower = CreateWithSnapToGround(transform, directionOfRayCast, offsetDistanceFromPoint, offsetDistanceToFloatFromGround, maxDistanceForRayCast, groundLayer );
                if (points != null) pathFollower.Follow(points, moveSpeed);
                else Debug.LogError(string.Format("[WaypointManager] couldn't find path"));
                return pathFollower;
            }
            public static void StopFollowing( Transform transform)
            {
                Stop(transform);
            }

            private static PathFollower Create(Transform transform)
            {
                var pathFollower = transform.GetComponent<PathFollower>();
                if (pathFollower == null) pathFollower = transform.gameObject.AddComponent<PathFollower>();
                pathFollower._transform = transform;
                return pathFollower;
            }

            private static PathFollower CreateWithSnapToGround(Transform transform, Vector3 directionOfRayCast, float offsetDistanceFromPoint, float offsetDistanceToFloatFromGround, int maxDistanceForRayCast, int groundLayer)
            {
                var pathFollower = transform.GetComponent<PathFollowerYSnap>();
                if (pathFollower == null) pathFollower = transform.gameObject.AddComponent<PathFollowerYSnap>();

                pathFollower.Init( directionOfRayCast, offsetDistanceFromPoint, offsetDistanceToFloatFromGround, maxDistanceForRayCast, groundLayer);

                pathFollower._transform = transform;
                return pathFollower;
            }

            private static void Stop(Transform transform)
            {
                var pathFollower = transform.GetComponent<PathFollower>();
                if (pathFollower != null) { pathFollower.StopFollowing(); GameObject.Destroy(pathFollower); }
            }
    }

    public class PathFollower : MonoBehaviour
    {
        


        public bool logMessage;
		protected List<Vector3> pointsToFollow;

        public float moveSpeed = 10f;
        public float rotateSpeed = 10f;

        public Transform _transform { get; set; }
        
		protected int _currentIndex;

        

        public void Follow(List<Vector3> pointsToFollow, float moveSpeed)
        {
            this.pointsToFollow = pointsToFollow;
            this.moveSpeed = moveSpeed;

            StopFollowing();

            _currentIndex = 0;
            StartCoroutine(FollowPath());
        }

        public void StopFollowing() { StopCoroutine("FollowPath");  }
        
        IEnumerator FollowPath()
        {
            yield return null;
            if (logMessage) Debug.Log(string.Format("[{0}] Follow(), Speed:{1}", name, moveSpeed));

            while (true)
            {
                _currentIndex = Mathf.Clamp(_currentIndex, 0, pointsToFollow.Count - 1);

                if (IsOnPoint(_currentIndex))
                {
                    if (IsEndPoint(_currentIndex)) break;
                    else _currentIndex = GetNextIndex(_currentIndex);
                }
                else
                {
                    MoveTo(_currentIndex);
                }
                yield return null;
            }
        }


        void MoveTo(int pointIndex)
        {
            var targetPos = ConvertPointIfNeeded( pointsToFollow[pointIndex] ) ;

                var deltaPos = targetPos - _transform.position;
                //deltaPos.z = 0f;
                _transform.up = Vector3.up;
                _transform.forward = deltaPos.normalized;

            _transform.position = Vector3.MoveTowards(_transform.position, targetPos, moveSpeed * Time.smoothDeltaTime);
        }

        protected virtual bool IsOnPoint(int pointIndex) { return (_transform.position - pointsToFollow[pointIndex]).sqrMagnitude < 0.1f; }
        bool IsEndPoint(int pointIndex)
        {
            return pointIndex == EndIndex();
        }

        int StartIndex()
        {
            return 0;
        }

        public virtual Vector3 ConvertPointIfNeeded ( Vector3 point )
        {
            return point;
        }

        int EndIndex()
        {
            return pointsToFollow.Count - 1;
        }

        int GetNextIndex(int currentIndex)
        {
            int nextIndex = -1;
            if (currentIndex < EndIndex()) 
				nextIndex = currentIndex + 1;
      
            return nextIndex;
        }

    }
}
