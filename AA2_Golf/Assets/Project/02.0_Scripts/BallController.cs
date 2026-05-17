using System.Collections.Generic;
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

    // Trajectory prediction
    [Header("Trajectory Prediction")]
    [SerializeField] private int trajectorySteps = 30;
    [SerializeField] private float trajectoryTimeStep = 0.05f;
    [SerializeField] private float trajectoryFriction = 0.4f; // fallback friction for preview

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
            Vector3 currentWorldPos = GetMouseWorldPos();
            Vector3 previewForce = (dragStartWorldPos - currentWorldPos) * forceMultiplier;
            previewForce = Vector3.ClampMagnitude(previewForce, maxForce);
            previewForce.y = 0f;

            if (previewForce.magnitude > 0.05f)
            {
                // Project launch force onto the surface, matching PhysicsBody.AddForce
                Vector3 startNormal = SampleSurfaceNormal(transform.position);
                Vector3 initialVelocity = Vector3.ProjectOnPlane(previewForce, startNormal);

                List<Vector3> points = SimulateTrajectory(transform.position, initialVelocity);
                ApplyToLineRenderer(velocityArrow, points);
            }
            else
            {
                velocityArrow.positionCount = 0;
            }
        }
        else
        {
            velocityArrow.positionCount = 0;
        }
    }

    // Simulates the ball trajectory step-by-step
    private List<Vector3> SimulateTrajectory(Vector3 startPos, Vector3 startVelocity)
    {
        List<Vector3> points = new List<Vector3>(trajectorySteps + 1);
        Vector3 pos = startPos;
        Vector3 vel = startVelocity;

        points.Add(pos);

        for (int i = 0; i < trajectorySteps; i++)
        {
            Vector3 normal = SampleSurfaceNormal(pos);
            bool grounded = normal != Vector3.up || IsGroundedAt(pos);

            if (grounded)
            {
                // Slope gravity projected onto surface plane
                Vector3 gravityForce = PhysicsManager.ReturnGravityOnAngledSurface(normal);
                vel += gravityForce * trajectoryTimeStep;

                // Friction opposes motion
                Vector3 frictionForce = PhysicsManager.CalculateFriction(vel, trajectoryFriction);
                vel += frictionForce * trajectoryTimeStep;
            }
            else
            {
                vel += Vector3.down * PhysicsManager.gravity * trajectoryTimeStep;
            }

            pos += vel * trajectoryTimeStep;

            // Snap to surface so the line hugs the terrain
            pos = SnapToSurface(pos, body.radius);

            points.Add(pos);

            // Stop early if ball would halt
            if (vel.magnitude < 0.05f)
                break;
        }

        return points;
    }

    // Returns the surface normal below the given position, or Vector3.up if none found.
    private Vector3 SampleSurfaceNormal(Vector3 pos)
    {
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, body.radius + 0.3f))
            return hit.normal;
        return Vector3.up;
    }

    private bool IsGroundedAt(Vector3 pos)
    {
        return Physics.Raycast(pos, Vector3.down, body.radius + 0.15f);
    }

    private Vector3 SnapToSurface(Vector3 pos, float radius)
    {
        if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, radius + 0.8f))
            return hit.point + hit.normal * radius;
        return pos;
    }

    private static void ApplyToLineRenderer(LineRenderer lr, List<Vector3> points)
    {
        lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            lr.SetPosition(i, points[i]);
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