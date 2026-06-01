using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    [Header("绘画设置")]
    public Material blueLineMaterial;
    public Material orangeLineMaterial;
    public float lineWidth = 0.2f;
    public float minDrawDistance = 0.5f;
    public float platformHeight = 0.3f;

    [Header("相机引用")]
    public Camera playerCamera;

    [Header("特效设置")]
    public float blueSpeedMultiplier = 1.5f;
    public float orangeJumpMultiplier = 1.5f;
    public float effectDuration = 3f;

    [Header("平台掉落设置")]
    public float skySpawnHeight = 225f;      // 天空画线的生成高度
    public LayerMask surfaceMask = ~0;       // 检测表面的层级

    private LineRenderer currentLineRenderer;
    private List<Vector3> currentPoints = new List<Vector3>();
    private bool isDrawing = false;
    private Color currentColor;
    private GameObject currentPlatform;

    // 记录绘画模式
    private bool startedOnSurface = false;    // 是否从表面开始画
    private float surfaceStartHeight = 0f;    // 起始表面的高度

    public enum DrawColor
    {
        Blue,
        Orange
    }

    public DrawColor selectedColor = DrawColor.Blue;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedColor = DrawColor.Blue;
            Debug.Log("蓝色画笔 - 加速平台");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedColor = DrawColor.Orange;
            Debug.Log("橙色画笔 - 跳高平台");
        }

        if (Input.GetMouseButtonDown(1))
        {
            StartDrawing();
        }

        if (Input.GetMouseButton(1) && isDrawing)
        {
            ContinueDrawing();
        }

        if (Input.GetMouseButtonUp(1) && isDrawing)
        {
            FinishDrawing();
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryDeletePlatform();
        }
    }

    void StartDrawing()
    {
        isDrawing = true;
        currentPoints.Clear();

        currentColor = (selectedColor == DrawColor.Blue) ? Color.blue : new Color(1f, 0.5f, 0f);

        // 创建画线
        GameObject lineObj = new GameObject("DrawingLine");
        currentLineRenderer = lineObj.AddComponent<LineRenderer>();

        if (selectedColor == DrawColor.Blue && blueLineMaterial != null)
            currentLineRenderer.material = blueLineMaterial;
        else if (selectedColor == DrawColor.Orange && orangeLineMaterial != null)
            currentLineRenderer.material = orangeLineMaterial;

        currentLineRenderer.startColor = currentColor;
        currentLineRenderer.endColor = currentColor;
        currentLineRenderer.startWidth = lineWidth;
        currentLineRenderer.endWidth = lineWidth;
        currentLineRenderer.positionCount = 0;
        currentLineRenderer.useWorldSpace = true;

        // 检测第一个点：是否命中表面
        Vector3 firstPoint;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200f, surfaceMask))
        {
            // 命中表面 → 表面模式
            startedOnSurface = true;
            surfaceStartHeight = hit.point.y;
            firstPoint = hit.point;
            Debug.Log($"表面绘画模式，高度: {surfaceStartHeight}");
        }
        else
        {
            // 没命中 → 天空模式
            startedOnSurface = false;
            firstPoint = GetSkyPoint();
            Debug.Log("天空绘画模式，将在 Y=" + skySpawnHeight + " 生成");
        }

        currentPoints.Add(firstPoint);
        currentLineRenderer.positionCount = 1;
        currentLineRenderer.SetPosition(0, firstPoint);
    }

    void ContinueDrawing()
    {
        Vector3 newPoint;

        if (startedOnSurface)
        {
            // 表面模式：继续检测表面，保持初始高度
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 200f, surfaceMask))
            {
                newPoint = hit.point;
            }
            else
            {
                // 鼠标移出表面：在初始高度跟随鼠标
                Plane heightPlane = new Plane(Vector3.up, new Vector3(0, surfaceStartHeight, 0));
                float distance;
                if (heightPlane.Raycast(ray, out distance))
                {
                    newPoint = ray.GetPoint(distance);
                }
                else
                {
                    newPoint = currentPoints[currentPoints.Count - 1];
                }
            }
        }
        else
        {
            // 天空模式：自由画
            newPoint = GetSkyPoint();
        }

        if (Vector3.Distance(currentPoints[currentPoints.Count - 1], newPoint) < minDrawDistance)
            return;

        currentPoints.Add(newPoint);
        currentLineRenderer.positionCount = currentPoints.Count;
        currentLineRenderer.SetPosition(currentPoints.Count - 1, newPoint);
    }

    Vector3 GetSkyPoint()
    {
        Plane skyPlane = new Plane(playerCamera.transform.forward,
                                    playerCamera.transform.position + playerCamera.transform.forward * 50f);
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        float distance;

        if (skyPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return playerCamera.transform.position + playerCamera.transform.forward * 50f;
    }

    void FinishDrawing()
    {
        isDrawing = false;

        if (currentPoints.Count < 2)
        {
            if (currentLineRenderer != null)
                Destroy(currentLineRenderer.gameObject);
            return;
        }

        CreatePlatform();

        if (currentLineRenderer != null && currentPlatform != null)
        {
            currentLineRenderer.transform.parent = currentPlatform.transform;
        }
    }

    void CreatePlatform()
    {
        currentPlatform = new GameObject("PaintedPlatform");
        currentPlatform.layer = 8;

        float platformY;

        if (startedOnSurface)
        {
            // 表面模式：平台生成在起始表面高度
            platformY = surfaceStartHeight;
        }
        else
        {
            // 天空模式：平台生成在 skySpawnHeight，需要掉落
            platformY = skySpawnHeight;
            // 添加掉落脚本
            currentPlatform.AddComponent<FallingPlatform>();
        }

        // 把所有点投影到目标高度
        for (int i = 0; i < currentPoints.Count - 1; i++)
        {
            Vector3 start = currentPoints[i];
            Vector3 end = currentPoints[i + 1];

            start.y = platformY;
            end.y = platformY;

            CreateSegment(start, end);
        }

        // 添加效果脚本
        PlatformEffect effect = currentPlatform.AddComponent<PlatformEffect>();
        effect.platformColor = currentColor;
        effect.blueSpeedMultiplier = blueSpeedMultiplier;
        effect.orangeJumpMultiplier = orangeJumpMultiplier;
        effect.effectDuration = effectDuration;

        // 设置颜色
        foreach (Renderer rend in currentPlatform.GetComponentsInChildren<Renderer>())
        {
            if (rend != null && rend.material != null)
            {
                rend.material.color = currentColor;
            }
        }

        string mode = startedOnSurface ? "表面" : "天空(会掉落)";
        Debug.Log($"平台已生成 [{mode}]，高度: {platformY}");
    }

    void CreateSegment(Vector3 start, Vector3 end)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.transform.parent = currentPlatform.transform;
        segment.layer = 8;

        Vector3 midPoint = (start + end) / 2f;
        float length = Vector3.Distance(start, end);

        segment.transform.position = midPoint;

        Vector3 direction = (end - start).normalized;
        if (direction != Vector3.zero)
        {
            segment.transform.rotation = Quaternion.LookRotation(direction);
        }

        segment.transform.localScale = new Vector3(lineWidth * 2, platformHeight, length);

        Renderer rend = segment.GetComponent<Renderer>();
        if (rend != null)
        {
            if (selectedColor == DrawColor.Blue && blueLineMaterial != null)
                rend.material = new Material(blueLineMaterial);
            else if (selectedColor == DrawColor.Orange && orangeLineMaterial != null)
                rend.material = new Material(orangeLineMaterial);
            else
                rend.material.color = currentColor;
        }
    }

    void TryDeletePlatform()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Transform parent = hit.collider.transform.parent;
            if (parent != null && parent.name == "PaintedPlatform")
            {
                Destroy(parent.gameObject);
                Debug.Log("平台已删除");
            }
        }
    }
}