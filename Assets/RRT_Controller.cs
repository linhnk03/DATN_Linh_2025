using System.Collections.Generic;
using UnityEngine;

public class RRT_Controller : MonoBehaviour
{
    [SerializeField] private float targetRadius = 0.1f;
    [SerializeField] private float explorationBias = 0.4f;
    [SerializeField] private float delta = 0.1f;
    [SerializeField] private int maxNodes = 500;

    [SerializeField] private Transform transTarget;
    private List<Vector3> nodes = new List<Vector3>();
    private List<int> parentNodes = new List<int>();
    private Vector3 targetPoint;
    private Vector3 sourcePoint;
    private bool isRunning = false;
    private bool pathFound = false; // Biến kiểm soát xem đường đi đã được tìm thấy hay chưa

    void Start()
    {
        sourcePoint = transform.position;
        targetPoint = transTarget.position;
        nodes.Add(sourcePoint);
        parentNodes.Add(-1); // No parent for the root node
        isRunning = true;
    }

    void Update()
    {
        if (isRunning)
        {
            StepRRT();
        }
    }

    private void StepRRT()
    {
        Vector3 randPoint = GetRandomPoint(targetPoint);
        int nearestNodeIndex = GetNearestNode(randPoint);
        Vector3 nearestNode = nodes[nearestNodeIndex];

        Vector3 direction = (randPoint - nearestNode).normalized;
        Vector3 newPoint = nearestNode + direction * delta * Random.value;

        nodes.Add(newPoint);
        parentNodes.Add(nearestNodeIndex);

        if (Distance(newPoint, targetPoint) <= targetRadius)
        {
            pathFound = true; // Đánh dấu đường đi đã được tìm thấy
            nodes.Add(targetPoint);
            parentNodes.Add(nearestNodeIndex); // Liên kết đến nút gần nhất
            isRunning = false; // Dừng thuật toán
            Debug.Log("Path found!");
            
        }

        if (nodes.Count > maxNodes)
        {
            Debug.Log("Max number of nodes reached, could not find the path yet.");
            nodes.Clear();
            parentNodes.Clear();
            nodes.Add(sourcePoint);
            parentNodes.Add(-1); // No parent for the root node
            isRunning = true;
        }
    }

    private float Distance(Vector3 point1, Vector3 point2)
    {
        return Vector3.Distance(point1, point2);
    }

    private Vector3 GetRandomPoint(Vector3 targetPoint)
    {
        if (Random.value < explorationBias)
        {
            return targetPoint;
        }
        else
        {
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (targetPoint - transform.position).magnitude;
        }
    }

    private int GetNearestNode(Vector3 point)
    {
        int nearestIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < nodes.Count; i++)
        {
            float distance = Distance(point, nodes[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void OnDrawGizmos()
    {
        // Vẽ các nút
        Gizmos.color = Color.red;
        foreach (var node in nodes)
        {
            Gizmos.DrawSphere(node, 0.02f);
        }

        

        // Tô màu xanh cho tất cả các điểm trong đường đi
        if (!pathFound)
        {
            // Vẽ đường đi
            Gizmos.color = Color.yellow; // Màu mặc định cho đường đi
            for (int i = 1; i < nodes.Count; i++)
            {
                Gizmos.DrawLine(nodes[parentNodes[i]], nodes[i]);
            }
        }
        else
        {
            Gizmos.color = Color.green; // Đổi màu cho đường đi
            int currentNode = nodes.Count - 1; // Bắt đầu từ nút đích
            while (parentNodes[currentNode] != -1)
            {
                Gizmos.DrawLine(nodes[currentNode], nodes[parentNodes[currentNode]]); // Tô màu cho nút
                currentNode = parentNodes[currentNode]; // Trở về nút cha
            }
        }
    }
}