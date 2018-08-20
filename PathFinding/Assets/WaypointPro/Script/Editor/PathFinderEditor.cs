using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace QPathFinder
{
    [CustomEditor(typeof(PathFinder))]
    public class PathFinderEditor : Editor 
    {
        enum SceneMode
        {
            AddNode,
            EditNode,
            ConnectPath,
            None
        }

        SceneMode sceneMode;
        PathFinder script;
        const string nodeTextcolor = "#ff00ffff";
        const string ConnectionTextcolor = "#00ffffff";
        const string CostTextcolor = "#0000ffff";


    #region Instantiation and Asset management 

        [MenuItem("GameObject/Create Waypoint Manager")]
        public static void CreateWaypointManager()
        {
            if (GameObject.FindObjectOfType<PathFinder>() == null)
            {
                var managerGo = new GameObject("WaypointManager");
                var manager = managerGo.AddComponent<PathFinder>();
                var boxCollider = managerGo.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(100f, 100f, 1f);
                boxCollider.isTrigger = true;
            }
            else
                Debug.LogError("Waypoint Manager already exists!");
        }


        private void MarkThisDirty ()
        {
            if ( PrefabUtility.GetPrefabParent( script.gameObject ) != null ) 
            {
                Debug.Log("Prefab found for WayPointManager!");
                EditorUtility.SetDirty(script);
            }
            else 
            {
                Debug.LogWarning("No Prefab found for WayPointManager!");   // This is not an issue , but make sure u save the scene when u modify WayPointManager data
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        void OnEnable()
        {
            sceneMode = SceneMode.EditNode;
            script = target as PathFinder;
            script.graphData.Refresh();
        }


    #endregion


        
    #region OnInspectorGUI's Display Method

        public override void OnInspectorGUI()
        {
            showDefaultInspector = EditorGUILayout.Toggle ( "Show Default inspector", showDefaultInspector );
            if ( showDefaultInspector )
            {
                DrawDefaultInspector();
            }
            else 
            {
                CustomGUI.DrawSeparator(Color.gray);
                ShowNodesAndPathInInspector();
            }
        }

        void ShowNodesAndPathInInspector()
        {
            script.graphData.nodeSize = EditorGUILayout.Slider("Node gizmo Size", script.graphData.nodeSize, 0.1f, 3f);
            script.graphData.lineColor = EditorGUILayout.ColorField("Path Color", script.graphData.lineColor);
            script.graphData.lineType = (PathLineType)EditorGUILayout.EnumPopup("Path Type", script.graphData.lineType);
            script.graphData.offsetFromTheGround = EditorGUILayout.FloatField("Offset from ground( Height )", script.graphData.offsetFromTheGround );
            EditorGUILayout.Space();

            GUILayout.Label ( "<size=12><b>Nodes</b></size>", CustomGUI.GetStyleWithRichText());

            if ( script.graphData.nodes.Count > 0 )
            {
                showNodeIDsInTheScene = EditorGUILayout.Toggle ( "Show Node IDs in scene", showNodeIDsInTheScene );

                List<Node> nodeList = script.graphData.nodes;
                for (int j = 0; j < nodeList.Count; j++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField ("\t" + "Node <Color=" + nodeTextcolor + ">" + nodeList[j].autoGeneratedID + "</Color>", CustomGUI.GetStyleWithRichText(), GUILayout.Width ( 120f ) );

                        nodeList[j].position = EditorGUILayout.Vector3Field("", nodeList[j].position);
                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                            AddNode(nodeList[j].position + Vector3.right + Vector3.up, j + 1);
                        if (GUILayout.Button("-", GUILayout.Width(25f)))
                            DeleteNode(j);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else 
            {
                EditorGUILayout.LabelField("<Color=green> Nodes are empty. Use <b>Add Node</b> in scene view to create Nodes!</Color>", CustomGUI.GetStyleWithRichText( CustomGUI.SetAlignmentForText(TextAnchor.MiddleCenter) ) );
            }
            EditorGUILayout.Space();

            GUILayout.Label ( "<size=12><b>Paths</b></size>", CustomGUI.GetStyleWithRichText());

            if ( script.graphData.paths.Count > 0 )
            {
                showPathIDsInTheScene = EditorGUILayout.Toggle ( "Show Path IDs in scene", showPathIDsInTheScene );
                drawPathsInTheScene = EditorGUILayout.Toggle ( "Draw Paths", drawPathsInTheScene );
                showCostsInTheScene = EditorGUILayout.Toggle ( "Show Path Costs in scene", showCostsInTheScene );

                List<Path> paths = script.graphData.paths;
                for (int j = 0; j < paths.Count; j++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField ("\t" + "Path <Color=" + ConnectionTextcolor + ">" + paths[j].autoGeneratedID + "</Color>",  CustomGUI.GetStyleWithRichText(), GUILayout.Width ( 120f ) );

                        EditorGUILayout.LabelField ("From", EditorStyles.miniLabel, GUILayout.Width(30f) ); paths[j].IDOfA = EditorGUILayout.IntField (paths[j].IDOfA, GUILayout.Width(50f)  );
                        EditorGUILayout.LabelField ("To", EditorStyles.miniLabel, GUILayout.Width(25f) ); paths[j].IDOfB = EditorGUILayout.IntField (paths[j].IDOfB, GUILayout.Width(50f)  );
                        EditorGUILayout.LabelField ( "<Color=" + CostTextcolor + ">" + "Cost" + "</Color>", CustomGUI.GetStyleWithRichText(EditorStyles.miniLabel), GUILayout.Width(30f) ); paths[j].cost = EditorGUILayout.IntField (paths[j].cost, GUILayout.Width(50f)  );

                        EditorGUILayout.LabelField ("One Way", EditorStyles.miniLabel, GUILayout.Width(50f) ); paths[j].isOneWay = EditorGUILayout.Toggle (paths[j].isOneWay );

                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                            AddPath(j + 1);
                        if (GUILayout.Button("-", GUILayout.Width(25f)))
                            DeletePath(j);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else 
            {
                EditorGUILayout.LabelField("<Color=green> Paths are empty. Use <b>Connect Nodes</b> in scene view to create Paths!</Color>", CustomGUI.GetStyleWithRichText( CustomGUI.SetAlignmentForText(TextAnchor.MiddleCenter) ) );            
            }

            if ( GUI.changed )
                MarkThisDirty();
        }

    #endregion

    #region Scene Rendering Display Method

        void OnSceneGUI()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            {
                DrawGUIWindowOnScene();
                UpdateMouseInput();

                if (sceneMode == SceneMode.AddNode)
                {
                    DrawNodes( Color.green );
                }
                else if (sceneMode == SceneMode.EditNode)
                {
                    DrawNodes ( Color.magenta, true );
                }
                else if ( sceneMode == SceneMode.ConnectPath )
                {
                    DrawNodes( Color.green, false, script.graphData.GetNode( selectedNodeForConnectNodesMode ), Color.red );
                }
                else 
                    DrawNodes ( Color.gray );

                DrawPathLine();
            }

            CheckGUIChanged();
        }

        void CheckGUIChanged()
        {
            if (GUI.changed)
            {
                //SetLinePoints();
                //AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
            }
        }
        void DrawGUIWindowOnScene()
        {
            GUILayout.Window(1, new Rect(0f, 25f, 70f, 80f), 
                                                            delegate ( int windowID )
                                                            {
                                                                EditorGUILayout.BeginHorizontal();

                                                                sceneMode = (SceneMode)GUILayout.SelectionGrid((int)sceneMode, new string[] { "Add Node", "Move Node", "Connect Nodes", "None" }, 1);

                                                                GUI.color = Color.white;

                                                                EditorGUILayout.EndHorizontal();
                                                            }
                , "Mode");
            GUILayout.Window(2, new Rect(0, 155f, 70f, 80f), 
                                                            delegate(int windowID)
                                                            {
                                                                EditorGUILayout.BeginVertical();

                                                                if (GUILayout.Button("Delete Node"))
                                                                    DeleteNode();

                                                                if (GUILayout.Button("Delete Path"))
                                                                    DeletePath();

                                                                if (GUILayout.Button("Clear All"))
                                                                {
                                                                    ClearNodes();
                                                                    ClearPaths();
                                                                }

                                                                if (GUILayout.Button("Refresh Data"))
                                                                {
                                                                    script.graphData.Refresh();   
                                                                }
                                                                GUI.color = Color.white;

                                                                EditorGUILayout.EndVertical();
                                                            }
                , "");
        }
        

        void DrawNodes( Color color, bool canMove = false, Node selectedNode = null, Color colorForSelected = default(Color) )
        {
            Handles.color = color;
            foreach (var node in script.graphData.nodes)
            {
                if ( selectedNode != null && node == selectedNode )
                    Handles.color = colorForSelected;
                else 
                    Handles.color = color;

                if ( canMove ) 
                    node.position = Handles.FreeMoveHandle(node.position, Quaternion.identity, script.graphData.nodeSize, Vector3.zero, Handles.SphereCap);
                else
                    Handles.SphereCap(0, node.position, Quaternion.identity, script.graphData.nodeSize);
            }
            Handles.color = Color.white;

            // GUI display about the way points in the scene view
            DrawGUIDisplayForNodes();

            Handles.color = Color.white;
        }



        void DrawPathLine()
        {
            List<Path> paths = script.graphData.paths;
            List<Node> nodes = script.graphData.nodes;
            Vector3 currNode;
            Vector2 guiPosition;

            if (paths == null || nodes == null ) return;

            Handles.color = script.graphData.lineColor;
            Node a,b;

            

            for (int i = 0; i < paths.Count; i++)
            {
                a = b = null;

                if ( script.graphData.nodesSorted.ContainsKey(paths[i].IDOfA))
                    a = script.graphData.nodesSorted[paths[i].IDOfA];

                if ( script.graphData.nodesSorted.ContainsKey(paths[i].IDOfB))
                    b = script.graphData.nodesSorted[paths[i].IDOfB];

                if ( a != null && b != null && a != b ) 
                {
                    if ( drawPathsInTheScene )
                        Handles.DrawLine(a.position, b.position);

                    Handles.BeginGUI();
                    {
                        currNode = (a.position + b.position) / 2;

                        guiPosition = HandleUtility.WorldToGUIPoint ( currNode );

                        string str = "";

                        if ( showPathIDsInTheScene )
                            str += "<Color=" + ConnectionTextcolor + ">" + paths[i].autoGeneratedID.ToString() + "</Color>";
                        if ( showCostsInTheScene )
                        {
                            if ( !string.IsNullOrEmpty( str ) )
                                str += "<Color=" + "#ffffff" + ">" + "  Cost: " + "</Color>";

                            str += "<Color=" + CostTextcolor + ">" + paths[i].cost.ToString()  + "</Color>";
                        }

                        if ( !string.IsNullOrEmpty( str ) )
                            GUI.Label (new Rect (guiPosition.x - 10, guiPosition.y - 30, 40, 20 ),  str , CustomGUI.GetStyleWithRichText() );
                    }
                    Handles.EndGUI();
                    
                }
            }



            Handles.color = Color.white;
        }

        public void DrawGUIDisplayForNodes()
        {
            if ( !showNodeIDsInTheScene )
                return;

            Node currNode;
            Vector2 guiPosition;

            Handles.BeginGUI();
            for ( int i = 0; i < script.graphData.nodes.Count; i++ ) 
            {
                currNode = script.graphData.nodes[i];

                guiPosition = HandleUtility.WorldToGUIPoint ( currNode.position );
                
                GUI.Label( new Rect (guiPosition.x - 10, guiPosition.y - 30, 20, 20 ), "<Color=" + nodeTextcolor + ">" + currNode.autoGeneratedID.ToString() + "</Color>", CustomGUI.GetStyleWithRichText());
            }
            Handles.EndGUI();
        }

    #endregion

    #region Input Method

        void UpdateMouseInput()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                    OnMouseClick(e.mousePosition);
            }
            else if ( e.type == EventType.MouseUp )
            {
                MarkThisDirty();
                SceneView.RepaintAll();
            }
                
        }

        void OnMouseClick(Vector2 mousePos)
        {
            if( sceneMode == SceneMode.AddNode ) 
            {
                LayerMask backgroundLayerMask = 1 << script.gameObject.layer;

                Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000f, backgroundLayerMask))
                {
                    Vector3 hitPos = hit.point;
                    hitPos += ( -ray.direction.normalized ) * script.graphData.offsetFromTheGround;
                    AddNode(hitPos);
                }
            }
            else if ( sceneMode == SceneMode.ConnectPath )
            {
                LayerMask backgroundLayerMask = 1 << script.gameObject.layer;

                Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000f, backgroundLayerMask))
                {
                    Vector3 hitPos = hit.point;
                    TryAddPath ( hitPos );
                }
            }
        }

    #endregion

        
    #region Node and Path methods

        void AddNode(Vector3 position, int addIndex = -1)
        {
            if (addIndex == -1)
                script.graphData.nodes.Add( new Node( position ));
            else
                script.graphData.nodes.Insert(addIndex, new Node( position ));

            script.graphData.Refresh();
        }
        
        void DeleteNode(int removeIndex = -1)
        {
            List<Node> nodeList = script.graphData.nodes;
            if (nodeList == null || nodeList.Count == 0)
                return;

            if (removeIndex == -1) removeIndex = nodeList.Count - 1;
            nodeList.RemoveAt(removeIndex);
            
            script.graphData.Refresh();
        }

        void ClearNodes()
        {
            script.graphData.nodes.Clear();
        }

        void AddPath( int addIndex = -1, int from = -1, int to = -1)
        {
            if ( from != -1 && to != -1 )
            {
                if ( from == to ) 
                {
                    Debug.LogError("Error. Preventing from adding Path to the same node");
                    return;
                }
                Path pd = script.graphData.GetPathBetween ( from, to );
                if ( pd != null ) 
                {
                    Debug.LogError("Error. We already have a path between these nodes. Aborted");
                    return;
                }
            }

            if (addIndex == -1)
                script.graphData.paths.Add( new Path( from, to ));
            else
                script.graphData.paths.Insert(addIndex, new Path( from, to ));

            // script.pathData.firstHandles.Add(Vector3.left);
            // script.pathData.secondHandles.Add(Vector3.right);
            script.graphData.Refresh();
        }

        void DeletePath(int removeIndex = -1)
        {
            List<Path> pathList = script.graphData.paths;
            if (pathList == null || pathList.Count == 0)
                return;

            if (removeIndex == -1) removeIndex = pathList.Count - 1;
            pathList.RemoveAt(removeIndex);
            // script.pathData.firstHandles.RemoveAt(removeIndex);
            // script.pathData.secondHandles.RemoveAt(removeIndex);
            script.graphData.Refresh();
        }

        void ClearPaths()
        {
            script.graphData.paths.Clear();
            // script.pathData.firstHandles.Clear();
            // script.pathData.secondHandles.Clear();
        }

        void TryAddPath(Vector3 position )
        {
            Node selectedNode =  script.graphData.GetNode( script.FindNearestNode ( position ) );

            if ( selectedNode == null )
            {
                Debug.LogError("ERROR. could not find any nearest Node");
                return;
            }

            if ( selectedNodeForConnectNodesMode != -1 ) 
            {
                AddPath ( -1, selectedNodeForConnectNodesMode, selectedNode.autoGeneratedID ) ;
                selectedNodeForConnectNodesMode = -1;
            }
            else 
            {
                selectedNodeForConnectNodesMode = selectedNode.autoGeneratedID;
            }

        }

    


    #endregion


        private int selectedNodeForConnectNodesMode = -1;
        private bool showNodeIDsInTheScene = true;
        private bool showPathIDsInTheScene = true;
        private bool drawPathsInTheScene = true;
        private bool showCostsInTheScene = false;
        private bool showDefaultInspector = false;
    }
}