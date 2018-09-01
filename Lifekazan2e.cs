using UnityEngine;
using System.Collections;

public class Lifekazan2e : MonoBehaviour
{

	public readonly float maxLife = 100;    //最大体力（readonlyは変数の変更ができなくなる）
	public float life = 100;    //現在体力

	// Use this for initialization
	void Start()
	{
		life = maxLife; //体力を全回復させる
	}

	// Update is called once per frame
	void Update()
	{
		if (life <= 0)
		{
			//体力が0になったら
			Dead();
		}
	}

	public void Damage(float damage)
	{
		life -= damage; //体力を減らす
	}

	public void LifeUp(float damage)
	{
		life += damage;//体力を増やす
	}

	//死亡処理（死亡時の演出）
	public void Dead()
	{
		GameOver(); //ゲームオーバーにする
	}

	//ゲームオーバー処理
	public void GameOver()
	{
		Cursor.visible = true;
		Application.LoadLevel("Gameoverkazan2e"); //シーンの再読み込み
	}

	//体力を表示

	public bool flag = true;    //trueの時体力を表示させる

    void OnGUI()
    {
        GUI.color = Color.magenta;



        if (flag)
        {
            GUIStyle style = GUI.skin.GetStyle("label");
            style.fontSize = (int)(20.0f + 5.0f * Mathf.Sin(Time.time));
            GUI.Label(new Rect(10, 40, 200, 200), "Life " + life);
        }




    }
}