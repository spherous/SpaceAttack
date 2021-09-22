using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : Enemy
{
    public ParticleSystemPooler burstPooler;
    public ShotgunPooler spikePooler;
    public LayerMask attackMask;
    public float attackRadius;
    public float amountOfSpikes;
    protected override bool Attack(Vector3 normalizedDirection)
    {
        if(!base.Attack(normalizedDirection))
            return false;
        
        PooledParticleSystem burst = burstPooler.pool.Get();
        burst.transform.position = transform.position;
        burst.SetSize(attackRadius * 2);

        burst.Play();

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRadius, attackMask);
        foreach(Collider2D hitCol in hitColliders)
        {
            if(hitCol.gameObject == gameObject)
                continue;
            
            if(hitCol.TryGetComponent<IHealth>(out IHealth hitHealth))
            {
                if(hitHealth is Enemy)
                    continue;
                hitHealth.TakeDamage(damage, transform);
            }
        }

        // spikes
        for(int i = 0; i < amountOfSpikes; i++)
        {
            LaserProjectile spike = spikePooler.pool.Get();
            Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, UnityEngine.Random.Range(-180f, 180f));
            spike.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(eulerRot));

            spike.Fire(transform, damage / 2f);
        }

        return true;
    }

    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}