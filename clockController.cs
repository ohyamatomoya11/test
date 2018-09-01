using System.Collections;
using System;   // DateTimeに必要
using System.Collections.Generic;
using UnityEngine;

public class clockController : MonoBehaviour {
    public bool sec;   // 秒針の有無
    public bool secTick;   // 秒針を秒ごとに動かすか

    public GameObject hour;
    public GameObject minute;
    public GameObject second;

    void Start()
    {
        if (!sec)
            Destroy(second); // 秒針を消す
    }

    void Update()
    {
        DateTime dt = DateTime.Now;

        hour.transform.eulerAngles = new Vector3(0, 0, (float)dt.Hour / 12 * -360 + (float)dt.Minute / 60 * -30);
        minute.transform.eulerAngles = new Vector3(0, 0, (float)dt.Minute / 60 * -360);
        if (sec)
        {
            if (secTick)
                second.transform.eulerAngles = new Vector3(0, 0, (float)dt.Second / 60 * -360);
            else
                second.transform.eulerAngles = new Vector3(0, 0, (float)dt.Second / 60 * -360 + (float)dt.Millisecond / 60 / 1000 * -360);
        }

    }
}
