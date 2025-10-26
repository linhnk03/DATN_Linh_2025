using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DroneManager : MonoBehaviour
{
    public float speedDrone;
    //public float interpolationSpeedDrone;
    public float moveTargetImportance;
    public float conflictDistanceImportance;
    public float distanceCheckDrone;
    public float distanceMoveTarget;
    public float localReassignRadius = 5f; // giống ρo trong bài báo
    public bool useLocalOptimal;

    [HideInInspector]
    public List<Transform> drones = new List<Transform>();
    public List<Drone> droneList = new List<Drone>();

    [SerializeField] List<GameObject> shapeList = new List<GameObject>();
    
    [SerializeField] int indexTask = 0;
    int nCheckShow = 0;
    public static DroneManager instance;
    public int IndexFrame = 0;
    List<List<Data>> datas = new List<List<Data>>();

    public UI_Controller uiController;
    List<int> checkState = new List<int>();
    bool isEnd = false;

    Dictionary<int, float> nextTransitionSpeeds = new Dictionary<int, float>();

    private void Awake()
    {
        indexTask = -1;
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < shapeList.Count; i++)
        {
            //shapeList[i].transform.GetComponent<TypeShape>().ExportFile();
            datas.Add(DataGame.ReadFile(shapeList[i].transform.GetComponent<TypeShape>()._name));
        }
        for(int i=0; i<DataGame.datas.Count; i++)
            datas.Insert(datas.Count - 1, DataGame.datas[i]);
        //return;   
        useLocalOptimal = PlayerPrefs.GetInt("useLocalOptimal", 0) == 0;
        StartCoroutine(SetUp(6));
    }
    public Vector3 DirSupervisoryDrone(int i)
    {
        Vector3 dir = Vector3.zero;
        for(int j=0; j<drones.Count; j++)
        {
            if(j != i)
            {
                float d = Vector3.Distance(drones[i].position, drones[j].position);
                if(d < distanceCheckDrone)
                {
                    dir += (drones[i].position - drones[j].position);
                }
            }
        }
        return dir.normalized* conflictDistanceImportance;
    }
    public Vector3 ComputeRepulsiveForce(int i)
    {
        Vector3 force = Vector3.zero;
        Vector3 pi = drones[i].position;

        for (int j = 0; j < drones.Count; j++)
        {
            if (j == i) continue;

            Vector3 pj = drones[j].position;
            float d = Vector3.Distance(pi, pj);

            if (d > 0f && d < distanceCheckDrone)
            {
                // công thức: k_rep * (1/d - 1/d0) * 1/d^2
                float term = (1f / d - 1f / distanceCheckDrone);
                float magnitude = term / (d * d);

                Vector3 dir = (pi - pj).normalized;
                force += dir * magnitude;
            }
        }

        return force;
    }
    // helper để Drone lấy speed khi bắt đầu interpolation
    public float GetTransitionSpeedForDrone(int droneId, int frameIndex)
    {
        if (nextTransitionSpeeds != null && nextTransitionSpeeds.TryGetValue(droneId, out var v))
            return Mathf.Max(0.01f, v); // bảo đảm > 0
        // fallback: dùng speedDrone làm mặc định
        return Mathf.Max(0.01f, speedDrone);
    }

    // Replace existing ShowDrone with this implementation
    public void ShowDrone(int id)
    {
        if (checkState.Contains(id)) return;
        checkState.Add(id);
        nCheckShow++;
        if (nCheckShow == drones.Count)
        {
            checkState.Clear();
            nCheckShow = 0;

            if (indexTask < 0 || indexTask >= datas.Count || datas[indexTask] == null || datas[indexTask].Count == 0)
            {
                Debug.LogWarning("ShowDrone: invalid current task data.");
                isEnd = true;
                return;
            }

            int currentFrameCount = datas[indexTask]
                .Where(d => d != null && d.positions != null)
                .Select(d => d.positions.Count)
                .DefaultIfEmpty(0)
                .Min();

            if (currentFrameCount == 0)
            {
                Debug.LogWarning("ShowDrone: no frames found in current task.");
                isEnd = true;
                return;
            }

            if (IndexFrame >= currentFrameCount - 1)
            {
                if (indexTask < datas.Count - 1)
                {
                    StartCoroutine(SetUpHungarian(2));
                }
                else
                {
                    isEnd = true;
                }
            }
            else
            {
                int nextFrame = IndexFrame + 1;
                nextTransitionSpeeds.Clear();

                var distances = new List<float>(drones.Count);
                for (int i = 0; i < drones.Count; i++)
                {
                    float dist = 0f;
                    try
                    {
                        var drone = drones[i].GetComponent<Drone>();
                        if (drone != null && drone.transTarget != null && drone.transTarget.positions != null && nextFrame < drone.transTarget.positions.Count)
                        {
                            Vector3 offset = drone.transTarget.positions[nextFrame];
                            dist = offset.magnitude;
                        }
                    }
                    catch { dist = 0f; }
                    distances.Add(dist);
                }

                float maxDist = distances.Count > 0 ? distances.Max() : 0f;

                float baselineDuration = (maxDist <= 1e-5f) ? 0f : maxDist / Mathf.Max(0.01f, speedDrone);

                for (int i = 0; i < drones.Count; i++)
                {
                    float d = distances[i];
                    float speedForDrone;
                    if (baselineDuration <= 0f)
                    {
                        speedForDrone = Mathf.Max(0.01f, speedDrone);
                    }
                    else
                    {
                        speedForDrone = (d <= 1e-5f) ? Mathf.Max(0.01f, speedDrone) : (d / baselineDuration);
                    }

                    int droneId = i;
                    var dComp = drones[i].GetComponent<Drone>();
                    if (dComp != null && dComp.inforDrone != null && dComp.inforDrone.id >= 0)
                        droneId = dComp.inforDrone.id;

                    nextTransitionSpeeds[droneId] = speedForDrone;
                }

                IndexFrame++;
            }
        }
    }
    IEnumerator SetUp(float delay)
    {
        yield return new WaitForSeconds(delay);
        int n = transform.childCount;
        for (int i = 0; i < n; i++)
        {
            drones.Add(transform.GetChild(i));
            droneList.Add(transform.GetChild(i).GetComponent<Drone>());
            droneList[i].SetValue(i, useLocalOptimal);
        }
        StartCoroutine(SetUpHungarian(0));
# if UNITY_EDITOR
        StartCoroutine(CheckProfiler(0.5f));
#endif
    }
    IEnumerator SetUpHungarian(float delay)
    {
        yield return new WaitForSeconds(delay);
        indexTask++;
        IndexFrame = 0;
        Hungarian.SetUpHungarian(datas[indexTask], ref drones);
    }
    public bool RequestLocalReassignment(Drone triggerDrone)
    {
        // B1: Tìm các drone xung quanh triggerDrone trong bán kính ρo
        List<Transform> nearbyDrones = drones.Where(d =>
            d != null &&
            Vector3.Distance(d.position, triggerDrone.transform.position) < localReassignRadius
        ).ToList();

        // B2: Lấy danh sách mục tiêu đang gán cho các drone này
        List<Data> subTargets = new List<Data>();
        foreach (var d in nearbyDrones)
        {
            var drone = d.GetComponent<Drone>();
            if (drone.transTarget != null)
                subTargets.Add(drone.transTarget);
        }
        if (subTargets.Count < 2) return false;
        Debug.Log(subTargets.Count);
        // B3: Áp dụng lại Hungarian nhưng chỉ cho nhóm nhỏ
        Hungarian.SetUpHungarian(subTargets, ref nearbyDrones);
        return true;
    }

    public IEnumerator CheckProfiler(float timeDelay)
    {
        int n = PlayerPrefs.GetInt("NumberCheck", 0);
        WaitForSeconds wait = new WaitForSeconds(timeDelay);

        // Lưu vị trí và trạng thái
        var posSamples = new List<List<Vector3>>();
        var stateSamples = new List<List<STATE_DRONE>>();

        // Thu thập mẫu cho đến khi isEnd = true
        while (!isEnd)
        {
            posSamples.Add(drones.Select(d => d.position).ToList());
            stateSamples.Add(drones.Select(d => d.GetComponent<Drone>().inforDrone.state).ToList());
            yield return wait;
        }

        int M = posSamples.Count;
        int N = drones.Count;
        var sb = new StringBuilder();

        // Header: time, then for each drone: x,y,z,state
        sb.Append("time");
        for (int i = 0; i < N; i++)
            sb.Append($",d{i}_x,d{i}_y,d{i}_z,d{i}_state");
        sb.AppendLine();

        // Rows
        for (int s = 0; s < M; s++)
        {
            float t = s * timeDelay;
            sb.Append(t.ToString("F3"));

            var posSnap = posSamples[s];
            var stateSnap = stateSamples[s];

            // Ghi x,y,z,state cho mỗi drone
            for (int i = 0; i < N; i++)
            {
                var p = posSnap[i];
                var st = stateSnap[i];
                sb.Append($",{p.x:F4},{p.y:F4},{p.z:F4},{st}");
            }

            sb.AppendLine();
        }

        // Ghi file CSV
        string path = Path.Combine(Application.dataPath, "Resources", "drone_profiler_coords_states" + (useLocalOptimal ? "local" : "") + n.ToString() + ".csv");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[Profiler] Exported {M} samples of coords+state to: {path}");

        PlayerPrefs.SetInt("NumberCheck", PlayerPrefs.GetInt("NumberCheck", 0) + 1);
        if(PlayerPrefs.GetInt("NumberCheck", 0) == 5)
        {
            PlayerPrefs.SetInt("NumberCheck", 0);
            PlayerPrefs.SetInt("useLocalOptimal", PlayerPrefs.GetInt("useLocalOptimal", 0) == 1 ? 0 : 1);
        }
        SceneManager.LoadScene(ValueConst.NAME_SCENE_PLAY);
    }


}
