using UnityEngine;

public class FieldItem : MonoBehaviour, IPoolable
{
    public ItemTemplete.Item myItem;
    private bool canCollect = true;

    private void OnTriggerEnter(Collider other)
    {
        if (canCollect && other.CompareTag("Player"))
        {
            UIManager.Instance.ShowPickupPopup(this);
        }
    }

    public void Collect()
    {
        if (!canCollect)
        {
            return;
        }

        canCollect = false;
        PoolManager.Instance.Return(this);
    }

    public void OnPoolSpawned()
    {
        canCollect = true;
    }

    public void OnPoolDespawned()
    {
        canCollect = false;
    }
}
