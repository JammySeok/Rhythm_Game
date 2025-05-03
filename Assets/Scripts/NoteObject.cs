using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoteObject : MonoBehaviour
{
    public bool life = false;
    public Note note = new Note();
    public float speed = 5f;

    public abstract void Move();
    public abstract IEnumerator IEMove();
    public abstract void SetPosition(Vector3[] pos);
    public abstract void Interpolate(float curruntTime, float interval);
    public abstract void SetCollider();
}

public class NoteShort : NoteObject
{
    public override void Move()
    {
        StartCoroutine(IEMove());
    }

    public override IEnumerator IEMove()
    {
        while (true)
        {
            transform.position += Vector3.down * speed * Time.deltaTime;
            if (transform.position.y < -3f)
                life = false;
            yield return null;
        }
    }

    public override void SetPosition(Vector3[] pos)
    {
        transform.position = pos[0];
    }

    public override void Interpolate(float curruntTime, float interval)
    {
        transform.position = new Vector3(transform.position.x, (note.time - curruntTime) * interval - 3.5f, transform.position.z);
    }

    public override void SetCollider()
    {
        // Always disable collider since editor mode is removed
        GetComponent<BoxCollider2D>().enabled = false;
    }
}

public class NoteLong : NoteObject
{
    LineRenderer lineRenderer;
    public GameObject head;
    public GameObject tail;
    GameObject line;

    void Awake()
    {
        head = transform.GetChild(0).gameObject;
        tail = transform.GetChild(1).gameObject;
        line = transform.GetChild(2).gameObject;
        lineRenderer = line.GetComponent<LineRenderer>();
    }

    public override void Move()
    {
        StartCoroutine(IEMove());
    }

    public override IEnumerator IEMove()
    {
        while (true)
        {
            transform.position += Vector3.down * speed * Time.deltaTime;

            if (tail.transform.position.y < -3f)
                life = false;

            yield return null;
        }
    }

    public override void SetPosition(Vector3[] pos)
    {
        transform.position = pos[0];
        head.transform.position = pos[0];
        tail.transform.position = pos[1];
        line.transform.position = head.transform.position;

        Vector3 linePos = tail.transform.position - head.transform.position;
        linePos.x = 0f;
        linePos.z = 0f;
        lineRenderer.SetPosition(1, linePos);
    }

    public override void Interpolate(float curruntTime, float interval)
    {
        float yHead = (note.time - curruntTime) * interval - 3.5f;
        float yTail = (note.tail - curruntTime) * interval - 3.5f;

        transform.position = new Vector3(transform.position.x, yHead, transform.position.z);
        head.transform.position = new Vector3(head.transform.position.x, yHead, head.transform.position.z);
        tail.transform.position = new Vector3(tail.transform.position.x, yTail, tail.transform.position.z);
        line.transform.position = head.transform.position;

        Vector3 linePos = tail.transform.position - head.transform.position;
        linePos.x = 0f;
        linePos.z = 0f;
        lineRenderer.SetPosition(1, linePos);
    }

    public override void SetCollider()
    {
        // Always disable colliders since Editor mode is removed
        head.GetComponent<BoxCollider2D>().enabled = false;
        tail.GetComponent<BoxCollider2D>().enabled = false;
    }
}
