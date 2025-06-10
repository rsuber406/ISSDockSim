using UnityEngine;
using UnityEngine.UI;
using System.IO.Ports;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public class PlatformController : MonoBehaviour
{
    [SerializeField] Slider[] sliders; // list of references to the sliders
    [SerializeField] bool useSliders = false;

    public enum PlatformModes { Mode_8Bit, Mode_Float32 };
    [SerializeField] PlatformModes mode = PlatformModes.Mode_Float32;

    SerialPort serialPort;
    public string comPort;
    public int baudRate;

    bool initialized = false; // a bool to check if this controller has been initialized

    // 6 DOF Axis Order for Simviz Stewart Platform: [Sway, Surge, Heave, Pitch, Roll, Yaw]
    public byte[] byteValues; // six byte values to be sent to the platform (8Bit Mode)
    public float[] floatValues; // six 32bit float values (Float32 mode)

    private string startFrame = "!"; // '!' startFrame character (33) (to indicate the start of a message)
    private string endFrame = "#"; // '#' endFrame character (35) (to indicate the end of a message)

    private float nextSendTimestamp = 0;
    [SerializeField] private float nextSendDelay = 0.02f; // delay in seconds (float)

    private void Start()
    {
        if (!initialized) { Init(comPort, baudRate); }
    }

    // Init is used to initialize our variables, and will attempt
    // to open the requested SerialPort() for communication
    public bool Init(string _com, int _baud)
    {
        if (initialized)
        {
            //Debug.LogWarning(typeof(PlatformController).ToString() + ": is already initialized");
            return false;
        }

        initialized = true;

        // Define and set some default values
        comPort = _com;
        baudRate = _baud;
        byteValues = new byte[] { 128, 128, 128, 128, 128, 128 };
        floatValues = new float[] { 0, 0, 0, 0, 0, 0 };

        // Create SerialPort instance(this does not open the connection)
        if (serialPort == null)
        {
            serialPort = new SerialPort(@"\\.\" + comPort); // special port formating to force Unity to recognize ports beyond COM9            
            serialPort.BaudRate = baudRate;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.ReadTimeout = 20; // miliseconds
        }

        // Attempt to open the SerialPort and log any errors
        try
        {
            serialPort.Open();
            Debug.Log("Initialize Serial Port: " + comPort);
        }
        catch (System.IO.IOException ex)
        {
            Debug.LogError("Error opening " + comPort + "\n" + ex.Message);
            return false;
        }

        // Reset sliders, if in use
        if (useSliders) { ReinitializeSliders(); }

        // Reset platform values
        HomePlatform();

        return true;
    }

    void Update()
    {
        // if true this will override user values with slider values
        if (useSliders == true) { UpdateValuesFromSliders(); }

        if (Time.time > nextSendTimestamp)
        {
            SendSerial(); // Send the data out on a fixed timeStamp 
            nextSendTimestamp = Time.time + nextSendDelay; // update time stamp (0.02 ms = 50 FPS)
        }
    }

    public void SendSerial()
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            return; // EARLY RETURN if no port open
        }

        if (mode == PlatformModes.Mode_8Bit)
        {
            serialPort.Write(startFrame); // start frame of message
            //serialPort.Write("8"); // message type: 8 indicates 8Bit mode
            serialPort.Write(byteValues, 0, byteValues.Length); // Packet Data: 6 bytes (6 bytes)
            serialPort.Write(endFrame); // end frame of message
        }
        else if (mode == PlatformModes.Mode_Float32)
        {
            serialPort.Write(startFrame); // start frame of message
            //serialPort.Write("F"); // message type: F indicates Floating point mode
            for (int i = 0; i < floatValues.Length; i++) // Packet Data: 6 Floats (24 bytes)
            {
                byte[] myBytes = System.BitConverter.GetBytes(floatValues[i]);
                serialPort.Write(myBytes, 0, myBytes.Length);
            }
            serialPort.Write(endFrame); // end frame of message
        }
    }

    public void HomePlatform()
    {
        if (useSliders)
        {
            ResetSliders();

            // 8 bit int mode (a range from 0 to 255)
            if (mode == PlatformModes.Mode_8Bit)
            {
                for (int i = 0; i < byteValues.Length; i++)
                {
                    byteValues[i] = 128;
                }
            }
            // 32 bit float mode
            else if (mode == PlatformModes.Mode_Float32)
            {
                for (int i = 0; i < floatValues.Length; i++)
                {
                    floatValues[i] = 0;
                }
            }

            SendSerial();
        }
    }


    // At shutdown, attempt to reset values and close ports   
    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            HomePlatform();
            serialPort.Close();
        }
    }

    #region Slider Code
    void ReinitializeSliders()
    {
        if (mode == PlatformModes.Mode_8Bit)
        {
            for (int i = 0; i < sliders.Length; i++)
            {
                sliders[i].wholeNumbers = true;
                sliders[i].minValue = 0;
                sliders[i].maxValue = 255;
                sliders[i].value = mode == PlatformModes.Mode_8Bit ? 128 : 0;
            }
        }
        else if (mode == PlatformModes.Mode_Float32)
        {
            for (int i = 0; i < sliders.Length; i++)
            {
                sliders[i].wholeNumbers = false;
                sliders[i].minValue = -30;
                sliders[i].maxValue = 30;
                sliders[i].value = mode == PlatformModes.Mode_8Bit ? 128 : 0;
            }
        }
    }

    public void UpdateValuesFromSliders()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            if (mode == PlatformModes.Mode_Float32) { floatValues[i] = sliders[i].value; }
            else if (mode == PlatformModes.Mode_8Bit) { byteValues[i] = (byte)sliders[i].value; }
        }
    }

    public void ResetSliders()
    {
        ReinitializeSliders();

        for (int i = 0; i < sliders.Length; i++)
        {
            // reset the sliders to their midpoint,
            // 128 for byte value, or 0 as a float
            sliders[i].value = mode == PlatformModes.Mode_8Bit ? 128 : 0;
        }
    }
    #endregion

    #region Getters
    public float Sway
    {
        get { return floatValues[0]; }
        set { floatValues[0] = value; }
    }
    public float Surge
    {
        get { return floatValues[1]; }
        set { floatValues[1] = value; }
    }
    public float Heave
    {
        get { return floatValues[2]; }
        set { floatValues[2] = value; }
    }
    public float Pitch
    {
        get { return floatValues[3]; }
        set { floatValues[3] = value; }
    }
    public float Roll
    {
        get { return floatValues[4]; }
        set { floatValues[4] = value; }
    }
    public float Yaw
    {
        get { return floatValues[5]; }
        set { floatValues[5] = value; }
    }
    #endregion

    #region PlatformController Singleton
    // A singleton implementation of this controller is very convenient
    // for switching scenes, maintaining persistence, and easy access.

    private static PlatformController _singleton;
    public static PlatformController singleton
    {
        get
        {
            // check if singleton instance exists
            if (_singleton == null)
            {
                // create a gameobject
                GameObject go = new GameObject("PlatformController");
                // mark it to be persistent (not destroyed on scene change)
                DontDestroyOnLoad(go);
                // attach/create the instance of the script
                _singleton = go.AddComponent<PlatformController>();
            }

            // return the singleton instance
            return _singleton;
        }
    }
    #endregion
}
