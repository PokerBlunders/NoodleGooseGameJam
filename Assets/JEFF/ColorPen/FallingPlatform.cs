using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    public float fallDelay = 0.5f;
    public float fallSpeed = 50f;      // 掉落速度
    public float destroyY = -200f;     // 掉到很低才销毁
    public bool bounceOnLand = false;  // 落地是否弹一下

    private bool isFalling = false;
    private float timer = 0f;
    private Rigidbody rb;
    private bool hasLanded = false;

    void Start()
    {
        // 添加 Rigidbody
        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.mass = 500f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // 不旋转
    }

    void Update()
    {
        if (!isFalling && !hasLanded)
        {
            timer += Time.deltaTime;
            if (timer >= fallDelay)
            {
                StartFalling();
            }
        }

        // 正在掉落时加速
        if (isFalling && !hasLanded)
        {
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0);
        }

        // 掉出世界才销毁
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
            Debug.Log("平台掉出世界，已销毁");
        }
    }

    void StartFalling()
    {
        isFalling = true;
        rb.isKinematic = false;
        rb.useGravity = false;  // 不用重力，手动控制速度
        Debug.Log("平台开始掉落！当前高度: " + transform.position.y);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasLanded && isFalling)
        {
            hasLanded = true;
            isFalling = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            Debug.Log("平台落地！落地高度: " + transform.position.y + " 碰撞到: " + collision.gameObject.name);
        }
    }
}