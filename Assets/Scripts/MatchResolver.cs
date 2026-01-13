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

            if (pair.a == null || pair.b == null)
            {
                yield return null;
                continue;
            }

            yield return WaitUntilFaceUpOrInvalid(pair.a, pair.b);

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
                float t = 0f;
                while (t < mismatchHoldTime)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (pair.a.State == CardView.CardState.FaceUp) pair.a.Hide();
                if (pair.b.State == CardView.CardState.FaceUp) pair.b.Hide();
            }
            OnPairResolved?.Invoke(isMatch);

            yield return null;
        }

        _runner = null;
    }

    private IEnumerator WaitUntilFaceUpOrInvalid(CardView a, CardView b)
    {
        float timeout = 2f;
        float t = 0f;

        while (t < timeout)
        {
            if (a == null || b == null) yield break;

            bool aOk = a.State == CardView.CardState.FaceUp || a.State == CardView.CardState.Matched;
            bool bOk = b.State == CardView.CardState.FaceUp || b.State == CardView.CardState.Matched;

            if (aOk && bOk) yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

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
