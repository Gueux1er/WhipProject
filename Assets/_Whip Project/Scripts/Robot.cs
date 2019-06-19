using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LibLabSystem;

namespace LibLabGames.WhipProject
{
    public class Robot : MonoBehaviour
    {
        public string robotName;
        public int robotIndex;
        public float production;

        public float baseLife;
        public float speedLowLife;
        public float[] addLifeWhip;
        public float currentLife;

        public Image image;
        public TextMeshProUGUI nameText;
        public Image separatorImage;

        public enum State { Awake, Productivity, Boost, Tired, Broken, Asleep };

        [System.Serializable]
        public struct RobotState
        {
            public State state;
            public Color color;
            public Sprite sprite;
        }
        public List<RobotState> robotStates;
        public State currentState;

        void Start()
        {
            currentLife = Random.Range(addLifeWhip[0], addLifeWhip[1]);
            nameText.text = string.Format("00{0} _{1}", robotIndex, robotName);
            ChangeVisualState(currentState, false);

            if (robotIndex == 0)
                separatorImage.gameObject.SetActive(false);
        }

        [ContextMenu("Manual Change Visual State")]
        public void ChangeVisualState()
        {
            ChangeVisualState(currentState);
        }

        public void ChangeVisualState(State state, bool log = true)
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
            case State.Awake:
                production = GameManager.instance.levelValues.GetFloatValue("normalProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("opportunityRoll"), GameManager.instance.levelValues.GetFloatValue("opportunityProbability"), State.Productivity);
                TiredStateCoroutine = COTiredState(GameManager.instance.levelValues.GetFloatValue("tiredTimerMin"), GameManager.instance.levelValues.GetFloatValue("tiredTimerMax"));
                StartCoroutine(TiredStateCoroutine);
                break;

            case State.Productivity:
                production = GameManager.instance.levelValues.GetFloatValue("normalProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("opportunityDuration"), 1, State.Awake);
                break;

            case State.Boost:
                production = GameManager.instance.levelValues.GetFloatValue("superBoostProduction");
                image.transform.DOMoveY(5, 0.2f).SetRelative().SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
                break;

            case State.Tired:
                production = GameManager.instance.levelValues.GetFloatValue("tiredProduction");
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("tiredDuration"), 1, State.Broken);
                break;

            case State.Broken:
                production = 0;
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("brokenDuration"), 1, State.Asleep);
                image.DOColor(Color.black, 0.5f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
                break;

            case State.Asleep:
                production = 0;
                break;
            }

            if (log)
                LLLog.Log("Robot", string.Format("Robot <color=green>#{0}</color> was changed state : <color=blue>{1}</color>.", robotIndex, state));
        }

        void Update()
        {
            switch (currentState)
            {
            case State.Awake:
                break;

            case State.Productivity:
                break;

            case State.Boost:
                break;

            case State.Tired:
                break;

            case State.Broken:
                break;

            case State.Asleep:
                break;
            }
        }

        public void Whipped()
        {
            switch (currentState)
            {
            case State.Awake:
                currentState = State.Broken;
                ChangeVisualState();
                break;

            case State.Productivity:
                currentState = State.Boost;
                ChangeVisualState();
                break;

            case State.Boost:
                DOChangeStateCoco(GameManager.instance.levelValues.GetFloatValue("superBoostDuration"), 1, State.Awake);
                break;

            case State.Tired:
                currentState = State.Awake;
                ChangeVisualState();
                break;

            case State.Broken:
                break;

            case State.Asleep:
                currentState = State.Awake;
                ChangeVisualState();
                break;
            }
        }

        private void DOChangeStateCoco(float timer, float chance, State newState)
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
        private IEnumerator COChangeState(float timer, float chance, State newState)
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

            currentState = State.Tired;
            ChangeVisualState();
        }
    }
}