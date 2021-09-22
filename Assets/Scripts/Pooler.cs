using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

public class Pooler<T> : MonoBehaviour where T : MonoBehaviour, IPoolable
{
    [SerializeField] protected T prefab;
    public bool collectionChecks = true;
    public int maxPoolSize = 10;
    protected IObjectPool<T> _pool;

    public IObjectPool<T> pool {get{
        if(_pool == null)
            _pool = new ObjectPool<T>(CreatePooledItem, OnTakeFromPool, OnReturnToPool, OnDestroyPoolObject, collectionChecks, 10, maxPoolSize);
        return _pool;
    } set{}}

    protected virtual T CreatePooledItem()
    {
        T newItem = Instantiate(prefab);
        newItem.inPool = true;
        // newItem.gameObject.SetActive(false);
        return newItem;
    }

    protected virtual void OnTakeFromPool(T item)
    {
        item.inPool = false;
        item.gameObject.SetActive(true);
    } 
    protected virtual void OnReturnToPool(T item)
    {
        item.transform.position = Vector3.zero;
        item.gameObject.SetActive(false);
    }

    protected virtual void OnDestroyPoolObject(T item) => Destroy(item.gameObject);
}

public class ReturnToPool<T> : MonoBehaviour where T : MonoBehaviour, IPoolable
{
    T item;
    [ShowInInspector] public IObjectPool<T> pool;
    private void Start() {
        item = GetComponent<T>();
        item.onReturnToPool += Return;
    }
    void Return()
    {
        if(!item.inPool)
        {
            item.inPool = true;
            pool.Release(item);
        }
    }
}