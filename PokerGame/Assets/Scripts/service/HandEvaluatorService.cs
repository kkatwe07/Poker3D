using System.Collections.Generic;
using System.Linq;
using models;

namespace service
{
    /// <summary>
    /// Evaluates poker hands.
    /// Given up to 7 cards, it finds the best possible 5-card hand.
    /// </summary>
    public class HandEvaluatorService
    {
        /// <summary>
        /// Main entry point.
        /// Takes all available cards
        /// generates every possible 5-card combination,
        /// and returns the strongest one.
        /// </summary>
        public (long score, string name, List<Card> bestCards) EvaluateDetailed(List<Card> cards)
        {
            var allCombos = Get5CardCombos(cards);

            long bestScore = -1;
            string bestName = "";
            List<Card> bestHand = null;

            foreach (var combo in allCombos)
            {
                var result = Evaluate5Cards(combo);

                if (result.score > bestScore)
                {
                    bestScore = result.score;
                    bestName = result.name;
                    bestHand = combo;
                }
            }

            return (bestScore, bestName, bestHand);
        }

        /// <summary>
        /// Evaluates exactly 5 cards and determines hand type + strength.
        /// This is where the real ranking logic lives.
        /// </summary>
        private (long score, string name) Evaluate5Cards(List<Card> cards)
        {
            // Extract ranks sorted high to low
            List<int> ranks = cards.Select(c => (int)c.Rank).OrderByDescending(r => r).ToList();

            // Extract suits
            List<Suit> suits = cards.Select(c => c.Suit).ToList();

            // Check for flush (all suits identical)
            bool isFlush = suits.All(s => s == suits[0]);

            // Check for straight (including Ace-low)
            bool isStraight = IsStraight(ranks, out int straightHigh);

            // Group cards by rank
            var groups = cards.GroupBy(c => (int)c.Rank)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)    // First by how many duplicates
                .ThenByDescending(g => g.Rank)      // Then by rank value
                .ToList();

            // Flatten grouped ranks into ordered list
            List<int> orderedRanks = new();

            foreach (var g in groups)
                for (int i = 0; i < g.Count; i++)
                    orderedRanks.Add(g.Rank);
            
            // Now determine hand strength from strongest to weakest
            // Royal Flush
            if (isFlush && isStraight && straightHigh == 14)
                return (EncodeScore(9, new List<int> { 14 }), "Royal Flush");

            // Straight Flush
            if (isFlush && isStraight)
                return (EncodeScore(8, new List<int> { straightHigh }), "Straight Flush");

            // Four of a kind
            if (groups[0].Count == 4)
                return (EncodeScore(7, orderedRanks), "Four of a Kind");

            // Full House
            if (groups[0].Count == 3 && groups[1].Count == 2)
                return (EncodeScore(6, orderedRanks), "Full House");

            // Flush
            if (isFlush)
                return (EncodeScore(5, ranks), "Flush");

            // Straight
            if (isStraight)
                return (EncodeScore(4, new List<int> { straightHigh }), "Straight");

            // Three of a kind
            if (groups[0].Count == 3)
                return (EncodeScore(3, orderedRanks), "Three of a Kind");

            // Two pair
            if (groups[0].Count == 2 && groups[1].Count == 2)
                return (EncodeScore(2, orderedRanks), "Two Pair");

            // One pair
            if (groups[0].Count == 2)
                return (EncodeScore(1, orderedRanks), "One Pair");

            // High card
            return (EncodeScore(0, ranks), "High Card");
        }

        /// <summary>
        /// Checks whether the ranks form a straight.
        /// Supports Ace-low straight (A-2-3-4-5).
        /// </summary>
        private bool IsStraight(List<int> ranks, out int highCard)
        {
            var distinct = ranks.Distinct().OrderByDescending(r => r).ToList();

            // Normal straight (10-9-8-7-6)
            for (int i = 0; i <= distinct.Count - 5; i++)
            {
                if (distinct[i] - distinct[i + 4] == 4)
                {
                    highCard = distinct[i];
                    return true;
                }
            }

            // Special case: Ace-low straight (A-2-3-4-5)
            if (distinct.Contains(14) &&
                distinct.Contains(2) &&
                distinct.Contains(3) &&
                distinct.Contains(4) &&
                distinct.Contains(5))
            {
                highCard = 5; // In A-2-3-4-5, 5 is the highest card
                return true;
            }

            highCard = 0;
            return false;
        }

        /// <summary>
        /// Generates all possible 5-card combinations
        /// from the given list.
        /// </summary>
        private List<List<Card>> Get5CardCombos(List<Card> cards)
        {
            var result = new List<List<Card>>();

            int n = cards.Count;

            // Brute-force combination generation
            for (int a = 0; a < n - 4; a++)
            for (int b = a + 1; b < n - 3; b++)
            for (int c = b + 1; c < n - 2; c++)
            for (int d = c + 1; d < n - 1; d++)
            for (int e = d + 1; e < n; e++)
            {
                result.Add(new List<Card>
                {
                    cards[a],
                    cards[b],
                    cards[c],
                    cards[d],
                    cards[e]
                });
            }

            return result;
        }

        /// <summary>
        /// Encodes hand rank and kicker values into a single long number.
        /// This allows direct numeric comparison between hands.
        /// </summary>
        private long EncodeScore(int handRank, List<int> ranks)
        {
            long score = handRank;

            // Each rank is appended into the number.
            // Higher handRank always dominates.
            foreach (var r in ranks)
            {
                score = score * 100 + r;
            }

            return score;
        }
    }
}