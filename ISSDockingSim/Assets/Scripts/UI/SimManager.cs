using TMPro;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private static SimManager instance;

    [Header("Shuttle References")] [SerializeField]
    public TextMeshProUGUI altitudeText;

    [SerializeField] public TextMeshProUGUI throttleText;
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

    void FixedUpdate()
    {
        UpdateAtmosphere();
    }

    void UpdateUIElements()
    {
        UpdateAltitudeElement();
        UpdateThrottleElement();
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

    void UpdateAtmosphere()
    {
        float altitude = shuttleTransform.position.y;
        float atmosphereThickness = CalculateAtmosphere(altitude);
        
        atmosphericMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);
       
        
    }

    float CalculateAtmosphere(float altitude)
    {
        // change start to 50,000
        // change end to 80,000
        float startTransition = 5000f;
        float endTransition = 8000f;

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
            return 1f - progress;
        }
    }

    void OnDestroy()
    {
        atmosphericMaterial.SetFloat("_AtmosphereThickness", originalAtmosphereThickness);
    }
}