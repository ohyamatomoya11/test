using UnityEngine;
using System.Collections;

public class EnemySpawn : MonoBehaviour
{
    public GameObject enemy;    //敵オブジェクト
    public Transform ground;    //地面オブジェクト
    public float count = 5;     //一度に何体のオブジェクトをスポーンさせるか
    public float interval = 5;  //何秒おきに敵を発生させるか
    private float timer;        //経過時間
    // Use this for initialization
    void Start()
    {
        Spawn();    //初期スポーン
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;    //経過時間加算
        if (timer >= interval)
        {
            Spawn();    //スポーン実行
            timer = 0;  //初期化
        }
    }

    void Spawn()
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-25f, 25f);
            float z = Random.Range(-25f, 25f);
            Vector3 pos = new Vector3(x, 5, z) + ground.position;
            GameObject.Instantiate(enemy, pos, Quaternion.identity);
        }
    }
}
