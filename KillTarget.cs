using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KillTarget : MonoBehaviour
{
    public GameObject target; // 敵
    public ParticleSystem hitEffect; // 視線が当たっている間のエフェクト
    public GameObject killEffect; // 敵を破壊した時のエフェクト
    public float timeToSelect = 3.0f; // 視線を当てている時間
    public int score;
    public Text scoreText;

    private ParticleSystem.EmissionModule hitEffectEmission;
    private float countDown;

    void Start()
    {
        score = 0;
        countDown = timeToSelect;
        hitEffectEmission = hitEffect.emission;
        hitEffectEmission.enabled = false;
        scoreText.text = "Score : 0";
    }

    void Update()
    {
        Transform camera = Camera.main.transform; // タグがMainCamera
        Ray ray = new Ray(camera.position, camera.rotation * Vector3.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && (hit.collider.gameObject == target))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            if (countDown > 0.0f)
            {
                // 的中した際の処理
                countDown -= Time.deltaTime;
                hitEffect.transform.position = hit.point;
                hitEffectEmission.enabled = true;
            }
            else
            {
                // 殺された際（カウントが０）の処理
                Instantiate(killEffect, target.transform.position,
                    target.transform.rotation);
                score += 1;
                scoreText.text = "Score : " + score;
                countDown = timeToSelect;
                SetRandomPosition(); // リポップ
            }
        }
        else
        {
            // リセットする
            countDown = timeToSelect;
            hitEffectEmission.enabled = false;
        }
    }

    // リポップ respawn（ランダムな場所に移動）
    void SetRandomPosition()
    {
        float x = Random.Range(-100.0f, 100.0f);
        float z = Random.Range(-100.0f, 100.0f);
        target.transform.position = new Vector3(x, 0.0f, z);
    }
}