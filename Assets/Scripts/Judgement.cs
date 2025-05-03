using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    readonly int fail = 800;
    readonly int miss = 300;
    readonly int good = 200;
    readonly int cool = 100;
    readonly int kool = 50;

    List<Queue<Note>> notes = new List<Queue<Note>>();
    Queue<Note> note1 = new Queue<Note>();
    Queue<Note> note2 = new Queue<Note>();
    Queue<Note> note3 = new Queue<Note>();
    Queue<Note> note4 = new Queue<Note>();

    int[] longNoteCheck = new int[4] { 0, 0, 0, 0 };

    int curruntTime = 0;
    public int judgeTimeFromUserSetting = 0;

    Coroutine coCheckMiss;

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
                    }
                    else if (absTime < cool)
                    {
                        Score.Instance.data.cool++;
                        Score.Instance.data.judge = JudgeType.Cool;
                    }
                    else
                    {
                        Score.Instance.data.good++;
                        Score.Instance.data.judge = JudgeType.Good;
                    }
                    Score.Instance.data.combo++;
                }
                else
                {
                    Score.Instance.data.miss++;
                    Score.Instance.data.judge = JudgeType.Miss;
                    Score.Instance.data.combo = 0;
                }
            }
            else
            {
                Score.Instance.data.fail++;
                Score.Instance.data.judge = JudgeType.Fail;
                Score.Instance.data.combo = 0;
            }

            Score.Instance.SetScore();

            if (note.type == (int)NoteType.Short)
            {
                notes[line].Dequeue();
            }
            else if (note.type == (int)NoteType.Long)
            {
                longNoteCheck[line] = 1;
            }
        }
    }

    public void CheckLongNote(int line)
    {
        if (notes[line].Count <= 0)
            return;

        Note note = notes[line].Peek();
        if (note.type != (int)NoteType.Long)
            return;

        int judgeTime = curruntTime - note.tail + judgeTimeFromUserSetting;
        int absTime = Mathf.Abs(judgeTime);

        if (absTime < fail)
        {
            if (absTime < kool)
            {
                Score.Instance.data.kool++;
                Score.Instance.data.judge = JudgeType.Kool;
                Score.Instance.data.combo++;
            }
            else if (absTime < cool)
            {
                Score.Instance.data.cool++;
                Score.Instance.data.judge = JudgeType.Cool;
                Score.Instance.data.combo++;
            }
            else if (absTime < good)
            {
                Score.Instance.data.good++;
                Score.Instance.data.judge = JudgeType.Good;
                Score.Instance.data.combo++;
            }
            else
            {
                Score.Instance.data.miss++;
                Score.Instance.data.judge = JudgeType.Miss;
                Score.Instance.data.combo = 0;
            }

            Score.Instance.SetScore();
            longNoteCheck[line] = 0;
            notes[line].Dequeue();
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
                    }
                }
            }

            yield return null;
        }
    }
}
