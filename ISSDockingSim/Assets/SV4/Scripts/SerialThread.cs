using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;

public class SerialThread : MonoBehaviour
{
    // Serial port settings
    private SerialPort serialPort;
    private string comPort = "COM3";
    private int baudRate = 9600;
    private bool isInitialized = false; // prevent re-initializing this object
    int maxMessages = 100; // max number of messages to hold

    // Thread variables
    private Thread inputThread;
    private readonly object queueLock = new object();
    private bool inputThreadIsLooping = false;
    private Queue<string> inputQueue; // string queue for reading ASCII

    [SerializeField] private List<MonoBehaviour> observers; // generic list of observers (optional)

    //----------------------------------------------    
    // Singleton instance 
    //----------------------------------------------
    private static SerialThread _singleton; // this var holds the instance reference but is private (access via the Getter)

    public static SerialThread singleton // this Getter is public and encapsulates the "_instance" singleton var
    {
        get
        {
            // If a singleton instance is null we must create one, otherwise
            // we return a reference to the existing one, thus ensuring there is
            // always exactly one instance.

            if (_singleton == null)
            {
                GameObject go = new GameObject("SerialThread_Singleton");

                // Get a reference to the component, this will be our singleton instance 
                _singleton = go.AddComponent<SerialThread>();

                // Prevent this object from getting unloaded/destroyed when changing scenes
                DontDestroyOnLoad(go);
            }

            // Return the instance
            return _singleton;
        }
    }

    public void Init(string com, int baud, MonoBehaviour observer = null)
    {
        if (!isInitialized)
        {
            print("Initialize: " + this.name);
            comPort = com;
            baudRate = baud;

            // initialize collections
            inputQueue = new Queue<string>();
            observers = new List<MonoBehaviour>();

            // Add an observer if one is provided on Init()
            if (observer != null)
            {
                AddObserver(observer);
            }

            // Attempt to open the serial port
            if (OpenPort())
            {
                // start the thread
                StartThread();
            }

            isInitialized = true;
        }
    }

    public void AddObserver(MonoBehaviour observer)
    {
        // add this observer to the list
        if (observers != null && observer != null)
        {
            observers.Add(observer);
        }
    }

    //----------------------------------------------
    // Serial Port Setup
    //----------------------------------------------
    bool OpenPort()
    {
        // Setup Serial Port
        if (serialPort == null)
        {
            serialPort = new SerialPort(@"\\.\" + comPort); // format to force Unity to recognize ports beyond COM9
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.ReadTimeout = 1000;
        }

        try
        {
            serialPort.Open();
            Debug.Log("Initialize Serial Port: " + comPort);
            return true;
        }

        catch (System.Exception ex)
        {
            Debug.LogError("Error opening " + comPort + "\n" + ex.Message);
        }
        return false;
    }

    private void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            print("Close serialPort");
            serialPort.Close();
        }

        StopInputThread();
    }

    private void Update()
    {
        // Safely dequeue incoming messages (transfer to Unity thread)
        lock (queueLock)
        {
            // dequeue all messages in queue
            while (inputQueue.Count > 0)
            {
                string msg = inputQueue.Dequeue();
                Debug.Log(msg);

                for (int i = 0; i < observers.Count; i++)
                {
                    observers[i]?.SendMessage("ReceiveMessage", msg);
                }
            }
        }
    }

    //----------------------------------------------
    // Threading Functionality
    //----------------------------------------------
    public void StartThread()
    {
        inputThreadIsLooping = true;
        inputThread = new Thread(InputThreadLoop);
        inputThread.Start();
    }

    // Thread Loop
    public void InputThreadLoop()
    {
        print("Thread Start");
        string stringBuffer = "";
        bool endOfMsg = false;

        while (inputThreadIsLooping)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    //string data = serialPort.ReadLine();
                    if (serialPort.BytesToRead > 0)
                    {
                        char inputRead = (char)serialPort.ReadChar();

                        if (inputRead == '\r' || inputRead == '\n')
                        {
                            if (stringBuffer.Length > 0)
                            {
                                endOfMsg = true;
                            }
                        }
                        else
                        {
                            stringBuffer += inputRead;
                        }
                    }

                    if (endOfMsg)
                    {
                        lock (queueLock)
                        {
                            // Use locked queue to safely transfer data from
                            // this thread to Unity's main/update thread 
                            if (inputQueue.Count < maxMessages)
                            {
                                inputQueue.Enqueue(stringBuffer);
                            }
                        }
                        stringBuffer = "";
                        endOfMsg = false;
                    }
                }
                catch (System.Exception ex)
                {
                    //if (ex.InnerException is not System.TimeoutException)
                    {
                        print(ex.Message);
                    }
                }
            }
        }
    }

    public void StopInputThread()
    {
        lock (queueLock)
        {
            inputThreadIsLooping = false;
        }
    }
}