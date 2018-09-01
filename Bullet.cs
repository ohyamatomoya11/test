using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{

    public float timer = 3;

    void Update()
    {
        Destroy(gameObject, timer);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "Enemy1")
        {
            col.gameObject.SendMessage("Damage");
        }
        Destroy(gameObject);
    }
}
