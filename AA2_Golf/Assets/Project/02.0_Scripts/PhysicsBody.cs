using UnityEngine;

public enum PhysicsBodyType
{
    Dynamic,
    Static
}
public class PhysicsBody : MonoBehaviour
{
    public Vector3 velocity;
    public PhysicsBodyType physicsType;
    private bool isStatic => physicsType == PhysicsBodyType.Static;

    public SurfaceMaterial material;

    [Header("Air")]
    public float airDensity = 1.2f;
    public float dragCoefficient = 0.47f;
    public float area = 0.1f;

    [Header("Ball")]
    public float radius = 0.5f;

    private SurfaceMaterial currentSurface;
    private Vector3 surfaceNormal = Vector3.up;
    private bool isGrounded = false;

    void Update ( )
    {
        DetectSurface();
        ApplyForces();
        Move();
        ClampVelocity();
    }

    void ApplyForces ( )
    {
        // GRAVEDAD
        if (isGrounded)
        {
            // pendiente
            Vector3 gravityForce = PhysicsManager.ProjectGravityOnPlane(surfaceNormal);
            velocity += gravityForce * Time.deltaTime;
        }
        else
        {
            // caída libre
            velocity += Vector3.down * PhysicsManager.gravity * Time.deltaTime;
        }

        // FRICCIÓN (solo en suelo)
        if (isGrounded)
        {
            float surfaceFriction = currentSurface != null ? currentSurface.friction : 0.4f;
            float friction = PhysicsManager.CombineFriction(surfaceFriction, material.friction);

            Vector3 frictionForce = PhysicsManager.CalculateFriction(velocity, friction);
            velocity += frictionForce * Time.deltaTime;
        }

        // AIRE (solo en aire)
        if (!isGrounded)
        {
            Vector3 air = PhysicsManager.CalculateAirResistance(
                velocity,
                airDensity,
                dragCoefficient,
                area
            );

            velocity += air * Time.deltaTime;
        }
    }

    void Move ( )
    {
        // Movimiento
        transform.position += velocity * Time.deltaTime;

        // Rotación realista
        if (velocity.magnitude > 0.01f && isGrounded)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, velocity.normalized);
            float angularSpeed = velocity.magnitude / radius;

            transform.Rotate(axis, angularSpeed * Mathf.Rad2Deg * Time.deltaTime, Space.World);
        }
    }

    void DetectSurface ( )
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, radius + 0.1f))
        {
            isGrounded = true;
            surfaceNormal = hit.normal;

            if (hit.collider.TryGetComponent(out PhysicsObject surface))
            {
                currentSurface = surface.material;
            }
        }
        else
        {
            isGrounded = false;
            currentSurface = null;
            surfaceNormal = Vector3.up;
        }
    }

    void OnCollisionEnter ( Collision collision )
    {
        if (collision.collider.TryGetComponent(out PhysicsObject surface))
        {
            float surfaceBounce = surface.material.bouncing;
            float e = PhysicsManager.CombineBounce(surfaceBounce, material.bouncing);

            Vector3 normal = collision.contacts[0].normal;

            velocity = Vector3.Reflect(velocity, normal) * e;
        }
    }

    void ClampVelocity ( )
    {
        if (velocity.magnitude < 0.05f)
        {
            velocity = Vector3.zero;
        }
    }
}