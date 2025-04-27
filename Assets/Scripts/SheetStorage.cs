using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SheetStorage : MonoBehaviour
{

    void Start()
    {

    }

    public void Save()
    {
        Sheet sheet = GameManager.Instance.sheets[GameManager.Instance.title];

        List<Note> notes = new List<Note>();
        string noteStr = string.Empty;
        float baseTime = sheet.BarPerSec / 16;
        foreach (NoteObject note in NoteGenerator.Instance.toReleaseList)
        {
            if (!note.gameObject.activeSelf) 
                continue;

            float line = note.transform.position.x;
            int findLine = 0;
            if (line < -1f && line > -2f)
            {
                findLine = 0;
            }
            else if (line < 0f && line > -1f)
            {
                findLine = 1;
            }
            else if (line < 1f && line > 0f)
            {
                findLine = 2;
            }
            else if (line < 2f && line > 1f)
            {
                findLine = 3;
            }

            if (note is NoteShort)
            {
                NoteShort noteShort = note as NoteShort;
                int noteTime = (int)(noteShort.transform.localPosition.y * baseTime * 1000) + sheet.offset;

                notes.Add(new Note(noteTime, (int)NoteType.Short, findLine + 1, -1));
                //noteStr += $"{noteTime}, {(int)NoteType.Short}, {findLine + 1}\n";
            }
            else if (note is NoteLong)
            {
                NoteLong noteLong = note as NoteLong;
                int headTime = (int)(noteLong.transform.localPosition.y * baseTime * 1000) + sheet.offset;
                int tailTime = (int)((noteLong.transform.localPosition.y + noteLong.tail.transform.localPosition.y) * baseTime * 1000) + sheet.offset;

                notes.Add(new Note(headTime, (int)NoteType.Long, findLine + 1, tailTime));
                //noteStr += $"{headTime}, {(int)NoteType.Long}, {findLine + 1}, {tailTime}\n";
            }
        }

        notes = notes.OrderBy(a => a.time).ToList();

        foreach (Note n in notes)
        {
            switch (n.type)
            {
                case (int)NoteType.Short:
                    noteStr += $"{n.time}, {n.type}, {n.line}\n";
                    break;
                case (int)NoteType.Long:
                    noteStr += $"{n.time}, {n.type}, {n.line}, {n.tail}\n";
                    break;
            }
        }


        string writer = $"[Description]\n" +
            $"Title: {sheet.title}\n" +
            $"Artist: {sheet.artist}\n\n" +
            $"[Audio]\n" +
            $"BPM: {sheet.bpm}\n" +
            $"Offset: {sheet.offset}\n" +
            $"Signature: {sheet.signature[0]}/{sheet.signature[1]}\n\n" +
            $"[Note]\n" +
            $"{noteStr}";

        writer.TrimEnd('\r', '\n');

        string pathSheet = $"{Application.dataPath}/Sheet/{sheet.title}/{sheet.title}.sheet";
        if (File.Exists(pathSheet))
        {
            try
            {
                File.Delete(pathSheet);
            }
            catch (IOException e)
            {
                Debug.LogError(e.Message);
                return;
            }
        }

        if (!File.Exists(pathSheet))
        {
            using (FileStream fs = File.Create(pathSheet))
            {

            }
        }
        else
        {
            Debug.LogError($"{sheet.title}.sheet");
            return;
        }

        using (StreamWriter sw = new StreamWriter(pathSheet))
        {
            sw.Write(writer);
        }
    }
}
