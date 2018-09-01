using UnityEngine;
using System.Collections;

public class CubeScript : MonoBehaviour
{
    public GameObject particle;//Particleを宣言

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnCollisionEnter(Collision other)
    {
        //衝突したオブジェクトがPlayerだったらParticleを発生させる
        if (other.gameObject.name == "Player")
        {
            Instantiate(particle, transform.position, transform.rotation);
        }
    }
}