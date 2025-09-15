using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class DataGame
{
    public static List<List<Data>> datas = new List<List<Data>>();

    public static void SaveData(List<TypeShape> shapeList)
    {
        datas = new List<List<Data>>();
        for (int i = 0; i < shapeList.Count; i++) 
            datas.Add(ReadFile(shapeList[i]._name));
    }
    public static List<Data> ReadFile(string path)
    {
        var result = new List<Data>();

        // Load file từ Resources (không ghi phần ".csv")
        TextAsset textAsset = Resources.Load<TextAsset>(path);
        if (textAsset == null)
        {
            Debug.LogError($"File not found in Resources: {path}");
            return result;
        }

        var lines = textAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(new[] { ',' }, 4);
            if (parts.Length < 4) continue;

            var colParts = parts[1].Split(';');
            Color color = new Color(
                float.Parse(colParts[0], CultureInfo.InvariantCulture),
                float.Parse(colParts[1], CultureInfo.InvariantCulture),
                float.Parse(colParts[2], CultureInfo.InvariantCulture),
                float.Parse(colParts[3], CultureInfo.InvariantCulture)
            );

            string[] infos = parts[3].Split(',');
            int numberFrame = int.Parse(parts[2], CultureInfo.InvariantCulture);

            var dataItem = new Data
            {
                color = color,
                positions = new List<Vector3>()
            };

            foreach (string info in infos)
            {
                var posSplit = info.Split(';');
                if (posSplit.Length < 3) continue;

                Vector3 position = new Vector3(
                    float.Parse(posSplit[0], CultureInfo.InvariantCulture),
                    float.Parse(posSplit[1], CultureInfo.InvariantCulture),
                    float.Parse(posSplit[2], CultureInfo.InvariantCulture)
                );

                dataItem.positions.Add(position);
            }

            result.Add(dataItem);
        }

        return result;
    }

}
