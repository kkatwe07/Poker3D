using System.Collections.Generic;
using models;
using UnityEngine;

namespace Utils
{
    public class CardDatabase : MonoBehaviour
    {
        public Texture cardBack;
        public List<Texture> cardTextures;

        private Dictionary<string, Texture> _lookup = new();

        void Awake()
        {
            foreach (var tex in cardTextures)
                _lookup[tex.name.ToLower()] = tex;
        }

        public Texture GetTexture(Card card)
        {
            string rankString = GetRankString(card.Rank);
            string key = $"card{card.Suit}_{rankString}".ToLower();

            if (_lookup.TryGetValue(key, out var tex))
                return tex;

            Debug.LogWarning("Missing texture: " + key);
            return null;
        }

        private string GetRankString(Rank rank)
        {
            if (rank == Rank.Ace) return "A";
            if (rank == Rank.King) return "K";
            if (rank == Rank.Queen) return "Q";
            if (rank == Rank.Jack) return "J";

            return ((int)rank).ToString();
        }
    }
}