using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Prospector : MonoBehaviour
{
    static public Prospector S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    /*public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);*/

    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    //public FloatingScore fsRun;



    void Awake()
    {
        S = this; // Объект-одиночка
    }

    void Start()
    {
       
        //Scoreboard.S.score = ScoreManager.SCORE;
        
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text); // передать в DeckXML
        Deck.Shuffle(ref deck.cards); // перемешать колоду и передать ее по ссылке

        /*Card c;
        for (int cNum = 0; cNum < deck.cards.Count; cNum++) // Вывести все карты в перемешанном порядке
        {
            c = deck.cards[cNum];
            c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
        }*/
        layout = GetComponent<Layout>();
        layout.ReadLayout(layoutXML.text);
        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();
    }
    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    // Снимает одну карту с вершины drawPie и возвращает ее
    CardProspector Draw()
    {
        CardProspector cd = drawPile[0]; // Снять нулевую карту с CardPros[ector
        drawPile.RemoveAt(0); // удалить из списка
        return (cd); // Вернуть ее
    }

    // Размещение по начальной раскладке
    void LayoutGame()
    {
        // Пустой игровой объект - центр раскладки
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardProspector cp;
        // Разложить карты
        foreach (SlotDef tSD in layout.slotDefs)
        {
            cp = Draw(); // Выбрать первую карту сверху из drawPile
            cp.faceUp = tSD.faceUp;
            cp.transform.parent = layoutAnchor; // назначить layoutAnchor ее родителем
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            cp.state = eCardState.tableau;
            cp.SetSortingLayerName(tSD.layerName); // назначить слой сортировки
            tableau.Add(cp); // Добавить карту в список tableau
        }

        // Настроить списки карт, мешающих перевернуть данную
        foreach (CardProspector tCP in tableau)
        {
            foreach (int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }

        MoveToTarget(Draw()); // Выбрать начальную целевую карту
        UpdateDrawPile(); // Разложить стопку свободных карт
    }

    // Преобразует номер слота layoutID в экземпляр CardProspector с этим номером
    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach (CardProspector tCP in tableau)
        {
            if (tCP.layoutID == layoutID) return tCP; // Если номер слота карты совпадает с искомым, вернуть ее
        }
        return (null);
    }

    // Поворачивает карты в основной раскладке лицевой стороной вверх или вниз
    void SetTableauFaces()
    {
        foreach (CardProspector cd in tableau)
        {
            bool faceUp = true; // Предположим, что карта должна быть лицом вверх
            foreach (CardProspector cover in cd.hiddenBy)
            {
                //Если любая из карт, перекрывающая текущую, присутствует в основной раскладке
                if (cover.state == eCardState.tableau)
                {
                    faceUp = false; // перевернуть лицом вниз
                }
            }
            cd.faceUp = faceUp; // повернуть в любом случае
        }
    }
    
    // Перемещает текущую целевую карту в стопку сброшенных карт
    void MoveToDiscard(CardProspector cd)
    {
        // Установить состояние карты как discard (сброшена)
        cd.state = eCardState.discard;
        discardPile.Add(cd);
        cd.transform.parent = layoutAnchor;

        // Перместить карту на позицию стопки сброшенных карт
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        // Поместить поверх стопки для сортировки по глубине
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    // Делает карту cd новой целевой картой
    void MoveToTarget(CardProspector cd)
    {
        // Если карта существует, поместить ее в стопку сброшенных карт
        if (target != null) MoveToDiscard(target);
        target = cd;
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        // Переместить на место для целевой карты
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID);
        cd.faceUp = true;
        // Настроить сортировку по глубине
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    // Раскладывает стопку свободных карт, чтобы было видно, сколько карт осталось
    void UpdateDrawPile()
    {
        CardProspector cd;
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            // Расположить с учетом смещения  layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x), layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y), -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = eCardState.drawpile;
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    // CardClicked вызывается в ответ на любой щелчок по карте
    public void CardClicked(CardProspector cd)
    {
        switch (cd.state)
        {
            case eCardState.target:
                break;
            case eCardState.drawpile: // Взять карту из доп. стопки
                MoveToDiscard(target); // переместить целевую карту в  discardPile
                MoveToTarget(Draw()); // Переместить верхнюю свободную карту на место целевой
                UpdateDrawPile(); // Повторно разложить стопку
                //FloatingScoreHandler(eScoreEvent.draw);
                break;
            case eCardState.tableau: // Проверим возможности поместить на место целевой
                bool validMatch = true;
                if (!cd.faceUp)
                {
                    validMatch = false; // карта, перевернутая вниз, не может перемещаться
                }
                if (!AdjacentRank(cd, target)) // Если правило старшинства не соблюдается
                {
                    validMatch = false;
                }
                if (!validMatch) return;
                // Если все ок, перемещаем карту
                tableau.Remove(cd);
                MoveToTarget(cd); // сделать целевой
                SetTableauFaces(); // Повернуть лицом вверх или вниз
                //ScoreManager.EVENT(eScoreEvent.mine);
                //FloatingScoreHandler(eScoreEvent.mine);
                break;
        }
        CheckForGameOver(); // Проверить завершение игры
    }

    // Проверяет завершение игры
    void CheckForGameOver()
    {
        if (tableau.Count == 0) { GameOver(true); return; } // Если закончились карты в основной колоде, выигрыш
        if (drawPile.Count > 0) return; // Если есть запасные карты, игра продолжается
        // Проверить, есть ли ходы
        foreach (CardProspector cd in tableau)
        {
            if (AdjacentRank(cd, target)) return;
        }
        GameOver(false); // проигрыш
    }

    // Конец игры
    void GameOver(bool won)
    {
        if (won)
        {
            //print("Game Over. You won! :)");
            //ScoreManager.EVENT(eScoreEvent.gameWin);
            //FloatingScoreHandler(eScoreEvent.gameWin);
        }
        else
        {
            //print("Game Over. You Lost. :(");
            //ScoreManager.EVENT(eScoreEvent.gameLoss);
            //FloatingScoreHandler(eScoreEvent.gameLoss);
        }
        SceneManager.LoadScene("Prospector_Scene"); // заново
        
    }


    // Возвращает true, если две карты соответсвуют правила старшинства
    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        // проверяем, есть ли перевернутая лицом вниз карта
        if (!c0.faceUp || !c1.faceUp) return (false);
        if (Mathf.Abs(c0.rank - c1.rank) == 1) // проверяем, отличаются ли на 1 друг от друга
        {
            return (true);
        }
        // случай короля и туза
        if (c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);
        return (false);
    }

    // Обрабатывает движение FloatingScore
    /*void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch (evt)
        {   // В случае завершения хода, победы или проигрыша
            case eScoreEvent.draw: //
            case eScoreEvent.gameWin:
            case eScoreEvent.gameLoss:
                // Добавить fsRun в Scoreboard
                if (fsRun != null)
                {
                    // Создать точки для кривой Безье
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 }); // Скопировать fontSize
                    fsRun = null;
                }
                break;
            case eScoreEvent.mine: // Удаление карты из основной раскладки
                // Создать FloatingScore для отображения кол-ва очков
                FloatingScore fs;
                // Переместить из позиции указателя мыши mousePosition в fsposRun
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if (fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }*/
}