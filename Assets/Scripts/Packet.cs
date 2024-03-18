using System;
using UnityEngine;

[Serializable]
public class Packet
{
    [SerializeField] public int event_type;
    [SerializeField] public float position;
    [SerializeField] public string state;
    [SerializeField] public float position_red;
    [SerializeField] public float position_blue;
    [SerializeField] public string mode;
}