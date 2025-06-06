using UnityEngine;
using System.Collections.Generic;

public class ShuttleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Separation Objects")] [SerializeField]
    GameObject[] separationObjects;

    [SerializeField] private Transform parentObjectTransform;

    [Header("Throttle Controls")] [SerializeField]
    public float throttleChangeRate;

    private Rigidbody shuttleRigidbody;
    private List<Rigidbody> separationRigidbodies = new List<Rigidbody>();
    private float totalMass = 0f;
    private float srbThrust = 12500000f;
    private float ssmeThrust = 2279000f;
    private float totalThrust;
    private bool stageOneSeparation = false;
    private bool stageTwoSeparation = false;
    private bool JettisonInProgress = false;
    private float throttleControl = 0f;
    private float originalShuttleMass;
    private int stageSepCounter = 0;

    [Header("Rotation Control")] [SerializeField]
    public float rollSpeed;

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
        originalShuttleMass = shuttleRigidbody.mass;
        this.transform.rotation = parentObjectTransform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        HandleThrottleControl();
        if (!JettisonInProgress) HandleJettisonControl();
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            if (shuttleRigidbody.isKinematic) DeactivateKinematics();
        }

        HandleThrustProportions();
        CalculateDragForce();
        HandleRollControl();
        HandlePitchControl();
    }

    void InitializeShuttleParams()
    {
        shuttleRigidbody = this.GetComponent<Rigidbody>();
        for (int i = 0; i < separationObjects.Length; i++)
        {
            separationRigidbodies.Add(separationObjects[i].GetComponent<Rigidbody>());
            totalMass += separationRigidbodies[i].mass;
        }

        shuttleRigidbody.mass += totalMass;
    }

    float CalculateThrust()
    {
        float combinedThrust = 0;

        combinedThrust += ssmeThrust * 3f * throttleControl;
        if (!stageOneSeparation)
        {
            combinedThrust += srbThrust;
        }

        return combinedThrust;
    }

    void CalculateDragForce()
    {
        Vector3 velocity = shuttleRigidbody.linearVelocity;
        float altitude = transform.position.y;
        float density = SimManager.GetInstance().CalculateAtmosphere(altitude);
        float frontalArea = 500f;
        float dragCoefficient = 1.2f;
        float dragMagnitude = 0.5f * density * velocity.sqrMagnitude * dragCoefficient * frontalArea;

        Vector3 dragForce = (-shuttleRigidbody.linearVelocity).normalized * dragMagnitude;
        shuttleRigidbody.AddForce(dragForce);
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
        if (stageTwoSeparation)
        {
            shuttleRigidbody.AddForce(transform.forward * (CalculateThrust()), ForceMode.Force);
        }
        else if (stageOneSeparation)
        {
            shuttleRigidbody.AddForce(transform.forward * (CalculateThrust()), ForceMode.Force);
            separationRigidbodies[0].linearVelocity = shuttleRigidbody.linearVelocity;
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
        if (!stageTwoSeparation && Input.GetButton("Horizontal"))
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
        if (!stageOneSeparation && Input.GetButton("Vertical"))
        {
            if (Input.GetKey(KeyCode.W)) pitchTorque = 50;
            else if (Input.GetKey(KeyCode.S)) pitchTorque = -50;
            Quaternion pitchRotation = Quaternion.Euler(pitchTorque, 1f, 1f);
            Quaternion combindedRotation = parentObjectTransform.rotation * pitchRotation;
            parentObjectTransform.rotation = Quaternion.Lerp(parentObjectTransform.rotation, combindedRotation,
                Time.fixedDeltaTime * 0.2f);
        }
        else if (Input.GetButton("Vertical"))
        {
            if (Input.GetKey(KeyCode.W)) pitchTorque = 1;
            else if (Input.GetKey(KeyCode.S)) pitchTorque = -1;
            Vector3 pitchVector = transform.right * (pitchTorque * pitchSpeed);
            shuttleRigidbody.AddTorque(pitchVector);
        }
    }

    void HandleJettisonControl()
    {
        if (stageOneSeparation && stageTwoSeparation) return;
        if (Input.GetButtonDown("Jettison"))
        {
            stageSepCounter++;
            switch (stageSepCounter)
            {
                case 1: SeparateSRB(); break;
                case 2: SeparateExternalTank(); break;
            }
        }

        JettisonInProgress = false;
    }

    void SeparateSRB()
    {
        stageOneSeparation = true;
        for (int i = 1; i < separationRigidbodies.Count; i++)
        {
            separationRigidbodies[i].isKinematic = false;
            separationRigidbodies[i].AddForce(-transform.forward * 3, ForceMode.Impulse);
            shuttleRigidbody.mass -= separationRigidbodies[i].mass;
        }
    }

    void SeparateExternalTank()
    {
        stageTwoSeparation = true;
        separationRigidbodies[0].isKinematic = false;
        separationRigidbodies[0].AddForce(-transform.forward * 3, ForceMode.Impulse);
        shuttleRigidbody.mass = originalShuttleMass;
    }

    
}