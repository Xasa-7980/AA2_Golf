using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Vector3 offSet;
    public GameObject golfBall;

    void Start()
    {
        offSet = golfBall.transform.position - transform.position;
        
    }

    void Update()
    {
        transform.position = golfBall.transform.position - offSet;
    }
}
