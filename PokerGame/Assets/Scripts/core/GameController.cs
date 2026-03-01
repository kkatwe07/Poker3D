using System;
using System.Collections.Generic;
using DG.Tweening;
using models;
using service;
using UnityEngine;
using Utils;
using view;
using Random = UnityEngine.Random;

namespace core
{
    /// <summary>
    /// Main game controller.
    /// Handles flow, turns, betting and showdown.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Scene References")]
        public Transform playerCardArea;
        public Transform aiCardArea;
        public Transform communityArea;
        public GameObject cardPrefab;
        public CardDatabase cardDB;
        public UiManager uiManager;
        public TurnManager turnManager;

        // Core services
        private DeckService _deck = new DeckService();
        private GameStateController _state = new GameStateController();
        private BettingService _betting = new BettingService();
        private HandEvaluatorService _evaluator = new HandEvaluatorService();
        private AiService _aiService;

        // private lists & dictionaries
        private List<PlayerModel> _players = new();
        private List<Card> _communityCards = new();
        private List<GameObject> _playerCardObjects = new();
        private List<GameObject> _aiCardObjects = new();
        private Dictionary<Card, GameObject> _cardMap = new();
    
        // Betting values
        private int _currentBet = 10;
        private int _raiseAmount = 20;
        
        // Tracks whose turn it is
        private TurnOwner _currentTurn;
        
        // Action events
        public Action<PlayerModel, PlayerModel, int> OnUIUpdate;
        public Action<string, float> OnStateMessage;
        public Action<string, float> OnWinnerMessage;
        public Action<int, int, int> OnBetButtonsUpdate;
        public Action<bool> OnTurnChanged;
        
        // track local player and opponent
        private PlayerModel LocalPlayer => _players.Find(p => p.IsLocal);
        private PlayerModel Opponent => _players.Find(p => !p.IsLocal);

        void Start()
        {
            // Create player and AI
            _players.Add(new PlayerModel { PlayerId = 0, Name = "Player", IsLocal = true });
            _players.Add(new PlayerModel { PlayerId = 1, Name = "AI", IsAI = true, Chips = 1000 });

            turnManager.Init(this, uiManager);
            _aiService = new AiService(_evaluator);
            _state.OnStateChanged += HandleStateChange;

            StartGame();
        }

        /// <summary>
        /// Starts a fresh round
        /// </summary>
        private void StartGame()
        {
            OnWinnerMessage?.Invoke("", 0.1f);// clears winner text
            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, LocalPlayer.Chips);

            _deck.CreateDeck();
            _communityCards.Clear();

            // Deal 2 cards to each player
            foreach (var p in _players)
            {
                p.ResetForNewRound();
                p.Hand.Add(_deck.Draw());
                p.Hand.Add(_deck.Draw());
            }
            SpawnCards();

