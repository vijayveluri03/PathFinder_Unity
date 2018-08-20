﻿#define ONE_WAY_LOGIC

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace QPathFinder
{
    public enum PathLineType
    {
        Straight,
        CatmullRomCurve,
    }

    [System.Serializable]
    public class Node 
    {
        public Node ( Vector3 pos ) { position = pos; }
        [SerializeField] public Vector3 position;
        [SerializeField] public int autoGeneratedID = -1;
        [HideInInspector] public Node previousWayPoint;
        [HideInInspector] public float  HeuristicDistance;
        [HideInInspector] public float  pathDistance;
        [HideInInspector] public float combinedHeuristic { get {return pathDistance + HeuristicDistance;}}
    }

    [System.Serializable]
    public class Path 
    {
        public Path ( int a, int b ) { IDOfA = a; IDOfB = b; }
        [SerializeField] public int cost;
        
        [SerializeField] public int autoGeneratedID;
        [SerializeField] public int IDOfA = -1;
        [SerializeField] public int IDOfB = -1;

#if ONE_WAY_LOGIC
        public bool isOneWay = false;       
#endif
    }


    [System.Serializable]
    public class GraphData
    {
        
        [SerializeField] public PathLineType lineType;
        [SerializeField] public Color lineColor = Color.yellow;
        [SerializeField] public float nodeSize = 0.5f;
        [SerializeField] public float offsetFromTheGround = 0;  // this represents how much offset we create our points from the ground ?

        [SerializeField] public List<Node> nodes;
        [SerializeField] public List<Path> paths;

        [HideInInspector] public Dictionary<int, Node> nodesSorted;
        [HideInInspector] public Dictionary<int, Path> pathsSorted;

        public GraphData()
        {
            nodes = new List<Node>();
            paths = new List<Path>();
            nodesSorted = new Dictionary<int, Node>();
            pathsSorted = new Dictionary<int, Path>(); 
        }

        public Node GetNode ( int ID )
        {
            if ( nodesSorted.ContainsKey ( ID ) )
                return nodesSorted[ID];
            return null;
        } 
        public Path GetPath ( int ID )
        {
            if ( pathsSorted.ContainsKey ( ID ) )
                return pathsSorted[ID];
            return null;
        } 

        public Path GetPathBetween ( int from, int to )
        {
            foreach ( Path pd in paths ) 
            {
                if ( 
                    (pd.IDOfA == from && pd.IDOfB == to )
                    || (pd.IDOfB == from && pd.IDOfA == to )
                )
                {
                    return pd;
                }
            }
            return null;
        }

        public void Refresh ( )
        {
            if ( nodes == null )
                return;

            //Generate IDs for way points
            {
                int maxID = 0;
                
                for ( int i = 0; i < nodes.Count; i++  )
                {
                    if ( nodes[i].autoGeneratedID > maxID ) 
                        maxID = nodes[i].autoGeneratedID;
                }

                maxID = maxID + 1;

                for ( int i = 0; i < nodes.Count; i++  )
                {
                    if ( nodes[i].autoGeneratedID <= 0  ) 
                        nodes[i].autoGeneratedID = maxID++;
                }
            }

            // generate IDs for way paths.
            {
                int maxID = 0;
                for ( int i = 0; i < paths.Count; i++  )
                {
                    if ( paths[i].autoGeneratedID > maxID ) 
                        maxID = paths[i].autoGeneratedID;
                }

                maxID = maxID + 1;

                for ( int i = 0; i < paths.Count; i++  )
                {
                    if ( paths[i].autoGeneratedID <= 0  ) 
                        paths[i].autoGeneratedID = maxID++;
                }
            }

            // refreshing dictionaries
            {
                pathsSorted.Clear();
                nodesSorted.Clear();

                for ( int i = 0; i < nodes.Count; i++  )
                {
                    nodesSorted[ nodes[i].autoGeneratedID ] = nodes[i] ;
                }

                for ( int i = 0; i < paths.Count; i++  )
                {
                    pathsSorted[ paths[i].autoGeneratedID ] = paths[i] ;
                }
            }
        }

    }
}