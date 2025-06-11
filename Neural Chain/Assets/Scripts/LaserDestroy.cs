using UnityEngine;

public class LaserDestroy : MonoBehaviour
{
    public int damageAmount = 1;

    // Start is called before the first frame update
    private float speed = 100f;

    void Start()
    {
        Destroy(gameObject, 3f);
    }

    private void FixedUpdate()
    {
        transform.position = transform.position + -transform.up * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("PlayerAgent") && gameObject.tag == "LaserEnemy") || (other.CompareTag("EnemyAgent") && gameObject.tag == "Laser"))
        {
            var agent = other.GetComponent<Agent>();
            if (agent != null)
            {
                agent.TakeDamage(damageAmount);
            }

            Destroy(gameObject);
        }

        //Destroy if hits wall
        else if (other.CompareTag("Obstacle")) Destroy(gameObject);
    }
}