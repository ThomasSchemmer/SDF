using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    private List<Card> cards;
    private float hoverScale = 1.3f;
    private int hoverIndex;
    private Vector3 cardScale = Vector3.one * 0.4f;
    private Vector3 cardOffset = Vector3.zero;
    private Vector3 dragStartPos = Vector3.zero;
    private float startAngle = 0;
    private int startIndex = 0;
    private Card draggedCard;
    private Card hoveredCard;
    private Vector2 posIncr = new Vector2(100, 0.0f);
    private Vector2 position = new Vector2(Screen.width / 2, 0.05f);


    public void Start() {
        cards = new List<Card>();
        foreach(Transform t in this.transform) {
            Card c = t.GetComponent<Card>();
            if(c)
                cards.Add(c);
        }
        position = position - new Vector2(cards.Count / 2f * posIncr.x, 0);

        Align();
    }

    private void Update() {
        Raycast();
        HandleDrag();
    }

    private void Raycast() {
        if (draggedCard)
            return;
        bool hovered = false;
        for(int i = cards.Count - 1; i >= 0; i--) {
            Card card = cards[i];
            if (card.ContainsPointUI(Input.mousePosition) && !hovered) {
                if (draggedCard && card != hoveredCard)
                    ResetCard(hoveredCard, hoverIndex);
                SetCard(card, i);
                hovered = true;
            } else {
                ResetCard(card, i);
            }
        }
    }

    private void SetCard(Card card, int i) {
        if (!card )
            return;
        card.transform.localScale = cardScale * hoverScale;
        hoverIndex = i;
        hoveredCard = card;
    }

    private void ResetCard(Card card, int i) {
        if (!card)
            return;
        card.transform.localScale = cardScale;
        if(hoveredCard == card)
            hoveredCard = null;
    }

    private void HandleDrag() {
        if (Input.GetMouseButtonUp(0) && draggedCard) {
            SetSiblingIndex(draggedCard, startIndex);
            draggedCard.ResetT();
            draggedCard = null;
            Align();
            return;
        }

        if (!Input.GetMouseButton(0))
            return;

        if (Input.GetMouseButtonDown(0) && hoveredCard) {
            draggedCard = hoveredCard;
            dragStartPos = Input.mousePosition;
            cardOffset = dragStartPos - Card.WorldToScreenPoint(draggedCard.transform.position);
            cardOffset.z = 0;
            startAngle = draggedCard.GetRotationUI();
            Debug.Log("Start Angle: " + startAngle);
            startIndex = draggedCard.GetSiblingIndex();
        }
        if (!draggedCard)
            return;
        draggedCard.SetPositionUI(Input.mousePosition - cardOffset, cards.Count - startIndex);
        Vector3 dragDistance = Input.mousePosition - dragStartPos;
        float angle = Mathf.LerpAngle(startAngle, 0, Mathf.Clamp(dragDistance.y / 50, 0, 1));
        draggedCard.SetRotationUI(angle);
        SetAsLastSibling(draggedCard);

        if(dragDistance.y > 50) {
            float t = (dragDistance.y - 50) / 200;
            draggedCard.DragUI(Mathf.Clamp(t, 0, 1.05f), cardOffset);   
        }
    }


    private void Align() {
        int count = this.transform.childCount;
        int i = 0;
        float angle = -30f;
        float increments = 60f / count;
        foreach(Card c in cards) {
            c.AlignUI(
                position + posIncr * i,
                angle + increments * i, cards.Count - i);
            i++;
        }
    }

    private void SetSiblingIndex(Card card, int i) {
        card.SetPositionUI(position + i * posIncr, cards.Count - i);
        card.SetSiblingIndex(i);
    }

    private void SetAsLastSibling(Card card) {
        for(int j = 0; j < cards.Count; j++) {
            if(cards[j] != card) {
                cards[j].SetPositionUI(position + j * posIncr, cards.Count - j);
            } else {
                cards[j].SetPositionUI(position + j * posIncr, -1);
            }
            cards[j].SetSiblingIndex(j);
        }
    }

}
