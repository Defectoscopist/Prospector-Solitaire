using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//------------------------------------------------------ Для чтения XML файлов ----------------------------------------\\
[System.Serializable]
public class PT_XMLReader
{
    static public bool SHOW_COMMENTS = false;

    //public string input;
    //public TextAsset inputTA;
    public string xmlText;
    public PT_XMLHashtable xml;

    /*
	void Awake() {
		inputTA = Resources.Load("WellFormedSample") as TextAsset;	
		input = inputTA.text;
		print(input);
		output = new XMLHashtable();
		Parse(input, output);
		// TODO: Make something which will trace a Hashtable or output it as XML
		print(output["videocollection"][0]["video"][1]["title"][0].text);
	}
	*/

    // This function creates a new XMLHashtable and calls the real Parse()
    public void Parse(string eS)
    {
        xmlText = eS;
        xml = new PT_XMLHashtable();
        Parse(eS, xml);
    }

    // This function will parse a possible series of tags
    void Parse(string eS, PT_XMLHashtable eH)
    {
        eS = eS.Trim();
        while (eS.Length > 0)
        {
            eS = ParseTag(eS, eH);
            eS = eS.Trim();
        }
    }

    // This function parses a single tag and calls Parse() if it encounters subtags
    string ParseTag(string eS, PT_XMLHashtable eH)
    {
        // search for "<"
        int ndx = eS.IndexOf("<");
        int end, end1, end2, end3;
        if (ndx == -1)
        {
            // It's possible that this is just a string (e.g. <someTagTheStringIsInside>string</someTagTheStringIsInside>)
            end3 = eS.IndexOf(">"); // This closes a standard tag; look for the closing tag
            if (end3 == -1)
            {
                // In that case, we just need to add an @ key/value to the hashtable
                eS = eS.Trim(); // I think this is redundant
                                //eH["@"] = eS;
                eH.text = eS;
            }
            return (""); // We're done with this tag
        }
        // Ignore this if it is just an XML header (e.g. <?xml version="1.0"?>)
        if (eS[ndx + 1] == '?')
        {
            // search for the closing tag of this header
            int ndx2 = eS.IndexOf("?>");
            string header = eS.Substring(ndx, ndx2 - ndx + 2);
            //eH["@XML_Header"] = header;
            eH.header = header;
            return (eS.Substring(ndx2 + 2));
        }
        // Ignore this if it is an XML comment (e.g. <!-- Comment text -->)
        if (eS[ndx + 1] == '!')
        {
            // search for the closing tag of this header
            int ndx2 = eS.IndexOf("-->");
            string comment = eS.Substring(ndx, ndx2 - ndx + 3);
            if (SHOW_COMMENTS) Debug.Log("XMl Comment: " + comment);
            //eH["@XML_Header"] = header;
            return (eS.Substring(ndx2 + 3));
        }

        // Find the end of the tag name
        // For the next few comments, this is what happens when this character is the first one found after the beginning of the tag
        end1 = eS.IndexOf(" ", ndx);    // This means that we'll have attributes
        end2 = eS.IndexOf("/", ndx);    // Immediately closes the tag, 
        end3 = eS.IndexOf(">", ndx);    // This closes a standard tag; look for the closing tag
        if (end1 == -1) end1 = int.MaxValue;
        if (end2 == -1) end2 = int.MaxValue;
        if (end3 == -1) end3 = int.MaxValue;


        end = Mathf.Min(end1, end2, end3);
        string tag = eS.Substring(ndx + 1, end - ndx - 1);

        // search for this tag in eH. If it's not there, make it
        if (!eH.ContainsKey(tag))
        {
            eH[tag] = new PT_XMLHashList();
        }
        // Create a hashtable to contain this tag's information
        PT_XMLHashList arrL = eH[tag] as PT_XMLHashList;
        //int thisHashIndex = arrL.Count;
        PT_XMLHashtable thisHash = new PT_XMLHashtable();
        arrL.Add(thisHash);

        // Pull the attributes string
        string atts = "";
        if (end1 < end3)
        {
            try
            {
                atts = eS.Substring(end1, end3 - end1);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Debug.Log("break");
            }
        }
        // Parse the attributes, which are all guaranteed to be strings
        string att, val;
        int eqNdx, spNdx;
        while (atts.Length > 0)
        {
            atts = atts.Trim();
            eqNdx = atts.IndexOf("=");
            if (eqNdx == -1) break;
            //att = "@"+atts.Substring(0,eqNdx);
            att = atts.Substring(0, eqNdx);
            spNdx = atts.IndexOf(" ", eqNdx);
            if (spNdx == -1)
            { // This is the last attribute and doesn't have a space after it
                val = atts.Substring(eqNdx + 1);
                if (val[val.Length - 1] == '/')
                { // If the trailing / from /> was caught, remove it
                    val = val.Substring(0, val.Length - 1);
                }
                atts = "";
            }
            else
            { // This attribute has a space after it
                val = atts.Substring(eqNdx + 1, spNdx - eqNdx - 2);
                atts = atts.Substring(spNdx);
            }
            val = val.Trim('\"');
            //thisHash[att] = val; // All attributes have to be unique, so this should be okay.
            thisHash.attSet(att, val);
        }


        // Pull the subs, which is everything contained by this tag but exclusing the tags on either side (e.g. <tag att="hi">.....subs.....</tag>)
        string subs = "";
        string leftoverString = "";
        // singleLine means this doesn't have a separate closing tag (e.g. <tag att="hi" />)
        bool singleLine = (end2 == end3 - 1);// ? true : false;
        if (!singleLine)
        { // This is a multiline tag (e.g. <tag> ....  </tag>)
          // find the closing tag
            int close = eS.IndexOf("</" + tag + ">");
            // TODO: Should this do something more if there is no closing tag?
            if (close == -1)
            {
                Debug.Log("XMLReader ERROR: XML not well formed. Closing tag </" + tag + "> missing.");
                return ("");
            }
            subs = eS.Substring(end3 + 1, close - end3 - 1);
            leftoverString = eS.Substring(eS.IndexOf(">", close) + 1);
        }
        else
        {
            leftoverString = eS.Substring(end3 + 1);
        }

        subs = subs.Trim();
        // Call Parse if this contains subs
        if (subs.Length > 0)
        {
            Parse(subs, thisHash);
        }

        // Trim and return the leftover string
        leftoverString = leftoverString.Trim();
        return (leftoverString);

    }

}



