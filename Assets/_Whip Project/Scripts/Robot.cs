using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using LibLabSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LibLabGames.WhipProject
{
    public class Robot : MonoBehaviour
    {
        public static FMOD.Studio.EventInstance fmodEvent;
        public static FMOD.Studio.EventInstance fmodEvent_Whipped;
        public static FMOD.Studio.EventInstance fmodEvent_Voice;

        [System.Serializable]
        public struct EventFMOD
        {
            public string tag;
            public string eventPath;
        }
        public List<EventFMOD> events;

        public string robotName;
        public int robotIndex;
        public float production;

        public Image image;
        public TextMeshProUGUI nameText;
        public Image separatorImage;

        public enum eState { Awake, Productivity, Boost, Tired, Broken, Asleep };

 [System.Serializable]
 public struct RobotState
 {
 public eState state;
 public Color color;
 public Sprite sprite;
        }
        public List<RobotState> robotStates;
        public eState currentState;

        public List<string> startPathVoices;
        public List<string> normalPathVoices;
        public List<string> productivityPathVoices;
        public List<string> boostPathVoices;
        public List<string> tiredPathVoices;
        public List<string> tiredToBorkenPathVoices;
        public List<string> whippedBrokenPathVoices;
        public List<string> whippedOnBoostPathVoices;
        public List<string> brokenToAwakePathVoices;

        IEnumerator Start()
        {
            nameText.text = string.Format("00{0} _{1}", robotIndex, robotName);

            if (robotIndex == 0)
                separatorImage.gameObject.SetActive(false);

            while (!GameManager.instance.gameHasStarted)
                yield return null;

            if (GameManager.instance.portFound)
            {
                while (!GameManager.instance.serialPort1.IsOpen || !GameManager.instance.serialPort2.IsOpen)
                {
                    print(string.Format("SerialPort 1 : {0}, SerialPort 2 : {1} ", GameManager.instance.serialPort1.IsOpen, GameManager.instance.serialPort2.IsOpen));
                    yield return null;
                }

                // Avoid timeOut;
                if (robotIndex != 0)
                    GameManager.instance.serialPort1.Write("AWA_" + robotIndex);
                else
                    GameManager.instance.serialPort2.Write("1");
            }

            FmodSoundEvent("AWAKE_START");

            if (robotIndex == 0)
                PlayVoiceFmodSoundEvent(startPathVoices[0]);
            else if (robotIndex == 2)
            {
                DOVirtual.DelayedCall(1f, () =>
                {
                    PlayVoiceFmodSoundEvent(startPathVoices[1]);
                });
            }
            else if (robotIndex == 4)
            {
                DOVirtual.DelayedCall(2.2f, () =>
                {
                    PlayVoiceFmodSoundEvent(startPathVoices[2]);
                });
            }

            yield return null;

            ChangeVisualState(currentState, false);

            DOVirtual.DelayedCall(3f, () =>
            {
                StartCoroutine(RandomNormalVoices());
            });
        }

        private IEnumerator RandomNormalVoices()
        {
            while (true)
            {
                while (currentState != eState.Awake)
                    yield return null;

                yield return new WaitForSeconds(Random.Range(6f, 30f));

                if (currentState == eState.Awake)
                    PlayVoiceFmodSoundEvent(normalPathVoices);
            }
        }

        [ContextMenu("Manual Change Visual State")]
        public void ChangeVisualState()
        {
            ChangeVisualState(currentState);
        }

        private eState lastState;
        public void ChangeVisualState(eState state, bool log = true)
        {
            if (ChangeStateCoroutine != null)
            {
                StopCoroutine(ChangeStateCoroutine);
                ChangeStateCoroutine = null;
            }
            if (TiredStateCoroutine != null)
            {
                StopCoroutine(TiredStateCoroutine);
                TiredStateCoroutine = null;
            }

            foreach (var v in robotStates)
            {
                if (v.state == state)
                {
                    image.sprite = v.sprite;
                    image.color = v.color;
                }
            }

            image.transform.DOKill();
            image.DOKill();
            image.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

            switch (state)
            {
            case eState.Awake:
                FmodSoundEvent("AWAKE_ON");
                production = GameManager.instance.levelValues.GetFloatValue("normalProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("opportunityRoll"), GameManager.instance.levelValues.GetFloatValue("opportunityProbability"), eState.Productivity);
                TiredStateCoroutine = COTiredState(GameManager.instance.levelValues.GetFloatValue("tiredTimerMin"), GameManager.instance.levelValues.GetFloatValue("tiredTimerMax"));
                StartCoroutine(TiredStateCoroutine);

                if (GameManager.instance.portFound)
                {
                    switch (robotIndex)
                    {
                    case 0:
                        GameManager.instance.serialPort2.Write("0");
                        break;
                    case 1:
                        GameManager.instance.serialPort1.Write("a");
                        break;
                    case 2:
                        GameManager.instance.serialPort1.Write("A");
                        break;
                    case 3:
                        GameManager.instance.serialPort1.Write("b");
                        break;
                    case 4:
                        GameManager.instance.serialPort1.Write("B");
                        break;
                    }
                }
                break;

            case eState.Productivity:
                PlayVoiceFmodSoundEvent(productivityPathVoices);
                FmodSoundEvent("PRODUCTIVITY");
                production = GameManager.instance.levelValues.GetFloatValue("normalProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("opportunityDuration"), 1, eState.Awake);

                if (GameManager.instance.portFound)
                {
                    switch (robotIndex)
                    {
                    case 0:
                        GameManager.instance.serialPort2.Write("1");
                        break;
                    case 1:
                        GameManager.instance.serialPort1.Write("c");
                        break;
                    case 2:
                        GameManager.instance.serialPort1.Write("C");
                        break;
                    case 3:
                        GameManager.instance.serialPort1.Write("d");
                        break;
                    case 4:
                        GameManager.instance.serialPort1.Write("D");
                        break;
                    }
                }
                break;

            case eState.Boost:
                PlayVoiceFmodSoundEvent(boostPathVoices);
                FmodSoundEvent("BOOST_START");
                production = GameManager.instance.levelValues.GetFloatValue("superBoostProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("superBoostDuration"), 1, eState.Awake);
                image.transform.DOMoveY(5, 0.2f).SetRelative().SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);

                if (GameManager.instance.portFound)
                {
                    switch (robotIndex)
                    {
                    case 0:
                        GameManager.instance.serialPort2.Write("2");
                        break;
                    case 1:
                        GameManager.instance.serialPort1.Write("e");
                        break;
                    case 2:
                        GameManager.instance.serialPort1.Write("E");
                        break;
                    case 3:
                        GameManager.instance.serialPort1.Write("f");
                        break;
                    case 4:
                        GameManager.instance.serialPort1.Write("F");
                        break;
                    }
                }
                break;

            case eState.Tired:
                PlayVoiceFmodSoundEvent(tiredPathVoices);
                production = GameManager.instance.levelValues.GetFloatValue("tiredProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("tiredDuration"), 1, eState.Broken);

                if (GameManager.instance.portFound)
                {
                    switch (robotIndex)
                    {
                    case 0:
                        GameManager.instance.serialPort2.Write("3");
                        break;
                    case 1:
                        GameManager.instance.serialPort1.Write("g");
                        break;
                    case 2:
                        GameManager.instance.serialPort1.Write("G");
                        break;
                    case 3:
                        GameManager.instance.serialPort1.Write("h");
                        break;
                    case 4:
                        GameManager.instance.serialPort1.Write("H");
                        break;
                    }
                }
                break;

            case eState.Broken:
                if (lastState == eState.Tired)
                    PlayVoiceFmodSoundEvent(tiredToBorkenPathVoices);

                FmodSoundEvent("BROKEN_START");
                production = 0;
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("brokenDuration"), 1, eState.Asleep);
                image.DOColor(Color.black, 0.5f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);

                if (GameManager.instance.portFound)
                {
                    switch (robotIndex)
                    {
                    case 0:
                        GameManager.instance.serialPort2.Write("4");
                        break;
                    case 1:
                        GameManager.instance.serialPort1.Write("i");
                        break;
                    case 2:
                        GameManager.instance.serialPort1.Write("I");
                        break;
                    case 3:
                        GameManager.instance.serialPort1.Write("j");
                        break;
                    case 4:
                        GameManager.instance.serialPort1.Write("J");
                        break;
                    }
                }
                break;

            case eState.Asleep:
                production = 0;

                if (GameManager.instance.portFound)
                {
                    switch (robotIndex)
                    {
                    case 0:
                        GameManager.instance.serialPort2.Write("5");
                        break;
                    case 1:
                        GameManager.instance.serialPort1.Write("k");
                        break;
                    case 2:
                        GameManager.instance.serialPort1.Write("K");
                        break;
                    case 3:
                        GameManager.instance.serialPort1.Write("l");
                        break;
                    case 4:
                        GameManager.instance.serialPort1.Write("L");
                        break;
                    }
                }
                break;
            }

            lastState = currentState;

            if (log)
                LLLog.Log("Robot", string.Format("Robot <color=green>#{0}</color> was changed state : <color=blue>{1}</color>.", robotIndex, state));
        }

        public void Whipped()
        {
            FmodSoundEventWhipped();

            switch (currentState)
            {
            case eState.Awake:
                PlayVoiceFmodSoundEvent(whippedBrokenPathVoices);
                currentState = eState.Broken;
                ChangeVisualState();
                break;

            case eState.Productivity:
                currentState = eState.Boost;
                ChangeVisualState();
                break;

            case eState.Boost:
                PlayVoiceFmodSoundEvent(whippedOnBoostPathVoices);
                break;

            case eState.Tired:
                currentState = eState.Awake;
                ChangeVisualState();
                break;

            case eState.Broken:
                break;

            case eState.Asleep:
                PlayVoiceFmodSoundEvent(brokenToAwakePathVoices);
                currentState = eState.Awake;
                ChangeVisualState();
                break;
            }
        }

        private void DOChangeStateCoco(float timer, float chance, eState newState)
        {
            if (ChangeStateCoroutine != null)
            {
                StopCoroutine(ChangeStateCoroutine);
                ChangeStateCoroutine = null;
            }

            ChangeStateCoroutine = COChangeState(timer, chance, newState);
            StartCoroutine(ChangeStateCoroutine);
        }

        private IEnumerator ChangeStateCoroutine;
        private IEnumerator COChangeState(float timer, float chance, eState newState)
        {
            yield return new WaitForSeconds(timer);

            if (chance != 1)
            {
                while (Random.Range(0f, 1f) > chance)
                    yield return new WaitForSeconds(timer);
            }

            currentState = newState;
            ChangeVisualState();
        }

        private IEnumerator TiredStateCoroutine;
        private IEnumerator COTiredState(float minTimer, float maxTimer)
        {
            float t = Random.Range(minTimer, maxTimer);

            yield return new WaitForSeconds(t);

            currentState = eState.Tired;
            ChangeVisualState();
        }

        string path;
        private void FmodSoundEvent(string tag)
        {
            path = string.Empty;
            foreach (var e in events)
            {
                if (tag == e.tag)
                {
                    path = e.eventPath;
                }
            }
            if (path == string.Empty)
            {
                LLLog.LogE("Robot", string.Format("Event Tag [{0}] not found", tag));
                return;
            }

            fmodEvent = FMODUnity.RuntimeManager.CreateInstance(string.Format(path, robotIndex));
            fmodEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
            fmodEvent.start();
        }

        private void FmodSoundEventWhipped()
        {
            // DOVirtual.DelayedCall(0.3f, () =>
            // {
            //     fmodEvent_Whipped = FMODUnity.RuntimeManager.CreateInstance(string.Format("event:/RobotTravail_SD/RobotTravail_{0}/Alarme_Dégat", robotIndex));
            //     fmodEvent_Whipped.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
            //     fmodEvent_Whipped.start();
            // });
        }

        private void PlayVoiceFmodSoundEvent(string voice)
        {
            fmodEvent_Voice = FMODUnity.RuntimeManager.CreateInstance(string.Format(voice, robotIndex));
            fmodEvent_Voice.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
            fmodEvent_Voice.start();
        }

        private void PlayVoiceFmodSoundEvent(List<string> voices)
        {
            fmodEvent_Voice = FMODUnity.RuntimeManager.CreateInstance(string.Format(voices[Random.Range(0, voices.Count)], robotIndex));
            fmodEvent_Voice.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero));
            fmodEvent_Voice.start();
        }
    }
}