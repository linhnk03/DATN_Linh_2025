using System.Collections.Generic;
using UnityEngine;

public static class Hungarian
{
    /// <summary>
    /// Giải bài toán assignment với ma trận chi phí vuông (n x n).
    /// Trả về array length n, assignment[i] = j nghĩa là row i được gán cho column j.
    /// </summary>
    public static int[] Solve(float[,] cost)
    {
        int n = cost.GetLength(0);
        // Nếu ma trận không vuông (phòng trường hợp), ta không xử lý ở đây:
        if (cost.GetLength(1) != n)
        {
            Debug.LogError("Hungarian: cost matrix must be square.");
            return null;
        }

        // Chuyển sang double để tính chính xác hơn trong thuật toán
        double[,] a = new double[n + 1, n + 1]; // 1-based
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= n; j++)
                a[i, j] = cost[i - 1, j - 1];

        double[] u = new double[n + 1];
        double[] v = new double[n + 1];
        int[] p = new int[n + 1];     // p[j] = i matched with column j
        int[] way = new int[n + 1];

        for (int i = 1; i <= n; ++i)
        {
            p[0] = i;
            int j0 = 0;
            double[] minv = new double[n + 1];
            bool[] used = new bool[n + 1];
            for (int j = 0; j <= n; ++j)
            {
                minv[j] = double.PositiveInfinity;
                used[j] = false;
            }
            do
            {
                used[j0] = true;
                int i0 = p[j0]; // current matched row
                int j1 = 0;
                double delta = double.PositiveInfinity;
                for (int j = 1; j <= n; ++j)
                {
                    if (used[j]) continue;
                    double cur = a[i0, j] - u[i0] - v[j];
                    if (cur < minv[j])
                    {
                        minv[j] = cur;
                        way[j] = j0;
                    }
                    if (minv[j] < delta)
                    {
                        delta = minv[j];
                        j1 = j;
                    }
                }

                // update potentials
                for (int j = 0; j <= n; ++j)
                {
                    if (used[j])
                    {
                        u[p[j]] += delta;
                        v[j] -= delta;
                    }
                    else
                    {
                        minv[j] -= delta;
                    }
                }
                j0 = j1;
            } while (p[j0] != 0);

            // Augmenting
            do
            {
                int j1 = way[j0];
                p[j0] = p[j1];
                j0 = j1;
            } while (j0 != 0);
        }

        // Tạo kết quả: row -> column
        int[] assignment = new int[n];
        for (int j = 1; j <= n; ++j)
        {
            if (p[j] != 0)
                assignment[p[j] - 1] = j - 1; // convert to 0-based
        }

        return assignment;
    }

    // --- Helper functions để dùng trong Unity (SetUpHungarian) ---
    static float[,] InitPrice(List<Vector3> listTarget, List<Transform> drones)
    {
        int n = Mathf.Min(listTarget.Count, drones.Count);
        float[,] matrix = new float[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                matrix[i, j] = Vector3.Distance(listTarget[i], drones[j].position);
        return matrix;
    }

    /// <summary>
    /// Updated SetUpHungarian: gọi Hungarian.Solve rồi gán task cho drone tương ứng.
    /// Lưu ý: nếu #targets != #drones thì sẽ lấy min(targets, drones).
    /// </summary>
    public static void SetUpHungarian(List<Data> typeShape, ref List<Transform> drones)
    {
        // Chuẩn bị danh sách targets
        List<Vector3> targets = new List<Vector3>();
        for (int j = 0; j < typeShape.Count; j++)
        {
            targets.Add(typeShape[j].positions[0]);
        }

        int n = Mathf.Min(targets.Count, drones.Count);
        if (n == 0)
        {
            Debug.LogWarning("SetUpHungarian: no targets or no drones.");
            return;
        }

        // Tạo ma trận chi phí (n x n) dựa trên n đã chọn (cắt bớt nếu cần)
        float[,] matrix = new float[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] = Vector3.Distance(targets[i], drones[j].position);
            }
        }

        // Gọi Hungarian
        int[] assignment = Solve(matrix);
        if (assignment == null)
        {
            Debug.LogError("Hungarian: assignment failed.");
            return;
        }

        // Gán task cho drone: assignment[row] = col -> row = target index, col = drone index
        for (int row = 0; row < n; row++)
        {
            int col = assignment[row];
            if (col >= 0 && col < drones.Count)
            {
                var drone = drones[col].GetComponent<Drone>();
                if (drone != null)
                {
                    // typeShape[row] tương ứng với target tại row
                    drone.SetTask(typeShape[row]);
                    drone.SetColor(Color.white);
                }
                else
                {
                    Debug.LogWarning($"Drone at index {col} does not have Drone component.");
                }
            }
        }
    }
}
