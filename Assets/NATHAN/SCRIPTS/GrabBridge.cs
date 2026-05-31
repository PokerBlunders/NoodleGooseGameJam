using UnityEngine;

public class GrabObject : MonoBehaviour
{
    [Header("Grab Settings")]
    public KeyCode grabKey = KeyCode.E;
    public float grabRange = 3f;
    public LayerMask grabbableLayer;
    public float throwForce = 10f;

    [Header("Outline")]
    public string outlineChildName = "Outline"; // name of the child GameObject

    [Header("References")]
    public Transform grabPoint;

    private Camera playerCamera;
    private Rigidbody grabbedRB;
    private GameObject grabbedObject;
    private GameObject currentHighlight;
    private GameObject currentOutlineChild; // the child we turned on
    private Transform originalParent;
    private bool wasKinematic;
    private float originalDrag;
    private Vector3 localOffset;
    private Quaternion rotationOffset;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            Debug.LogError("No camera found!");

        if (grabPoint == null)
        {
            GameObject point = new GameObject("GrabPoint");
            point.transform.SetParent(playerCamera.transform);
            point.transform.localPosition = new Vector3(0, 0, 1f);
            grabPoint = point.transform;
        }
    }

    void Update()
    {
        UpdateHighlight();

        if (Input.GetKeyDown(grabKey))
        {
            if (grabbedObject == null)
                TryGrab();
            else
                DropObject();
        }

        if (Input.GetMouseButtonDown(1) && grabbedObject != null)
            ThrowObject();
    }

    void UpdateHighlight()
    {
        // Turn off previous highlight
        if (currentOutlineChild != null)
        {
            currentOutlineChild.SetActive(false);
            currentOutlineChild = null;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayer))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (grabbedObject == null || hitObj != grabbedObject)
            {
                currentHighlight = hitObj;
                // Find the outline child inside the hit object
                Transform outline = hitObj.transform.Find(outlineChildName);
                if (outline != null)
                {
                    currentOutlineChild = outline.gameObject;
                    currentOutlineChild.SetActive(true);
                }
            }
        }
    }

    void TryGrab()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                grabbedObject = hit.collider.gameObject;
                grabbedRB = rb;
                originalParent = grabbedObject.transform.parent;
                wasKinematic = rb.isKinematic;
                originalDrag = rb.linearDamping;

                // Disable physics and parent to grab point
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearDamping = 5f;

                // Calculate offset to keep object in place
                Vector3 worldOffset = grabbedObject.transform.position - grabPoint.position;
                localOffset = grabPoint.InverseTransformDirection(worldOffset);
                rotationOffset = Quaternion.Inverse(grabPoint.rotation) * grabbedObject.transform.rotation;

                grabbedObject.transform.SetParent(grabPoint);
                grabbedObject.transform.localPosition = localOffset;
                grabbedObject.transform.localRotation = rotationOffset;

                // Turn off highlight if it was on
                if (currentOutlineChild != null)
                {
                    currentOutlineChild.SetActive(false);
                    currentOutlineChild = null;
                }
            }
        }
    }

    void DropObject()
    {
        if (grabbedObject == null) return;

        grabbedObject.transform.SetParent(originalParent);
        grabbedRB.isKinematic = false;
        grabbedRB.useGravity = true;
        grabbedRB.linearDamping = originalDrag;

        grabbedObject = null;
        grabbedRB = null;
    }

    void ThrowObject()
    {
        if (grabbedObject == null) return;

        Vector3 throwDir = playerCamera.transform.forward;
        grabbedObject.transform.SetParent(originalParent);
        grabbedRB.isKinematic = false;
        grabbedRB.useGravity = true;
        grabbedRB.linearDamping = originalDrag;
        grabbedRB.AddForce(throwDir * throwForce, ForceMode.Impulse);

        grabbedObject = null;
        grabbedRB = null;
    }
}