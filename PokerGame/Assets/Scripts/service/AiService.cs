using System.Collections.Generic;
using models;

namespace service
{
    public class AiService
    {
        private HandEvaluatorService _evaluator;

        public AiService(HandEvaluatorService evaluator)
        {
            _evaluator = evaluator;
        }

        public PlayerAction DecideAction(PlayerModel aiPlayer, List<Card> communityCards, int currentBet, int raiseAmount)
        {
            List<Card> cards = new(aiPlayer.Hand);
            cards.AddRange(communityCards);

            var eval = _evaluator.EvaluateDetailed(cards);
            long strength = eval.score;

            int handTier = (int)(strength / 1000000000);

            if (handTier >= 6)
                return PlayerAction.Raise;

            if (handTier >= 2)
                return PlayerAction.Call;

            return UnityEngine.Random.value < 0.4f ? PlayerAction.Fold : PlayerAction.Call;
        }
    }

    public enum PlayerAction
    {
        Fold,
        Call,
        Raise
    }
}