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

        public SerialPort serialPort1;
        public SerialPort serialPort2;
        string[] stringDelimiters = new string[] { ":" };
        public int[] accelerometerRobots;

        public List<Robot> robots;
        
        public TextMeshProUGUI productionText;
        public float production;

        public LineRenderer lineRenderer;
        // public int lineSize;
        // public float spacePoint;
        // public float maxPointScore;
        // public float minPointScore;

        public GameObject debugDisplay;
        public List<InputField> accelerometerWhipInputFields;

        public static FMOD.Studio.EventInstance fmodEvent_StartGame;
        public static FMOD.Studio.EventInstance fmodEvent_Programm;

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

            fmodEvent_StartGame = FMODUnity.RuntimeManager.CreateInstance("event:/Systeme/Jingle_Annonce_DébutJeu");
            fmodEvent_StartGame.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
            fmodEvent_StartGame = FMODUnity.RuntimeManager.CreateInstance("event:/Systeme/Jingle_Annonce_Programme");
            fmodEvent_StartGame.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
        }

        private void Start()
        {
            // DOStart can be place as you wish
            base.DOStart();

            accelerometerRobots = new int[(int) settingValues.GetFloatValue("robots_amout")];

            for (int i = 0; i < accelerometerWhipInputFields.Count; ++i)
            {
                accelerometerWhipInputFields[i].gameObject.SetActive(i < accelerometerRobots.Length);
            }

            serialPort1 = new SerialPort("COM8", 9600, Parity.None, 8, StopBits.One);

            serialPort1.DtrEnable = false;
            serialPort1.ReadTimeout = 1;
            serialPort1.WriteTimeout = 1;
            serialPort1.Open();

            serialPort2 = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);

            serialPort2.DtrEnable = false;
            serialPort2.ReadTimeout = 1;
            serialPort2.WriteTimeout = 1;
            serialPort2.Open();
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
                string inData = serialPort1.ReadLine();
                serialPort1.BaseStream.Flush();
                serialPort1.DiscardInBuffer();
                return inData;
            }
            catch { return string.Empty; }
        }

        private void OnApplicationQuit()
        {
            if (serialPort1 != null && serialPort1.IsOpen)
            {
                serialPort1.Write("!");
                serialPort1.Close();
            }
            if (serialPort2 != null && serialPort2.IsOpen)
            {
                serialPort2.Write("!");
                serialPort2.Close();
            }
        }

        private void OnDisable()
        {
            if (serialPort1 != null && serialPort1.IsOpen)
            {
                serialPort1.Write("!");
                serialPort1.Close();
            }
            if (serialPort2 != null && serialPort2.IsOpen)
            {
                serialPort2.Write("!");
                serialPort2.Close();
            }
        }

        private void OnDestroy()
        {
            if (serialPort1 != null && serialPort1.IsOpen)
            {
                serialPort1.Write("!");
                serialPort1.Close();
            }
            if (serialPort2 != null && serialPort2.IsOpen)
            {
                serialPort2.Write("!");
                serialPort2.Close();
            }
        }
    }
}