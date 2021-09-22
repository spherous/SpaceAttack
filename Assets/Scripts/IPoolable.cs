public delegate void OnReturnToPool();

public interface IPoolable
{
    event OnReturnToPool onReturnToPool;
    bool inPool {get; set;}
}