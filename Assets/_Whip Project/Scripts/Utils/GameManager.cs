using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using DG.Tweening;
using LibLabSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        public TextMeshProUGUI perSecondText;
        public float perSecond;

        public Image productiveCirclePart;
        public Image superboostCirclePart;
        public Image tiredCirclePart;
        public Image unproductiveCirclePart;

        public LineRenderer lineRenderer;
        // public int lineSize;
        // public float spacePoint;
        // public float maxPointScore;
        // public float minPointScore;

        public GameObject titleDisplay;

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

            productionText.text = "0";
        }

        private string[] portNames;
        [HideInInspector] public bool portFound;
        private void Start()
        {
            //base.DOStart();

            portNames = SerialPort.GetPortNames();

            foreach (var p in portNames)
            {
                if (p == "COM8")
                    portFound = true;
            }
            print(string.Format("FIND PORT : {0}", portFound));

            accelerometerRobots = new int[(int) settingValues.GetFloatValue("robots_amout")];

            for (int i = 0; i < accelerometerWhipInputFields.Count; ++i)
            {
                accelerometerWhipInputFields[i].gameObject.SetActive(i < accelerometerRobots.Length);
            }

            StartCoroutine(COUpdateProductiveCirclePart());
            StartCoroutine(COUpdateBoostCirclePart());
            StartCoroutine(COUpdateTiredCirclePart());

            if (!portFound)
                return;

            serialPort1 = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);

            serialPort1.DtrEnable = false;
            serialPort1.ReadTimeout = 10;
            serialPort1.WriteTimeout = 10;
            serialPort1.Open();

            serialPort2 = new SerialPort("COM8", 9600, Parity.None, 8, StopBits.One);

            serialPort2.DtrEnable = false;
            serialPort2.ReadTimeout = 10;
            serialPort2.WriteTimeout = 10;
            serialPort2.Open();
        }

        private void FixedUpdate()
        {
            if (!gameHasStarted)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    base.DOStart();
                    titleDisplay.SetActive(false);

                    unproductiveCirclePart.fillAmount = 0;
                    unproductiveCirclePart.DOFillAmount(1, 1).SetEase(Ease.Linear).SetDelay(0.5f);
                }
                return;
            }

            if (Time.frameCount % 2 == 0)
            {
                UpdateCircleChart();
            }

            perSecond = 0;
            for (int i = 0; i < robots.Count; ++i)
            {
                production += robots[i].production * Time.deltaTime;
                perSecond += robots[i].production;
            }
            if (production > 0)
                productionText.text = production.ToString("##,#", CultureInfo.CreateSpecificCulture("en-US"));
            else
                productionText.text = "0";

            if (perSecond > 0)
                perSecondText.text = "<size=50>+" + perSecond.ToString("##,#", CultureInfo.CreateSpecificCulture("en-US")) + "</size> per second";
            else
                perSecondText.text = "<size=50>+0</size> per second";

            if (portFound)
            {
                string cmd = CheckForRecievedData();
                if (cmd.StartsWith("W"))
                {
                    cmd = cmd.Remove(0, 2);
                    for (int i = 0; i < accelerometerRobots.Length; ++i)
                    {
                        accelerometerRobots[i] = int.Parse(cmd.Split('|') [i]);

                        if (accelerometerRobots[i] > PlayerPrefs.GetInt(string.Format("acceleroValue{0}", i)))
                        {
                            LLLog.Log("GameManager", string.Format("Robot <color=green>#{0}</color> was whipped with <color=red>{1}</color> force point.", i, accelerometerRobots[i]));
                            robots[i].Whipped();
                        }
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

        private float prodCount;
        private float boostCount;
        private float tiredCount;
        public void UpdateCircleChart()
        {
            prodCount = 0;
            boostCount = 0;
            tiredCount = 0;

            for (int i = 0; i < robots.Count; ++i)
            {
                switch (robots[i].currentState)
                {
                case Robot.eState.Awake:
                case Robot.eState.Productivity:
                    prodCount++;
                    break;

                case Robot.eState.Boost:
                    boostCount++;
                    break;

                case Robot.eState.Tired:
                    tiredCount++;
                    break;
                }
            }

            boostCount += prodCount;
            tiredCount += boostCount;
        }

        private float lastProdCount;
        private float prodStartTime;
        private float prodEvaluate;
        private IEnumerator COUpdateProductiveCirclePart()
        {
            while (true)
            {
                prodEvaluate = 0;
                lastProdCount = prodCount;

                while (lastProdCount == prodCount)
                    yield return null;

                prodStartTime = Time.time;
                while (Time.time - prodStartTime < 1)
                {
                    prodEvaluate += Time.deltaTime;

                    productiveCirclePart.fillAmount = Mathf.Lerp(lastProdCount * 2 / 10, prodCount * 2 / 10, prodEvaluate);

                    yield return null;
                }
            }
        }

        private float lastBoostCount;
        private float boostStartTime;
        private float boostEvaluate;
        private IEnumerator COUpdateBoostCirclePart()
        {
            while (true)
            {
                boostEvaluate = 0;
                lastBoostCount = boostCount;

                while (lastBoostCount == boostCount)
                    yield return null;

                boostStartTime = Time.time;
                while (Time.time - boostStartTime < 1)
                {
                    boostEvaluate += Time.deltaTime;

                    superboostCirclePart.fillAmount = Mathf.Lerp(lastBoostCount * 2 / 10, boostCount * 2 / 10, boostEvaluate);

                    yield return null;
                }
            }
        }

        private float lastTiredCount;
        private float tiredStartTime;
        private float tiredEvaluate;
        private IEnumerator COUpdateTiredCirclePart()
        {
            while (true)
            {
                tiredEvaluate = 0;
                lastTiredCount = tiredCount;

                while (lastTiredCount == tiredCount)
                    yield return null;

                tiredStartTime = Time.time;
                while (Time.time - tiredStartTime < 1)
                {
                    tiredEvaluate += Time.deltaTime;

                    tiredCirclePart.fillAmount = Mathf.Lerp(lastTiredCount * 2 / 10, tiredCount * 2 / 10, tiredEvaluate);

                    yield return null;
                }
            }
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