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

    [Header("Rotation Control")] 
    [SerializeField] public float rollSpeed;

    [SerializeField] public float pitchSpeed;
    [SerializeField] public float yawSpeed;
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
        HandleRollControl();
        HandlePitchControl();
        HandleJettisonControl();
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

    float CalculateThrust()
    {
        float combinedThrust = 0;
        
        combinedThrust += ssmeThrust * 3f * throttleControl;
        if (!stageSeparation)
        {
            combinedThrust += srbThrust;
        }
        return combinedThrust;
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
        if (shuttleRigidbody.isKinematic) return; // exit to eliminate warnings
        if (stageSeparation)
        {
            shuttleRigidbody.AddForce(transform.forward * (CalculateThrust()),ForceMode.Force);
        }
        else
        {
            Vector3 thrustVector = this.transform.forward * (CalculateThrust());
            shuttleRigidbody.AddForce(thrustVector, ForceMode.Force);
            for (int i = 0; i < separationRigidbodies.Count; i++)
            {
                separationRigidbodies[i].linearVelocity = shuttleRigidbody.linearVelocity;
            }
        }
    }

    void HandleThrottleControl()
    {
        if (Input.GetButton("IncreaseThrottle"))
        {
            throttleControl += throttleChangeRate * Time.deltaTime;
        }

        if (Input.GetButton("DecreaseThrottle"))
        {
            throttleControl -= throttleChangeRate * Time.deltaTime;
        }

        if (throttleControl < 0) throttleControl = 0;
        throttleControl = Mathf.Clamp(throttleControl, 0f, 1f);
    }

  public float ThrottlePercentage()
    {
        return throttleControl * 100f;
    }

    void HandleRollControl()
    {
        float rollTorque = 0f;
        if (shuttleRigidbody.isKinematic) return;
        if (!stageSeparation && Input.GetButton("Horizontal"))
        {
            if (Input.GetButton("Horizontal") && Input.GetKey(KeyCode.A)) rollTorque = 50;
            else if (Input.GetButton("Horizontal") && Input.GetKey(KeyCode.D)) rollTorque = -50;
            Quaternion rollRotation = Quaternion.Euler(1f, 1f, rollTorque * rollSpeed);
            Quaternion combinedRotation = parentObjectTransform.rotation * rollRotation;
            parentObjectTransform.rotation = Quaternion.Slerp(parentObjectTransform.rotation, combinedRotation,
                Time.fixedDeltaTime * 0.2f);

        }
       else
        {
            
            if (Input.GetButton("Horizontal") && Input.GetKey(KeyCode.A)) rollTorque = -1f;
            else if (Input.GetButton("Horizontal") && Input.GetKey(KeyCode.D)) rollTorque = 1f;
            Vector3 rollVector = transform.forward * (rollTorque * this.rollSpeed);
            shuttleRigidbody.AddTorque(rollVector);
            
        }
    }

    void HandlePitchControl()
    {
        float pitchTorque = 0f;
        if (!stageSeparation && Input.GetButton("Vertical"))
        {
            if(Input.GetKey(KeyCode.W)) pitchTorque = 50;
            else if(Input.GetKey(KeyCode.S)) pitchTorque = -50;
            Quaternion pitchRotation = Quaternion.Euler(pitchTorque, 1f, 1f);
            Quaternion combindedRotation = parentObjectTransform.rotation * pitchRotation;
            parentObjectTransform.rotation = Quaternion.Lerp(parentObjectTransform.rotation, combindedRotation, Time.fixedDeltaTime * 0.2f);
        }
        else if(Input.GetButton("Vertical"))
        {
            if(Input.GetKey(KeyCode.W)) pitchTorque = 1;
            else if(Input.GetKey(KeyCode.S)) pitchTorque = -1;
            Vector3 pitchVector = transform.right * (pitchTorque * pitchSpeed);
            shuttleRigidbody.AddTorque(pitchVector);
        }
    }

    void HandleJettisonControl()
    {
        if (Input.GetButtonDown("Jettison"))
        {
            stageSeparation = true;
            for (int i = 0; i < separationRigidbodies.Count; i++)
            {
                separationRigidbodies[i].isKinematic = false;
                separationRigidbodies[i].AddForce(-transform.forward * 3 , ForceMode.Impulse);
            }
        }
    }
}
