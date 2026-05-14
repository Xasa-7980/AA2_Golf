using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BallController : MonoBehaviour
{
    private PhysicsBody body;

    private LineRenderer lineRenderer;
    private Vector3 dragStartWorldPos;
    private bool isDragging = false;

    [SerializeField] private float forceMultiplier = 5f;
    [SerializeField] private float maxForce = 10f;

    // Velocity arrow
    [Header("Velocity Arrow")]
    [SerializeField] private float arrowScale = 1f;
    private LineRenderer velocityArrow;

    void Start()
    {
        body = GetComponent<PhysicsBody>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        // Create a child GameObject with its own LineRenderer for the velocity arrow
        GameObject arrowObj = new GameObject("VelocityArrow");
        arrowObj.transform.SetParent(transform);
        velocityArrow = arrowObj.AddComponent<LineRenderer>();

        // Copy visual settings from the main line 
        velocityArrow.startWidth = lineRenderer.startWidth;
        velocityArrow.endWidth = 0f;
        velocityArrow.material = lineRenderer.material;
        velocityArrow.startColor = Color.red;
        velocityArrow.endColor = Color.blue;
        velocityArrow.positionCount = 0;
        velocityArrow.useWorldSpace = true;
    }

    void Update()
    {
        HandleInput();
        UpdateVelocityArrow();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStartWorldPos = GetMouseWorldPos();
            isDragging = true;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentWorldPos = GetMouseWorldPos();

            // Drag line shows the pull-back direction
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentWorldPos);
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            lineRenderer.positionCount = 0;

            Vector3 endWorldPos = GetMouseWorldPos();
            // Inverted drag, launch forward
            Vector3 force = (dragStartWorldPos - endWorldPos) * forceMultiplier;
            force = Vector3.ClampMagnitude(force, maxForce);
            force.y = 0f;

            body.AddForce(force);
        }
    }

    void UpdateVelocityArrow()
    {
        if (isDragging)
        {
            // Arrow shows the launch direction, opposite to the drag line
            Vector3 currentWorldPos = GetMouseWorldPos();
            Vector3 previewForce = (dragStartWorldPos - currentWorldPos) * forceMultiplier;
            previewForce = Vector3.ClampMagnitude(previewForce, maxForce);
            previewForce.y = 0f;

            if (previewForce.magnitude > 0.05f)
            {
                velocityArrow.positionCount = 2;
                velocityArrow.SetPosition(0, transform.position);
                velocityArrow.SetPosition(1, transform.position + previewForce * arrowScale);
            }
            else
            {
                velocityArrow.positionCount = 0;
            }
        }
        else if (body.velocity.magnitude > 0.05f)
        {
            velocityArrow.positionCount = 2;
            velocityArrow.SetPosition(0, transform.position);
            velocityArrow.SetPosition(1, transform.position + body.velocity * arrowScale);
        }
        else
        {
            velocityArrow.positionCount = 0;
        }
    }

    // Projects the mouse onto the XZ plane (y = ball height)
    private Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position); // XZ plane at ball's Y

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return transform.position;
    }
}