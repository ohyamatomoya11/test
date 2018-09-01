using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyScript : MonoBehaviour {


    void OnTriggerEnter(Collider col)
    {
               Destroy(col.gameObject);
          } 
 }
