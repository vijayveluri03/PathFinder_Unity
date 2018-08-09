using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

using Mr1;

[CustomEditor(typeof(WaypointManager))]
public class WaypointManagerEditor : Editor 
{
    enum SceneMode
    {
        Add,
        Edit,
    }

    SceneMode sceneMode;
    WaypointManager script;
    const string WayPointTextcolor = "#ff00ffff";
    const string ConnectionTextcolor = "#00ffffff";
    const string CostTextcolor = "#0000ffff";


#region Instantiation and Asset management 

    [MenuItem("GameObject/Create Waypoint Manager")]
    public static void CreateWaypointManager()
    {
        if (GameObject.FindObjectOfType<WaypointManager>() == null)
        {
            var managerGo = new GameObject("WaypointManager");
            var manager = managerGo.AddComponent<WaypointManager>();
            var boxCollider = managerGo.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(100f, 100f, 1f);
            boxCollider.isTrigger = true;
        }
        else
            Debug.LogError("Waypoint Manager already exists!");
    }

    PathData CreatePathAsset()
    {
        string strAssetPath = EditorUtility.SaveFilePanelInProject("New Path", "NewPath", "asset", "");
        
        if (string.IsNullOrEmpty(strAssetPath))
            return null; 

        strAssetPath = AssetDatabase.GenerateUniqueAssetPath(strAssetPath);

        int startIndex = strAssetPath.LastIndexOf("/") + 1;
        int length = strAssetPath.LastIndexOf(".") - startIndex;
        string strAssetName = strAssetPath.Substring(startIndex, length);

        PathData newPath = ScriptableObject.CreateInstance<PathData>();
        newPath.pathName = strAssetName;

        AssetDatabase.CreateAsset(newPath, strAssetPath);
        AssetDatabase.SaveAssets();

        return newPath;
    }

    PathData LoadPathAsset()
    {
        string strAssetPath = EditorUtility.OpenFilePanel("Load Path", "Assets/", "asset");
        strAssetPath = strAssetPath.Substring(strAssetPath.IndexOf("Assets/"));

        if (string.IsNullOrEmpty(strAssetPath))
            return null;
        
        PathData loadedPath = (PathData)AssetDatabase.LoadAssetAtPath(strAssetPath, typeof(PathData));

        int startIndex = strAssetPath.LastIndexOf("/") + 1;
        int length = strAssetPath.LastIndexOf(".") - startIndex;
        string strAssetName = strAssetPath.Substring(startIndex, length);

        loadedPath.pathName = strAssetName;

        return loadedPath;
    }
    
    void SavePathAsset()
    {
        AssetDatabase.SaveAssets();
        
        foreach (var path in script.pathList)
            EditorUtility.SetDirty(path);
    }

    void OnEnable()
    {
   
        sceneMode = SceneMode.Edit;
        script = target as WaypointManager;
        if (script.pathList == null) script.pathList = new List<PathData>();
        for (int i = 0; i < script.pathList.Count; i++)
        {
            if (script.pathList[i] == null) script.pathList.RemoveAt(i);
        }

        //script.selected = null;
        foreach(var path in script.pathList)
        {
            if (path != null)
            {
                String strAssetPath = AssetDatabase.GetAssetPath(path);
                int startIndex = strAssetPath.LastIndexOf("/") + 1;
                int length = strAssetPath.LastIndexOf(".") - startIndex;
                path.pathName = strAssetPath.Substring(startIndex, length);
            }
        }

        if ( script.selected != null ) 
            script.selected.Refresh();
    }

    void OnDisable()
    {
       
    }


#endregion


    
#region OnInspectorGUI's Display Method

    public override void OnInspectorGUI()
    {
        RenderButtons();

        CustomGUI.DrawSeparator(Color.gray);

        ShowPointsAndPathInInspector();
    }

