using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum eFSState
{
    idle,
    pre,
    active,
    post
}

public class FloatingScore : MonoBehaviour
{
    [Header("Set Dynamically")]
    public eFSState state = eFSState.idle;

    [SerializeField]
    protected int _score = 0;
    public string scoreString;

    public int score
    {
        get { return (_score); }
        set
        {
            _score = value;
            scoreString = _score.ToString("N0"); // N0 требует добавить точки в число чмок :*
            GetComponent<Text>().text = scoreString;
        }
    }

    public List<Vector2> bezierPts; // Точки, определяющие кривую Безье
    public List<float> fontSizes; // Точки кривойБезье для масштабирования шрифта
    public float timeStart = -1f;
    public float timeDuration = 1f;
    public string easingCurve = Easing.InOut; // Функция сглаживание из Utils.cs
    // Игровой объект, для которого будет вызван метод SendMessage, когда экземпляр FloatingScore закончит движение
    public GameObject reportFinishTo = null;
    private RectTransform rectTrans;

    private Text txt;

    // Настроить FloatingScore и параметры движения
    public void Init(List<Vector2> ePts, float eTimeS = 0, float eTimeD = 1)
    {
        rectTrans = GetComponent<RectTransform>();
        rectTrans.anchoredPosition = Vector2.zero;
        txt = GetComponent<Text>();
        bezierPts = new List<Vector2>(ePts);
        if (ePts.Count == 1) // Если задана одна точка
        {
            transform.position = ePts[0]; // Просто переместиться в нее
            return;
        }
        if (eTimeS == 0) eTimeS = Time.time; // Если eTimes имеет значение по умолчанию, запустить отсчет от текущего времени
        timeStart = eTimeS;
        timeDuration = eTimeD;
        state = eFSState.pre;
    }

    // Когда SendMessage вызовет эту функцию, она должна добавить очки из вызвавшего экземпляра FloatingScore
    public void FSCallback(FloatingScore fs)
    {
        score += fs.score;
    }

    void Update()
    {
        if (state == eFSState.idle) return; // Если объект никуда не перемещается

        // Вычислить u на основе текущего времени и продолжительности движения
        float u = (Time.time - timeStart) / timeDuration;
        // Использовать класс Easing и Utils.cs для корректировки значения u
        float uC = Easing.Ease(u, easingCurve);
        if (u < 0) // Объект не должен двигаться
        {
            state = eFSState.pre;
            txt.enabled = false; // Изначально скрыть число
        }
        else
        {
            if (u >= 1) // Выполняется движение
            {
                uC = 1; // Чтобы не  выйти за крайнюю точку
                state = eFSState.post;
                // Если игровой объект указан, использовать SendMessage для вызова метода FSCallBack и передачи ему текущего экземпляра в параметре
                if (reportFinishTo != null)
                {
                    reportFinishTo.SendMessage("FSCallback", this);
                    Destroy(gameObject);
                }
                else // иначе оставить его в покое
                {
                    state = eFSState.idle;
                }
            }
            else // Текущий экземпляр движется
            {
                state = eFSState.active;
                txt.enabled = true; // показать число очков
            }
            Vector2 pos = Utils.Bezier(uC, bezierPts); // Использовать кривую безье для движения
            // Опорные точки RectTransform можно использовать для позиционирования объектов пользовательского интерфейса
            // относительного общего размера экрана
            rectTrans.anchorMin = rectTrans.anchorMax = pos;
            if (fontSizes != null && fontSizes.Count > 0) // Если список содержит значения
            {
                int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes)); // скорректировать fontSize этого объекта GUITexts
                GetComponent<Text>().fontSize = size;
            }
        }
    }
}
