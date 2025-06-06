using TMPro;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private static SimManager instance;


    [Header("Shuttle References")] [SerializeField]
    public TextMeshProUGUI altitudeText;
    [SerializeField] public TextMeshProUGUI throttleText;
    [SerializeField] public TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI maxQDisplay;
    [SerializeField] private GameObject shuttlePrefab;
    private ShuttleController shuttleController;
    private Rigidbody shuttleRigidBody;
    private Transform shuttleTransform;
    
    [Header("Atmospheric Effects")]
    [SerializeField] public Material atmosphericMaterial;

    private float originalAtmosphereThickness;
    void Start()
    {
        instance = this;
        shuttleRigidBody = shuttlePrefab.GetComponent<Rigidbody>();
        shuttleTransform = shuttlePrefab.GetComponent<Transform>();
        shuttleController = shuttlePrefab.GetComponent<ShuttleController>();
        RenderSettings.skybox = atmosphericMaterial;
        originalAtmosphereThickness = atmosphericMaterial.GetFloat("_AtmosphereThickness");
    }

    // Update is called once per frame
    void Update()
    {
        UpdateUIElements();
       
    }

    public static SimManager GetInstance()
    {
        return instance;
    }
    void FixedUpdate()
    {
        UpdateAtmosphere();
    }

    void UpdateUIElements()
    {
        UpdateAltitudeElement();
        UpdateThrottleElement();
        UpdateShuttleSpeedElement();
        UpdateMaxQDisplay();
    }

    void UpdateThrottleElement()
    {
        float throttlePercent = shuttleController.ThrottlePercentage();
        throttleText.text = $"Throttle: {throttlePercent}%";

    }

    void UpdateAltitudeElement()
    {
        float height = shuttleTransform.position.y / 1000f;
        altitudeText.text = $"Altitude: {height} km";
    }

    void UpdateShuttleSpeedElement()
    {
        float velocity = shuttleRigidBody.linearVelocity.magnitude;
        speedText.text = $"Speed: {velocity} m/s";
    }

    void UpdateMaxQDisplay()
    {
        float seaLevelDensity = 1.225f;  
        float scaleHeight = 60000f;
        float altitude = shuttleTransform.position.y;
        float calculateDensity = CalculateAtmosphere(shuttleTransform.position.y);
        float density = seaLevelDensity * Mathf.Exp(-altitude / scaleHeight);
        float dynamicPressure = 0.5f * density *
                     (shuttleRigidBody.linearVelocity.magnitude * shuttleRigidBody.linearVelocity.magnitude);
        float maxQ = dynamicPressure / 101325f; // this number is translating pascals to atmospheres
        if (maxQ > 0.30f)
        {
            maxQDisplay.color = Color.red;
            maxQDisplay.text = $"Atmosphere: {maxQ} ";
            throttleText.color = Color.red;
        }
        else if (maxQ >= 0.25f && maxQ < 0.30f)
        {
            maxQDisplay.color = Color.yellow;
            maxQDisplay.text = $"Atmosphere: {maxQ}";
            throttleText.color = Color.yellow;
        }
        else
        {
            maxQDisplay.color = Color.green;
            maxQDisplay.text = $"Atmosphere: {maxQ}";
        }
        
    }

    void UpdateAtmosphere()
    {
        float altitude = shuttleTransform.position.y;
        float atmosphereThickness = CalculateAtmosphere(altitude);
        
        atmosphericMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);
       
        
    }

   public float CalculateAtmosphere(float altitude)
    {
        // change start to 50,000
        // change end to 80,000
        float startTransition = 5500f;
        float endTransition = 60000f;

        if (altitude <= startTransition)
        {
            return atmosphericMaterial.GetFloat("_AtmosphereThickness");
        }
        else if (altitude >= endTransition)
        {
            return 0f;
        }
        else
        {
            // this is not quite correct, happening too quickly
            float progress = (altitude - startTransition) / (endTransition - startTransition);
            if (progress == 1f)
            {
                return 1f - progress;
            }
            return 1.22f - progress;
        }
    }

    void OnDestroy()
    {
        atmosphericMaterial.SetFloat("_AtmosphereThickness", originalAtmosphereThickness);
    }

    void AdjustGravity()
    {
        
    }
}