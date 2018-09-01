using UnityEngine; 
using UnityEngine.UI; 
 using System.Collections; 
 
 
public class gameOverScript : MonoBehaviour
{
    bool gameOverflg = false; 
	 
	void Start()
    {
              this.gameObject.GetComponent<Text>().enabled = false;
          }  	 
	void Update()
    {
             if (gameOverflg == true)
        {
                     if (Input.GetMouseButtonDown(0))
            {
                              Application.LoadLevel("Coin2");
                          }
                 }
          } 
	 
	public void Lose()
    {
              this.gameObject.GetComponent<Text>().enabled = true;
              GameObject[] clones = GameObject.FindGameObjectsWithTag("Finish");
              foreach (GameObject obj in clones)
        {
                     Destroy(obj);
                 }
             gameOverflg = true;
          } 
 }
