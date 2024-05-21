using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//------------------------------------------------------ ��� ������ XML ������ ----------------------------------------\\
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
    // �����
    public Sprite suitClub; // �����
    public Sprite suitDiamond; // �����
    public Sprite suitHeart; // �����
    public Sprite suitSpade; // ����

    public Sprite[] faceSprites;
    public Sprite[] rankSprites;

    public Sprite cardBack;
    public Sprite cardBackGold;
    public Sprite cardFront;
    public Sprite cardFrontGold;
    // �������
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

    // InitDeck ���������� ����������� Prospector, ����� ����� �����
    public void InitDeck(string deckXMLText)
    {
        // ������� ����� �������� ��� ���� ������� �������� Card � ��������
       if (GameObject.Find("_Deck") == null)
        {
            GameObject anchorGO = new GameObject("_Deck");
            deckAnchor = anchorGO.transform;
        }

        // ���������������� ������� �� ��������� ������� ������
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

    /*����� ReadDeck() ������ ���������� XML-����� � �����������
    � ������ ����������� Decorator (����� � ����������� � ����� �����)
    � ����������� CardDefinition (� ����������� � ����������� ������ ����� (�� ���� �� ������)*/

    // ReadDeck ������ ��������� XML-���� � ������� ������ ����������� CardDefinition
    public void ReadDeck(string deckXMLText)
    {
        xmlr = new PT_XMLReader(); // ������� ����� ��������� PT_XMLReader
        xmlr.Parse(deckXMLText); // ����������� ��� ��� ������ DeckXML

        // ����� ����������� ������
        string s = "xml[0] decorator[0]";
        s += "type=" + xmlr.xml["xml"][0]["decorator"][0].att("type");
        s += "x=" + xmlr.xml["xml"][0]["decorator"][0].att("x");
        s += "y=" + xmlr.xml["xml"][0]["decorator"][0].att("y");
        s += "scale=" + xmlr.xml["xml"][0]["decorator"][0].att("scale");
        //print(s);
        
        // ��������� �������� <decorator> ��� ���� ����
        decorators = new List<Decorator>();
        // ������� ������ PT_XMLHashList���� ��������� ,decorator> �� XML-�����
        PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];
        Decorator deco;
        for (int i = 0; i < xDecos.Count; i++)
        {
            // ��� ������� �������� <decorator> � XML
            deco = new Decorator(); // ������� ��������� Decorator
                                    // ����������� �������� �� <decorator> � Decorator
            deco.type = xDecos[i].att("type");
            // deco.flip ������� �������� true, ���� ������� flip �������� ����� "1" 
            deco.flip = (xDecos[i].att("flip") == "1");
            // �������� �������� float �� ��������� ���������
            deco.scale = float.Parse(xDecos[i].att("scale"));
            // Vector3 loc ���������������� ��� [0, 0, 0], �������� ���
            deco.loc.x = float.Parse(xDecos[i].att("x"));
            deco.loc.y = float.Parse(xDecos[i].att("y"));
            deco.loc.z = float.Parse(xDecos[i].att("z"));
            // �������� deco  � ������ decorators
            decorators.Add(deco);
        }

        // ��������� ���������� ��� �������, ������������ ����������� �����
        cardDefs = new List<CardDefinition>(); // ���������������� ������ ����
                                               // ������� ������ PT_XMLHashList ���� ��������� <card> �� XML �����
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];
        for (int i = 0; i < xCardDefs.Count; i++)
        {
            // ��� ������� �������� <card> ������� ��������� CardDefinition
            CardDefinition cDef = new CardDefinition();
            // �������� �������� �������� � �������� �� � cDef
            cDef.rank = int.Parse(xCardDefs[i].att("rank"));
            // ������� ������ PT_XMLHashList �� ���� ��������� <pip> ������ ������� ��������  <card>
            PT_XMLHashList xPips = xCardDefs[i]["pip"];
            if (xPips != null)
            {
                for (int j = 0; j < xPips.Count; j++)
                {
                    deco = new Decorator();
                    // �������� <pip> � <card> �������������� ������� Decorator
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
            // ������� �����
            if (xCardDefs[i].HasAtt("face"))
            {
                cDef.face = xCardDefs[i].att("face");
            }
            cardDefs.Add(cDef);
        }
    }
    
    // ��������  CardDefinition �� ������ �������� �����������
    public CardDefinition GetCardDefinitionByRank(int rnk)
    {
        // ����� �� ���� ����������� CardDefinition
        foreach (CardDefinition cd in cardDefs)
        {
            // ���� ����������� ���������, ������� �����������
            if (cd.rank == rnk)
            {
                return (cd);
            }
        }
        return (null);
    }

    // ������� ������� ������� ����
    public void MakeCards()
    {
        //  cardNames ����� ��������� ����� ����������������� ����
        // ������ ����� ����� 14 �������� �����������
        cardNames = new List<string>();
        string[] letters = new string[] { "C", "D", "H", "S" };
        foreach (string s in letters)
        {
            for (int i = 0; i < 13; i++)
            {
                cardNames.Add(s + (i + 1));
            }
        }

        // ������� ������ �� ����� �������
        cards = new List<Card>();

        // ������ ��� ������ ��� ��������� ����� ����
        for (int i = 0; i < cardNames.Count; i++)
        {
            cards.Add(MakeCard(i)); // ������� ����� � �������� �� � ������
        }
    }

    private Card MakeCard(int cNum)
    {
        // ������� ������� ������ � ������
        GameObject cgo = Instantiate(prefabCard) as GameObject;
        // ��������� transform.parent ����� ����� � ������������ � ������ ��������
        cgo.transform.parent = deckAnchor;
        Card card = cgo.GetComponent<Card>(); // �������� ��������� Card

        // ����������� � ���������� ���
        cgo.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);

        // ��������� �������� ��������� �����
        card.name = cardNames[cNum];
        card.suit = card.name[0].ToString();
        card.rank = int.Parse(card.name.Substring(1));
        if (card.suit == "D" || card.suit == "H") // ��� ������������� ������ � �������
        {
            card.colS = "Red";
            card.color = Color.red;
        }
        // �������� CardDefinition ��� ���� �����
        card.def = GetCardDefinitionByRank(card.rank);

        AddDecorators(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return card;
    }

    // ��� ������� ���������� ������������ ���������������� ��������
    private Sprite _tSp = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;
    /*private Sprite tSp;
    private GameObject tGO;
    private SpriteRenderer tSR;*/

    private void AddDecorators(Card card)
    {
        // �������� ����������
        foreach (Decorator deco in decorators)
        {
            if (deco.type == "suit")
            {
                _tGO = Instantiate(prefabSprite) as GameObject; // ������� �������� �������� ������� �������
                _tSR = _tGO.GetComponent<SpriteRenderer>(); // �������� ������ �� ��������� SpriteRenderer
                _tSR.sprite = dictSuits[card.suit]; // ���������� ������ �����
            }
            else
            {
                _tGO = Instantiate(prefabSprite) as GameObject;
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                _tSp = rankSprites[card.rank]; // �������� ������ ��� ����������� �����������
                _tSR.sprite = _tSp; // ���������� ������ ����������� � SpriteRenderer
                _tSR.color = card.color; // ���������� ���� ��������������� �����
            }
            _tSR.sortingOrder = 1; // ��������� ������� ��� ������
            _tGO.transform.SetParent(card.transform); // ������� ������ �������� �� ��������� � �����
            _tGO.transform.localPosition = deco.loc; // Location ��� � DeckXML
            if (deco.flip) // ����������� ������
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            if (deco.scale != 1) // ���������� �������, ����� ��������� ������ �������
            {
                _tGO.transform.localScale = Vector3.one * deco.scale;
            }
            _tGO.name = deco.type; // ���� ���
            card.decoGOs.Add(_tGO); // �������� ���� ������� ������ � ������ card.decoGOs
        }
    }

    private void AddPips(Card card)
    {
        foreach (Decorator pip in card.def.pips) // ��� ������� ������ � �����������
        {
            _tGO = Instantiate(prefabSprite) as GameObject; // ������� ������� ������ �������
            _tGO.transform.SetParent(card.transform); // ��������� ��������� ������� ������ �����
            _tGO.transform.localPosition = pip.loc; // ���������� localPosition ��� � XML �����
            if (pip.flip) // ����������� ���� ����������
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            if (pip.scale != 1) // �������������� (������ ��� ����)
            {
                _tGO.transform.localScale = Vector3.one * pip.scale;
            }
            _tGO.name = "pip"; // ���� ��� �������� �������
            _tSR = _tGO.GetComponent<SpriteRenderer>(); // �������� ������ �� ��������� SpriteRenderer
            _tSR.sprite = dictSuits[card.suit]; // ���������� ������ �����
            _tSR.sortingOrder = 1; // ���������� sortingOrder, ����� ������ ����������� ��� Card_Front
            card.pipGOs.Add(_tGO); // �������� ���� ������� ������ � ������ �������
        }
    }

    private void AddFace(Card card)
    {
        if (card.def.face == "") // �����, ���� ��� �� ����� � ���������
        {
            return;
        }
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSp = GetFace(card.def.face + card.suit); // ������������� ��� � �������� ��� � GetFace()
        _tSR.sprite = _tSp; // ���������� ���� ������ � _tSR
        _tSR.sortingOrder = 1; // ���������� sortingOrder
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "face";
    }

    // ������� ������ � ��������� ��� �����
    private Sprite GetFace(string faceS)
    {
        foreach (Sprite _tSP in faceSprites)
        {
            if (_tSP.name == faceS) // ���� ������ ������ � ��������� ������
            {
                return (_tSP); // ������� ���
            }
        }
        return (null); // ���� �� ������
    }

    // �������� �������
    private void AddBack(Card card)
    {
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        // ������� �������� sortingOrder, ��� � ������ ��������
        _tSR.sortingOrder = 2;
        _tGO.name = "back";
        card.back = _tGO;

        //  �� ��������� �������� �����
        card.faceUp = startFaceUp;
    }
    // ������������� ����
    static public void Shuffle(ref List<Card> oCards) // ref - ������
    {
        // ������� ��������� ������ ��� �������� ����
        List<Card> tCards = new List<Card>();
        int ndx; // ������ �������������� �����
        tCards = new List<Card>();
        while (oCards.Count > 0) // ������������
        {
            ndx = Random.Range(0, oCards.Count);
            tCards.Add(oCards[ndx]);
            oCards.RemoveAt(ndx);
        }
        // �������� �������� ������ ���������
        oCards = tCards;
    }
}









