using UnityEngine;
using System.Collections.Generic;
public class ShuttleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Separation Objects")]
    [SerializeField] GameObject[] separationObjects;

    [SerializeField] private Transform parentObjectTransform;

    [Header("Throttle Controls")] [SerializeField]
    public float throttleChangeRate;
    private Rigidbody shuttleRigidbody;
    private List<Rigidbody> separationRigidbodies = new List<Rigidbody>();
    private float totalMass = 0f;
    private float srbThrust = 12500000f;
    private float ssmeThrust = 1860000f;
    private float totalThrust;
    private bool stageSeparation = false;
    private float throttleControl = 0f;
    /// <summary>
    ///  295,000 mass for srb
    /// 76,000 for mass fuel tank
    /// </summary>
    void Start()
    {
        InitializeShuttleParams();
        totalThrust = srbThrust + ssmeThrust;
        this.transform.rotation = parentObjectTransform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        HandleThrottleControl();
    }
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            if(shuttleRigidbody.isKinematic) DeactivateKinematics();
          
            
        }
        HandleThrustProportions();
    }

    void InitializeShuttleParams()
    {
        shuttleRigidbody = this.GetComponent<Rigidbody>();
        for (int i = 0; i < separationObjects.Length; i++)
        {
            separationRigidbodies.Add(separationObjects[i].GetComponent<Rigidbody>());
            totalMass += separationRigidbodies[i].mass;
        }
        totalMass += shuttleRigidbody.mass;
        shuttleRigidbody.mass = totalMass;
    }

    void DeactivateKinematics()
    {
        shuttleRigidbody.isKinematic = false;
        for (int i = 0; i < separationRigidbodies.Count; i++)
        {
            separationRigidbodies[i].isKinematic = false;
        }
    }

    void HandleThrustProportions()
    {
        if (stageSeparation)
        {
            shuttleRigidbody.AddForce(transform.forward * (totalThrust  * throttleControl));
        }
        else
        {
            Vector3 thrustVector = this.transform.forward * (totalThrust * throttleControl);
            shuttleRigidbody.AddForce(thrustVector);
            for (int i = 0; i < separationRigidbodies.Count; i++)
            {
                separationRigidbodies[i].linearVelocity = shuttleRigidbody.linearVelocity;
            }
        }
    }

    void HandleThrottleControl()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttleControl += throttleChangeRate * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            throttleControl -= throttleChangeRate * Time.deltaTime;
        }

        if (throttleControl < 0) throttleControl = 0;
        throttleControl = Mathf.Clamp(throttleControl, 0f, 1f);
    }
}