            _state.SetState(GameState.PreFlop);
            StartPlayerTurn();
        }

        /// <summary>
        /// Spawns both players cards
        /// </summary>
        private void SpawnCards()
        {
            SpawnHand(LocalPlayer, playerCardArea);
            SpawnHand(Opponent, aiCardArea);
        }

        /// <summary>
        /// Spawns a single player's hand
        /// </summary>
        private void SpawnHand(PlayerModel player, Transform area)
        {
            for (int i = 0; i < player.Hand.Count; i++)
            {
                Vector3 deckPosition = communityArea.position;
                Vector3 pos = area.position + new Vector3(i * 0.8f, 0, 0);
                var cardGo = Instantiate(cardPrefab, deckPosition, Quaternion.Euler(90, 0, 0), area);
                _cardMap[player.Hand[i]] = cardGo;
                cardGo.transform.DOMove(pos, 0.4f).SetEase(Ease.OutQuad);

                cardGo.transform.DORotate(new Vector3(90,0,0), 0.3f);

                CardView view = cardGo.GetComponent<CardView>();

                if (player.IsAI)
                {
                    view.SetTexture(cardDB.cardBack);
                    _aiCardObjects.Add(cardGo);
                }
                else
                {
                    view.SetTexture(cardDB.GetTexture(player.Hand[i]));
                    _playerCardObjects.Add(cardGo);
                }
            }
        }
    
        /// <summary>
        /// Deals community cards
        /// </summary>
        private void DealCommunityCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var card = _deck.Draw();
                _communityCards.Add(card);
                SpawnSingleCommunity(card, _communityCards.Count - 1);
            }
        }
        
        /// <summary>
        /// Spawns a single community card
        /// </summary>
        private void SpawnSingleCommunity(Card card, int index)
        {
            Vector3 pos = communityArea.position + new Vector3(index * 0.8f, 0, 0);
            Vector3 deckPos = communityArea.position + Vector3.up * 2f;

            var cardGo = Instantiate(cardPrefab, deckPos, Quaternion.Euler(90, 0, 0), communityArea);
            _cardMap[card] = cardGo;
            cardGo.transform.DOMove(pos, 0.4f).SetEase(Ease.OutQuad);

            var view = cardGo.GetComponent<CardView>();
            view.SetTexture(cardDB.GetTexture(card));
        }
        
        /// <summary>
        /// Called whenever game state changes
        /// </summary>
        private void HandleStateChange(GameState state)
        {
            Debug.Log("Game Phase → " + state);

            if (state == GameState.Showdown)
                return;

            // AI decision after cards dealt
            if (_currentTurn == TurnOwner.AI)
                Invoke(nameof(AITurn), 1f);
        }
        
        /// <summary>
        /// Moves to next game phase
        /// </summary>
        private void NextState()
        {
            switch (_state.CurrentState)
            {
                case GameState.PreFlop:
                    _state.SetState(GameState.Flop);
                    DealCommunityCards(3);
                    break;

                case GameState.Flop:
                    _state.SetState(GameState.Turn);
                    DealCommunityCards(1);
                    break;

                case GameState.Turn:
                    _state.SetState(GameState.River);
                    DealCommunityCards(1);
                    break;

                case GameState.River:
                    DoShowdown();
                    return;
            }

            StartPlayerTurn();
        }
        
        /// <summary>
        /// Starts player turn
        /// </summary>
        private void StartPlayerTurn()
        {
            _currentTurn = TurnOwner.Player;
            OnTurnChanged?.Invoke(true);
            turnManager.StartTimer();
        }

        /// <summary>
        /// Ends player turn and switches to AI
        /// </summary>
        private void EndPlayerTurn()
        {
            turnManager.StopTimer();
            _currentTurn = TurnOwner.AI;
            OnTurnChanged?.Invoke(false);
            Invoke(nameof(AITurn), 1f);
        }
    
        /// <summary>
        /// Player folds
        /// </summary>
        public void PlayerFold()
        {
            if (_currentTurn != TurnOwner.Player) return;

            LocalPlayer.Folded = true;

            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
            OnStateMessage?.Invoke("Player Folds", 1.5f);
            OnWinnerMessage?.Invoke("AI Wins!", 3f);
            OnTurnChanged?.Invoke(false);

            // Flip player cards face down
            foreach (var c in _playerCardObjects)
            {
                CardView view = c.GetComponent<CardView>();
                view.SetTexture(cardDB.cardBack);
            }

            AwardPotTo(Opponent);
            turnManager.StopTimer();
        }

        /// <summary>
        /// Player calls current bet
        /// </summary>
        public void PlayerCall()
        {
            if (_currentTurn != TurnOwner.Player) return;

            _betting.PlaceBet(LocalPlayer, _currentBet);
            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, LocalPlayer.Chips);
            EndPlayerTurn();
        }

        /// <summary>
        /// Player raises bet
        /// </summary>
        public void PlayerRaise()
        {
            if (_currentTurn != TurnOwner.Player) return;

            _currentBet += _raiseAmount;
            _betting.PlaceBet(LocalPlayer, _currentBet);
            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, LocalPlayer.Chips);
            EndPlayerTurn();
        }
    
        /// <summary>
        /// AI decision logic
        /// </summary>
        private void AITurn()
        {
            if ( Opponent.Folded || _currentTurn != TurnOwner.AI) return;
            
            PlayerAction action = _aiService.DecideAction(Opponent, _communityCards, _currentBet, _raiseAmount);
            switch (action)
            {
                case PlayerAction.Raise:
                    _currentBet += _raiseAmount;
                    OnStateMessage?.Invoke("AI Raises", 1f);
                    _betting.PlaceBet(Opponent, _currentBet);
                    break;

                case PlayerAction.Call:
                    _betting.PlaceBet(Opponent, _currentBet);
                    OnStateMessage?.Invoke("AI Calls", 1f);
                    break;

                case PlayerAction.Fold:
                    Opponent.Folded = true;
                    OnStateMessage?.Invoke("AI Folds", 1.5f);
                    OnWinnerMessage?.Invoke("Player Wins!", 2.5f);
                    OnTurnChanged?.Invoke(false);
                    AwardPotTo(LocalPlayer);
                    return;
            }

            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
            Invoke(nameof(NextState), 1.5f);
        }

        /// <summary>
        /// Final hand comparison
        /// </summary>
        private void DoShowdown()
        {
            _state.SetState(GameState.Showdown);
            RevealAICards();

            List<Card> playerCards = new(LocalPlayer.Hand);
            playerCards.AddRange(_communityCards);
            
            List<Card> aiCards = new(Opponent.Hand);
            aiCards.AddRange(_communityCards);

            var playerEval = _evaluator.EvaluateDetailed(playerCards);
            var aiEval = _evaluator.EvaluateDetailed(aiCards);

            string winnerText;

            if (playerEval.score > aiEval.score)
            {
                winnerText = $"Player Wins with {playerEval.name}";
                AwardPotTo(LocalPlayer);
                HighlightWinningCards(playerEval.bestCards);
            }
            else if (aiEval.score > playerEval.score)
            {
                winnerText = $"AI Wins with {aiEval.name}";
                AwardPotTo(Opponent);
                HighlightWinningCards(aiEval.bestCards);
            }
            else
            {
                winnerText = "It's a Tie!";
                AwardPotToBothWhenTie(LocalPlayer, Opponent);
            }
            OnStateMessage?.Invoke($"Player: {playerEval.name}\nAI: {aiEval.name}", 3f);
            OnWinnerMessage?.Invoke(winnerText, 3f);
        }

        /// <summary>
        /// Lifts winning cards slightly for emphasis
        /// </summary>
        private void HighlightWinningCards(List<Card> bestCards)
        {
            foreach (var card in bestCards)
            {
                if (_cardMap.TryGetValue(card, out var go))
                {
                    // Kill any existing animations on this card
                    go.transform.DOKill();

                    Vector3 targetPos = go.transform.position + new Vector3(0f, 0.2f, 0.6f);

                    go.transform.DOMove(targetPos, 0.35f).SetEase(Ease.OutBack);
                }
            }
        }

        /// <summary>
        /// Reveals AI's hidden cards
        /// </summary>
        private void RevealAICards()
        {
            for (int i = 0; i < _aiCardObjects.Count; i++)
            {
                var view = _aiCardObjects[i].GetComponent<CardView>();
                view.SetTexture(cardDB.GetTexture(Opponent.Hand[i]));
            }
        }
        
        /// <summary>
        /// Gives full pot to winner
        /// </summary>
        private void AwardPotTo(PlayerModel winner)
        {
            winner.Chips += _betting.Pot;
            _betting.ResetPot();
            _currentBet = 10;

            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, LocalPlayer.Chips);
            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
        }

        /// <summary>
        /// Splits pot in case of tie
        /// </summary>
        private void AwardPotToBothWhenTie(PlayerModel player1, PlayerModel player2)
        {
            int newPot = _betting.Pot / 2;
            player1.Chips += newPot;
            player2.Chips += newPot;

            _betting.ResetPot();
            _currentBet = 10;
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, LocalPlayer.Chips);
            
            OnUIUpdate?.Invoke(LocalPlayer, Opponent, _betting.Pot);
        }

        /// <summary>
        /// Clears everything and restarts
        /// </summary>
        public void RestartGame()
        {
            foreach (Transform t in playerCardArea) Destroy(t.gameObject);
            foreach (Transform t in aiCardArea) Destroy(t.gameObject);
            foreach (Transform t in communityArea) Destroy(t.gameObject);
        
            _playerCardObjects.Clear();
            _aiCardObjects.Clear();
            _communityCards.Clear();
            _cardMap.Clear();

            _betting.ResetPot();
            _currentBet = 10;

            StartGame();
        }
    }
}