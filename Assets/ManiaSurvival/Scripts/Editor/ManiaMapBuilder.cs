using UnityEditor;
using UnityEngine;

public class ManiaMapBuilder : EditorWindow
{
    private GameObject selectedPrefab;
    private bool paintModeEnabled;
    private float gridSize = 1f;
    private float yOffset = 0f;
    private Transform parentTransform;
    private bool randomYRotation = false;
    private bool drawGridPreview = true;
    private int gridPreviewExtent = 10;
    private LayerMask placementMask = ~0;

    [MenuItem("Mania Survival/Mania Map Builder")]
    public static void ShowWindow()
    {
        ManiaMapBuilder window = GetWindow<ManiaMapBuilder>("Mania Map Builder");
        window.minSize = new Vector2(280f, 360f);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Mania Map Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click in the Scene View to place the Selected Prefab.\nHold Shift + Click to delete a placed prefab.",
            MessageType.Info);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Selected Prefab", selectedPrefab, typeof(GameObject), false);
        parentTransform = (Transform)EditorGUILayout.ObjectField(
            "Parent Transform", parentTransform, typeof(Transform), true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Painting", EditorStyles.boldLabel);
        bool newPaintMode = EditorGUILayout.Toggle("Paint Mode", paintModeEnabled);
        if (newPaintMode != paintModeEnabled)
        {
            paintModeEnabled = newPaintMode;
            SceneView.RepaintAll();
        }

        randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);
        placementMask = LayerMaskField("Placement Layers", placementMask);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        gridSize = EditorGUILayout.FloatField("Grid Size (units)", gridSize);
        if (gridSize < 0.01f)
        {
            gridSize = 0.01f;
        }

        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        drawGridPreview = EditorGUILayout.Toggle("Draw Grid Preview", drawGridPreview);
        gridPreviewExtent = EditorGUILayout.IntSlider("Grid Preview Extent", gridPreviewExtent, 1, 50);

        EditorGUILayout.Space();

        if (paintModeEnabled)
        {
            EditorGUILayout.HelpBox(
                selectedPrefab == null
                    ? "Assign a Selected Prefab to start painting."
                    : "Paint Mode is ON. Click in the Scene View to place. Shift + Click to delete.",
                selectedPrefab == null ? MessageType.Warning : MessageType.None);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!paintModeEnabled)
        {
            return;
        }

