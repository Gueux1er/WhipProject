using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LibLabGames.WhipProject
{
    public class Debug_WhipForceInputField : MonoBehaviour
    {
        public int accelerometerIndex;
        public Text currentValueText;
        public InputField inputField;

        public int m_acceleroValue = 1;
        public int acceleroValue
        {
            get
            {
                m_acceleroValue = PlayerPrefs.GetInt(string.Format("acceleroValue{0}", accelerometerIndex), 700);
                return m_acceleroValue;
            }

            set
            {
                PlayerPrefs.SetInt(string.Format("acceleroValue{0}", accelerometerIndex), value);
                PlayerPrefs.Save();
            }
        }

        private void Start()
        {
            currentValueText.text = acceleroValue.ToString();
        }

        private string CSVfileContent;
        private List<string> CSVlines;
        public void ChangeWhipForce()
        {
            if (!int.TryParse(inputField.text, out int value)) return;

            acceleroValue = int.Parse(inputField.text);

            inputField.text = string.Empty;
            
            currentValueText.text = acceleroValue.ToString();
        }
    }
}