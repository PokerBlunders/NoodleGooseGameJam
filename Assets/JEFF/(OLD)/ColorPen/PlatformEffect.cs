using UnityEngine;

public class PlatformEffect : MonoBehaviour
{
    public Color platformColor;
    public float blueSpeedMultiplier = 1.5f;
    public float orangeJumpMultiplier = 1.5f;
    public float effectDuration = 3f;

    private bool isBluePlatform;
    private bool isOrangePlatform;

    void Start()
    {
        // ÅÐ¶ÏÆ½Ì¨ÑÕÉ«
        if (platformColor == Color.blue ||
            (platformColor.r < 0.3f && platformColor.g < 0.3f && platformColor.b > 0.7f))
        {
            isBluePlatform = true;
            GetComponent<Renderer>().material.color = Color.blue;
        }
        else if (platformColor.r > 0.7f && platformColor.g > 0.3f && platformColor.g < 0.7f && platformColor.b < 0.2f)
        {
            isOrangePlatform = true;
            GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerControllero player = collision.gameObject.GetComponent<PlayerControllero>();
            if (player != null)
            {
                if (isBluePlatform)
                {
                    player.ApplySpeedBoost(blueSpeedMultiplier, effectDuration);
                }
                else if (isOrangePlatform)
                {
                    player.ApplyJumpBoost(orangeJumpMultiplier, effectDuration);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllero player = other.GetComponent<PlayerControllero>();
            if (player != null)
            {
                if (isBluePlatform)
                {
                    player.ApplySpeedBoost(blueSpeedMultiplier, effectDuration);
                }
                else if (isOrangePlatform)
                {
                    player.ApplyJumpBoost(orangeJumpMultiplier, effectDuration);
                }
            }
        }
    }
}