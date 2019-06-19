using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using LibLabSystem;
using UnityEngine;
using UnityEngine.UI;

namespace LibLabGames.WhipProject
{
    public class GameManager : IGameManager
    {
        public static GameManager instance;

        SerialPort serialPort;
        string[] stringDelimiters = new string[] { ":" };
        public int[] accelerometerRobots;

        public Transform robotParent;
        public GameObject robotPrefab;
        public List<Robot> robots;

        public GameObject debugDisplay;
        public List<InputField> accelerometerWhipInputFields;

        public override void GetSettingGameValues()
        {
            // Example :
            // int value = settingValues.GetIntValue("exampleThree");
        }

        private void Awake()
        {
            if (!DOAwake()) return;

            instance = this;

            settingValues.UpdateValuesByCSV(false);

            debugDisplay.SetActive(false);
        }

        private void Start()
        {
            // DOStart can be place as you wish
            base.DOStart();

            accelerometerRobots = new int[(int) settingValues.GetFloatValue("robots_amout")];

            robots = new List<Robot>();
            for (int i = 0; i < accelerometerRobots.Length; ++i)
            {
                robots.Add(Instantiate(robotPrefab, robotParent).GetComponent<Robot>());
            }

            for (int i = 0; i < accelerometerWhipInputFields.Count; ++i)
            {
                accelerometerWhipInputFields[i].gameObject.SetActive(i < accelerometerRobots.Length);
            }

            string[] portNames = SerialPort.GetPortNames();
            serialPort = new SerialPort(portNames[0], 9600, Parity.None, 8, StopBits.One);

            serialPort.DtrEnable = false;
            serialPort.ReadTimeout = 1;
            serialPort.WriteTimeout = 1;
            serialPort.Open();
        }

        private void FixedUpdate()
        {
            string cmd = CheckForRecievedData();
            if (cmd.StartsWith("W"))
            {
                cmd = cmd.Remove(0, 2);
                for (int i = 0; i < accelerometerRobots.Length; ++i)
                {
                    accelerometerRobots[i] = int.Parse(cmd.Split('|')[i]);

                    if (accelerometerRobots[i] > PlayerPrefs.GetInt(string.Format("acceleroValue{0}", i)))
                    {
                        LLLog.Log("GameManager", string.Format("Robot <color=green>#{0}</color> was whipped with <color=red>{1}</color> force point.", i, accelerometerRobots[i]));
                        robots[i].Whipped();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                debugDisplay.SetActive(!debugDisplay.activeInHierarchy);
            }
        }

        public string CheckForRecievedData()
        {
            try
            {
                string inData = serialPort.ReadLine();
                serialPort.BaseStream.Flush();
                serialPort.DiscardInBuffer();
                return inData;
            }
            catch { return string.Empty; }
        }

        private void OnApplicationQuit()
        {
            if (serialPort != null)
                serialPort.Close();
        }

        private void OnDisable()
        {
            if (serialPort != null)
                serialPort.Close();
        }

        private void OnDestroy()
        {
            if (serialPort != null)
                serialPort.Close();
        }
    }
}