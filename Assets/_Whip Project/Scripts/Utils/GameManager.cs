using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using LibLabSystem;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LibLabGames.WhipProject
{
    public class GameManager : IGameManager
    {
        public static GameManager instance;

        public SettingValues levelValues;

        SerialPort serialPort;
        string[] stringDelimiters = new string[] { ":" };
        public int[] accelerometerRobots;

        public Transform robotParent;
        public GameObject robotPrefab;
        public List<string> robotNames;
        public List<Robot> robots;
        
        public TextMeshProUGUI productionText;
        public float production;

        public LineRenderer lineRenderer;
        public int lineSize;
        public float spacePoint;
        public float maxPointScore;
        public float minPointScore;

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

            debugDisplay.SetActive(false);

            levelValues.UpdateValuesByCSV(false);
        }

        private void Start()
        {
            // DOStart can be place as you wish
            base.DOStart();

            accelerometerRobots = new int[(int) settingValues.GetFloatValue("robots_amout")];

            for (int i = robotParent.childCount - 1; i > -1; --i)
            {
                Destroy(robotParent.GetChild(i).gameObject);
            }

            robots = new List<Robot>();
            for (int i = 0; i < accelerometerRobots.Length; ++i)
            {
                robots.Add(Instantiate(robotPrefab, robotParent).GetComponent<Robot>());
                robots[i].robotName = robotNames[i];
                robots[i].robotIndex = i;
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
            for (int i = 0; i < robots.Count; ++i)
            {
                production += robots[i].production * Time.deltaTime;
            }
            productionText.text = String.Format("{0:#,###0}", production);

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

            if (LLDebug.instance.DEBUG_DISPLAY)
            {
                debugDisplay.SetActive(true);

                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    robots[0].Whipped();
                }
                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    robots[1].Whipped();
                }
                if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    robots[2].Whipped();
                }
                if (Input.GetKeyDown(KeyCode.Keypad3))
                {
                    robots[3].Whipped();
                }
                if (Input.GetKeyDown(KeyCode.Keypad4))
                {
                    robots[4].Whipped();
                }
            }
            else
            {
                debugDisplay.SetActive(false);
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