using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchResolver : MonoBehaviour
{
    [System.Serializable]
    public struct Pair
    {
        public CardView a;
        public CardView b;
    }

    [Header("Timing")]
    [SerializeField] private float mismatchHoldTime = 0.55f;

    private readonly Queue<Pair> _queue = new Queue<Pair>();
    private Coroutine _runner;

    // GameController subscribes to this
    public System.Action<bool> OnPairResolved;

    public void Enqueue(CardView a, CardView b)
    {
        if (a == null || b == null) return;
        _queue.Enqueue(new Pair { a = a, b = b });

        if (_runner == null)
            _runner = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            var pair = _queue.Dequeue();

            // If cards got destroyed or changed state unexpectedly, skip safely
            if (pair.a == null || pair.b == null)
            {
                yield return null;
                continue;
            }

            // Wait until both finishes flipping up (important!)
            // This prevents missing the "FaceUp" state if comparison happens too early.
            yield return WaitUntilFaceUpOrInvalid(pair.a, pair.b);

            // If they are no longer valid, skip
            if (pair.a == null || pair.b == null) { yield return null; continue; }
            if (pair.a.State != CardView.CardState.FaceUp || pair.b.State != CardView.CardState.FaceUp)
            {
                yield return null;
                continue;
            }

            bool isMatch = pair.a.FaceId == pair.b.FaceId;

            if (isMatch)
            {
                pair.a.SetMatched();
                pair.b.SetMatched();
            }
            else
            {
                // Let player see mismatch briefly
                float t = 0f;
                while (t < mismatchHoldTime)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Flip back if still face up (player might have interacted in the meantime)
                if (pair.a.State == CardView.CardState.FaceUp) pair.a.Hide();
                if (pair.b.State == CardView.CardState.FaceUp) pair.b.Hide();
            }

            // IMPORTANT: always invoke, so GameController updates score/hud
            OnPairResolved?.Invoke(isMatch);

            yield return null;
        }

        _runner = null;
    }

    private IEnumerator WaitUntilFaceUpOrInvalid(CardView a, CardView b)
    {
        // Safety timeout so we never get stuck if something goes wrong
        float timeout = 2f;
        float t = 0f;

        while (t < timeout)
        {
            if (a == null || b == null) yield break;

            bool aOk = a.State == CardView.CardState.FaceUp || a.State == CardView.CardState.Matched;
            bool bOk = b.State == CardView.CardState.FaceUp || b.State == CardView.CardState.Matched;

            // For queued evaluation we specifically want FaceUp, but allow Matched to pass too.
            if (aOk && bOk) yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // Optional: clear queue if restarting mid-process
    public void Clear()
    {
        _queue.Clear();
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }
}