        Event currentEvent = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        if (currentEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlId);
        }

        if (!TryGetSnappedPointUnderMouse(currentEvent.mousePosition, out Vector3 snappedPoint, out GameObject hitObject))
        {
            sceneView.Repaint();
            return;
        }

        DrawPlacementPreview(snappedPoint);

        if (drawGridPreview)
        {
            DrawGridPreview(snappedPoint);
        }

        sceneView.Repaint();

        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
        {
            return;
        }

        if (currentEvent.shift)
        {
            DeleteHitPrefab(hitObject);
        }
        else
        {
            PlacePrefab(snappedPoint);
        }

        currentEvent.Use();
    }

    private bool TryGetSnappedPointUnderMouse(Vector2 mousePosition, out Vector3 snappedPoint, out GameObject hitObject)
    {
        snappedPoint = Vector3.zero;
        hitObject = null;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementMask, QueryTriggerInteraction.Ignore))
        {
            hitObject = hit.collider != null ? hit.collider.gameObject : null;
            snappedPoint = SnapToGrid(hit.point);
            snappedPoint.y += yOffset;
            return true;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            snappedPoint = SnapToGrid(worldPoint);
            snappedPoint.y += yOffset;
            return true;
        }

        return false;
    }

    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        float snap = Mathf.Max(0.01f, gridSize);
        float snappedX = Mathf.Round(worldPosition.x / snap) * snap;
        float snappedZ = Mathf.Round(worldPosition.z / snap) * snap;
        return new Vector3(snappedX, worldPosition.y, snappedZ);
    }

    private void PlacePrefab(Vector3 position)
    {
        if (selectedPrefab == null)
        {
            Debug.LogWarning("[ManiaMapBuilder] No Selected Prefab assigned.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        if (instance == null)
        {
            instance = Instantiate(selectedPrefab);
        }

        instance.transform.position = position;

        if (randomYRotation)
        {
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else
        {
            instance.transform.rotation = Quaternion.identity;
        }

        if (parentTransform != null)
        {
            instance.transform.SetParent(parentTransform, true);
        }

        Undo.RegisterCreatedObjectUndo(instance, "Place Map Prefab");
        Selection.activeGameObject = instance;
    }

    private void DeleteHitPrefab(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return;
        }

        GameObject rootToDelete = ResolveDeletionRoot(hitObject);
        if (rootToDelete == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(rootToDelete);
    }

    private GameObject ResolveDeletionRoot(GameObject hitObject)
    {
        Transform nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(hitObject)?.transform;
        if (nearestPrefabRoot != null)
        {
            if (parentTransform == null || nearestPrefabRoot.IsChildOf(parentTransform))
            {
                return nearestPrefabRoot.gameObject;
            }
        }

        if (parentTransform != null)
        {
            Transform candidate = hitObject.transform;
            while (candidate != null)
            {
                if (candidate.parent == parentTransform)
                {
                    return candidate.gameObject;
                }

                candidate = candidate.parent;
            }

            return null;
        }

        return hitObject.transform.root.gameObject;
    }

    private void DrawPlacementPreview(Vector3 snappedPoint)
    {
        float snap = Mathf.Max(0.01f, gridSize);
        Vector3 halfSize = new Vector3(snap * 0.5f, 0.02f, snap * 0.5f);

        Color fillColor = new Color(0.2f, 0.9f, 0.4f, 0.25f);
        Color outlineColor = new Color(0.2f, 0.9f, 0.4f, 0.9f);

        if (Event.current != null && Event.current.shift)
        {
            fillColor = new Color(0.9f, 0.25f, 0.25f, 0.25f);
            outlineColor = new Color(0.9f, 0.25f, 0.25f, 0.9f);
        }

        Vector3[] verts = new Vector3[]
        {
            snappedPoint + new Vector3(-halfSize.x, 0f, -halfSize.z),
            snappedPoint + new Vector3(-halfSize.x, 0f,  halfSize.z),
            snappedPoint + new Vector3( halfSize.x, 0f,  halfSize.z),
            snappedPoint + new Vector3( halfSize.x, 0f, -halfSize.z)
        };

        Handles.DrawSolidRectangleWithOutline(verts, fillColor, outlineColor);
    }

    private void DrawGridPreview(Vector3 centerPoint)
    {
        float snap = Mathf.Max(0.01f, gridSize);
        int extent = Mathf.Max(1, gridPreviewExtent);
        Vector3 origin = new Vector3(
            Mathf.Round(centerPoint.x / snap) * snap,
            centerPoint.y,
            Mathf.Round(centerPoint.z / snap) * snap);

        Color previousColor = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.2f);

        for (int i = -extent; i <= extent; i++)
        {
            float offset = i * snap;
            Vector3 lineStartX = origin + new Vector3(-extent * snap, 0f, offset);
            Vector3 lineEndX = origin + new Vector3(extent * snap, 0f, offset);
            Handles.DrawLine(lineStartX, lineEndX);

            Vector3 lineStartZ = origin + new Vector3(offset, 0f, -extent * snap);
            Vector3 lineEndZ = origin + new Vector3(offset, 0f, extent * snap);
            Handles.DrawLine(lineStartZ, lineEndZ);
        }

        Handles.color = previousColor;
    }

    private static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        string[] layerNames = new string[32];
        int maskFromUI = 0;
        int displayBit = 0;

        for (int layer = 0; layer < 32; layer++)
        {
            string name = LayerMask.LayerToName(layer);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            layerNames[displayBit] = name;
            if ((layerMask.value & (1 << layer)) != 0)
            {
                maskFromUI |= 1 << displayBit;
            }

            displayBit++;
        }

        System.Array.Resize(ref layerNames, displayBit);
        int newMaskFromUI = EditorGUILayout.MaskField(label, maskFromUI, layerNames);

        int newMask = 0;
        displayBit = 0;
        for (int layer = 0; layer < 32; layer++)
        {
            string name = LayerMask.LayerToName(layer);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if ((newMaskFromUI & (1 << displayBit)) != 0)
            {
                newMask |= 1 << layer;
            }

            displayBit++;
        }

        return newMask;
    }
}
