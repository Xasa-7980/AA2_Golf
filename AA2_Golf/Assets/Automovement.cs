using UnityEngine;
public class Automovement : MonoBehaviour
{
    public float speed = 2f;
    public float distance = 3f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float move = Mathf.Sin(Time.time * speed) * distance;
        transform.position = startPos + new Vector3(move, 0, 0);
    }
}