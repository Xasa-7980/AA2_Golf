using UnityEngine;
using UnityEngine.SceneManagement;

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
    public float mass = 1f;

    [Header("Win Condition")]
    public float maxWinSpeed = 0.5f;

    private SurfaceMaterial currentSurface;
    private Vector3 surfaceNormal = Vector3.up;
    private bool isGrounded = false;

    void FixedUpdate()
    {
        if (isStatic) return;
        DetectSurface();
        ApplyForces();
        Move();
        ClampVelocity();
    }

    // Applies a force to the body. If grounded on an angled surface,
    // the force is projected onto the surface plane so it follows the slope.
    public void AddForce(Vector3 force)
    {
        if (isGrounded)
        {
            Vector3 projected = Vector3.ProjectOnPlane(force, surfaceNormal);
            Debug.Log($"Applied force: {force}, projected on surface: {projected}");
            velocity += projected;
        }
        else
        {
            velocity += force;
        }
    }

    void ApplyForces()
    {
        // GRAVEDAD
        if (isGrounded)
        {
            // pendiente
            Vector3 gravityForce = PhysicsManager.ReturnGravityOnAngledSurface(surfaceNormal);
            velocity += gravityForce * Time.fixedDeltaTime;
        }
        else
        {
            // caída libre
            velocity += Vector3.down * PhysicsManager.gravity * Time.fixedDeltaTime;
        }

        // FRICCIÓN (solo en suelo)
        if (isGrounded)
        {
            float surfaceFriction = currentSurface != null ? currentSurface.friction : 0.4f;
            float friction = PhysicsManager.CombineFriction(surfaceFriction, material.friction);

            Vector3 frictionForce = PhysicsManager.CalculateFriction(velocity, friction);
            velocity += frictionForce * Time.fixedDeltaTime; // was Time.deltaTime — fixed
        }

        // AIRE — active only above y > 1m
        if (!isGrounded && transform.position.y > 1f)
        {
            Vector3 air = PhysicsManager.CalculateAirResistance(
                velocity,
                airDensity,
                dragCoefficient,
                area
            );

            velocity += air * Time.fixedDeltaTime;
        }
    }

    void Move()
    {
        Vector3 motion = velocity * Time.fixedDeltaTime;

        if (motion.magnitude > 0.001f)
        {
            if (Physics.SphereCast(
                    transform.position,
                    radius * 0.99f,
                    motion.normalized, out RaycastHit hit, motion.magnitude))
            {
                if (CheckIfWin(hit)) return;

                float bounciness = material != null ? material.bouncing : 0.5f;

                if (hit.collider.TryGetComponent(out PhysicsObject surface))
                {
                    bounciness = PhysicsManager.CombineBounce(surface.material.bouncing, material.bouncing);
                }

                velocity = Vector3.Reflect(velocity, hit.normal) * bounciness;

                float distanceToHit = Mathf.Max(0f, hit.distance - 0.001f);
                transform.position += motion.normalized * distanceToHit;
                transform.position += hit.normal * 0.002f;

                float remainingTime = Time.fixedDeltaTime - (distanceToHit / (motion.magnitude / Time.fixedDeltaTime + Mathf.Epsilon));
                transform.position += velocity * remainingTime;
            }
            else
            {
                transform.position += motion;
            }
        }

        // Rotación: W = v / r
        if (velocity.magnitude > 0.01f && isGrounded)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, velocity.normalized);
            float angularSpeed = velocity.magnitude / radius;
            transform.Rotate(axis, angularSpeed * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.World);
        }
    }

    bool CheckIfWin(RaycastHit hit)
    {
        if (!hit.collider.CompareTag("Hole")) return false;

        if (velocity.magnitude < maxWinSpeed)
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            LevelLoader.Instance.LoadLevelByIndex(nextIndex);
            return true;
        }

        Debug.Log("Too fast to win!, Velocity is: " + velocity.magnitude + " Slow down and try again.");

        return false;
    }

    void DetectSurface()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, radius + 0.1f))
        {
            isGrounded = true;
            surfaceNormal = hit.normal;

            if (hit.collider.gameObject.layer.Equals(0))
            {
                if (hit.collider.TryGetComponent<PhysicsBody>(out PhysicsBody surface))
                {
                    currentSurface = surface.material;
                }
                else if (hit.collider.TryGetComponent<PhysicsObject>(out PhysicsObject physObj))
                {
                    currentSurface = physObj.material;
                }
            }
        }
        else
        {
            isGrounded = false;
            currentSurface = null;
            surfaceNormal = Vector3.up;
        }
    }

    void ClampVelocity()
    {
        if (velocity.magnitude < 0.05f)
        {
            velocity = Vector3.zero;
        }
    }
}