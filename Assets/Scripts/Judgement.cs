using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum JudgeType
{
    Kool,
    Cool,
    Good,
    Miss,
    Fail
}

public class Judgement : MonoBehaviour
{
    readonly int fail = 600;
    readonly int miss = 300;
    readonly int good = 200;
    readonly int cool = 150;
    readonly int kool = 100;

    List<Queue<Note>> notes = new List<Queue<Note>>();
    Queue<Note> note1 = new Queue<Note>();
    Queue<Note> note2 = new Queue<Note>();
    Queue<Note> note3 = new Queue<Note>();
    Queue<Note> note4 = new Queue<Note>();

    int[] longNoteCheck = new int[4] { 0, 0, 0, 0 };

    int curruntTime = 0;
    public int judgeTimeFromUserSetting = 0;

    Coroutine coCheckMiss;

    Dictionary<int, Note> activeLongNotes = new Dictionary<int, Note>();

    public void Init()
    {
        foreach (var note in notes)
        {
            note.Clear();
        }
        notes.Clear();

        foreach (var note in GameManager.Instance.sheets[GameManager.Instance.title].notes)
        {
            if (note.line == 1)
                note1.Enqueue(note);
            else if (note.line == 2)
                note2.Enqueue(note);
            else if (note.line == 3)
                note3.Enqueue(note);
            else
                note4.Enqueue(note);
        }

        notes.Add(note1);
        notes.Add(note2);
        notes.Add(note3);
        notes.Add(note4);

        if (coCheckMiss != null)
        {
            StopCoroutine(coCheckMiss);
        }
        coCheckMiss = StartCoroutine(IECheckMiss());
    }

    public void Judge(int line)
    {
        if (notes[line].Count <= 0 || !AudioManager.Instance.IsPlaying())
            return;

        Note note = notes[line].Peek();
        int judgeTime = curruntTime - note.time + judgeTimeFromUserSetting;
        int absTime = Mathf.Abs(judgeTime);

        if (absTime < fail)
        {
            if (absTime < miss)
            {
                if (absTime < good)
                {
                    if (absTime < kool)
                    {
                        Score.Instance.data.kool++;
                        Score.Instance.data.judge = JudgeType.Kool;
                        GameManager.Instance.UpdateJudgeUI();
                    }
                    else if (absTime < cool)
                    {
                        Score.Instance.data.cool++;
                        Score.Instance.data.judge = JudgeType.Cool;
                        GameManager.Instance.UpdateJudgeUI();
                    }
                    else
                    {
                        Score.Instance.data.good++;
                        Score.Instance.data.judge = JudgeType.Good;
                        GameManager.Instance.UpdateJudgeUI();
                    }
                    Score.Instance.data.combo++;
                }
                else
                {
                    Score.Instance.data.miss++;
                    Score.Instance.data.judge = JudgeType.Miss;
                    Score.Instance.data.combo = 0;
                    GameManager.Instance.UpdateJudgeUI();
                }
            }
            else
            {
                Score.Instance.data.fail++;
                Score.Instance.data.judge = JudgeType.Fail;
                Score.Instance.data.combo = 0;
                GameManager.Instance.UpdateJudgeUI();
            }

            Score.Instance.SetScore();

            if (note.type == (int)NoteType.Short)
            {
                notes[line].Dequeue();
            }
            else if (note.type == (int)NoteType.Long)
            {
                activeLongNotes[line] = note;
                notes[line].Dequeue();
            }
        }
    }

    public void CheckLongNote(int line)
    {
        if (!activeLongNotes.ContainsKey(line)) return;

        Note note = activeLongNotes[line];
        int judgeTime = curruntTime - note.tail + judgeTimeFromUserSetting;
        int absTime = Mathf.Abs(judgeTime);

        if (absTime < fail)
        {
            if (absTime < kool)
            {
                Score.Instance.data.kool++;
                Score.Instance.data.judge = JudgeType.Kool;
                Score.Instance.data.combo++;
                GameManager.Instance.UpdateJudgeUI();
            }
            else if (absTime < cool)
            {
                Score.Instance.data.cool++;
                Score.Instance.data.judge = JudgeType.Cool;
                Score.Instance.data.combo++;
                GameManager.Instance.UpdateJudgeUI();
            }
            else if (absTime < good)
            {
                Score.Instance.data.good++;
                Score.Instance.data.judge = JudgeType.Good;
                Score.Instance.data.combo++;
                GameManager.Instance.UpdateJudgeUI();
            }
            else
            {
                Score.Instance.data.miss++;
                Score.Instance.data.judge = JudgeType.Miss;
                Score.Instance.data.combo = 0;
                GameManager.Instance.UpdateJudgeUI();
            }

            Score.Instance.SetScore();
            activeLongNotes.Remove(line);
        }
    }

    IEnumerator IECheckMiss()
    {
        while (true)
        {
            curruntTime = (int)AudioManager.Instance.GetMilliSec();

            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].Count <= 0)
                    continue;

                Note note = notes[i].Peek();
                int judgeTime = note.time - curruntTime + judgeTimeFromUserSetting;

                if (note.type == (int)NoteType.Long)
                {
                    if (longNoteCheck[note.line - 1] == 0)
                    {
                        if (judgeTime < -fail)
                        {
                            Score.Instance.data.fail++;
                            Score.Instance.data.judge = JudgeType.Fail;
                            Score.Instance.data.combo = 0;
                            Score.Instance.SetScore();
                            notes[i].Dequeue();
                            GameManager.Instance.UpdateJudgeUI();
                        }
                    }
                }
                else
                {
                    if (judgeTime < -fail)
                    {
                        Score.Instance.data.fail++;
                        Score.Instance.data.judge = JudgeType.Fail;
                        Score.Instance.data.combo = 0;
                        Score.Instance.SetScore();
                        notes[i].Dequeue();
                        GameManager.Instance.UpdateJudgeUI();
                    }
                }
            }

            foreach (var kvp in activeLongNotes.ToList())
            {
                int line = kvp.Key;
                Note note = kvp.Value;
                int judgeTime = note.tail - curruntTime + judgeTimeFromUserSetting;

                if (judgeTime < -fail)
                {
                    Score.Instance.data.fail++;
                    Score.Instance.data.judge = JudgeType.Fail;
                    Score.Instance.data.combo = 0;
                    Score.Instance.SetScore();
                    activeLongNotes.Remove(line);
                    GameManager.Instance.UpdateJudgeUI();
                }
            }

            yield return null;
        }
    }
    
}