    void RenderButtons()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("New Path"))
        {
            PathData newPath = CreatePathAsset();
            if (newPath != null) script.pathList.Add(newPath);
        }
        if (GUILayout.Button("Load Path"))
        {
            PathData loadedPath = LoadPathAsset();
            if (loadedPath != null)
            {
                if (!script.pathList.Contains(loadedPath))
                    script.pathList.Add(loadedPath);
            }
        }
        if (GUILayout.Button("Save Path"))
        {
            SavePathAsset();
        }

        GUILayout.EndHorizontal();
    }

    void ShowPointsAndPathInInspector()
    {
        for (int i = 0; i < script.pathList.Count; i++)
        {
            Action delAction = () => { script.pathList.RemoveAt(i); };
            if (script.pathList[i] == null || string.IsNullOrEmpty(script.pathList[i].pathName)) continue;
            if (CustomGUI.HeaderButton(script.pathList[i].pathName, null, delAction))
            {
                script.selected = script.pathList[i];
                script.selected.pointSize = EditorGUILayout.Slider("Point Size", script.selected.pointSize, 0.1f, 3f);
                script.selected.lineColor = EditorGUILayout.ColorField("Path Color", script.selected.lineColor);
                script.selected.lineType = (PathLineType)EditorGUILayout.EnumPopup("Path Type", script.selected.lineType);


                GUILayout.Label ( "<size=15><b>Way Points</b></size>", Utility.GetStyleWithRichText());

                
                showPointIDsInTheScene = EditorGUILayout.Toggle ( "Show Point IDs in scene", showPointIDsInTheScene );

                List<WayPoint> wayPointList = script.selected.points;
                for (int j = 0; j < wayPointList.Count; j++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField ("\t" + "Point <Color=" + WayPointTextcolor + ">" + wayPointList[j].autoGeneratedID + "</Color>", Utility.GetStyleWithRichText(), GUILayout.Width ( 120f ) );

                        wayPointList[j].position = EditorGUILayout.Vector3Field("", wayPointList[j].position);
                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                            AddWaypoint(wayPointList[j].position + Vector3.right + Vector3.up, j + 1);
                        if (GUILayout.Button("-", GUILayout.Width(25f)))
                            DeleteWaypoint(j);
                    }
                    GUILayout.EndHorizontal();
                }


                GUILayout.Label ( "<size=15><b>Way Paths</b></size>", Utility.GetStyleWithRichText());
                showPathIDsInTheScene = EditorGUILayout.Toggle ( "Show Path IDs in scene", showPathIDsInTheScene );
                showCostsInTheScene = EditorGUILayout.Toggle ( "Show Path Costs in scene", showCostsInTheScene );

                List<WayPath> wayPaths = script.selected.paths;
                for (int j = 0; j < wayPaths.Count; j++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField ("\t" + "Path <Color=" + ConnectionTextcolor + ">" + wayPaths[j].autoGeneratedID + "</Color>", Utility.GetStyleWithRichText(), GUILayout.Width ( 120f ) );

                        EditorGUILayout.LabelField ("From", EditorStyles.miniLabel, GUILayout.Width(30f) ); wayPaths[j].IDOfA = EditorGUILayout.IntField (wayPaths[j].IDOfA, GUILayout.Width(50f)  );
                        EditorGUILayout.LabelField ("To", EditorStyles.miniLabel, GUILayout.Width(25f) ); wayPaths[j].IDOfB = EditorGUILayout.IntField (wayPaths[j].IDOfB, GUILayout.Width(50f)  );
                        EditorGUILayout.LabelField ( "<Color=" + CostTextcolor + ">" + "Cost" + "</Color>", Utility.GetStyleWithRichText(EditorStyles.miniLabel), GUILayout.Width(30f) ); wayPaths[j].cost = EditorGUILayout.IntField (wayPaths[j].cost, GUILayout.Width(50f)  );

                        EditorGUILayout.LabelField ("One Way", EditorStyles.miniLabel, GUILayout.Width(50f) ); wayPaths[j].isOneWay = EditorGUILayout.Toggle (wayPaths[j].isOneWay );

                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                            AddWayPath(j + 1);
                        if (GUILayout.Button("-", GUILayout.Width(25f)))
                            DeleteWayPath(j);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }

#endregion

#region Scene Rendering Display Method

    void OnSceneGUI()
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        if (script.selected != null)
        {
            DrawWindow();
            UpdateMouseInput();

            if (sceneMode == SceneMode.Add)
            {
                DrawWaypointInAddMode();
            }
            else if (sceneMode == SceneMode.Edit)
            {
                DrawHandlePointInEditMode();
            }

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
    void DrawWindow()
    {
        GUILayout.Window(1, new Rect(0f, 25f, 70f, 80f), DoWaypointWindow, script.selected.pathName);
    }

    void DoWaypointWindow(int windowID)
    {
        EditorGUILayout.BeginVertical();

        sceneMode = (SceneMode)GUILayout.SelectionGrid((int)sceneMode, System.Enum.GetNames(typeof(SceneMode)), 1);

        if (GUILayout.Button("Add - Immediate"))
            AddImmediateWaypoint();

        if (GUILayout.Button("Del"))
            DeleteWaypoint();

        if (GUILayout.Button("Clear"))
        {
            ClearWaypoint();
            ClearWayPath();
        }

		if (GUILayout.Button("Refresh"))
        {
            if ( script.selected != null ) 
                script.selected.Refresh();   
        }

        if (GUILayout.Button("Do something "))
		{
            string str = "";
            foreach ( var a in script.FindShortedPath( 1, 8 ) ) 
            {
                str += "=>" + a.autoGeneratedID.ToString();
            }
            Debug.LogWarning("Path:" + str);
            // {
            //     float startTime = Time.realtimeSinceStartup;
            //     Debug.Log("Hello: " + Time.realtimeSinceStartup );
            //     script.FindShortestPathAsynchronous( 1, 8, delegate ( List<WayPoint> wayPoints ) 
            //     { 
            //         string str = "";
            //         foreach ( var a in wayPoints ) 
            //         {
            //             str += "=>" + a.autoGeneratedID.ToString();
            //         }
            //         Debug.LogWarning("Path: " + str);
            //         Debug.Log("Time Taken:" + (Time.realtimeSinceStartup - startTime));
            //     } );
            // }
        }

        GUI.color = Color.green;
        script.selected.lineType = (PathLineType)EditorGUILayout.EnumPopup(script.selected.lineType);
        GUI.color = Color.white;

        EditorGUILayout.EndVertical();
    }

    void DrawWaypointInAddMode()
    {
        Handles.color = Color.green;
        foreach (var point in script.selected.points)
        {
            Handles.SphereCap(0, point.position, Quaternion.identity, script.selected.pointSize);
        }
        Handles.color = Color.white;

        // GUI display about the way points in the scene view
        DrawGUIDisplayForPoints();

        Handles.color = Color.white;
    }

    void DrawPathLine()
    {
        List<WayPath> paths = script.selected.paths;
        List<WayPoint> linePoints = script.selected.points;
        Vector3 currPoint;
        Vector2 guiPosition;

        if (paths == null || linePoints == null ) return;

        Handles.color = script.selected.lineColor;
        WayPoint a,b;

        

        for (int i = 0; i < paths.Count; i++)
        {
            a = b = null;

            if ( script.selected.pointsSorted.ContainsKey(paths[i].IDOfA))
                a = script.selected.pointsSorted[paths[i].IDOfA];

            if ( script.selected.pointsSorted.ContainsKey(paths[i].IDOfB))
                b = script.selected.pointsSorted[paths[i].IDOfB];

            if ( a != null && b != null && a != b ) 
            {
                Handles.DrawLine(a.position, b.position);

                Handles.BeginGUI();
                {
                    currPoint = (a.position + b.position) / 2;

                    guiPosition = HandleUtility.WorldToGUIPoint ( currPoint );

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
                        GUI.Label (new Rect (guiPosition.x - 10, guiPosition.y - 30, 40, 20 ),  str , Utility.GetStyleWithRichText() );
                }
                Handles.EndGUI();
                
            }
        }



        Handles.color = Color.white;
    }

    public void DrawGUIDisplayForPoints()
    {
        if ( !showPointIDsInTheScene )
            return;

        WayPoint currPoint;
        Vector2 guiPosition;

        Handles.BeginGUI();
        for ( int i = 0; i < script.selected.points.Count; i++ ) 
        {
            currPoint = script.selected.points[i];

            guiPosition = HandleUtility.WorldToGUIPoint ( currPoint.position );
            
            GUI.Label( new Rect (guiPosition.x - 10, guiPosition.y - 30, 20, 20 ), "<Color=" + WayPointTextcolor + ">" + currPoint.autoGeneratedID.ToString() + "</Color>", Utility.GetStyleWithRichText());
        }
        Handles.EndGUI();
    }

    void DrawHandlePointInEditMode()
    {
        List<WayPoint> wayPoints = script.selected.points;

        for (int i = 0; i < wayPoints.Count; i++)
        {
            Handles.color = Color.magenta;
            wayPoints[i].position = Handles.FreeMoveHandle(wayPoints[i].position, Quaternion.identity, script.selected.pointSize, Vector3.zero, Handles.SphereCap);

            // if (script.selected.lineType == PathLineType.BezierCurve)
            // {
            //     Vector3 firstControlPoint = wayPoints[i] + script.selected.firstHandles[i];
            //     Vector3 secondControlPoint = wayPoints[i] + script.selected.secondHandles[i];

            //     Handles.color = Color.gray;
            //     if (i != 0)
            //     {
            //         Vector3 movedPoint = Handles.FreeMoveHandle(firstControlPoint, Quaternion.identity, script.selected.pointSize, Vector3.zero, Handles.SphereCap);
            //         if (firstControlPoint != movedPoint)
            //         {
            //             firstControlPoint = movedPoint - wayPoints[i];

            //             Quaternion qRot = Quaternion.FromToRotation(script.selected.firstHandles[i], firstControlPoint);
            //             script.selected.secondHandles[i] = qRot * script.selected.secondHandles[i];
            //             script.selected.firstHandles[i] = firstControlPoint;
            //         }
            //         Handles.DrawLine(wayPoints[i], firstControlPoint);
            //     }
            //     if (i != wayPoints.Count - 1)
            //     {
            //         Vector3 movedPoint = Handles.FreeMoveHandle(secondControlPoint, Quaternion.identity, script.selected.pointSize, Vector3.zero, Handles.SphereCap);
            //         if (secondControlPoint != movedPoint)
            //         {
            //             secondControlPoint = movedPoint - wayPoints[i];

            //             Quaternion qRot = Quaternion.FromToRotation(script.selected.secondHandles[i], secondControlPoint);
            //             script.selected.firstHandles[i] = qRot * script.selected.firstHandles[i];
            //             script.selected.secondHandles[i] = secondControlPoint;
            //         }
            //         Handles.DrawLine(wayPoints[i], secondControlPoint);
            //     }
            //     Handles.color = Color.white;
            // }
        }

        Handles.color = Color.white;

        // GUI display about the way points in the scene view
        Handles.BeginGUI();
        DrawGUIDisplayForPoints();

        Handles.color = Color.white;
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
            EditorUtility.SetDirty(script);
        }
            
    }

    void OnMouseClick(Vector2 mousePos)
    {
        if( sceneMode != SceneMode.Add ) 
            return;

        LayerMask backgroundLayerMask = 1 << script.gameObject.layer;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f, backgroundLayerMask))
        {
            Vector3 hitPos = hit.point;
            AddWaypoint(hitPos);
        }
    }

