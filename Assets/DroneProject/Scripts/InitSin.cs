using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// InitSin: tạo sóng sin lan truyền từ một điểm nguồn cho danh sách các đối tượng.
/// </summary>
public class InitSin : MonoBehaviour
{
    [Header("Nguồn sóng")]
    [Tooltip("Transform gốc phát sóng")]
    public Transform indexStart;

    [Header("Danh sách đối tượng")]
    [Tooltip("Danh sách các Transform sẽ dao động theo sóng")]
    public List<Transform> targets;

    [Header("Thông số sóng")]
    [Tooltip("Bước sóng (khoảng cách giữa hai đỉnh sóng liên tiếp)")]
    public float wavelength = 5f;
    [Tooltip("Biên độ sóng")]
    public float amplitude = 20f;
    [Tooltip("Tần số sóng (chu kỳ mỗi giây)")]
    public float frequency = 1f;

    // Công thức góc:
    private float omega; // 2π f
    private float k;     // 2π / wavelength
    private int total;

    bool isStop = false;
    List<List<Vector3>> positions;  
    void Start()
    {
        total = targets.Count;
        omega = 2f * Mathf.PI * frequency;
        k = 2f * Mathf.PI / wavelength;

        positions = new List<List<Vector3>>();
        // Lưu vị trí gốc của mỗi target
        for (int i = 0; i < total; i++)
        {
            positions.Add(new List<Vector3>() { targets[i].position });
        }
    }

    void Update()
    {
        if(isStop) return;  
        float t = Time.time;
        Vector3 origin = indexStart.position;

        for (int i = 0; i < total; i++)
        {
            // Khoảng cách từ nguồn sóng đến đối tượng
            float dist = positions[i][0].x - origin.x;

            // Dao động sin: y = A * sin(k * dist - ω * t)
            Vector3 yOffset = new Vector3(0, amplitude * Mathf.Sin(k * dist - omega * t), 0);

            // Cập nhật vị trí (chỉ thay đổi theo trục Y)
            Vector3 pos = targets[i].position;
            
            targets[i].position = positions[i][0] + yOffset;
            positions[i].Add(targets[i].position - pos);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Export();
        }
    }
    void Export()
    {
        isStop = true;
        string filePath = Application.dataPath + "/Resources/" + 1 + ".csv";
        var sb = new StringBuilder();
        sb.AppendLine("NumberFrame, Infor");
        for (int i=0; i< positions.Count; i++)
        {
            string s = positions[0].Count +"";
            for(int j=0; j < positions[i].Count; j++)
            {
                s += string.Format(",{0};{1};{2}", positions[i][j].x, positions[i][j].y, positions[i][j].z);
            }
            sb.AppendLine(s);
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}