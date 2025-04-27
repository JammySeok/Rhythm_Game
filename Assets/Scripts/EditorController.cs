using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorController : MonoBehaviour
{
    static EditorController instance;
    public static EditorController Instance
    {
        get
        {
            return instance;
        }
    }

    public GameObject cursorPrefab;
    GameObject cursorObj;

    public bool isCtrl;
    float scrollValue;
    Coroutine coCtrl;

    Camera cam;
    Vector3 worldPos;

    InputManager inputManager;
    AudioManager audioManager;
    Editor editor;

    public Action<int> GridSnapListener;

    GameObject selectedNoteObject;
    Vector3 selectedGridPosition;
    Vector3 lastSelectedGridPosition;
    Transform headTemp;
    int selectedLine = 0;

    int longNoteMakingCount = 0;
    GameObject longNoteTemp;
    bool isDispose;

    public bool isShortNoteActive;
    public bool isLongNoteActive;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        cam = Camera.main;

        inputManager = FindObjectOfType<InputManager>();
        audioManager = AudioManager.Instance;
        editor = Editor.Instance;

        cursorObj = Instantiate(cursorPrefab);
    }

    void Update()
    {
        // �׸��忡 ���̽��� ��ġ �˾Ƴ�����
        // ���� ������ ����, ������ ��ġ �˾Ƴ�����
        //Debug.Log(inputManager.mousePos);
        Vector3 mousePos = inputManager.mousePos;
        mousePos.z = -cam.transform.position.z;
        worldPos = cam.ScreenToWorldPoint(mousePos);
        //int layerMask = (1 << LayerMask.NameToLayer("Grid")) + (1 << LayerMask.NameToLayer("Note"));

        // Ŀ�� ��ǥ
        cursorObj.transform.position = worldPos;

        Debug.DrawRay(worldPos, cam.transform.forward * 2, Color.red, 0.2f);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, cam.transform.forward, 2f);
        if (hit.transform == null)
        {
            isDispose = false;
            selectedNoteObject = null;
            return;
        }
        else if (hit.transform.CompareTag("Note"))
        {
            //Debug.Log("note");
            isDispose = false;
            selectedNoteObject = hit.transform.gameObject;
        }
        else
        {
            //Debug.Log("grid");
            int beat = int.Parse(hit.transform.name.Split('_')[1]);
            int index = hit.transform.parent.GetComponent<GridObject>().index;
            float y = hit.transform.TransformDirection(hit.transform.position).y; // Local Position To World Position

            if (worldPos.x < -1f && worldPos.x > -2f)
            {
                //Debug.Log($"0�� ���� : {index}�� �׸��� : {beat} ��Ʈ");
                selectedLine = 0;
            }
            else if (worldPos.x < 0f && worldPos.x > -1f)
            {
                //Debug.Log($"1�� ���� : {index}�� �׸��� : {beat} ��Ʈ");
                selectedLine = 1;
            }
            else if (worldPos.x < 1f && worldPos.x > 0f)
            {
                //Debug.Log($"2�� ���� : {index}�� �׸��� : {beat} ��Ʈ");
                selectedLine = 2;
            }
            else if (worldPos.x < 2f && worldPos.x > 1f)
            {
                //Debug.Log($"3�� ���� : {index}�� �׸��� : {beat} ��Ʈ");
                selectedLine = 3;
            }


            /*
             * �ճ�Ʈ ��ġ ���� ���� �ַ��
                ��带 ��� ��ũ���� �ø��ų� ������
                ������ ������ ��尡 ����ö���ų� ������..
             */
            if (longNoteMakingCount == 0)
                headTemp = hit.transform; // head ����س��ٰ� Ȱ��


            selectedGridPosition = new Vector3(NoteGenerator.Instance.linePos[selectedLine], y, 0f);
            cursorObj.transform.position = selectedGridPosition;

            isDispose = true;
        }
    }

    /// <summary>
    /// �����̽� - ���/�Ͻ�����( Space - Play/Puase )
    /// </summary>
    public void Space()
    {
        Editor.Instance.Play();
    }

    /// <summary>
    /// ��Ŭ�� - ��Ʈ ��ġ ( Mouse leftBtn - Dispose note )
    /// ��Ŭ�� - ��Ʈ ���� ( Mouse rightBtn - Cancel note )
    /// </summary>
    /// <param name="btnName"></param>
    public void MouseBtn(string btnName)
    {
        if (btnName == "leftButton")
        {
            if (selectedNoteObject != null)
            {
                Debug.Log("��Ʈ�� �̹� �����մϴ�");
            }
            if (isDispose)
            {
                if (isLongNoteActive)
                {
                    if (longNoteMakingCount == 0)
                    {
                        lastSelectedGridPosition = selectedGridPosition;

                        NoteGenerator.Instance.DisposeNoteLong(longNoteMakingCount, new Vector3[] { lastSelectedGridPosition, selectedGridPosition });

                        longNoteMakingCount++;
                    }
                    else if (longNoteMakingCount == 1)
                    {
                        Vector3 tailPositon = selectedGridPosition;
                        tailPositon.x = lastSelectedGridPosition.x; // �ճ�Ʈ�� �缱���� �ۼ��� �� �����Ƿ�, �ٸ� ����(x)�� �� ������ ������ ��ġ�� ����
                        lastSelectedGridPosition.y = headTemp.TransformDirection(headTemp.transform.position).y;

                        // tail�� head���� ���� ��ġ���� ��� �������־����
                        if (lastSelectedGridPosition.y < tailPositon.y)
                        {
                            NoteGenerator.Instance.DisposeNoteLong(longNoteMakingCount, new Vector3[] { lastSelectedGridPosition, tailPositon });
                        }
                        else
                        {
                            NoteGenerator.Instance.DisposeNoteLong(longNoteMakingCount, new Vector3[] { tailPositon, lastSelectedGridPosition });
                        }

                        longNoteMakingCount = 0;
                    }
                }
                else if (isShortNoteActive)
                {
                    NoteGenerator.Instance.DisposeNoteShort(NoteType.Short, selectedGridPosition );
                }
            }
        }
        else if (btnName == "rightButton")
        {
            if (selectedNoteObject != null)
            {
                //Debug.Log("��Ʈ ����");
                if (isLongNoteActive)
                {
                    // long�� �θ� ã�Ƽ� ��Ȱ��ȭ
                    selectedNoteObject.transform.parent.gameObject.SetActive(false);
                }
                else if (isShortNoteActive)
                {
                    selectedNoteObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// ���콺�� - ���� �� �׸��� ��ġ �̵� ( Mouse wheel - Move music and grids pos )
    /// </summary>
    /// <param name="value"></param>
    public void Scroll(float value)
    {
        scrollValue = value;

        // ��ũ�� �� �ش� ������ŭ �̵� (��Ʈ��Ű�� �Էµ����ʾ�������)
        if (!isCtrl)
        {
            float snap = Editor.Instance.Snap;
            if (scrollValue > 0)
            {
                Editor.Instance.objects.transform.position += Vector3.down * snap * 0.25f;         
                AudioManager.Instance.MovePosition(GameManager.Instance.sheets[GameManager.Instance.title].BeatPerSec * 0.001f * snap);
                //Debug.Log(GameManager.Instance.sheets[GameManager.Instance.title].BeatPerSec * 0.001f * snap);
            }
            else if (scrollValue < 0)
            {
                Editor.Instance.objects.transform.position += Vector3.up * snap * 0.25f;
                AudioManager.Instance.MovePosition(-GameManager.Instance.sheets[GameManager.Instance.title].BeatPerSec * 0.001f * snap);
                //Debug.Log(-GameManager.Instance.sheets[GameManager.Instance.title].BeatPerSec * 0.001f * snap);
            }
        }
    }

    /// <summary>
    /// ��Ʈ�� + ���콺�� - �׸��� ���� ���� ( Ctrl + Mouse wheel - Change snap of grids )
    /// </summary>
    public void Ctrl()
    {
        if (coCtrl != null)
        {
            StopCoroutine(coCtrl);
        }
        coCtrl = StartCoroutine(IEWaitMouseWheel());
    }

    IEnumerator IEWaitMouseWheel()
    {
        while (isCtrl)
        {
            if (scrollValue > 0)
            {
                // ������
                Editor.Instance.Snap /= 2;
                GridSnapListener.Invoke(Editor.Instance.Snap);
            }
            else if (scrollValue < 0)
            {
                // �����ٿ�
                Editor.Instance.Snap *= 2;
                GridSnapListener.Invoke(Editor.Instance.Snap);
            }
            scrollValue = 0;

            yield return null;
        }
    }

    //private void OnGUI()
    //{
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 36;
    //    GUI.Label(new Rect(100, 100, 100, 100), "Mouse Pos : " + inputManager.mousePos.ToString(), style);
    //    GUI.Label(new Rect(100, 200, 100, 100), "ScreenToWorld : " + worldPos.ToString(), style);
    //    GUI.Label(new Rect(100, 300, 100, 100), "CurrentBar : " + Editor.Instance.currentBar.ToString(), style);
    //    GUI.Label(new Rect(100, 400, 100, 100), "Snap : " + Editor.Instance.Snap.ToString(), style);
    //}
}