public class PT_XMLHashList
{
    public ArrayList list = new ArrayList();

    public PT_XMLHashtable this[int s]
    {
        get
        {
            return (list[s] as PT_XMLHashtable);
        }
        set
        {
            list[s] = value;
        }
    }

    public void Add(PT_XMLHashtable eH)
    {
        list.Add(eH);
    }

    public int Count
    {
        get
        {
            return (list.Count);
        }
    }

    public int length
    {
        get
        {
            return (list.Count);
        }
    }
}


public class PT_XMLHashtable
{

    public List<string> keys = new List<string>();
    public List<PT_XMLHashList> nodesList = new List<PT_XMLHashList>();
    public List<string> attKeys = new List<string>();
    public List<string> attributesList = new List<string>();

    public PT_XMLHashList Get(string key)
    {
        int ndx = Index(key);
        if (ndx == -1) return (null);
        return (nodesList[ndx]);
    }

    public void Set(string key, PT_XMLHashList val)
    {
        int ndx = Index(key);
        if (ndx != -1)
        {
            nodesList[ndx] = val;
        }
        else
        {
            keys.Add(key);
            nodesList.Add(val);
        }
    }

    public int Index(string key)
    {
        return (keys.IndexOf(key));
    }

    public int AttIndex(string attKey)
    {
        return (attKeys.IndexOf(attKey));
    }


    public PT_XMLHashList this[string s]
    {
        get
        {
            return (Get(s));
        }
        set
        {
            Set(s, value);
        }
    }

    public string att(string attKey)
    {
        int ndx = AttIndex(attKey);
        if (ndx == -1) return ("");
        return (attributesList[ndx]);
    }

    public void attSet(string attKey, string val)
    {
        int ndx = AttIndex(attKey);
        if (ndx == -1)
        {
            attKeys.Add(attKey);
            attributesList.Add(val);
        }
        else
        {
            attributesList[ndx] = val;
        }
    }

    public string text
    {
        get
        {
            int ndx = AttIndex("@");
            if (ndx == -1) return ("");
            return (attributesList[ndx]);
        }
        set
        {
            int ndx = AttIndex("@");
            if (ndx == -1)
            {
                attKeys.Add("@");
                attributesList.Add(value);
            }
            else
            {
                attributesList[ndx] = value;
            }
        }
    }


