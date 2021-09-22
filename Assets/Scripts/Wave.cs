using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public struct Wave
{
    [FoldoutGroup("Wave")] public float maxDuration;
    [FoldoutGroup("Wave")] public UnitBurst meleeBurst;
    [FoldoutGroup("Wave")] public UnitBurst rangedBurst;
    [FoldoutGroup("Wave")] public UnitBurst rockBurst;
    [FoldoutGroup("Wave")] public bool hasBoss;
}


[System.Serializable]
public struct UnitBurst
{
    public float time;
    public int count;
    public int cycles;
    public float interval;
    public UnitBurst(float time, int count, int cycles, float interval)
    {
        this.time = time;
        this.count = count;
        this.cycles = cycles;
        this.interval = interval;
    }
}