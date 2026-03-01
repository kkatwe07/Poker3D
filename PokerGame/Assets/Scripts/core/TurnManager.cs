using System.Collections;
using UnityEngine;
using view;

namespace core
{
    public class TurnManager : MonoBehaviour
    {
        public int turnTime = 15;

        private Coroutine _timerRoutine;
        private GameController _game;
        private UiManager _ui;

        public void Init(GameController g, UiManager u)
        {
            _game = g;
            _ui = u;
        }

        public void StartTimer()
        {
            StopTimer();

            _timerRoutine = StartCoroutine(Timer());
        }
    
        public void StopTimer()
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
            }
        }

        private IEnumerator Timer()
        {
            int t = turnTime;

            while (t > 0)
            {
                _ui.UpdateTimer(t);
                yield return new WaitForSeconds(1);
                t--;
            }

            _ui.UpdateTimer(0);
            Debug.Log("Timer ended → Auto Fold");
            _game.PlayerFold();
        }
    }

// turn owner enum to determine whose turn it is
    public enum TurnOwner
    {
        Player,
        AI
    }
}