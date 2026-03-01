using System.Collections.Generic;

namespace models
{
    public class PlayerModel
    {
        public string Name;
        public List<Card> Hand = new List<Card>();

        public int Chips = 1000;
        public bool Folded = false;
        public bool IsAI = false;

        public void ResetForNewRound()
        {
            Hand.Clear();
            Folded = false;
        }
    }
}