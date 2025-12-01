using UnityEngine;

// Agrega este script automáticamente a tus enemigos en el Pool
public class PoolableObject : MonoBehaviour
{
    private EnemyPool myPool;

    public void Setup(EnemyPool pool)
    {
        myPool = pool;
    }

    // Unity llama a esto automáticamente cuando el objeto hace gameObject.SetActive(false)
    void OnDisable()
    {
        if (myPool != null)
        {
            // Le decimos al pool: "Ya me apagué (terminó mi animación), recíclame"
            myPool.ReturnEnemyToPool(this.gameObject);
        }
    }
}