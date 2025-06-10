using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

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
    private float srbFuelWeight = 1006000f;
    private float externalTankFuelWeight = 73500f;
    private float srbBurnRate = 4000f;
    private float ssmeBurnRate = 500f;
    private Coroutine fuelBurn = null;
    private bool srbFuelRemaining = true;

    [Header("Rotation Control")] [SerializeField]
    public float rollSpeed;

    [SerializeField] public float pitchSpeed;
    [SerializeField] public float yawSpeed;

    [Header("Destination")] [SerializeField]
    public GameObject destination;

    /// <summary>
    ///  295,000 mass for srb
    /// 76,000 for mass fuel tank
    /// </summary>
    
    
    // You need to subtract mass from the total mass every second of flight and add bools for srb active and a counter
    // for seconds so that after 126 seconds, srb thrust is not active and alert the user to perform stage 1 sep
    // This part of physics is required to achieve appropriate maxQ times and to achieve proper speeds.
    void Start()
    {
        InitializeShuttleParams();
        totalThrust = srbThrust + ssmeThrust;
        originalShuttleMass = shuttleRigidbody.mass;
        this.transform.rotation = parentObjectTransform.rotation;
        PlatformController.singleton.Init("COM3", 115200);
        
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
       // PerformGravityTurn(); // feature is cool, does not work well with project and needing more user input
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
        totalMass += srbFuelWeight;
        totalMass += externalTankFuelWeight;
        totalMass += shuttleRigidbody.mass;
        shuttleRigidbody.mass = totalMass;
    }

    float CalculateThrust()
    {
        float combinedThrust = 0;

        combinedThrust += ssmeThrust * 3f * throttleControl;
        if (!stageOneSeparation && srbFuelRemaining)
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
        if (fuelBurn == null)
        {
            fuelBurn = StartCoroutine(FuelBurnRate());
        }
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

    Vector3 CalculateGravityTurn()
    {
        float startAlt = 5000f;
        float endAlt = 20000f;
        float altitude = this.transform.position.y;
        if (altitude < startAlt) return Vector3.up;

        else if (altitude < endAlt)
        {

            float startCurve = 3000f;
            float endCurve = 25000f;
            Vector3 issPosition = destination.transform.position;
            Vector3 shuttlePosition = transform.position;
        
            // Create horizontal vector toward ISS
            Vector3 horizontalToISS = issPosition - shuttlePosition;
            horizontalToISS.y = 0f; // Force horizontal
            horizontalToISS = horizontalToISS.normalized;
        
            // Smooth transition from vertical to horizontal targeting
            float progress = (altitude - startCurve) / (endCurve - startCurve);
            progress = Mathf.SmoothStep(0f, 1f, progress);
        
            Vector3 direction = Vector3.Slerp(Vector3.up, horizontalToISS, progress);
        
            Debug.Log($"Targeting ISS - Direction: {direction}, Progress: {progress}");
            return direction.normalized;
            // This works well enough for simple curve
            // float progress = (altitude - 5000f) / (20000f - 5000f);
            // progress = Mathf.SmoothStep(0f, 1f, progress);
            // float angle = progress * 45f; // 0° to 45° tilt
            //
            // Vector3 direction = new Vector3(0.5f, Mathf.Cos(angle * Mathf.Deg2Rad), 0);
            // direction = direction.normalized;
            //
            // Debug.Log($"Simple turn - Altitude: {altitude}, Angle: {angle}, Direction: {direction}");
            // return direction;
        }
        else
        {
            return CalculateInterceptTrajectory();
        }
    }

    void PerformGravityTurn()
    {
        Vector3 gravityTurnDir = CalculateGravityTurn();
        if (gravityTurnDir.y <= 0f) gravityTurnDir.y = 0.1f;
        if (stageTwoSeparation)
        {
            transform.rotation = Quaternion.LookRotation(gravityTurnDir);
        }

        Quaternion targetRotation = Quaternion.LookRotation(gravityTurnDir);
        
        parentObjectTransform.rotation = Quaternion.Slerp(parentObjectTransform.rotation, targetRotation, 2f * Time.fixedDeltaTime);
    }

    Vector3 CalculateInterceptTrajectory()
    {
        Vector3 direction = (destination.transform.position - transform.position).normalized;
        direction.y = 0f;
        return direction.normalized;
    }

    IEnumerator FuelBurnRate()
    {
        if (!stageOneSeparation && !stageTwoSeparation)
        {
            shuttleRigidbody.mass -= (srbBurnRate * 2 + ssmeBurnRate);
        }
        else if (stageOneSeparation && !stageTwoSeparation)
        {
            shuttleRigidbody.mass -= ssmeBurnRate;
        }

        if (srbFuelWeight <= 0.5f) srbFuelRemaining = false;
        
        if(shuttleRigidbody.mass < 78000f) shuttleRigidbody.mass = 78000f;

        yield return new WaitForSeconds(1f);
        fuelBurn = null;
    }
}