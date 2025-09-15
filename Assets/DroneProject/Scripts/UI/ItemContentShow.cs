using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemContentShow : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txtId;
    [SerializeField] TextMeshProUGUI txtV;
    [SerializeField] TextMeshProUGUI txtD;
    [SerializeField] TextMeshProUGUI txtHuong;
    [SerializeField] TextMeshProUGUI txtTarget;
    [SerializeField] Image imgBg;

    [SerializeField] Color[] colorsShow;
    public void SetUp(InforDrone drone)
    {
        txtId.text = drone.id + "";
        txtV.text = $"{drone.v.magnitude: .0}";
        txtD.text = $"{drone.distance: .0}";
        txtHuong.text = $"({drone.huong.x: .0}, {drone.huong.y: .0}, {drone.huong.z: .0})";
        txtTarget.text = $"{drone.target.x: .0}, {drone.target.y: .0}, {drone.target.z: .0})";
        imgBg.color = colorsShow[(int)drone.state];
    }
}
