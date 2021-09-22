using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemy : Enemy
{
    LaserPooler laserPooler;
    [SerializeField] private Transform firePoint;
    public float accuracy;
    protected override void Awake()
    {
        base.Awake();
        laserPooler = GameObject.FindObjectOfType<LaserPooler>();
    }
    protected override bool Attack(Vector3 normalizedDirection)
    {
        if(!base.Attack(normalizedDirection))
            return false;
        
        LaserProjectile proj = laserPooler.pool.Get();
        Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, UnityEngine.Random.Range(-15f * (1f - accuracy), 15f * (1f - accuracy)));

        proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.Euler(eulerRot));
        proj.Fire(transform, damage);

        return true;
    }
}