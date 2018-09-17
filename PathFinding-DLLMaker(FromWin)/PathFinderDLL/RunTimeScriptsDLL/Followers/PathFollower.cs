using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPathFinder
{
    public class PathFollower : MonoBehaviour
    {
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

        public void StopFollowing() { StopAllCoroutines(); }
        
        IEnumerator FollowPath()
        {
            yield return null;
            if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo(string.Format("[{0}] Follow(), Speed:{1}", name, moveSpeed));

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

            if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("PathFollower completed!");
        }

        public virtual void MoveTo(int pointIndex)
        {
            var targetPos = pointsToFollow[pointIndex] ;

                var deltaPos = targetPos - _transform.position;
                //deltaPos.z = 0f;
                _transform.up = Vector3.up;
                _transform.forward = deltaPos.normalized;

			_transform.position =	Vector3.MoveTowards(_transform.position, targetPos, moveSpeed * Time.smoothDeltaTime);
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
