using UnityEngine;
using System;
public class LegController2D : MonoBehaviour
{
    //Target Position of the tip of right Leg
    public GameObject LegTarget;

    //Controllable Thigh And Calf Gameobjects
    public GameObject Thigh;
    public GameObject Calf;


    //joints of respective Thigh and Calf
    ConfigurableJoint ThighJoint;
    ConfigurableJoint CalfJoint;
    
    //Hitboxes of respective Thigh and Calf
    CapsuleCollider ThighCol;
    CapsuleCollider CalfCol;

    //Length of thigh and calf, plus squared
    double thighLen;
    double calfLen;
    double thighLenSqrd;
    double calfLenSqrd;

    void Start()
    {
        ThighJoint = Thigh.GetComponent<ConfigurableJoint>();
        CalfJoint = Calf.GetComponent<ConfigurableJoint>();

        ThighCol = Thigh.GetComponent<CapsuleCollider>();
        CalfCol = Calf.GetComponent<CapsuleCollider>();

        thighLen = ThighCol.bounds.size.y;
        calfLen = CalfCol.bounds.size.y;
        thighLenSqrd = Math.Pow(thighLen, 2f);
        calfLenSqrd = Math.Pow(calfLen, 2f);


    }

    void Update()
    {


    }

    
}
