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

    public InforDrone inforDrone {get; private set;}
    
    Rigidbody rb;
    Light light;
    bool showLight = false;
    private const float deadZoneAngle = 150f;
    private const float tangentKick = 100f;
    private const float timeKickOff = 1f;
    float _timeKickOff = 0;
    float timeReset;
    Color colorShow;

    bool useLocalOptimal = false;

    private void Awake()
    {
        inforDrone = new InforDrone(-1);
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        light = GetComponentInChildren<Light>();
        light.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(inforDrone.id == -1 || transTarget == null) return;
        if(timeReset > 0) timeReset -= Time.deltaTime;
        if (DroneManager.instance.indexFrame == 0)
        {
            Vector3 dir = transTarget.positions[DroneManager.instance.indexFrame] - transform.position;
            inforDrone.distance = dir.magnitude;
            float k = Mathf.Clamp(inforDrone.distance / DroneManager.instance.distanceMoveTarget, 0, 1);

            Vector3 dirDroneCheck = DroneManager.instance.DirSupervisoryDrone(inforDrone.id);
            inforDrone.huong = dir.normalized * DroneManager.instance.moveTargetImportance + dirDroneCheck;

            if (_timeKickOff > 0 && !useLocalOptimal)
            {
                //inforDrone.huong += new Vector3(0, 0, tangentKick);
                _timeKickOff -= Time.deltaTime;
                if (_timeKickOff <= 0)
                {
                    inforDrone.state = STATE_DRONE.FOLLOW;
                }
            }
            inforDrone.v = inforDrone.huong.normalized * DroneManager.instance.speedDrone * k;
            if (k < 0.1f && !showLight)
            {
                inforDrone.state = STATE_DRONE.COMPLED;
                ShowLight();
                DroneManager.instance.ShowDrone(inforDrone.id);
            }
            rb.velocity = inforDrone.v;
        }
        else
        {
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            transform.position += transTarget.positions[DroneManager.instance.indexFrame];
            DroneManager.instance.ShowDrone(inforDrone.id);
        }
    }
    public void SetValue(int id, bool isLocal)
    {
        useLocalOptimal = isLocal;
        inforDrone = new InforDrone(id); 
        light.gameObject.SetActive(true);
    }
    public void SetTask(Data data)
    {
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
        light.color = colorShow;
    }
    public void SetColor(Color x)
    {
        light.color = x;
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.transform.CompareTag("Drone"))
        {
            // only consider collisions with other drones when following
            if (timeReset > 0f || (_timeKickOff > 0 && !useLocalOptimal) || inforDrone.state != STATE_DRONE.FOLLOW || collision.transform.GetComponent<Drone>().inforDrone.state == STATE_DRONE.ERRO) return;

            Vector3 attractDir = inforDrone.huong.normalized;                           // intended flight
            Vector3 repelDir = (transform.position - collision.transform.position)
                                 .normalized;                                         // away from the other drone

            float angle = Vector3.Angle(attractDir, repelDir);
            if (angle >= deadZoneAngle)
            {
                _timeKickOff = timeKickOff;
                inforDrone.state = STATE_DRONE.ERRO;
                if(useLocalOptimal)
                    DroneManager.instance.RequestLocalReassignment(this);
            }
        }
    }

}
