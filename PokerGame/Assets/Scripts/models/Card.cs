// File holds all data related to the card
namespace models
{
    
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum Rank
    {
        Two   = 2, 
        Three = 3, 
        Four  = 4, 
        Five  = 5, 
        Six   = 6,
        Seven = 7, 
        Eight = 8, 
        Nine  = 9, 
        Ten   = 10,
        Jack  = 11, 
        Queen = 12, 
        King  = 13, 
        Ace   = 14,
    }

    public struct Card
    {
        public Suit Suit;
        public Rank Rank;

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }
    }
}