    public string header
    {
        get
        {
            int ndx = AttIndex("@XML_Header");
            if (ndx == -1) return ("");
            return (attributesList[ndx]);
        }
        set
        {
            int ndx = AttIndex("@XML_Header");
            if (ndx == -1)
            {
                attKeys.Add("@XML_Header");
                attributesList.Add(value);
            }
            else
            {
                attributesList[ndx] = value;
            }
        }
    }


    public string nodes
    {
        get
        {
            string s = "";
            foreach (string key in keys)
            {
                s += key + "   ";
            }
            return (s);
        }
    }

    public string attributes
    {
        get
        {
            string s = "";
            foreach (string attKey in attKeys)
            {
                s += attKey + "   ";
            }
            return (s);
        }
    }

    public bool ContainsKey(string key)
    {
        return (Index(key) != -1);
    }

    public bool ContainsAtt(string attKey)
    {
        return (AttIndex(attKey) != -1);
    }

    public bool HasKey(string key)
    {
        return (Index(key) != -1);
    }

    public bool HasAtt(string attKey)
    {
        return (AttIndex(attKey) != -1);
    }

}
//----------------------------------------------------------------------------------------------\\
public class Deck : MonoBehaviour
{
    [Header("Set in Inspector")]
    public bool startFaceUp = false;
    // Масти
    public Sprite suitClub; // Трефы
    public Sprite suitDiamond; // Бубны
    public Sprite suitHeart; // Черви
    public Sprite suitSpade; // Пики

    public Sprite[] faceSprites;
    public Sprite[] rankSprites;

    public Sprite cardBack;
    public Sprite cardBackGold;
    public Sprite cardFront;
    public Sprite cardFrontGold;
    // Шаблоны
    public GameObject prefabCard;
    public GameObject prefabSprite;

    [Header("Set Dynamically")]
    public PT_XMLReader xmlr;
    public List<string> cardNames;
    public List<Card> cards;
    public List<Decorator> decorators;
    public List<CardDefinition> cardDefs;
    public Transform deckAnchor;
    public Dictionary<string, Sprite> dictSuits;

    // InitDeck вызывается экземпляром Prospector, когда будет готов
    public void InitDeck(string deckXMLText)
    {
        // Создать точку привязки для всех игровых объектов Card в иерархии
       if (GameObject.Find("_Deck") == null)
        {
            GameObject anchorGO = new GameObject("_Deck");
            deckAnchor = anchorGO.transform;
        }

        // Инициализировать словарь со спрайтами значков мастей
        dictSuits = new Dictionary<string, Sprite>()
    {
      { "C", suitClub     },
      { "D", suitDiamond  },
      { "H", suitHeart    },
      { "S", suitSpade    }
    };
        ReadDeck(deckXMLText);
       MakeCards();
    }

    /*метод ReadDeck() читает содержимое XML-файла и преобразует
    в списки экземпляров Decorator (масть и достоинство в углах карты)
    и экземпляров CardDefinition (с информацией о достоинстве каждой карты (от туза до короля)*/

    // ReadDeck читает указанный XML-файл и создает массив экземпляров CardDefinition
    public void ReadDeck(string deckXMLText)
    {
        xmlr = new PT_XMLReader(); // создать новый экземпляр PT_XMLReader
        xmlr.Parse(deckXMLText); // использвать его для чтения DeckXML

        // Вывод проверочной строки
        string s = "xml[0] decorator[0]";
        s += "type=" + xmlr.xml["xml"][0]["decorator"][0].att("type");
        s += "x=" + xmlr.xml["xml"][0]["decorator"][0].att("x");
        s += "y=" + xmlr.xml["xml"][0]["decorator"][0].att("y");
        s += "scale=" + xmlr.xml["xml"][0]["decorator"][0].att("scale");
        //print(s);
        
        // Прочитать элементы <decorator> для всех карт
        decorators = new List<Decorator>();
        // Извлечь список PT_XMLHashListвсех элементов ,decorator> из XML-файла
        PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];
        Decorator deco;
        for (int i = 0; i < xDecos.Count; i++)
        {
            // Для каждого элемента <decorator> в XML
            deco = new Decorator(); // Создать экземпляр Decorator
                                    // Скопировать атрибуты из <decorator> в Decorator
            deco.type = xDecos[i].att("type");
            // deco.flip получит значение true, если атрибут flip содержит текст "1" 
            deco.flip = (xDecos[i].att("flip") == "1");
            // Получить значение float из строковых атрибутов
            deco.scale = float.Parse(xDecos[i].att("scale"));
            // Vector3 loc инициализируется как [0, 0, 0], изменяем его
            deco.loc.x = float.Parse(xDecos[i].att("x"));
            deco.loc.y = float.Parse(xDecos[i].att("y"));
            deco.loc.z = float.Parse(xDecos[i].att("z"));
            // Добавить deco  в список decorators
            decorators.Add(deco);
        }

