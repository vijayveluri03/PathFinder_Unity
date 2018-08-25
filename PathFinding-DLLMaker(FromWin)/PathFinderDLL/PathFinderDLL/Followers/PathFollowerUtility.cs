
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace QPathFinder
{
	public enum SearchMode 
	{
		Simple = 0,
		Intermediate, 
		Complex
	}
    namespace Runtime.CompilerServices
    {
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class
             | AttributeTargets.Method)]
        public sealed class ExtensionAttribute : Attribute { }
    }


    public static class PathFinderExtensions 
	{
		//
		// Finds the shortest path between **Nodes** asynchronously 

		public static void FindShortestPathBetweenNodesAsynchronous (  this PathFinder manager, int startNodeID, int endNodeID, PathLineType pathType, Action<List<Vector3>> OnPathFound )
		{
			PathFollowerUtility.FindShortestPathBetweenNodesAsynchronous( manager, startNodeID, endNodeID, pathType, OnPathFound );
		}

		// 
		// Finds the shortest path between **Points** asynchronously

		public static void FindShortestPathBetweenPointsAsynchronous (  this PathFinder manager, Vector3 startPoint, Vector3 endPoint, PathLineType pathType, SearchMode searchMode,  Action<List<Vector3>> OnPathFound )
		{
			PathFollowerUtility.FindShortestPathBetweenPointsAsynchronous ( manager, startPoint, endPoint, pathType, searchMode, OnPathFound);
		}

		//
		// Finds the shortest path between **Points** Immediately

		public static List<Vector3> FindShortestPathBetweenPointsSynchronous ( this PathFinder manager, Vector3 startPoint, Vector3 endPoint, PathLineType pathType, SearchMode searchMode  )
		{
			return PathFollowerUtility.FindShortestPathBetweenPointsSynchronous (  manager, startPoint, endPoint, pathType, searchMode  );
		}
	}

    public static class PathFollowerUtility 
    {
		//
		//	This will move the game object through the points specified

		public static PathFollower FollowPath( Transform transform, List<Vector3> points, float moveSpeed, PathLineType pathType = PathLineType.Straight )
		{
			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Initiated Follow path for transform " + transform.name +" with path type:" + pathType, true );
			var pathFollower = CreateOrGet(transform);
			if (points != null) 
				pathFollower.Follow(points, moveSpeed);
			else
			{ 
				if ( QPathFinder.Logger.CanLogError ) QPathFinder.Logger.LogError("Could not find the path for path follower to follow!", true );
			}
			return pathFollower;
		}

		//
		// This will movee ethe game object through the points specified, Also, it will keep the gameobject snapped to the ground
		// So if your Nodes are a little above the ground, your target will still move on the ground

		public static PathFollower FollowPathWithGroundSnap( Transform transform, List<Vector3> points, float moveSpeed
																, Vector3 directionOfRayCast			// if your ground is down, the ray cast has to be down too, so its Vector3.down
																, float offsetDistanceToFloatFromGround // the character offset distance from the ground 
																, int groundGameObjectLayer				// this is the ground Gameobject layer
																, float offsetDistanceFromPoint = 10	// this is to calculate the raycast origin, from where we shoot rays. raycast origin is generally above the player casting rays towards ground. how high do u want the origin depends on your scene.
																, int maxDistanceForRayCast = 40		// this is the distance of ray from the raycast origin. 
																, PathLineType pathType = PathLineType.Straight )
		{
			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Initiated Follow path[With ground snap] for transform " + transform.name +" with path type:" + pathType, true );
			var pathFollower = CreateWithSnapToGround(transform, directionOfRayCast, offsetDistanceFromPoint, offsetDistanceToFloatFromGround, maxDistanceForRayCast, groundGameObjectLayer );
			if (points != null) 
				pathFollower.Follow(points, moveSpeed);
			else 
			{
				if ( QPathFinder.Logger.CanLogError ) QPathFinder.Logger.LogError("Could not find the path for path follower to follow!", true );
			}
			return pathFollower;
		}

		
		public static void StopFollowing( Transform transform)
		{
			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Stopping FollowPath");
			Stop(transform);
		}

		// 
		// *** PRIVATE AND INTERNAL ***

		#region PRIVATE AND INTERNAL

		internal static void FindShortestPathBetweenNodesAsynchronous (  PathFinder manager, int startNodeID, int endNodeID, PathLineType pathType, Action<List<Vector3>> OnPathFound )
		{
			int nearestPointFromStart = startNodeID;
			int nearestPointFromEnd = endNodeID;


			if ( nearestPointFromEnd == -1 || nearestPointFromStart == -1 )
			{
				if ( QPathFinder.Logger.CanLogError ) QPathFinder.Logger.LogError("Could not find path between " + nearestPointFromStart + " and " + nearestPointFromEnd, true );
				OnPathFound( null );
				return;
			}

			float startTime = Time.realtimeSinceStartup;
            
			manager.FindShortestPathAsynchronous( nearestPointFromStart, nearestPointFromEnd, 
				delegate ( List<Node> wayPoints ) 
                { 
					if ( wayPoints == null || wayPoints.Count == 0 )
						OnPathFound ( null );

					List<global::System.Object> allWayPoints	= new List<global::System.Object>();
					List<Vector3> path = null;

                    if ( wayPoints != null )
                    {
                        foreach ( var a in wayPoints ) 
                        {
							allWayPoints.Add ( a.Position );
                        }
                    }
					path = (pathType == PathLineType.Straight ? GetStraightPathPoints(allWayPoints) : GetCatmullRomCurvePathPoints ( allWayPoints ) );

					if ( QPathFinder.Logger.CanLogInfo )
						for ( int i = 1; i < path.Count; i++ )
						{
							Debug.DrawLine(path[i - 1], path[i], Color.red, 10f);
						}

					OnPathFound ( path );
                } );
		}

		internal static void FindShortestPathBetweenPointsAsynchronous (  PathFinder manager, Vector3 startPoint, Vector3 endPoint, PathLineType pathType, SearchMode searchMode,  Action<List<Vector3>> OnPathFound )
		{
			bool makeItMoreAccurate = searchMode == SearchMode.Intermediate || searchMode == SearchMode.Complex;
			int nearestPointFromStart = manager.FindNearestNode ( startPoint );
			int nearestPointFromEnd = -1;
			if ( nearestPointFromStart != -1 )
				nearestPointFromEnd = manager.FindNearestNode ( endPoint );
			
			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Nearest point from start" + startPoint + " is " + nearestPointFromStart, true );
			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Nearest point from end:" + endPoint + " is " + nearestPointFromEnd, true );

			if ( nearestPointFromEnd == -1 || nearestPointFromStart == -1 )
			{
				if ( QPathFinder.Logger.CanLogError ) QPathFinder.Logger.LogError("Could not find path between " + nearestPointFromStart + " and " + nearestPointFromEnd, true );
				OnPathFound( null );
				return;
			}

			float startTime = Time.realtimeSinceStartup;
            
			manager.FindShortestPathAsynchronous( nearestPointFromStart, nearestPointFromEnd, 
				delegate ( List<Node> wayPoints ) 
                { 
					if ( wayPoints == null || wayPoints.Count == 0 )
					{
						OnPathFound ( null );
						return;
					}

					List<global::System.Object> allWayPoints	= new List<global::System.Object>();
                    if ( wayPoints != null )
                    {
                        foreach ( var a in wayPoints ) 
                        {
							allWayPoints.Add ( a );
                        }
                    }

					if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Search Mode " + searchMode.ToString() + " opted", true );

					if ( makeItMoreAccurate )
					{
						if ( allWayPoints.Count > 1 ) 
						{
							bool tryOtherLines = false;
							int id = ((Node) allWayPoints[0]).autoGeneratedID;

							allWayPoints[0] = ComputeClosestPointFromPointToLine( startPoint, GetPositionFromNodeOrVector( allWayPoints, 0 ), GetPositionFromNodeOrVector( allWayPoints, 1 ), out tryOtherLines );

							if ( tryOtherLines ) 
							{
								allWayPoints.Insert(0, GetClosestPointOnAnyPath( id, manager, startPoint ) );
							}

							tryOtherLines = false;
							id = ((Node) allWayPoints[allWayPoints.Count - 1]).autoGeneratedID;
							allWayPoints[allWayPoints.Count - 1] = ComputeClosestPointFromPointToLine( endPoint, GetPositionFromNodeOrVector( allWayPoints, allWayPoints.Count - 2), GetPositionFromNodeOrVector( allWayPoints, allWayPoints.Count - 1), out tryOtherLines );

							if ( tryOtherLines ) 
							{
								allWayPoints.Add( GetClosestPointOnAnyPath( id, manager, endPoint ) );
							}
						}
						else
						{
							if ( QPathFinder.Logger.CanLogWarning ) QPathFinder.Logger.LogWarning("Unable to get the best result due to less node count", true ); 
						}
					}

					List<Vector3> path = null;
					
					{
						allWayPoints.Insert ( 0, startPoint );
						allWayPoints.Add ( endPoint );

						path = (pathType == PathLineType.Straight ? GetStraightPathPoints(allWayPoints) : GetCatmullRomCurvePathPoints ( allWayPoints ) );
					}
					
					if ( QPathFinder.Logger.CanLogInfo )
					{
						for ( int i = 1; i < path.Count; i++ )
						{
							Debug.DrawLine(path[i - 1], path[i], Color.red, 10f);
						}
					}

					OnPathFound ( path );
                } );
		}
		internal static List<Vector3> FindShortestPathBetweenPointsSynchronous (  PathFinder manager, Vector3 startPoint, Vector3 endPoint, PathLineType pathType, SearchMode searchMode  )
		{
			bool makeItMoreAccurate = searchMode == SearchMode.Intermediate || searchMode == SearchMode.Complex;
			int nearestPointFromStart = manager.FindNearestNode ( startPoint );
			int nearestPointFromEnd = -1;
			if ( nearestPointFromStart != -1 )
				nearestPointFromEnd = manager.FindNearestNode ( endPoint );

			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Nearest point from start" + startPoint + " is " + nearestPointFromStart, true );
			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Nearest point from end:" + endPoint + " is " + nearestPointFromEnd, true );


			if ( nearestPointFromEnd == -1 || nearestPointFromStart == -1 )
			{
				if ( QPathFinder.Logger.CanLogError ) QPathFinder.Logger.LogError("Could not find path between " + nearestPointFromStart + " and " + nearestPointFromEnd, true );
				return null;
			}

			float startTime = Time.realtimeSinceStartup;
            
			List<Node> wayPoints = manager.FindShortestPathSynchronous( nearestPointFromStart, nearestPointFromEnd ) ;
				
			{ 
				if ( wayPoints == null || wayPoints.Count == 0 )
					return null;

				List<global::System.Object> allWayPoints	= new List<global::System.Object>();
				if ( wayPoints != null )
				{
					foreach ( var a in wayPoints ) 
					{
						allWayPoints.Add ( a );
					}
				}

				if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo("Search Mode " + searchMode.ToString() + " opted", true );

				if ( makeItMoreAccurate )
				{
					if ( allWayPoints.Count > 1 ) 
					{
						bool tryOtherLines = false;
						int id = ((Node) allWayPoints[0]).autoGeneratedID;

						allWayPoints[0] = ComputeClosestPointFromPointToLine( startPoint, GetPositionFromNodeOrVector( allWayPoints, 0 ), GetPositionFromNodeOrVector( allWayPoints, 1 ), out tryOtherLines );

						if ( tryOtherLines ) 
						{
							allWayPoints.Insert(0, GetClosestPointOnAnyPath( id, manager, startPoint ) );
						}

						tryOtherLines = false;
						id = ((Node) allWayPoints[allWayPoints.Count - 1]).autoGeneratedID;
						allWayPoints[allWayPoints.Count - 1] = ComputeClosestPointFromPointToLine( endPoint, GetPositionFromNodeOrVector( allWayPoints, allWayPoints.Count - 2), GetPositionFromNodeOrVector( allWayPoints, allWayPoints.Count - 1), out tryOtherLines );

						if ( tryOtherLines ) 
						{
							allWayPoints.Add( GetClosestPointOnAnyPath( id, manager, endPoint ) );
						}
					}
					else 
					{
						if ( QPathFinder.Logger.CanLogWarning ) QPathFinder.Logger.LogWarning("Unable to get the best result due to less node count", true ); 
					}
				}

				List<Vector3> path = null;
				
				{
					allWayPoints.Insert ( 0, startPoint );
					allWayPoints.Add ( endPoint );

					path = (pathType == PathLineType.Straight ? GetStraightPathPoints(allWayPoints) : GetCatmullRomCurvePathPoints ( allWayPoints ) );
				}

				if ( QPathFinder.Logger.CanLogInfo )
				{
					for ( int i = 1; i < path.Count; i++ )
					{
						Debug.DrawLine(path[i - 1], path[i], Color.red, 10f);
					}
				}

				return ( path );
			}
		}

		internal static List<Vector3> GetStraightPathPoints( List<global::System.Object> nodePoints )
        {
            if ( nodePoints == null )
                return null;

			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("Straight line path choosen!");

            List<Vector3> path = new List<Vector3>();
            
            if (nodePoints.Count < 2)
            {
                return null;
            }

            for (int i = 0; i < nodePoints.Count; i++)
            {
					path.Add( GetPositionFromNodeOrVector( nodePoints, i) );	
            }
            return path;
        }

        internal static List<Vector3> GetCatmullRomCurvePathPoints( List<global::System.Object> nodePoints )
        {
            if ( nodePoints == null )
                return null;

			if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("CatmullRomCurve line path choosen!");

            List<Vector3> path = new List<Vector3>();
            
            if (nodePoints.Count < 3)
            {
                for( int i = 0; i < nodePoints.Count; i++ )
                {
                    path.Add ( GetPositionFromNodeOrVector( nodePoints, i ) );
                }
                return path;
            }

            Vector3[] catmullRomPoints = new Vector3[nodePoints.Count + 2];
            for( int i = 0; i < nodePoints.Count; i++ )
            {
                catmullRomPoints[i+1] = GetPositionFromNodeOrVector( nodePoints, i );
            }
            int endIndex = catmullRomPoints.Length - 1;

            catmullRomPoints[0] = catmullRomPoints[1] + (catmullRomPoints[1] - catmullRomPoints[2]) + (catmullRomPoints[3] - catmullRomPoints[2]);
            catmullRomPoints[endIndex] = catmullRomPoints[endIndex - 1] + (catmullRomPoints[endIndex - 1] - catmullRomPoints[endIndex - 2])
                                    + (catmullRomPoints[endIndex - 3] - catmullRomPoints[endIndex - 2]);

            path.Add( GetPositionFromNodeOrVector( nodePoints, 0 ) );

            for (int i = 0; i < catmullRomPoints.Length - 3; i++)
            {
                for (float t = 0.05f; t <= 1.0f; t += 0.05f)
                {
                    Vector3 pt = ComputeCatmullRom(catmullRomPoints[i], catmullRomPoints[i + 1], catmullRomPoints[i + 2], catmullRomPoints[i + 3], t);
                    path.Add(pt);
                }
            }

            path.Add( GetPositionFromNodeOrVector( nodePoints, nodePoints.Count - 1 ) );
            return path;
        }

		private static PathFollower CreateOrGet(Transform transform)
		{
			var pathFollower = transform.GetComponent<PathFollower>();
			if (pathFollower == null) 
			{
				if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("PathFollower Script created and attached");
				pathFollower = transform.gameObject.AddComponent<PathFollower>();
			}
			else 
			{
				if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("Using existing PathFollower Script to follow the path!");
			}
			pathFollower._transform = transform;
			return pathFollower;
		}
		private static PathFollower CreateWithSnapToGround(Transform transform, Vector3 directionOfRayCast, float offsetDistanceFromPoint, float offsetDistanceToFloatFromGround, int maxDistanceForRayCast, int groundLayer)
		{
			var pathFollower = transform.GetComponent<PathFollowerSnapToGround>();
			if (pathFollower == null)
			{
				if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("PathFollowerSnapToGround Script created and attached");
				pathFollower = transform.gameObject.AddComponent<PathFollowerSnapToGround>();
			}
			else 
			{
				if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("Using existing PathFollowerSnapToGround Script to follow the path!");
			}

			pathFollower.Init( directionOfRayCast, offsetDistanceFromPoint, offsetDistanceToFloatFromGround, maxDistanceForRayCast, groundLayer);

			pathFollower._transform = transform;
			return pathFollower;
		}
		private static void Stop(Transform transform)
		{
			var pathFollower = transform.GetComponent<PathFollower>();
			if (pathFollower != null) 
			{ 
				pathFollower.StopFollowing(); GameObject.Destroy(pathFollower); 
				if ( QPathFinder.Logger.CanLogInfo ) QPathFinder.Logger.LogInfo ("PathFollower stopped and its Script is destroyed!");
			}
		}
		private static Vector3 ComputeCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            Vector3 pt = 0.5f * ((-p0 + 3f * p1 - 3f * p2 + p3) * t3
                        + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                        + (-p0 + p2) * t
                        + 2f * p1);

            return pt;
        }
		private static Vector3 GetClosestPointOnAnyPath ( int nodeID , PathFinder manager, Vector3 pos )
		{
			Node node = manager.graphData.GetNode ( nodeID );
			Vector3 vClosestPoint = node.Position;
			float fClosestDist = (pos - node.Position).sqrMagnitude;
			bool isOnExtremities = false;

			foreach ( Path p in manager.graphData.paths )
			{
				if ( p.IDOfA == node.autoGeneratedID || p.IDOfB == node.autoGeneratedID ) 
				{
					Vector3 vPos = ComputeClosestPointFromPointToLine(pos, manager.graphData.GetNode( p.IDOfA ).Position , manager.graphData.GetNode(p.IDOfB).Position, out isOnExtremities );
					float fDist = (vPos - pos).sqrMagnitude;

					if (fDist < fClosestDist)
					{
						fClosestDist = fDist;
						vClosestPoint = vPos;
					}
				}
			}

			//Debug.DrawLine(pos, vClosestPoint, Color.green, 1f);
			return vClosestPoint;
		}

		private static Vector3 ComputeClosestPointFromPointToLine(Vector3 vPt, Vector3 vLinePt0, Vector3 vLinePt1, out bool isOnExtremities )
        {
            float t = -Vector3.Dot(vPt - vLinePt0, vLinePt1 - vLinePt0) / Vector3.Dot(vLinePt0 - vLinePt1, vLinePt1 - vLinePt0);

            Vector3 vClosestPt;

            if (t < 0f)
			{
                vClosestPt = vLinePt0;
				isOnExtremities = true;
			}
            else if (t > 1f)
			{
                vClosestPt = vLinePt1;
				isOnExtremities = true; 
			}
            else
			{
                vClosestPt = vLinePt0 + t * (vLinePt1 - vLinePt0);
				isOnExtremities = false; 
			}

            //Debug.DrawLine(vPt, vClosestPt, Color.red, 1f);
            return vClosestPt;
        }

		static Vector3 GetPositionFromNodeOrVector ( List<global::System.Object> list, int index )
        {
            return (list[index] is Vector3 ? (Vector3)list[index] : ((Node)list[index]).Position);
        }

		#endregion
    }
}

