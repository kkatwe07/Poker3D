using core;
using DG.Tweening;
using models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace view
{
    public class UiManager : MonoBehaviour
    {
        [Header("General Texts")]
        [SerializeField] private TMP_Text potText;
        [SerializeField] private TMP_Text playerChipsText;
        [SerializeField] private TMP_Text aiChipsText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text winnerText;

        [Header("Button & Button Texts References")]
        [SerializeField] private Button callButton;
        [SerializeField] private Button raiseButton;
        [SerializeField] private TMP_Text callButtonText;
        [SerializeField] private TMP_Text raiseButtonText;

        [Header("Buttons Panel Object")]
        [SerializeField] private GameObject buttonsPanel;

        [Header("Instance of the Game Controller")]
        [SerializeField] private GameController gameController;

        private void Start()
        {
            stateText.gameObject.SetActive(false);
            winnerText.gameObject.SetActive(false);
        }
    
        /// <summary>
        /// Enables/Disable the player's buttons to Fold, Call or Raise 
        /// </summary>
        public void SetPlayerTurn(bool isPlayerTurn)
        {
            buttonsPanel.SetActive(isPlayerTurn);
        }

        /// <summary>
        /// Performs Fold action, referenced in the scene
        /// </summary>
        public void OnFold()
        {
            gameController.PlayerFold();
        }

        /// <summary>
        /// Performs Call action, referenced in the scene
        /// </summary>
        public void OnCall()
        {
            gameController.PlayerCall();
        }

        /// <summary>
        /// Performs Raise action, referenced in the scene
        /// </summary>
        public void OnRaise()
        {
            gameController.PlayerRaise();
        }

        /// <summary>
        /// Performs Restart action, referenced in the scene
        /// </summary>
        public void OnRestart()
        {
            gameController.RestartGame();
        }
    
        /// <summary>
        /// Updates UI texts
        /// </summary>
        public void UpdateUI(PlayerModel player, PlayerModel ai, int pot)
        {
            potText.text = $"Pot: {pot}";
            playerChipsText.text = $"Player Chips: {player.Chips}";
            aiChipsText.text = $"AI Chips: {ai.Chips}";
        }
    
        /// <summary>
        /// Updates the timer text
        /// </summary>
        public void UpdateTimer(int seconds)
        {
            timerText.text = $"Timer: {seconds}";
        }

        /// <summary>
        /// This animates the state text
        /// </summary>
        public void AnimateStateText(string msg, float duration)
        {
            stateText.gameObject.SetActive(true);
            stateText.DOFade(1f, 0f); 
            stateText.text = msg;

            RectTransform rectTransform = stateText.rectTransform;
            Vector2 startPos = rectTransform.anchoredPosition;

            rectTransform.DOAnchorPos(rectTransform.anchoredPosition + new Vector2(0, 50), duration)
                .SetEase(Ease.OutQuad);

            stateText.DOFade(0f, duration)
                .OnComplete(() =>
                {
                    stateText.gameObject.SetActive(false);
                    stateText.rectTransform.anchoredPosition = startPos; ;
                });
        }

        /// <summary>
        /// Shows the winner text
        /// </summary>
        public void ShowWinnerText(string msg, float duration)
        {
            winnerText.text = msg;
            winnerText.DOFade(1f, 0f);
            winnerText.gameObject.SetActive(true);

            winnerText.transform
                .DOScale(1.3f, duration)
                .SetLoops(4, LoopType.Yoyo);

            winnerText.DOFade(0f, duration)
                .SetDelay(duration / 2f)
                .OnComplete(() => stateText.gameObject.SetActive(false));
        }

        /// <summary>
        /// Animates the Pot text
        /// </summary>
        public void AnimatePotText()
        {
            potText.transform
                .DOScale(1.3f, 0.5f)
                .SetLoops(2, LoopType.Yoyo);
        }

        /// <summary>
        /// Updates the betting button texts
        /// </summary>
        public void UpdateBetButtons(int currentBet, int raiseAmount, int playerChips)
        {
            callButtonText.text = $"Call ({currentBet})";
            raiseButtonText.text = $"Raise ({currentBet + raiseAmount})";
            
            // Enable / Disable logic
            callButton.interactable = playerChips >= currentBet;
            raiseButton.interactable = playerChips >= (currentBet + raiseAmount);
        }
    }
}