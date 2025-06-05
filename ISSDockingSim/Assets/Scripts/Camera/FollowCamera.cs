using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Configuration Camera")]
    public Transform target;

    public float offset;
    public float transitionSpeed;

    private Camera followCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        followCamera = Camera.main;
        followCamera.transform.position = target.position - new Vector3(0, 0, offset);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        FollowCameraTarget();
    }

    void FollowCameraTarget()
    {
        Vector3 desiredPosition = target.position - new Vector3(0, 0, offset);
        transform.position = Vector3.Slerp(transform.position, desiredPosition, transitionSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
