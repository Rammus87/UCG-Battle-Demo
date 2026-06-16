using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace CardFanUI
{
    public class CardDealerManager : MonoBehaviour
    {
        public GameObject[] cardPrefabs;
        public Transform cardHolder;
        public int cardCount = 5;

        private void Start()
        {
            for (int i = 0; i < cardCount; i++)
            {
                GenerateCard();
            }
        }

        public void DrawCard()
        {
            GenerateCard();
        }

        private void GenerateCard()
        {
            var card = Instantiate(cardPrefabs[Random.Range(0, cardPrefabs.Length)], cardHolder);
            card.transform.GetChild(1).GetComponentInChildren<Image>().color = new Color(UnityEngine.Random.value,
                UnityEngine.Random.value, UnityEngine.Random.value);
        }
    }
}
