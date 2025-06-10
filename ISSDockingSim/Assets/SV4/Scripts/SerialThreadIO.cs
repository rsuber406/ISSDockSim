using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using System;
using System.Security.Cryptography;

public class SerialThreadIO : MonoBehaviour
{
    // Serial port settings
    private SerialPort serialPort;
    private string comPort = "COM3";
    private int baudRate = 9600;
    private bool isInitialized = false; // prevent re-initializing this object
    int maxMessages = 100; // max number of messages to hold

    // Thread variables
    private Thread inputThread;
    private Thread outputThread;
    private readonly object queueLock = new object();
    private bool inputThreadIsLooping = false;
    private bool outputThreadIsLooping = false;
    private Queue<string> inputQueue; // string queue for reading ASCII
    private Queue<byte[]> outputByteQueue; // string queue for reading ASCII

    //[SerializeField] private List<MonoBehaviour> observers; // generic list of observers (optional)
    [SerializeField] private List<ISerialReader> observers; // generic list of observers (optional)

    //----------------------------------------------    
    // Singleton instance 
    //----------------------------------------------
    private static SerialThreadIO _instance; // this var holds the instance reference but is private (access via the Getter)

    public static SerialThreadIO instance // this Getter is public and encapsulates the "_instance" singleton var
    {
        get
        {
            // If a singleton instance is null we must create one, otherwise
            // we return a reference to the existing one, thus ensuring there is
            // always exactly one instance.

            if (_instance == null)
            {
                GameObject go = new GameObject("SerialThreadIO_Singleton");

                // Get a reference to the component, this will be our singleton instance 
                _instance = go.AddComponent<SerialThreadIO>();

                // Prevent this object from getting unloaded/destroyed when changing scenes
                DontDestroyOnLoad(go);
            }

            // Return the instance
            return _instance;
        }
    }

    public void Init(string com, int baud, ISerialReader observer = null)
    {
        if (!isInitialized)
        {
            print("Initialize: " + this.name);
            comPort = com;
            baudRate = baud;

            inputQueue = new Queue<string>();
            //outputFloatQueue = new Queue<float>();
            outputByteQueue = new Queue<byte[]>();
            observers = new List<ISerialReader>();

            if (observer != null)
            {
                AddObserver(observer);
            }

            OpenPort();
            StartThreads();

            isInitialized = true;
        }
    }

    public void AddObserver(ISerialReader observer)
    {
        // add this observer to the list
        if (observers != null && observer != null)
        {
            observers.Add(observer);
        }
    }

    public void RemoveObserver(ISerialReader observer)
    {
        // remove this observer from the list
        if (observers != null && observer != null)
        {
            int index = observers.IndexOf(observer);
            if(index > -1)
            {
                observers.Remove(observer);
            }
        }
    }

    //----------------------------------------------
    // Serial Port Setup
    //----------------------------------------------
    void OpenPort()
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
        }

        catch (System.Exception ex)
        {
            Debug.LogError("Error opening " + comPort + "\n" + ex.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            print("Close serialPort");
            serialPort.Close();
        }

        StopOutputThread();
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
                // Debug.Log(msg);

                for (int i = 0; i < observers.Count; i++)
                {
                    //observers[i]?.SendMessage("ReceiveMessage", msg);
                    observers[i]?.OnMessageReceived(msg);
                }
            }
        }
    }

    public void EnqueueFloats(float[] values)
    {
        byte[] bytesToSend = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
        {
            byte[] bytes = System.BitConverter.GetBytes(values[i]);
            for (int j = 0; j < bytes.Length; j++)
            {
                bytesToSend[i * 4 + j] = (bytes[j]);
            }
        }
        EnqueueBytes(bytesToSend);
    }

    public void EnqueueBytes(byte[] bytes)
    {
        lock (queueLock)
        {
            outputByteQueue.Enqueue(bytes);
        }
    }

    //----------------------------------------------
    // Threading Functionality
    //----------------------------------------------
    public void StartThreads()
    {
        outputThreadIsLooping = true;
        outputThread = new Thread(OutputThreadLoop);
        outputThread.Start();

        inputThreadIsLooping = true;
        inputThread = new Thread(InputThreadLoop);
        inputThread.Start();
    }

    public void OutputThreadLoop()
    {
        print("OutputThread Start");
        while (outputThreadIsLooping)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    if (outputByteQueue.Count > 0)
                    {
                        serialPort.Write("!");
                        lock (queueLock)
                        {
                            byte[] bytes = outputByteQueue.Dequeue();
                            serialPort.Write(bytes, 0, bytes.Length);
                        }
                        serialPort.Write("#");
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

    // Thread Loop
    public void InputThreadLoop()
    {
        print("InputThread Start");
        string stringBuffer = "";
        bool endOfMsg = false;

        while (inputThreadIsLooping)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    //string data = serialPort.ReadLine(); blocking call
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

    // Stop the thread (by setting the loop bool to false, causing the thread while loop to stop.
    public void StopOutputThread()
    {
        lock (queueLock)
        {
            inputThreadIsLooping = false;
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