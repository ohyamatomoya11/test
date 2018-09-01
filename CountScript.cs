using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class CountScript : MonoBehaviour {
    int score = 30; //初期値を30に変更  	
    private Text uScore;
    public GameObject GameOver;
 	void Start()
    {
             uScore = GameObject.FindGameObjectWithTag("Score").GetComponent<Text>();
              uScore.text = "Score: " + score.ToString();
          } 
    void Update()
    {
        if(score <= 0)
        {
            GameOver.SendMessage("Lose");
        }
    }
 	 
 	void CountUp()
    {
             score += 3;
             uScore.text = "Score: " + score.ToString();
          } 
    void CountDown()
    {
        score -= 1;
        uScore.text = "Score: " + score.ToString();
    }
 }
