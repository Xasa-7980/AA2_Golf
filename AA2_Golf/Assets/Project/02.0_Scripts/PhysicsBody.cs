using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Restart Conditions")]
    public int maxWallBounces = 3;
    public float fallThreshold = -10f;

    [Header("UI")]
    public Image fadePanelImage;
    public float fadeDuration = 0.5f;
    public TMP_Text bouncetext;


    private SurfaceMaterial currentSurface;
    private Vector3 surfaceNormal = Vector3.up;
    private bool isGrounded = false;

    private int wallBounceCount = 0;
    private bool isRestarting = false;
    //Guizmos
    private Vector3 _lastProjectedForce = Vector3.zero;
    private Vector3 _lastForceOrigin = Vector3.zero;

    void FixedUpdate()
    {
        if (isStatic || isRestarting) return;
        DetectSurface();
        ApplyForces();
        Move();
        ClampVelocity();
        CheckFallThreshold();
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

            _lastProjectedForce = projected;
            _lastForceOrigin = transform.position;
        }
        else
        {
            velocity += force;

            _lastProjectedForce = force;
            _lastForceOrigin = transform.position;
        }
    }

    void ApplyForces()
    {
        // GRAVEDAD
        if (isGrounded)
        {
            // pendiente
            Vector3 gravityOnProjection = PhysicsManager.ReturnGravityOnAngledSurface(surfaceNormal);
            
            // v = v + aΔt
            velocity += gravityOnProjection * Time.fixedDeltaTime;
        }
        else
        {
            // caída libre
            // v = v + gΔt
            velocity += Vector3.down * PhysicsManager.gravity * Time.fixedDeltaTime;
        }

        // FRICCIÓN (solo en suelo)
        if (isGrounded)
        {
            float surfaceFriction = currentSurface != null ? currentSurface.friction : 0.4f;
            float friction = PhysicsManager.CombineFriction(surfaceFriction, material.friction);

            Vector3 frictionForce = PhysicsManager.CalculateFriction(velocity, friction);
            velocity += frictionForce * Time.fixedDeltaTime;
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

            // v = v + aΔt
            velocity += air * Time.fixedDeltaTime;
        }
    }

    void Move()
    {
        // Δx = vΔt
        Vector3 motion = velocity * Time.fixedDeltaTime;

        if (motion.magnitude > 0.001f)
        {
            if (Physics.SphereCast(
                    transform.position,
                    radius * 0.99f,
                    motion.normalized, out RaycastHit hit, motion.magnitude))
            {
                if (CheckIfWin(hit)) return;

                // Count wall bounces (surface steeper than 80 degrees so we have some margin)

                // θ = cos⁻¹((A·B)/|A||B|)
                if (Vector3.Angle(Vector3.up, hit.normal) > 80f)
                {
                    wallBounceCount++;
                    bouncetext.text = "Bounces: " + (wallBounceCount);
                    Debug.Log($"Wall bounce #{wallBounceCount}");

                    if (wallBounceCount >= maxWallBounces)
                    {
                        StartCoroutine(RestartWithFade());
                        return;
                    }
                }

                float bounciness = material != null ? material.bouncing : 0.5f;

                if (hit.collider.TryGetComponent(out PhysicsObject surface))
                {
                    bounciness = PhysicsManager.CombineBounce(surface.material.bouncing, material.bouncing);
                }

                // R = V - 2(V·N)N
                velocity = Vector3.Reflect(velocity, hit.normal) * bounciness;

                float distanceToHit = Mathf.Max(0f, hit.distance - 0.001f);
                transform.position += motion.normalized * distanceToHit;
                transform.position += hit.normal * 0.002f;

                // t_remaining = Δt - (distance / speed)
                float remainingTime = Time.fixedDeltaTime - (distanceToHit / (motion.magnitude / Time.fixedDeltaTime + Mathf.Epsilon));

                // Δx = vΔt
                transform.position += velocity * remainingTime;
            }
            else
            {
                // x = x + vΔt
                transform.position += motion;
            }
        }

        if (velocity.magnitude > 0.01f && isGrounded)
        {
            // A × B = vector perpendicular a ambos
            Vector3 axis = Vector3.Cross(Vector3.up, velocity.normalized);
            // ω = v / r
            float angularSpeed = velocity.magnitude / radius;
            // θ = ωt
            transform.Rotate(axis, angularSpeed * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.World);
        }
    }

    void CheckFallThreshold()
    {
        if (transform.position.y < fallThreshold)
        {
            StartCoroutine(RestartWithFade());
        }
    }

    IEnumerator RestartWithFade()
    {
        if (isRestarting) yield break;
        isRestarting = true;
        velocity = Vector3.zero;

        // Fade in
        yield return StartCoroutine(FadePanel(0f, 1f));

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Fade out 
        yield return StartCoroutine(FadePanel(1f, 0f));

        isRestarting = false;
        wallBounceCount = 0;
    }

    IEnumerator FadePanel(float from, float to)
    {
        if (fadePanelImage == null) yield break;

        fadePanelImage.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadePanelImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadePanelImage.color = new Color(0f, 0f, 0f, to);

        if (to == 0f)
            fadePanelImage.gameObject.SetActive(false);
    }

    bool CheckIfWin(RaycastHit hit)
    {
        if (!hit.collider.CompareTag("Hole")) return false;

        if (velocity.magnitude < maxWinSpeed)
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                LevelLoader.Instance.LoadLevelByIndex(0);
                return true;
            }
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

    void OnDrawGizmos()
    {
        if (_lastProjectedForce == Vector3.zero) return;

        float magnitude = _lastProjectedForce.magnitude;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_lastForceOrigin, _lastForceOrigin + _lastProjectedForce);
        Gizmos.DrawSphere(_lastForceOrigin + _lastProjectedForce, magnitude * 0.05f);
    }
}