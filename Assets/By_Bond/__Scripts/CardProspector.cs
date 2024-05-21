using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eCardState
{   //состояния карт
    drawpile, // доп. стопка
    tableau, // 
    target,
    discard
}

public class CardProspector : Card
{
    [Header("Set Dynamically: CardProspector")]
    public eCardState state = eCardState.drawpile;
    public List<CardProspector> hiddenBy = new List<CardProspector>(); // списко карт, не позволяющих перевернуть карту
    public int layoutID; // номер карты в раскладке
    public SlotDef slotDef; // информация из <slot>

    override public void OnMouseUpAsButton()
    {
        Prospector.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }
}
