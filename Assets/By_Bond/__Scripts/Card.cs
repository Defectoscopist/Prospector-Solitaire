using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Set Dynamically")]
    public string suit; // масть (C, D, H или S)
    public int rank; // Достоинство карты
    public Color color = Color.black; // Цвет значков
    public string colS = "Black"; // или "Red" - цвет карты,
    public List<GameObject> decoGOs = new List<GameObject>(); // для хранения всех Decorator
    public List<GameObject> pipGOs = new List<GameObject>(); // для хранения всех Pip

    public GameObject back; // рубашка
    public CardDefinition def; // Извлекается из DeckXML.xml
    public SpriteRenderer[] spriteRenderers;

    void Start()
    {
        SetSortOrder(0);
    }

    // Если spriteRenderers не определен, эта функция определит его
    public void PopulateSpriteRenderers()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    // Иничциализирует поле  sortingLayerName во всех компонентах SpriteRenderer
    public void SetSortingLayerName(string tSLN)
    {
        PopulateSpriteRenderers();
        foreach (SpriteRenderer tSR in spriteRenderers)
        {
            tSR.sortingLayerName = tSLN;
        }
    }

    // Иничциализирует поле  sortingOrder всех компонентов SpriteRenderer
    public void SetSortOrder(int sOrd)
    {
        PopulateSpriteRenderers();
        foreach (SpriteRenderer tSR in spriteRenderers)
        {
            if (tSR.gameObject == this.gameObject) // Если компонент принадлежит текущему игровому объекту
            {
                tSR.sortingOrder = sOrd; // Значит это фон
                continue;
            }
            switch (tSR.gameObject.name)
            {
                case "back": // рубашка
                    tSR.sortingOrder = sOrd + 2;
                    break;
                case "face":
                default: // лицевая сторона или другое
                    tSR.sortingOrder = sOrd + 1;
                    break;
            }
        }

    }

    // переключение режима отображения
    public bool faceUp
    {
        get { return (!back.activeSelf); }
        set { back.SetActive(!value); }
    }


    //Виртуальные методы могут определяться в подклассах определением методов с теми же именами
    virtual public void OnMouseUpAsButton()
    {
        print(name);
    }
}

[System.Serializable]
public class Decorator
{ // Хранит информацию из DeckXML о каждом значке карты
    public string type; // достоинство карты
    public Vector3 loc; // положение спрайта на карте
    public bool flip = false; // переворот карты
    public float scale = 1f; // Масштаб спрайта
}

[System.Serializable]
public class CardDefinition
{ // Информация о достоинстве карты
    public string face; // Спрайт лицевой стороны карты
    public int rank; // достоинство карты
    public List<Decorator> pips = new List<Decorator>(); // значки
}