using UnityEngine;
using System.Collections;

public class camerasnow2 : MonoBehaviour
{

    public GameObject player;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(-39, player.transform.position.y, player.transform.position.z);
    }
}
