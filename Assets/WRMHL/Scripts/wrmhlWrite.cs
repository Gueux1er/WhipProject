using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is used to read the data coming from the device.
public class wrmhlWrite : MonoBehaviour {

	wrmhl myDevice1 = new wrmhl(); // wrmhl is the bridge beetwen your computer and hardware.
	wrmhl myDevice2 = new wrmhl(); // wrmhl is the bridge beetwen your computer and hardware.

	[Tooltip("SerialPort of your device.")]
	public string portName1 = "COM8";
	public string portName2 = "COM7";

	[Tooltip("Baudrate")]
	public int baudRate = 9600;


	[Tooltip("Timeout")]
	public int ReadTimeout = 20;

	[Tooltip("Something you want to send.")]
	public string dataToSend = "Hello World!";

	[Tooltip("QueueLenght")]
	public int QueueLenght = 1;

	void Start () {
		myDevice1.set (portName1, baudRate, ReadTimeout, QueueLenght); // This method set the communication with the following vars;
		myDevice2.set (portName2, baudRate, ReadTimeout, QueueLenght); // This method set the communication with the following vars;
		//                              Serial Port, Baud Rates, Read Timeout and QueueLenght.
		myDevice1.connect (); // This method open the Serial communication with the vars previously given.
		myDevice2.connect (); // This method open the Serial communication with the vars previously given.
	}

	// Update is called once per frame
	void Update () {
		myDevice1.send(dataToSend); // Send data to the device using thread.
		myDevice2.send(dataToSend); // Send data to the device using thread.
	}

	void OnApplicationQuit() { // close the Thread and Serial Port
		myDevice1.close();
		myDevice2.close();
	}
}