#endregion

    
    
 

#region Line Points Setting Method
    void SetStraightLine()
    {
        // List<Vector3> wayPoints = script.selected.points;
        // if (wayPoints.Count < 2)
        //     return;

        // for (int i = 0; i < wayPoints.Count-1; i++)
        // {
        //     for (float t = 0f; t <= 1.0f; t += 0.05f)
        //     {
        //         Vector3 pt = wayPoints[i] * (1f - t) + wayPoints[i + 1] * t;
        //         script.selected.linePoints.Add(pt);
        //     }
        // }

        // script.selected.linePoints.Add(wayPoints[wayPoints.Count - 1]);
    }

    void SetCatmullRomCurveLine()
    {
        // List<Vector3> wayPoints = script.selected.points;

        // if (wayPoints.Count < 3)
        //     return;

        // Vector3[] catmullRomPoints = new Vector3[wayPoints.Count + 2];
        // wayPoints.CopyTo(catmullRomPoints, 1);

        // int endIndex = catmullRomPoints.Length - 1;

        // catmullRomPoints[0] = catmullRomPoints[1] + (catmullRomPoints[1] - catmullRomPoints[2]) + (catmullRomPoints[3] - catmullRomPoints[2]);
        // catmullRomPoints[endIndex] = catmullRomPoints[endIndex - 1] + (catmullRomPoints[endIndex - 1] - catmullRomPoints[endIndex - 2])
        //                         + (catmullRomPoints[endIndex - 3] - catmullRomPoints[endIndex - 2]);

        // script.selected.linePoints.Add(wayPoints[0]);

        // for (int i = 0; i < catmullRomPoints.Length - 3; i++)
        // {
        //     for (float t = 0.05f; t <= 1.0f; t += 0.05f)
        //     {
        //         Vector3 pt = ComputeCatmullRom(catmullRomPoints[i], catmullRomPoints[i + 1], catmullRomPoints[i + 2], catmullRomPoints[i + 3], t);
        //         script.selected.linePoints.Add(pt);
        //     }
        // }

        // script.selected.linePoints.Add(wayPoints[wayPoints.Count - 1]);
    }

    Vector3 ComputeCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 pt = 0.5f * ((-p0 + 3f * p1 - 3f * p2 + p3) * t3
                    + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                    + (-p0 + p2) * t
                    + 2f * p1);

        return pt;
    }

    void SetBezierCurveLine()
    {
        // List<Vector3> wayPoints = script.selected.points;
        // List<Vector3> firstControls = script.selected.firstHandles;
        // List<Vector3> secondControls = script.selected.secondHandles;

        // if (wayPoints.Count < 2)
        //     return;

        // script.selected.linePoints.Add(wayPoints[0]);

        // for (int i = 0; i < wayPoints.Count - 1; i++)
        // {
        //     Vector3 waypoint1 = wayPoints[i];
        //     Vector3 waypoint2 = wayPoints[i + 1];
        //     Vector3 controlPoint1 = wayPoints[i] + secondControls[i];
        //     Vector3 controlPoint2 = wayPoints[i + 1] + firstControls[i + 1];

        //     for (float t = 0.05f; t <= 1.0f; t += 0.05f)
        //     {
        //         Vector3 pt = ComputeBezier(waypoint1, controlPoint1, controlPoint2, waypoint2, t);

        //         script.selected.linePoints.Add(pt);
        //     }
        // }

        // script.selected.linePoints.Add(wayPoints[wayPoints.Count-1]);
    }

    Vector3 ComputeBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t * t * t;
        float _t2 = (1 - t) * (1 - t);
        float _t3 = (1 - t) * (1 - t) * (1 - t);

        return p0 * _t3 + 3f * p1 * t * _t2 + 3 * p2 * t2 * (1 - t) + p3 * t3;
    }

