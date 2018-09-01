using UnityEngine;
using System.Collections;

public class BallSetap : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider col)
    {
        Vector3 pos = col.transform.forward;
        Vector3 newVector = (pos).normalized;
        GetComponent<Rigidbody>().velocity = newVector * 30;
        this.GetComponent<Collider>().isTrigger = false;
    }
}