        // Прочитать координаты для значков, определяющих достоинство карты
        cardDefs = new List<CardDefinition>(); // Инициализировать список карт
                                               // Извлечь список PT_XMLHashList всех элементов <card> из XML файла
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];
        for (int i = 0; i < xCardDefs.Count; i++)
        {
            // Для каждого элемента <card> Создать экземпляр CardDefinition
            CardDefinition cDef = new CardDefinition();
            // Получить значения атрибута и добавить их в cDef
            cDef.rank = int.Parse(xCardDefs[i].att("rank"));
            // Извлечь список PT_XMLHashList из всех элементов <pip> внутри второго элемента  <card>
            PT_XMLHashList xPips = xCardDefs[i]["pip"];
            if (xPips != null)
            {
                for (int j = 0; j < xPips.Count; j++)
                {
                    deco = new Decorator();
                    // Элементы <pip> в <card> обрабатываются классом Decorator
                    deco.type = "pip";
                    deco.flip = (xPips[j].att("flip") == "1");
                    deco.loc.x = float.Parse(xPips[j].att("x"));
                    deco.loc.y = float.Parse(xPips[j].att("y"));
                    deco.loc.z = float.Parse(xPips[j].att("z"));
                    if (xPips[j].HasAtt("scale"))
                    {
                        deco.scale = float.Parse(xPips[j].att("scale"));
                    }
                    cDef.pips.Add(deco);
                }
            }
            // считать масть
            if (xCardDefs[i].HasAtt("face"))
            {
                cDef.face = xCardDefs[i].att("face");
            }
            cardDefs.Add(cDef);
        }
    }
    
    // Получает  CardDefinition на основе значения достоинства
    public CardDefinition GetCardDefinitionByRank(int rnk)
    {
        // Поиск во всех опредлениях CardDefinition
        foreach (CardDefinition cd in cardDefs)
        {
            // Если достоинство совпадает, вернуть определение
            if (cd.rank == rnk)
            {
                return (cd);
            }
        }
        return (null);
    }

    // Создает игровые объекты карт
    public void MakeCards()
    {
        //  cardNames будет содержать имена сконструированных карт
        // Каждая масть имеет 14 значений достоинства
        cardNames = new List<string>();
        string[] letters = new string[] { "C", "D", "H", "S" };
        foreach (string s in letters)
        {
            for (int i = 0; i < 13; i++)
            {
                cardNames.Add(s + (i + 1));
            }
        }

        // Создать список со всеми картами
        cards = new List<Card>();

        // Обойти все только что созданные имена карт
        for (int i = 0; i < cardNames.Count; i++)
        {
            cards.Add(MakeCard(i)); // Создать карту и добавить ее в колоду
        }
    }

    private Card MakeCard(int cNum)
    {
        // Создать игровой объект с картой
        GameObject cgo = Instantiate(prefabCard) as GameObject;
        // Настроить transform.parent новой карты в соответствии с точкой привязки
        cgo.transform.parent = deckAnchor;
        Card card = cgo.GetComponent<Card>(); // получить компонент Card

        // Выкладываем в аккуратный ряд
        cgo.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);

        // Настроить основные параметры карты
        card.name = cardNames[cNum];
        card.suit = card.name[0].ToString();
        card.rank = int.Parse(card.name.Substring(1));
        if (card.suit == "D" || card.suit == "H") // при необходимости красим в карсный
        {
            card.colS = "Red";
            card.color = Color.red;
        }
        // Получить CardDefinition для этой карты
        card.def = GetCardDefinitionByRank(card.rank);

        AddDecorators(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return card;
    }

    // Эти скрытые переменные используются вспомогательными методами
    private Sprite _tSp = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;
    /*private Sprite tSp;
    private GameObject tGO;
    private SpriteRenderer tSR;*/

    private void AddDecorators(Card card)
    {
        // Добавить оформление
        foreach (Decorator deco in decorators)
        {
            if (deco.type == "suit")
            {
                _tGO = Instantiate(prefabSprite) as GameObject; // Создать эземпляр игрового объекта спрайта
                _tSR = _tGO.GetComponent<SpriteRenderer>(); // Получить ссылку на компонент SpriteRenderer
                _tSR.sprite = dictSuits[card.suit]; // Установить спрайт масти
            }
            else
            {
                _tGO = Instantiate(prefabSprite) as GameObject;
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                _tSp = rankSprites[card.rank]; // Получить спрайт для отображения достоинства
                _tSR.sprite = _tSp; // Установить спрайт достоинства в SpriteRenderer
                _tSR.color = card.color; // Установить цвет соответствующей масти
            }
            _tSR.sortingOrder = 1; // Поместить спрайты над картой
            _tGO.transform.SetParent(card.transform); // Сделать спрайт дочерним по отношению к карте
            _tGO.transform.localPosition = deco.loc; // Location как в DeckXML
            if (deco.flip) // Перевернуть значок
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            if (deco.scale != 1) // Установить масштаб, чтобы уменьшить размер спрайта
            {
                _tGO.transform.localScale = Vector3.one * deco.scale;
            }
            _tGO.name = deco.type; // Дать имя
            card.decoGOs.Add(_tGO); // Добавить этот игровой объект в список card.decoGOs
        }
    }

    private void AddPips(Card card)
    {
        foreach (Decorator pip in card.def.pips) // Для каждого значка в определении
        {
            _tGO = Instantiate(prefabSprite) as GameObject; // Создать игровой объект спрайта
            _tGO.transform.SetParent(card.transform); // Назначить родителем игровой объект карты
            _tGO.transform.localPosition = pip.loc; // Установить localPosition как в XML файле
            if (pip.flip) // Перевернуть если необходимо
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            if (pip.scale != 1) // Масштабировать (только для туза)
            {
                _tGO.transform.localScale = Vector3.one * pip.scale;
            }
            _tGO.name = "pip"; // Дать имя игровому объекту
            _tSR = _tGO.GetComponent<SpriteRenderer>(); // Получить ссылку на компонент SpriteRenderer
            _tSR.sprite = dictSuits[card.suit]; // Установить спрайт масти
            _tSR.sortingOrder = 1; // Установить sortingOrder, чтобы значок отображался над Card_Front
            card.pipGOs.Add(_tGO); // Добавить этот игровой объект в список значков
        }
    }

    private void AddFace(Card card)
    {
        if (card.def.face == "") // Выйти, если это не карта с картинкой
        {
            return;
        }
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSp = GetFace(card.def.face + card.suit); // Сгенерировать имя и передать его в GetFace()
        _tSR.sprite = _tSp; // Установить этот спрайт в _tSR
        _tSR.sortingOrder = 1; // Установить sortingOrder
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "face";
    }

    // Находит спрайт с картинкой для карты
    private Sprite GetFace(string faceS)
    {
        foreach (Sprite _tSP in faceSprites)
        {
            if (_tSP.name == faceS) // Если найден спрайт с требуемым именем
            {
                return (_tSP); // вернуть его
            }
        }
        return (null); // Если не найден
    }

    // Добавить рубашку
    private void AddBack(Card card)
    {
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        // Большее значение sortingOrder, чем у других спрайтов
        _tSR.sortingOrder = 2;
        _tGO.name = "back";
        card.back = _tGO;

        //  По умолчанию рубашкой вверх
        card.faceUp = startFaceUp;
    }
    // Перемешивание карт
    static public void Shuffle(ref List<Card> oCards) // ref - ссылка
    {
        // Создать временный список для хранения карт
        List<Card> tCards = new List<Card>();
        int ndx; // индекс перемешиваемой карты
        tCards = new List<Card>();
        while (oCards.Count > 0) // перемешиваем
        {
            ndx = Random.Range(0, oCards.Count);
            tCards.Add(oCards[ndx]);
            oCards.RemoveAt(ndx);
        }
        // Заменить исходный список временным
        oCards = tCards;
    }
}