#endregion

#region Waypoint and WayPath Method

    void AddWaypoint(Vector3 position, int addIndex = -1)
    {
        if (addIndex == -1)
            script.selected.points.Add( new WayPoint( position ));
        else
            script.selected.points.Insert(addIndex, new WayPoint( position ));

        if ( script.selected != null ) 
            script.selected.Refresh();
    }
	
	void AddImmediateWaypoint()
    {
        Vector3 position = Vector3.zero;;

        const float distanceOffset = 5;

        if (script.selected.points == null || script.selected.points.Count == 0)
        {
            position = Vector3.zero;
        }
        else if ( script.selected.points.Count == 1 )
        {
            position += Vector3.right * distanceOffset;
        }
        else 
        {
            position = script.selected.points[script.selected.points.Count - 1].position;
            Vector3 dir = ( script.selected.points[script.selected.points.Count - 1].position - script.selected.points[script.selected.points.Count - 2].position ).normalized;
            position += dir * distanceOffset;
        }

        script.selected.points.Add( new WayPoint( position ));

        if ( script.selected != null ) 
            script.selected.Refresh();
    }

    void DeleteWaypoint(int removeIndex = -1)
    {
        List<WayPoint> wayPointList = script.selected.points;
        if (wayPointList == null || wayPointList.Count == 0)
            return;

        if (removeIndex == -1) removeIndex = wayPointList.Count - 1;
        wayPointList.RemoveAt(removeIndex);
        
        if ( script.selected != null ) 
            script.selected.Refresh();
    }

    void ClearWaypoint()
    {
        script.selected.points.Clear();
    }

    void AddWayPath( int addIndex = -1)
    {
        if (addIndex == -1)
            script.selected.paths.Add( new WayPath( -1, -1 ));
        else
            script.selected.paths.Insert(addIndex, new WayPath( -1, -1 ));

        // script.selected.firstHandles.Add(Vector3.left);
        // script.selected.secondHandles.Add(Vector3.right);
        if ( script.selected != null ) 
            script.selected.Refresh();
    }

    void DeleteWayPath(int removeIndex = -1)
    {
        List<WayPath> wayPathList = script.selected.paths;
        if (wayPathList == null || wayPathList.Count == 0)
            return;

        if (removeIndex == -1) removeIndex = wayPathList.Count - 1;
        wayPathList.RemoveAt(removeIndex);
        // script.selected.firstHandles.RemoveAt(removeIndex);
        // script.selected.secondHandles.RemoveAt(removeIndex);
        if ( script.selected != null ) 
            script.selected.Refresh();
    }

    void ClearWayPath()
    {
        script.selected.paths.Clear();
        // script.selected.firstHandles.Clear();
        // script.selected.secondHandles.Clear();
    }


#endregion

#region Path finding 

   

#endregion

    private bool showPointIDsInTheScene = true;
    private bool showPathIDsInTheScene = true;
    private bool showCostsInTheScene = false;
}
