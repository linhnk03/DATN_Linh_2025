using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Hungarian
{
    static List<bool> indexColCheck;
    static List<bool> indexRowCheck;
    static List<Vector2> indexChoose = new List<Vector2>();
    public static void SetUpHungarian(List<Data> typeShape,ref List<Transform> drones)
    {
        List<Vector3> targets = new List<Vector3>();
        List<Color> colors = new List<Color>();
        for (int j = 0; j < typeShape.Count; j++)
        {
            targets.Add(typeShape[j].positions[0]);
        }
        float[,] matrix = InitPrice(targets, drones);
        //{ {80, 120, 125, 140 },
        //                   {20, 115, 145, 60 },
        //                   {40, 100, 85, 45 },
        //                   {65, 35, 25, 75 } };
        int n = drones.Count;
        //ShowMatrix(matrix, n);

        matrix = SubMinRowValue(matrix, n);
        matrix = SubMinColValue(matrix, n);

        bool isStop = false;
        while (!isStop)
        {
            isStop = true;
            indexColCheck = new List<bool>(new bool[n]);
            indexRowCheck = new List<bool>(new bool[n]);
            indexChoose.Clear();

            SelectRowSolution(matrix, n, ref indexColCheck, ref indexChoose);
            SelectColSolution(matrix, n, ref indexColCheck, ref indexRowCheck, ref indexChoose);

            if (indexChoose.Count < n)
            {
                isStop = false;
                matrix = Adjust(matrix, n, ref indexColCheck, ref indexRowCheck);
            }
        }
        //indexChoose.Sort((a, b) => ((int)a.y).CompareTo((int)b.y));
        //for (int i = 0; i < drones.Count; i++)
        //{
        //    drones[(int)indexChoose[i].x].GetComponent<Drone>().SetTask(targets[(int)indexChoose[i].y], colors[(int)indexChoose[i].y]);
        //    drones[(int)indexChoose[i].x].GetComponent<Drone>().SetColor(Color.white);
        //}
        for (int k = 0; k < indexChoose.Count; k++)
        {
            int targetIndex = (int)indexChoose[k].x;
            int droneIndex = (int)indexChoose[k].y;
            var drone = drones[droneIndex].GetComponent<Drone>();
            drone.SetTask(typeShape[targetIndex]);
            drone.SetColor(Color.white);
        }
    }
    static void ShowMatrix(float[,] matrix, int n)
    {
        string sCheck = "";
        for (int i = 0; i < n; i++)
        {
            string s = "Line: " + i + " | ";
            for (int j = 0; j < n; j++)
            {
                s += $"{matrix[i, j]:000} |";
            }
            sCheck += s + "\n";
        }
        Debug.Log(sCheck);
    }
    static float[,] InitPrice(List<Vector3> listTarget, List<Transform> drones) // B1
    {
        int n = listTarget.Count;
        float[,] matrix = new float[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                float d = Vector3.Distance(listTarget[i], drones[j].position);
                matrix[i, j] = d;
            }
        }
        return matrix;
    }
    static float[,] SubMinRowValue(float[,] matrix, int n) // B2
    {
        for (int i = 0; i < n; i++)
        {
            int indexMin = 0;
            for (int j = 1; j < n; j++)
            {
                if (matrix[i, j] < matrix[i, indexMin])
                {
                    indexMin = j;
                }
            }
            float minValue = matrix[i, indexMin];
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] -= minValue;
            }
        }

        //ShowMatrix(matrix, n);
        return matrix;
    }
    static float[,] SubMinColValue(float[,] matrix, int n) // B3
    {
        for (int i = 0; i < n; i++)
        {
            int indexMin = 0;
            for (int j = 1; j < n; j++)
            {
                if (matrix[j, i] < matrix[indexMin, i])
                {
                    indexMin = j;
                }
            }
            float minValue = matrix[indexMin, i];
            for (int j = 0; j < n; j++)
            {
                matrix[j, i] -= minValue;
            }
        }
        //ShowMatrix(matrix, n);
        return matrix;
    }
    static void SelectRowSolution(float[,] matrix, int n, ref List<bool> indexColCheck, ref List<Vector2> indexChoose)  // B4.1
    {
        bool isStop = false;
        while (!isStop)
        {
            isStop = true;
            for (int i = 0; i < n; i++)
            {
                int indexCheck = -2;
                for (int j = 0; j < n; j++)
                {
                    if (matrix[i, j] == 0 && !indexColCheck[j])
                    {
                        if (indexCheck == -2)
                            indexCheck = j;
                        else
                            indexCheck = -1;
                    }
                }
                if (indexCheck > -1)
                {
                    isStop = false;
                    indexColCheck[indexCheck] = true;
                    indexChoose.Add(new Vector2(i, indexCheck));
                }
            }
        }
    }
    static void SelectColSolution(float[,] matrix, int n, ref List<bool> indexColCheck, ref List<bool> indexRowCheck, ref List<Vector2> indexChoose)  // B4.2
    {
        bool isStop = false;
        while (!isStop)
        {
            isStop = true;
            for (int i = 0; i < n; i++)
            {
                if (indexColCheck[i])
                    continue;
                int indexCheck = -2;
                for (int j = 0; j < n; j++)
                {
                    if (matrix[j, i] == 0 && !indexRowCheck[j])
                    {
                        if (indexCheck == -2)
                            indexCheck = j;
                        else
                            indexCheck = -1;
                    }
                }
                if (indexCheck > -1)
                {
                    isStop = false;
                    indexRowCheck[indexCheck] = true;
                    indexChoose.Add(new Vector2(indexCheck, i));
                }
            }
        }
    }
    static float[,] Adjust(float[,] matrix, int n, ref List<bool> indexColCheck, ref List<bool> indexRowCheck) // B5
    {
        // B1: Tìm giá trị min trong các số chưa gạch
        float minValue = -1;
        for (int i = 0; i < n; i++)
        {
            if (indexRowCheck[i])
                continue;
            for (int j = 0; j < n; j++)
            {
                if (indexColCheck[j])
                    continue;
                if (minValue == -1 || matrix[i, j] < minValue)
                    minValue = matrix[i, j];
            }
        }
        // B2: các số gạch 1 lần giữ nguyên, gạch 2 lần + minValue, còn lại - minValue
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (indexRowCheck[i] && indexColCheck[j])
                {
                    matrix[i, j] += minValue;
                }
                else if (!indexRowCheck[i] && !indexColCheck[j])
                    matrix[i, j] -= minValue;
            }
        }
        return matrix;
    }
}
