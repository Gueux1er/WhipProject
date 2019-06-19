using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LibLabGames.WhipProject
{
    public class Robot : MonoBehaviour
    {
        public float baseLife;
        public float speedLowLife;
        public float[] addLifeWhip;
        public float currentLife;

        public Image image;
        public Color baseColor;
        public Color whippedColor;

        void Start()
        {
            currentLife = Random.Range(addLifeWhip[0], addLifeWhip[1]);
        }

        void Update()
        {
            currentLife -= speedLowLife * Time.deltaTime;
            image.color = Color.Lerp(whippedColor, baseColor, currentLife / baseLife);
        }

        public void Whipped()
        {
            currentLife += Random.Range(addLifeWhip[0], addLifeWhip[1]);
            currentLife = Mathf.Clamp(currentLife, 0, baseLife);
        }
    }
}

