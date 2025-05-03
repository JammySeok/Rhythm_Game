using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class NoteGenerator : MonoBehaviour
{
    static NoteGenerator instance;
    public static NoteGenerator Instance
    {
        get
        {
            return instance;
        }
    }

    public GameObject parent;
    public GameObject notePrefab;
    public Material lineRendererMaterial;

    public readonly float[] linePos = { -3.37f, -2.5f, -1.5f, -0.61f };
    readonly float defaultInterval = 0.005f;
    public float Interval { get; private set; }

    public float judgeLineY = -3f; 

    IObjectPool<NoteShort> poolShort;
    public IObjectPool<NoteShort> PoolShort
    {
        get
        {
            if (poolShort == null)
            {
                poolShort = new ObjectPool<NoteShort>(CreatePooledShort, defaultCapacity: 256);
            }
            return poolShort;
        }
    }
    NoteShort CreatePooledShort()
    {
        GameObject note = Instantiate(notePrefab, parent.transform);
        note.AddComponent<NoteShort>();
        return note.GetComponent<NoteShort>();
    }

    IObjectPool<NoteLong> poolLong;
    public IObjectPool<NoteLong> PoolLong
    {
        get
        {
            if (poolLong == null)
            {
                poolLong = new ObjectPool<NoteLong>(CreatePooledLong, defaultCapacity: 64);
            }
            return poolLong;
        }
    }
    NoteLong CreatePooledLong()
    {
        GameObject note = new GameObject("NoteLong");
        note.transform.parent = parent.transform;

        GameObject head = Instantiate(notePrefab);
        head.name = "head";
        head.transform.parent = note.transform;

        GameObject tail = Instantiate(notePrefab);
        tail.transform.parent = note.transform;
        tail.name = "tail";

        GameObject line = new GameObject("line");
        line.transform.parent = note.transform;

        line.AddComponent<LineRenderer>();
        LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
        lineRenderer.material = lineRendererMaterial;
        lineRenderer.sortingOrder = 0;
        lineRenderer.widthMultiplier = 0.8f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;

        note.AddComponent<NoteLong>();
        return note.GetComponent<NoteLong>();
    }

    int currentBar = 3; 
    int next = 0;
    int prev = 0;
    public List<NoteObject> toReleaseList = new List<NoteObject>();

    Coroutine coGenTimer;
    Coroutine coReleaseTimer;
    Coroutine coInterpolate;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void StartGen()
    {
        Interval = defaultInterval * GameManager.Instance.Speed;
        coGenTimer = StartCoroutine(IEGenTimer(GameManager.Instance.sheets[GameManager.Instance.title].BarPerMilliSec * 0.001f)); 
        coReleaseTimer = StartCoroutine(IEReleaseTimer(GameManager.Instance.sheets[GameManager.Instance.title].BarPerMilliSec * 0.001f * 0.5f));
        coInterpolate = StartCoroutine(IEInterpolate(0.1f, 4f));
    }

    public void StopGen()
    {
        if (coGenTimer != null)
        {
            StopCoroutine(coGenTimer);
            coGenTimer = null;
        }
        if (coReleaseTimer != null)
        {
            StopCoroutine(coReleaseTimer);
            coReleaseTimer = null;
        }
        ReleaseCompleted();

        toReleaseList.Clear();
        currentBar = 3;
        next = 0;
        prev = 0;
    }

    void Gen()
    {
        List<Note> notes = GameManager.Instance.sheets[GameManager.Instance.title].notes;
        List<Note> reconNotes = new List<Note>();

        for (; next < notes.Count; next++)
        {
            if (notes[next].time > currentBar * GameManager.Instance.sheets[GameManager.Instance.title].BarPerMilliSec)
                break;
        }

        for (int j = prev; j < next; j++)
            reconNotes.Add(notes[j]);

        prev = next;

        float currentTime = AudioManager.Instance.GetMilliSec();
        float noteSpeed = Interval * 1000;

        foreach (Note note in reconNotes)
        {
            NoteObject noteObject = null;

            switch (note.type)
            {
                case (int)NoteType.Short:
                    noteObject = PoolShort.Get();
                    noteObject.SetPosition(new Vector3[] {
                        new Vector3(linePos[note.line - 1], judgeLineY + (note.time - currentTime) * Interval, 0f)
                    });
                    break;

                case (int)NoteType.Long:
                    noteObject = PoolLong.Get();
                    noteObject.SetPosition(new Vector3[] {
                        new Vector3(linePos[note.line - 1], judgeLineY + (note.time - currentTime) * Interval, 0f),
                        new Vector3(linePos[note.line - 1], judgeLineY + (note.tail - currentTime) * Interval, 0f)
                    });
                    break;
            }

            if (noteObject != null)
            {
                noteObject.speed = noteSpeed;
                noteObject.note = note;
                noteObject.life = true;
                noteObject.gameObject.SetActive(true);
                noteObject.SetCollider();
                noteObject.Move();
                toReleaseList.Add(noteObject);
            }
        }
    }

    void ReleaseCompleted()
    {
        foreach (NoteObject note in toReleaseList)
        {
            note.gameObject.SetActive(false);

            if (note is NoteShort)
                PoolShort.Release(note as NoteShort);
            else
                PoolLong.Release(note as NoteLong);
        }
    }

    void Release()
    {
        List<NoteObject> reconNotes = new List<NoteObject>();
        foreach (NoteObject note in toReleaseList)
        {
            if (!note.life)
            {
                if (note is NoteShort)
                    PoolShort.Release(note as NoteShort);
                else
                    PoolLong.Release(note as NoteLong);

                note.gameObject.SetActive(false);
            }
            else
            {
                reconNotes.Add(note);
            }
        }
        toReleaseList.Clear();
        toReleaseList.AddRange(reconNotes);
    }

    public void Interpolate()
    {
        if (coInterpolate != null)
            StopCoroutine(coInterpolate);

        coInterpolate = StartCoroutine(IEInterpolate());
    }

    IEnumerator IEGenTimer(float interval)
    {
        while (true)
        {
            Gen();
            yield return new WaitForSeconds(interval);
            currentBar++;
        }
    }

    IEnumerator IEReleaseTimer(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            Release();
        }
    }

    IEnumerator IEInterpolate(float rate = 1f, float duration = 1f)
    {
        float time = 0;
        Interval = defaultInterval * GameManager.Instance.Speed;
        float noteSpeed = Interval * 1000;
        while (time < duration)
        {
            float milli = AudioManager.Instance.GetMilliSec();

            foreach (NoteObject note in toReleaseList)
            {
                note.speed = noteSpeed;
                note.Interpolate(milli, Interval);
            }
            time += rate;
            yield return new WaitForSeconds(rate);
        }
    }
}
