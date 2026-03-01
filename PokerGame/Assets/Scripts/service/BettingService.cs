using models;

namespace service
{
    public class BettingService
    {
        public int Pot { get; private set; }

        public void PlaceBet(PlayerModel player, int amount)
        {
            if (player.Chips <= 0) return;

            amount = UnityEngine.Mathf.Min(amount, player.Chips);
            player.Chips -= amount;
            Pot += amount;
        }

        public void ResetPot()
        {
            Pot = 0;
        }
    }
}