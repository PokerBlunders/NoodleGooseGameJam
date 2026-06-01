using UnityEngine;
using System.Collections.Generic;

public class WaterWave : MonoBehaviour
{
    [SerializeField] private float expandSpeed = 5f;
    [SerializeField] private float maxRadius = 10f;
    [SerializeField] private float startScale = 0.1f;

    private Vector3 originPosition;
    private float currentRadius = 0f;
    private float birthTime;
    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();

    void Start()
    {
        originPosition = transform.position;
        birthTime = Time.time;
        transform.localScale = Vector3.one * startScale;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }

        Debug.Log($"水波生成！位置: {transform.position}");
    }

    void Update()
    {
        float elapsed = Time.time - birthTime;
        currentRadius = elapsed * expandSpeed;

        if (currentRadius >= maxRadius)
        {
            Debug.Log($"水波消失。检测到 {hitObjects.Count} 个物体");
            Destroy(gameObject);
            return;
        }

        // 更新大小
        float scale = currentRadius;
        transform.localScale = new Vector3(scale, scale, scale);

        // 淡出
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = startColor.a * (1f - currentRadius / maxRadius);
            spriteRenderer.color = color;
        }

        // 检测物体
        Collider[] hitColliders = Physics.OverlapSphere(originPosition, currentRadius);

        foreach (Collider col in hitColliders)
        {
            if (hitObjects.Contains(col.gameObject) || col.gameObject == gameObject)
                continue;

            hitObjects.Add(col.gameObject);

            // 计算距离和音量
            float distance = Vector3.Distance(originPosition, col.transform.position);
            float volume = Mathf.Clamp01(1f - distance / maxRadius);

            // Debug输出
            Debug.Log($"?? 水波碰到 [{col.tag}] {col.name} | " +
                     $"坐标: {col.transform.position} | " +
                     $"距离: {distance:F2}m | " +
                     $"声音大小: {volume:F2}");
        }
    }
}