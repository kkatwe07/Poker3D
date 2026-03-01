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

        void Start()
        {
            // Create player and AI
            _players.Add(new PlayerModel { Name = "Player" });
            _players.Add(new PlayerModel { Name = "AI", IsAI = true, Chips = 1000 });

            turnManager.Init(this, uiManager);
            _state.OnStateChanged += HandleStateChange;

            StartGame();
        }

        /// <summary>
        /// Starts a fresh round
        /// </summary>
        private void StartGame()
        {
            OnWinnerMessage?.Invoke("", 0.1f);// clears winner text
            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, _players[0].Chips);

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
            SpawnHand(_players[0], playerCardArea);
            SpawnHand(_players[1], aiCardArea);
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

            _players[0].Folded = true;

            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
            OnStateMessage?.Invoke("Player Folds", 1.5f);
            OnWinnerMessage?.Invoke("AI Wins!", 3f);
            OnTurnChanged?.Invoke(false);

            // Flip player cards face down
            foreach (var c in _playerCardObjects)
            {
                CardView view = c.GetComponent<CardView>();
                view.SetTexture(cardDB.cardBack);
            }

            AwardPotTo(_players[1]);
            turnManager.StopTimer();
        }

        /// <summary>
        /// Player calls current bet
        /// </summary>
        public void PlayerCall()
        {
            if (_currentTurn != TurnOwner.Player) return;

            _betting.PlaceBet(_players[0], _currentBet);
            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, _players[0].Chips);
            EndPlayerTurn();
        }

        /// <summary>
        /// Player raises bet
        /// </summary>
        public void PlayerRaise()
        {
            if (_currentTurn != TurnOwner.Player) return;

            _currentBet += _raiseAmount;
            _betting.PlaceBet(_players[0], _currentBet);
            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, _players[0].Chips);
            EndPlayerTurn();
        }
    
        /// <summary>
        /// AI decision logic
        /// </summary>
        private void AITurn()
        {
            if ( _players[1].Folded || _currentTurn != TurnOwner.AI) return;
            
            List<Card> aiCards = new(_players[1].Hand);
            aiCards.AddRange(_communityCards);
            
            var eval = _evaluator.EvaluateDetailed(aiCards);
            long strength = eval.score;
            
            // Hand rank tier
            int handTier = (int)(strength / 1000000000);

            if (handTier >= 6) // Full House+
            {
                OnStateMessage?.Invoke("AI Raises", 1f);
                _currentBet += _raiseAmount;
                OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, _players[0].Chips);
                _betting.PlaceBet(_players[1], _currentBet);
            }
            else if (handTier >= 2) // Pair or better
            {
                OnStateMessage?.Invoke("AI Calls", 1f);
                _betting.PlaceBet(_players[1], _currentBet);
            }
            else
            {
                // 40% chance to fold weak hands
                if (Random.value < 0.4f)
                {
                    _players[1].Folded = true;
                    OnStateMessage?.Invoke("AI Folds", 1.5f);
                    OnWinnerMessage?.Invoke("Player Wins!", 3f);
                    OnTurnChanged?.Invoke(false);
                    AwardPotTo(_players[0]);
                    return;
                }
                OnStateMessage?.Invoke("AI Calls", 1f);

                _betting.PlaceBet(_players[1], _currentBet);
            }

            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
            Invoke(nameof(NextState), 1.5f);
        }

        /// <summary>
        /// Final hand comparison
        /// </summary>
        private void DoShowdown()
        {
            _state.SetState(GameState.Showdown);
            RevealAICards();

            List<Card> playerCards = new(_players[0].Hand);
            playerCards.AddRange(_communityCards);
            
            List<Card> aiCards = new(_players[1].Hand);
            aiCards.AddRange(_communityCards);

            var playerEval = _evaluator.EvaluateDetailed(playerCards);
            var aiEval = _evaluator.EvaluateDetailed(aiCards);

            string winnerText;

            if (playerEval.score > aiEval.score)
            {
                winnerText = $"Player Wins with {playerEval.name}";
                AwardPotTo(_players[0]);
                HighlightWinningCards(playerEval.bestCards);
            }
            else if (aiEval.score > playerEval.score)
            {
                winnerText = $"AI Wins with {aiEval.name}";
                AwardPotTo(_players[1]);
                HighlightWinningCards(aiEval.bestCards);
            }
            else
            {
                winnerText = "It's a Tie!";
                AwardPotToBothWhenTie(_players[0], _players[1]);
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
                view.SetTexture(cardDB.GetTexture(_players[1].Hand[i]));
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

            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, _players[0].Chips);
            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
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
            OnBetButtonsUpdate?.Invoke(_currentBet, _raiseAmount, _players[0].Chips);
            
            OnUIUpdate?.Invoke(_players[0], _players[1], _betting.Pot);
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