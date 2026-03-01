using System;
using models;
using UnityEngine;

namespace core
{
    public class GameStateController
    {
        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnStateChanged;

        /// <summary>
        /// Handles the state changes for
        /// (Pre-Flop, Flop, Turn, River, Showdown).
        /// </summary>
        /// <param name="state"></param>
        public void SetState(GameState state)
        {
            CurrentState = state;
            Debug.Log("State → " + state);
            OnStateChanged?.Invoke(state);
        }
    }
}