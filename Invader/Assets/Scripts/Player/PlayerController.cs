﻿//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 初期残機
    /// </summary>
    [SerializeField]
    private int startHp = 0;

    /// <summary>
    /// Playerのプレファブ
    /// </summary>
    [SerializeField]
    private GameObject playerPrefab = null;
    /// <summary>
    /// 画面左下の座標からどれだけ離れた位置からPlayerの移動を開始するか
    /// </summary>
    [SerializeField]
    private Vector3 minPosDiffAtStart = Vector3.zero;

    /// <summary>
    /// 最初に動き出せるのにかかる時間
    /// </summary>
    [SerializeField]
    private float startWaitTime = 0f;

    /// <summary>
    /// プレファブから生成したPlayerの参照
    /// </summary>
    private GameObject player = null;
    /// <summary>
    /// PlayerにアタッチされたPlayerMoverコンポーネントの参照
    /// </summary>
    private PlayerMover playerMover = null;
    /// <summary>
    /// PlayerにアタッチされたPlayerHealthコンポーネントの参照
    /// </summary>
    private PlayerHealth playerHealth = null;
    /// <summary>
    /// PlayerにアタッチされたPlayerShotコンポーネントの参照
    /// </summary>
    private PlayerShot playerShot = null;

    /// <summary>
    /// 左下の座標
    /// </summary>
    private Vector3 minPos = Vector3.zero;

    /// <summary>
    /// 残り残機
    /// </summary>
    private int remainHp = 0;
    public int RemainHp => remainHp;

    /// <summary>
    /// 弾を打って良いかどうか
    /// </summary>
    private bool canShot = false;

    /// <summary>
    /// 死んだあとに復活するのにかかる時間
    /// </summary>
    private float waitTimeForRevival = 0f;

    /// <summary>
    /// 死んだ時に呼ぶデリゲートメソッド
    /// </summary>
    private UnityAction<int> onDeath = null;

    void Awake()
    {
        remainHp = startHp;
    }

    /// <summary>
    /// playerの生成
    /// </summary>
    public void BootUp(Vector3 _maxPos, Vector3 _minPos, float waitTime, UnityAction<int> onDeath, UnityAction onGameover)
    {
        minPos = _minPos;
        waitTimeForRevival = waitTime;
        this.onDeath = onDeath;

        player = Instantiate(playerPrefab, minPos + minPosDiffAtStart, Quaternion.identity);

        playerMover = player.GetComponent<PlayerMover>();
        playerHealth = player.GetComponent<PlayerHealth>();
        playerShot = player.GetComponent<PlayerShot>();

        playerHealth.SetDeathAction(() =>
        {
            Damage();
            if (remainHp <= 0)
            {
                onGameover();
            }
        });

        //playerのx座標方向の移動可能範囲を設定
        playerMover.SetLimitPos(_maxPos.x, _minPos.x);
    }

    public void MoveStart()
    {
        //一定時間待ったら動きを開始
        StartCoroutine(WaitTime(Restart));
    }

    void Update()
    {
        playerMover.Move(Input.GetAxis(Dictionary.InputText.HORIZONTAL));
        if (Input.GetKeyDown(KeyCode.Space) && canShot)
        {
            playerShot.Shot();
        }
    }

    /// <summary>
    /// 攻撃を受けた時の処理
    /// </summary>
    void Damage()
    {
        DecreaseHp();
        this.onDeath(RemainHp);     //HPを減らした後に呼ばないとずれる
        DamageEffect();
    }

    /// <summary>
    /// HPを減らす
    /// </summary>
    void DecreaseHp()
    {
        remainHp--;
    }

    /// <summary>
    /// 攻撃を受けた時の見た目に関する処理
    /// </summary>
    void DamageEffect()
    {
        player.gameObject.SetActive(false);
        canShot = false;
        StartCoroutine(WaitToRevival(() =>
        {
            if (remainHp > 0)
            {
                Restart();
            }
        }));
    }

    IEnumerator WaitToRevival(UnityAction callback)
    {
        yield return new WaitForSeconds(waitTimeForRevival);
        callback();
    }

    void Restart()
    {
        player.transform.position = minPos + minPosDiffAtStart;
        player.gameObject.SetActive(true);
        canShot = true;
    }

    IEnumerator WaitTime(UnityAction callback)
    {
        yield return new WaitForSeconds(startWaitTime);
        callback();
    }

#if UNITY_EDITOR
    public void InvalidatePlayerDamage()
    {
        playerHealth.SetInvalidation(true);
    }

    public void EnablePlauyerDamage()
    {
        playerHealth.SetInvalidation(false);
    }
#endif
}
