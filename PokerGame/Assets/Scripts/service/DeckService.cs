using System.Collections.Generic;
using models;
using UnityEngine;

namespace service
{
    public class DeckService
    {
        private Stack<Card> _deck;

        public void CreateDeck()
        {
            List<Card> cards = new List<Card>();

            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                cards.Add(new Card(suit, rank));

            Shuffle(cards);
            _deck = new Stack<Card>(cards);
        }

        private void Shuffle(List<Card> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
        }

        public Card Draw()
        {
            return _deck.Pop();
        }
    }
}