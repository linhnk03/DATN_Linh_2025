using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum STATE_DRONE
{
    NORMAIL,
    FOLLOW,
    COMPLED,
    ERRO
}
[System.Serializable]
public class InforDrone
{
    public int id = -1;
    public Vector3 v;
    public STATE_DRONE state;
    public Vector3 huong;
    public float distance;
    public Vector3 target;
    public InforDrone(int id)
    {
        this.id = id;
        v = Vector3.zero;
        state = STATE_DRONE.NORMAIL;
        huong = Vector3.zero;
        distance = 0;
    }
}
public class Drone : MonoBehaviour
{
    public Data transTarget;

    public InforDrone inforDrone { get; private set; }

    Rigidbody rb;
    Light droneLight;
    bool showLight = false;
    private const float deadZoneAngle = 150f;
    private const float tangentKick = 100f;
    private const float timeKickOff = 1f;
    float _timeKickOff = 0;
    float timeReset;
    Color colorShow;

    bool useLocalOptimal = false;

    // NEW: Inter-frame mover
    DroneFrameInterpolator frameMover;
    // NEW: remember the last frame index we already completed interpolation for
    int lastInterpolatedFrame = 0;

    private void Awake()
    {
        inforDrone = new InforDrone(-1);
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        droneLight = GetComponentInChildren<Light>();
        if (droneLight != null) droneLight.gameObject.SetActive(false);

        frameMover = GetComponent<DroneFrameInterpolator>();
    }

    void Update()
    {
        if (inforDrone.id == -1 || transTarget == null) return;
        if (timeReset > 0) timeReset -= Time.deltaTime;

        // Frame 0: keep your current physics-based steering (unchanged)
        if (DroneManager.instance.IndexFrame == 0)
        {
            Vector3 dir = transTarget.positions[DroneManager.instance.IndexFrame] - transform.position;
            inforDrone.distance = dir.magnitude;
            float k = Mathf.Clamp(inforDrone.distance / DroneManager.instance.distanceMoveTarget, 0, 1);

            Vector3 dirDroneCheck = DroneManager.instance.DirSupervisoryDrone(inforDrone.id);
            inforDrone.huong = dir.normalized * DroneManager.instance.moveTargetImportance + dirDroneCheck;

            if (_timeKickOff > 0 && !useLocalOptimal)
            {
                _timeKickOff -= Time.deltaTime;
                if (_timeKickOff <= 0)
                {
                    inforDrone.state = STATE_DRONE.FOLLOW;
                }
            }
            inforDrone.v = inforDrone.huong.normalized * DroneManager.instance.speedDrone * k;

            if (k < 0.1f && !showLight)
            {
                // snap to exact target and stop physics drift
                transform.position = transTarget.positions[0];
                rb.velocity = Vector3.zero;
                rb.useGravity = false;

                inforDrone.state = STATE_DRONE.COMPLED;
                ShowLight();
                DroneManager.instance.ShowDrone(inforDrone.id);
                // ready for next frame transitions
                lastInterpolatedFrame = 0;
            }
            rb.velocity = inforDrone.v;
        }
        else
        {
            int f = DroneManager.instance.IndexFrame;

            if (frameMover != null && !frameMover.IsMoving && f != lastInterpolatedFrame)
            {
                Vector3 offset = Vector3.zero;

                if (transTarget != null && transTarget.positions != null && f < transTarget.positions.Count)
                {
                     Vector3 absC = transTarget.positions[f];
                     Vector3 deltaC = absC - transTarget.positions[f - 1];
                     offset = (deltaC.sqrMagnitude <= absC.sqrMagnitude) ? deltaC : absC;
                }

                float speedOverride = DroneManager.instance.GetTransitionSpeedForDrone(inforDrone.id, f);

                frameMover.MoveByOffset(offset, speedOverride, () =>
                {
                    lastInterpolatedFrame = f;
                    DroneManager.instance.ShowDrone(inforDrone.id);
                });
            }
        }
    }
    public void SetValue(int id, bool isLocal)
    {
        useLocalOptimal = isLocal;
        inforDrone = new InforDrone(id);
        if (droneLight != null) droneLight.gameObject.SetActive(true);
    }
    public void SetTask(Data data)
    {
        // Cancel any ongoing inter-frame motion when a new target (or local reassignment) is applied
        if (frameMover != null) frameMover.Cancel();
        lastInterpolatedFrame = 0;

        timeReset = 1;
        rb.useGravity = true;
        transTarget = data;
        inforDrone.state = STATE_DRONE.FOLLOW;
        inforDrone.target = data.positions[0];
        showLight = false;
        colorShow = data.color;
    }
    void ShowLight()
    {
        showLight = true;
        if (droneLight != null) droneLight.color = colorShow;
    }
    public void SetColor(Color x)
    {
        if (droneLight != null) droneLight.color = x;
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.transform.CompareTag("Drone"))
        {
            if (timeReset > 0f || (_timeKickOff > 0 && !useLocalOptimal) || inforDrone.state != STATE_DRONE.FOLLOW || collision.transform.GetComponent<Drone>().inforDrone.state == STATE_DRONE.ERRO) return;

            Vector3 attractDir = inforDrone.huong.normalized;
            Vector3 repelDir = (transform.position - collision.transform.position).normalized;

            float angle = Vector3.Angle(attractDir, repelDir);
            if (angle >= deadZoneAngle)
            {
                _timeKickOff = timeKickOff;
                inforDrone.state = STATE_DRONE.ERRO;
                if (useLocalOptimal)
                    DroneManager.instance.RequestLocalReassignment(this);
            }
        }
    }

}