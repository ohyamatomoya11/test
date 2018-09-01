
using UnityEngine;
using System.Collections;

//敵による攻撃を設定する。
public class EnemyAttack : MonoBehaviour
{

    public float attack = 10f;  //攻撃力

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //衝突した時一回だけ呼びだされる
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "Player")
        {
            //プレイヤーと衝突した時
            Attack(col.gameObject); //攻撃する
        }
    }

    //攻撃する際に呼び出す（なんとなくpublicにしてある）
    public void Attack(GameObject hit)
    {
        hit.gameObject.SendMessage("Damage", attack);   //相手の"Damage"関数を呼び出す
    }
}

