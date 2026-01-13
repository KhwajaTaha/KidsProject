using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    public enum CardState { FaceDown, FlippingUp, FaceUp, Matched, FlippingDown }

    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private Sprite backSprite;

    public string InstanceId { get; private set; }
    public string FaceId { get; private set; }
    public CardState State { get; private set; } = CardState.FaceDown;

    private Sprite _faceSprite;
    private Coroutine _flipRoutine;

    public event Action<CardView> Clicked;

    public void Init(string instanceId, string faceId, Sprite faceSprite)
    {
        InstanceId = instanceId;
        FaceId = faceId;
        _faceSprite = faceSprite;

        image.sprite = backSprite;
        State = CardState.FaceDown;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public bool CanInteract => State == CardState.FaceDown || State == CardState.FaceUp;

    public void SetMatched()
    {
        State = CardState.Matched;
        button.interactable = false;
    }

    public void Reveal(float duration = 0.16f)
    {
        if (State != CardState.FaceDown) return;
        StartFlip(up: true, duration);
    }

    public void Hide(float duration = 0.16f)
    {
        if (State != CardState.FaceUp) return;
        StartFlip(up: false, duration);
    }

    private void StartFlip(bool up, float duration)
    {
        if (_flipRoutine != null) StopCoroutine(_flipRoutine);
        _flipRoutine = StartCoroutine(FlipRoutine(up, duration));
    }

    private IEnumerator FlipRoutine(bool up, float duration)
    {
        State = up ? CardState.FlippingUp : CardState.FlippingDown;

        button.interactable = false;

        float half = duration * 0.5f;
        var t = 0f;

        // shrink X to 0
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = t / half;
            transform.localScale = new Vector3(Mathf.Lerp(1f, 0f, k), 1f, 1f);
            yield return null;
        }

        image.sprite = up ? _faceSprite : backSprite;

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = t / half;
            transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, k), 1f, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        State = up ? CardState.FaceUp : CardState.FaceDown;
        _flipRoutine = null;
        button.interactable = (State != CardState.Matched);

    }
}
