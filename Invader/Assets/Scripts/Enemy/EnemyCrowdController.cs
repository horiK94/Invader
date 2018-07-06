﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Assertions.Must;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EnemyCrowdController : MonoBehaviour {
    private Transform enemyParent = null;
    
    /// <summary>
    /// Enemyの列数
    /// </summary>
    [SerializeField] private int enemyWidthNum = 11;
    /// <summary>
    /// Enemyの段数
    /// </summary>
    [SerializeField] private int enemyHeightNum = 5;
    /// <summary>
    /// Enemyの列ごとのプレファブ情報
    /// </summary>
    [SerializeField] private EnemyLineInfo[] enemyLineInfo;
    /// <summary>
    /// ufoを出現させるのに必要なEnemyの最低の数
    /// </summary>
    [SerializeField] private int ufoPopMinEnemyNum = 8;
    /// <summary>
    /// スタート前の停止時間
    /// </summary>
    [SerializeField] private float startWaitTime = 2.5f;
    /// <summary>
    /// Enemyの攻撃間隔
    /// </summary>
    [SerializeField] private float shotInterval = 1;
    /// <summary>
    /// Enemyの一番上の段のy座標と画面上のy座標の差
    /// </summary>
    [SerializeField]private float enemyTopPosYDiff;
    /// <summary>
    /// Enemyの一番下の段のy座標と画面下のy座標の差
    /// </summary>
    [SerializeField] private float enemyBottomPosYDiff;
    /// <summary>
    /// Enemyの横の間隔(基本、8移動でそこまで移動)
    /// </summary>
    [SerializeField] private float enemyWidthInterval;
    /// <summary>
    /// 段数の数
    /// </summary>
    [SerializeField]int stageNum = 19;
    /// <summary>
    /// １行移動するのに待つ時間
    /// </summary>
    [SerializeField] private float moveRowWaitTime = 1.0f;

    private EnemyRowController[] enemyRows;
    private int remainColumn = 0;
    private UnityAction<int> onAddScore = null;
    private UnityAction onDeath = null;
    private UnityAction onBelowUfoPopMinEnemyNum = null;
    private Vector3 maxPos = Vector3.zero, minPos = Vector3.zero;
    private float enemyHeightInterval;
    private int minStage, maxStage;
    private int[] rowAliveEnemuNum;
    private bool isTurn = false;

    void Awake()
    {
        minStage = 0;
        maxStage = enemyHeightNum - 1;
    }
    
    public void BootUp(UnityAction<int> _onAddScore, UnityAction _onDeath, UnityAction _onBelowUfoPopMinEnemyNum, Vector3 _maxPos, Vector3 _minPos)
    {   
        this.onAddScore = _onAddScore;
        this.onDeath = _onDeath;
        this.onBelowUfoPopMinEnemyNum = _onBelowUfoPopMinEnemyNum;
        this.maxPos = _maxPos;
        this.minPos = _minPos;
        
        remainColumn = enemyWidthNum;
        
        SortEnemyPrefab();
        Create();
    }

    void SortEnemyPrefab()
    {
        Array.Sort(enemyLineInfo, (x, y) => { return x.HighestLine - y.HighestLine; });
        if (enemyLineInfo[enemyLineInfo.Length - 1].HighestLine != enemyHeightNum - 1)
        {
            Debug.LogError("enemyLineInfo[enemyLineInfo.Length - 1].HighestLine != enemyHeightNum - 1となっています");
        }
    }

    private void Create()
    {
        enemyParent = new GameObject("Enemys").transform;

        rowAliveEnemuNum = new int[enemyHeightNum];
        for (int i = 0; i < enemyHeightNum; i++)
        {
            rowAliveEnemuNum[i] = enemyWidthNum;
        }

        enemyRows = new EnemyRowController[enemyWidthNum];
        for (int i = 0; i < enemyHeightNum; i++)
        {
            //列の生成
            int lineNumber = i + 1;
            GameObject column = new GameObject("Enemy" + lineNumber + "ColumnController");
            EnemyRowController controller = column.AddComponent<EnemyRowController>();
            enemyRows[i] = controller;
            
            column.transform.parent = transform;
            
            EnemyRowCreateInfo rowInfo = new EnemyRowCreateInfo();
            rowInfo.rowId = i;
            rowInfo.rowInfo = FindEnemyInfo(i);
            rowInfo.enemyHeightNum = enemyHeightNum;
            rowInfo.enemyWidthNum = enemyWidthNum;
            rowInfo.startUpStageId = stageNum - 1;        //TODO ステージ数によって変化させる
            rowInfo.enemyWidthInterval = enemyWidthInterval;
            rowInfo.stageNum = stageNum;
            rowInfo.enemyMinPos = new Vector3(minPos.x, minPos.y + enemyBottomPosYDiff, 0);
            rowInfo.enemyMaxPos = new Vector3(maxPos.x, maxPos.y - enemyTopPosYDiff, 0);
            
            controller.Create(rowInfo, enemyParent, onAddScore, (id) =>
            {
                rowAliveEnemuNum[id]--;
                if (rowAliveEnemuNum.Sum() == 0)
                {
                    onDeath();
                }

                if (rowAliveEnemuNum[id] == 0)
                {
                    // TODO
                }
            });
        }

        StartCoroutine(Move());
        StartCoroutine(Shot());
    }

    IEnumerator Move()
    {
        yield return new WaitForSeconds(startWaitTime);
        while (enemyRows.Where(e => e != null).ToArray().Length != 0)
        {
            bool canMoveSide = CanMoveSide();
            for (int i = 0; i < enemyHeightNum; i++)
            {
                if (canMoveSide)
                {
                    enemyRows[i].MoveSide();
                }
                else
                {
                    enemyRows[i].MoveBefore();
                }
                yield return  new WaitForSeconds(moveRowWaitTime);
            }
        }
    }

    bool CanMoveSide()
    {
        for (int i = 0; i < enemyHeightNum; i++)
        {
            if (!enemyRows[i].CanMoveSide() && !isTurn)
            {
                isTurn = true;
                return false;
            }
        }
        isTurn = false;
        return true;
    }

    IEnumerator Shot()
    {
        yield return new WaitForSeconds(shotInterval);
    }
    
    EnemyLineInfo FindEnemyInfo(int line)
    {
        for (int j = 0; j < enemyLineInfo.Length; j++)
        {
            int lowerLine = j == 0 ? 0: enemyLineInfo[j - 1].HighestLine + 1;
            if (line >= lowerLine && line <= enemyLineInfo[j].HighestLine)
            {
                return  enemyLineInfo[j];
            }
        }
        return null;
    }

}