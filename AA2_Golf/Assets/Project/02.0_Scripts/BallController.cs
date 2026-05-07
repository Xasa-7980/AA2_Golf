using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class BallController : MonoBehaviour
{
    private PhysicsBody body;
    private LineRenderer lineRenderer;
    private Vector3 startMouse;
    [SerializeField] private float maxLineDistance = 3;
    void Start ( )
    {
        body = GetComponent<PhysicsBody>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update ( )
    {
        if (Input.GetMouseButtonDown(0))
        {
            startMouse = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            if (plane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorldPos = ray.GetPoint(distance);

                lineRenderer.positionCount = 2;

                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, new Vector3(mouseWorldPos.x, transform.position.y, mouseWorldPos.z));
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 endMouse = Input.mousePosition;

            Vector3 drag = startMouse - endMouse;

            // AQUÍ nace la dirección
            Vector3 force = new Vector3(drag.x, 0, drag.y) * 0.01f;

            body.velocity += force;
        }
    }
}