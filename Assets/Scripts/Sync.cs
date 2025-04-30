using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sync : MonoBehaviour
{
    Judgement judgement;

    public GameObject judgeLine;
    SpriteRenderer sr;
    UIText text;

    Coroutine coPopup;

    float moveAmount = 0.025f;
    float initialY = -3.5f;    // 초기 y 위치 기준

    void Start()
    {
        judgement = FindObjectOfType<Judgement>();
        sr = judgeLine.GetComponent<SpriteRenderer>();
        sr.color = new Color(1, 0, 0);

        // 초기 위치 설정
        judgeLine.transform.position = new Vector3(judgeLine.transform.position.x, initialY, judgeLine.transform.position.z);
    }

    public void Down()
    {
        AdjustSync(-moveAmount);
    }

    public void Up()
    {
        AdjustSync(moveAmount);
    }

    void AdjustSync(float deltaY)
    {
        judgeLine.transform.position += Vector3.up * deltaY;

        // y 이동량을 시간(ms) 오차로 환산
        float msOffset = deltaY / NoteGenerator.Instance.Interval;
        judgement.judgeTimeFromUserSetting += Mathf.RoundToInt(msOffset);

        UpdateUIText();
    }

    void UpdateUIText()
    {
        text = UIController.Instance.FindUI("UI_G_SyncTime").uiObject as UIText;

        int time = Mathf.Abs(judgement.judgeTimeFromUserSetting);
        string txt = $"{time} ms";

        if (judgement.judgeTimeFromUserSetting < 0)
            txt = $"{time} ms SLOW";
        else if (judgement.judgeTimeFromUserSetting > 0)
            txt = $"{time} ms FAST";

        text.SetText(txt);
    }
}
