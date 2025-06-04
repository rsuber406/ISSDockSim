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

    void Start()
    {
        instance = this;
        shuttleRigidBody = shuttlePrefab.GetComponent<Rigidbody>();
        shuttleTransform = shuttlePrefab.GetComponent<Transform>();
        shuttleController = shuttlePrefab.GetComponent<ShuttleController>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateUIElements();
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
}