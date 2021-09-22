public class ParticleSystemPooler : Pooler<PooledParticleSystem>
{
    public class ParticleSystemReturn : ReturnToPool<PooledParticleSystem>{}

    protected override void OnReturnToPool(PooledParticleSystem item)
    {
        item.Stop();
        item.Clear();
        base.OnReturnToPool(item);
    }

    protected override PooledParticleSystem CreatePooledItem()
    {
        PooledParticleSystem sparks = base.CreatePooledItem();
        ParticleSystemReturn sparksReturn = sparks.gameObject.AddComponent<ParticleSystemReturn>();
        sparksReturn.pool = pool;
        return sparks;
    }
}