using UnityEngine;
using System.Collections.Generic;

public class ShadowSolidifier : MonoBehaviour
{
    [Header("引用")]
    public Transform player;               // 玩家Transform（可拖拽或自动查找）
    public Light shadowLight;              // 产生影子的灯光（需平行光或聚光灯）

    [Header("影子实体设置")]
    public GameObject shadowPrefab;        // 影子实体预制体（若为空则动态创建）
    public Material shadowMaterial;        // 影子材质（黑色半透明）
    public float shadowThickness = 0.1f;   // 影子平面厚度
    public int maxShadows = 3;             // 最大影子数量

    [Header("地面设置")]
    public LayerMask groundLayer = 1;      // 地面层（默认Default）
    public float groundYOffset = 0.05f;    // 影子生成后在地面上的抬高量

    // 当前存在的影子列表
    private List<GameObject> activeShadows = new List<GameObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("未找到玩家！请拖拽或设置 Tag 为 Player。");
        }

        if (shadowLight == null)
        {
            shadowLight = GetComponent<Light>();
            if (shadowLight == null)
                Debug.LogError("未指定灯光！请拖拽灯光到 shadowLight 字段，或将脚本挂在灯光物体上。");
        }

        // 如果没有提供预制体，则在运行时动态创建一个基础影子平面
        if (shadowPrefab == null)
        {
            shadowPrefab = CreateBaseShadowPrefab();
        }
    }

    void Update()
    {
        // 按 R 键确认生成影子实体
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (activeShadows.Count >= maxShadows)
            {
                // 移除最早的影子
                Destroy(activeShadows[0]);
                activeShadows.RemoveAt(0);
            }

            Vector3 shadowPosition = CalculateShadowPosition();
            if (shadowPosition != Vector3.zero) // 有效位置
            {
                GameObject newShadow = Instantiate(shadowPrefab, shadowPosition, Quaternion.identity);
                activeShadows.Add(newShadow);
            }
            else
            {
                Debug.Log("无法计算影子位置，请确保玩家站在灯光范围内且地面存在。");
            }
        }
    }

    // 计算玩家在灯光方向下投射到地面的影子落点
    Vector3 CalculateShadowPosition()
    {
        if (player == null || shadowLight == null) return Vector3.zero;

        Vector3 lightDir;
        // 确定光线方向：平行光用 forward，聚光灯/点光源用从灯光指向玩家的方向
        if (shadowLight.type == LightType.Directional)
            lightDir = shadowLight.transform.forward;
        else
            lightDir = (player.position - shadowLight.transform.position).normalized;

        // 从玩家位置沿灯光方向发射射线，碰到地面层即为影子落点
        Ray ray = new Ray(player.position, lightDir);
        RaycastHit hit;
        float maxDistance = 50f;
        if (Physics.Raycast(ray, out hit, maxDistance, groundLayer))
        {
            // 返回击中点并稍微抬高，防止穿模
            return hit.point + Vector3.up * groundYOffset;
        }
        else
        {
            // 如果没有直接命中，尝试计算玩家投影到水平面的位置（假设地面 y=0）
            // 这是一个备用方案
            if (Mathf.Abs(lightDir.y) > 0.001f)
            {
                float t = -player.position.y / lightDir.y;
                if (t > 0)
                    return player.position + lightDir * t + Vector3.up * groundYOffset;
            }
            return Vector3.zero;
        }
    }

    // 动态创建一个基础的影子四边形（带碰撞体）
    GameObject CreateBaseShadowPrefab()
    {
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadow.name = "ShadowPlatform";
        shadow.transform.localScale = new Vector3(2f, 2f, 1f);   // 可调整影子尺寸

        // 添加碰撞体（Quad默认有MeshCollider，也可以用BoxCollider）
        Destroy(shadow.GetComponent<MeshCollider>());
        BoxCollider box = shadow.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, shadowThickness, 1f);
        box.center = new Vector3(0f, -shadowThickness * 0.5f, 0f);

        // 设置材质（黑色半透明）
        MeshRenderer renderer = shadow.GetComponent<MeshRenderer>();
        if (shadowMaterial != null)
            renderer.material = shadowMaterial;
        else
        {
            // 创建一个简单的黑色半透明材质
            Material mat = new Material(Shader.Find("Transparent/Diffuse"));
            mat.color = new Color(0, 0, 0, 0.7f);
            renderer.material = mat;
        }

        // 默认不激活，仅作为预制体模板使用
        shadow.SetActive(false);
        return shadow;
    }

    // 公开方法：获取当前影子数量（可选）
    public int GetShadowCount() => activeShadows.Count;
}