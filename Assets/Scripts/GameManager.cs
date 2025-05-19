using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            return instance;
        }
    }

    public enum GameState
    {
        Game,
        Edit,
    }
    public GameState state = GameState.Game;

    public bool isPlaying = true;
    public string title;
    Coroutine coPlaying;

    public Dictionary<string, Sheet> sheets = new Dictionary<string, Sheet>();

    float speed = 1.0f;
    public float Speed
    {
        get
        {
            return speed;
        }
        set
        {
            speed = Mathf.Clamp(value, 1.0f, 5.0f);
        }
    }

    public List<GameObject> canvases = new List<GameObject>();
    enum Canvas
    {
        Title,
        Select,
        SFX,
        Game,
        Result,
    }
    CanvasGroup sfxFade;

    [System.Serializable]
    public struct HighScore
    {
        public int score;
        public string title;
        public string date;
    }
    public List<HighScore> highScores = new List<HighScore>();

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        LoadHighScores();
        StartCoroutine(IEInit());
    }

    public void ChangeMode(UIObject uiObject)
    {
        TextMeshProUGUI text = uiObject.transform.GetComponentInChildren<TextMeshProUGUI>();
        text.text = "Game\nMode"; 
    }

    public void Title()
    {
        StartCoroutine(IETitle());
    }

    public void Select()
    {
        StartCoroutine(IESelect());
    }

    public void Play()
    {
        StartCoroutine(IEInitPlay());
    }

    public void Stop()
    {
        if (state == GameState.Game)
        {
            canvases[(int)Canvas.Game].SetActive(false);

            if (coPlaying != null)
            {
                StopCoroutine(coPlaying);
                coPlaying = null;
            }
        }

        NoteGenerator.Instance.StopGen();

        AudioManager.Instance.progressTime = 0f;
        AudioManager.Instance.Stop();

        Select();
    }

    IEnumerator IEInit()
    {
        SheetLoader.Instance.Init();

        foreach (GameObject go in canvases)
        {
            go.SetActive(true);
        }
        sfxFade = canvases[(int)Canvas.SFX].GetComponent<CanvasGroup>();
        sfxFade.alpha = 1f;

        UIController.Instance.Init();
        Score.Instance.Init();

        yield return new WaitForSeconds(2f);
        canvases[(int)Canvas.Game].SetActive(false);
        canvases[(int)Canvas.Result].SetActive(false);
        canvases[(int)Canvas.Select].SetActive(false);        

        yield return new WaitUntil(() => SheetLoader.Instance.bLoadFinish == true);
        ItemGenerator.Instance.Init();

        Title();
    }

    IEnumerator IETitle()
    {
        canvases[(int)Canvas.SFX].SetActive(true);
        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, false, 1f));

        yield return new WaitForSeconds(1f);

        Select();
    }

    IEnumerator IESelect()
    {
        canvases[(int)Canvas.SFX].SetActive(true);
        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, true, 2f));

        canvases[(int)Canvas.Title].SetActive(false);

        canvases[(int)Canvas.Result].SetActive(false);

        canvases[(int)Canvas.Select].SetActive(true);

        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, false, 2f));
        canvases[(int)Canvas.SFX].SetActive(false);

        isPlaying = false;
    }

    IEnumerator IEInitPlay()
    {
        isPlaying = true;

        canvases[(int)Canvas.SFX].SetActive(true);
        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, true, 2f));

        canvases[(int)Canvas.Select].SetActive(false);

        title = sheets.ElementAt(ItemController.Instance.page).Key;
        sheets[title].Init();

        AudioManager.Instance.Insert(sheets[title].clip);

        canvases[(int)Canvas.Game].SetActive(true);

        Score.Instance.Clear();
        UpdateJudgeUI();
        
        FindObjectOfType<Judgement>().Init(); 

        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, false, 2f));
        canvases[(int)Canvas.SFX].SetActive(false);

        NoteGenerator.Instance.StartGen();

        yield return new WaitForSeconds(3f);

        AudioManager.Instance.progressTime = 0f;
        AudioManager.Instance.Play();

        coPlaying = StartCoroutine(IEEndPlay());
    }

    IEnumerator IEEndPlay()
    {
        while (true)
        {
            if (!AudioManager.Instance.IsPlaying())
            {
                break;
            }
            yield return new WaitForSeconds(1f);
        }

        canvases[(int)Canvas.SFX].SetActive(true);
        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, true, 2f));
        canvases[(int)Canvas.Game].SetActive(false);
        canvases[(int)Canvas.Result].SetActive(true);

        UIText rscore = UIController.Instance.FindUI("UI_R_Score").uiObject as UIText;
        UIText rkool = UIController.Instance.FindUI("UI_R_Kool").uiObject as UIText;
        UIText rcool = UIController.Instance.FindUI("UI_R_Cool").uiObject as UIText;
        UIText rgood = UIController.Instance.FindUI("UI_R_Good").uiObject as UIText;
        UIText rmiss = UIController.Instance.FindUI("UI_R_Miss").uiObject as UIText;
        UIText rfail = UIController.Instance.FindUI("UI_R_Fail").uiObject as UIText;

        rscore.SetText(Score.Instance.data.score.ToString());
        rkool.SetText(Score.Instance.data.kool.ToString());
        rcool.SetText(Score.Instance.data.cool.ToString());
        rgood.SetText(Score.Instance.data.good.ToString());
        rmiss.SetText(Score.Instance.data.miss.ToString());
        rfail.SetText(Score.Instance.data.fail.ToString());

        // 하이스코어 갱신 로직을 UI 표시 전으로 이동
        HighScore newScore = new HighScore();
        newScore.score = Score.Instance.data.score;
        newScore.title = title;
        newScore.date = DateTime.Now.ToString("yyyy-MM-dd");
        
        UpdateHighScores(newScore);

        // 수정된 랭크 표시 방식
        List<UIText> rankTexts = new List<UIText>
        {
            UIController.Instance.FindUI("UI_R_Rank1").uiObject as UIText,
            UIController.Instance.FindUI("UI_R_Rank2").uiObject as UIText,
            UIController.Instance.FindUI("UI_R_Rank3").uiObject as UIText,
            UIController.Instance.FindUI("UI_R_Rank4").uiObject as UIText,
            UIController.Instance.FindUI("UI_R_Rank5").uiObject as UIText
        };

        for (int i = 0; i < 5; i++)
        {
            if (i < highScores.Count)
            {
                rankTexts[i].SetText($"{i+1}. {highScores[i].score}");
            }
            else
            {
                rankTexts[i].SetText($"{i+1}. ---");
            }
        }

        // # Once Imeage processing is done, uncomment this
        // UIImage rBG = UIController.Instance.FindUI("UI_R_BG").uiObject as UIImage;
        // rBG.SetSprite(sheets[title].img);

        NoteGenerator.Instance.StopGen();
        AudioManager.Instance.Stop();

        yield return StartCoroutine(AniPreset.Instance.IEAniFade(sfxFade, false, 2f));
        canvases[(int)Canvas.SFX].SetActive(false);

        yield return new WaitForSeconds(5f);

        UpdateRankUI();

        Select();
    }

    void UpdateHighScores(HighScore newScore)
    {
        highScores.Add(newScore);
        highScores = highScores
            .OrderByDescending(s => s.score)
            .Take(5)
            .ToList();
            
        SaveHighScores();
    }

    void UpdateRankUI()
    {
        for(int i=0; i<5; i++)
        {
            string uiName = $"UI_R_Rank{i+1}";
            UIText rankText = UIController.Instance.FindUI(uiName).uiObject as UIText;
            
            if(i < highScores.Count)
            {
                rankText.SetText($"{i+1}. {highScores[i].score}");
            }
            else
            {
                rankText.SetText($"{i+1}. ---");
            }
        }
    }

    void LoadHighScores()
    {
        string json = PlayerPrefs.GetString("HighScores");
        if(!string.IsNullOrEmpty(json))
        {
            highScores = JsonUtility.FromJson<List<HighScore>>(json);
        }
    }

    void SaveHighScores()
    {
        string json = JsonUtility.ToJson(highScores);
        PlayerPrefs.SetString("HighScores", json);
        PlayerPrefs.Save();
    }

    public void UpdateJudgeUI()
    {
        UIText koolText = UIController.Instance.FindUI("UI_G_Kool").uiObject as UIText;
        UIText coolText = UIController.Instance.FindUI("UI_G_Cool").uiObject as UIText;
        UIText goodText = UIController.Instance.FindUI("UI_G_Good").uiObject as UIText;
        UIText missText = UIController.Instance.FindUI("UI_G_Miss").uiObject as UIText;
        UIText failText = UIController.Instance.FindUI("UI_G_Fail").uiObject as UIText;

        koolText.SetText(Score.Instance.data.kool.ToString());
        coolText.SetText(Score.Instance.data.cool.ToString());
        goodText.SetText(Score.Instance.data.good.ToString());
        missText.SetText(Score.Instance.data.miss.ToString());
        failText.SetText(Score.Instance.data.fail.ToString());
    }

}
