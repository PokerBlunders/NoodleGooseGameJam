using UnityEngine;

public class LightMover : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3f;          // 移动速度
    public float minX = -5f;              // 左边界
    public float maxX = 5f;               // 右边界

    void Update()
    {
        float moveInput = 0f;

        if (Input.GetKey(KeyCode.Q))
            moveInput = -1f;
        if (Input.GetKey(KeyCode.E))
            moveInput = 1f;

        if (moveInput != 0f)
        {
            Vector3 newPos = transform.position;
            newPos.x += moveInput * moveSpeed * Time.deltaTime;
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            transform.position = newPos;
        }
    }
}