using UnityEngine;

public class BallController : MonoBehaviour
{
    private PhysicsBody body;

    private Vector3 startMouse;

    void Start ( )
    {
        body = GetComponent<PhysicsBody>();
    }

    void Update ( )
    {
        if (Input.GetMouseButtonDown(0))
        {
            startMouse = Input.mousePosition;
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