using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchResolver : MonoBehaviour
{
    public struct Pair { public CardView a, b; }

    [SerializeField] private float mismatchHoldTime = 0.55f;

    private readonly Queue<Pair> _queue = new Queue<Pair>();
    private bool _running;

    public System.Action<bool> OnPairResolved; // true=match, false=mismatch

    public void Enqueue(CardView a, CardView b)
    {
        _queue.Enqueue(new Pair { a = a, b = b });
        if (!_running) StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        _running = true;

        while (_queue.Count > 0)
        {
            var p = _queue.Dequeue();

            if (p.a == null || p.b == null) continue;
            if (p.a.State != CardView.CardState.FaceUp || p.b.State != CardView.CardState.FaceUp) continue;

            bool match = p.a.FaceId == p.b.FaceId;

            if (match)
            {
                p.a.SetMatched();
                p.b.SetMatched();
                OnPairResolved?.Invoke(true);
            }
            else
            {
                OnPairResolved?.Invoke(false);
                yield return new WaitForSecondsRealtime(mismatchHoldTime);

                if (p.a.State == CardView.CardState.FaceUp) p.a.Hide();
                if (p.b.State == CardView.CardState.FaceUp) p.b.Hide();
            }

            yield return null;
        }

        _running = false;
    }
}
