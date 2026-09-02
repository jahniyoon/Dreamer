using DG.Tweening;
using Dreamer.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Dreamer.Enemy
{

    public class RockBoss : BossEnemy
    {
        [Header("낙석 패턴 프리팹 및 레이어")]
        [SerializeField] private GameObject fallingRockPrefab;
        [SerializeField] private Transform[] rockSpawnPoints;
        [SerializeField] private float cameraShakeIntensity = 0.5f;

        protected override void StartPattern()
        {
            // 보스 턴마다 무작위 패턴 실행
            StartCoroutine(PatternRoutine());
        }

        Coroutine curPattern;
        /// <summary>
        /// 보스 무작위 패턴 실행 (낙석 경고 후 투하 / 몸집 흔들기)
        /// </summary>
        private IEnumerator PatternRoutine()
        {
            isAttacking = true;
            ResetScale();

            if(curPattern != null)
            {
                StopCoroutine(curPattern);
                curPattern = null;
            }

            // 1. 보스 차징 연출 (DOShakeScale)
            spriteRenderer.transform.DOShakePosition(0.5f, new Vector3(0.1f, 0f, 0f), 20, 90f);
            JuiceManager.Instance.ShakeCamera();
            yield return new WaitForSeconds(0.6f);

            // 2. 패턴 무작위 실행
            int patternIndex = Random.Range(0, 2);

            if (patternIndex == 0)
            {
                curPattern = StartCoroutine(SpawnFallingRocksRoutine());
                // [패턴 1] 무작위 2~3개 위치에 낙석 투하
                yield return curPattern;
            }
            else
            {
                // [패턴 2] 보스가 1칸 찍는 진동 패턴
                curPattern = StartCoroutine(SlamPatternRoutine());
                yield return curPattern;
            }

            isAttacking = false;
        }

        private IEnumerator SpawnFallingRocksRoutine()
        {
            int rockCount = Random.Range(2, 4);
            int lastSpawnIndex = -1; // 직전에 뽑힌 위치 인덱스 기록

            for (int i = 0; i < rockCount; i++)
            {
                Vector3 spawnWorldPos;

                if (rockSpawnPoints != null && rockSpawnPoints.Length > 0)
                {
                    int randomIndex;

                    // 스폰 지점이 2개 이상이면 직전에 뽑힌 지점과 다른 곳을 추첨
                    if (rockSpawnPoints.Length > 1)
                    {
                        do
                        {
                            randomIndex = Random.Range(0, rockSpawnPoints.Length);
                        }
                        while (randomIndex == lastSpawnIndex);
                    }
                    else
                    {
                        randomIndex = 0;
                    }

                    lastSpawnIndex = randomIndex;
                    spawnWorldPos = rockSpawnPoints[randomIndex].position;
                }
                else
                {
                    int halfWidth = bossWidth / 2;
                    int randomX = Random.Range(-halfWidth, halfWidth + 1);
                    spawnWorldPos = new Vector3(randomX, transform.position.y + 2f, 0f);
                }

                if (fallingRockPrefab != null)
                {
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.SpawnFromPool(fallingRockPrefab, spawnWorldPos, Quaternion.identity, null, 10f);
                    }
                    else
                    {
                        Instantiate(fallingRockPrefab, spawnWorldPos, Quaternion.identity);
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
            ResetScale();
            curPattern = null;
        }

        private IEnumerator SlamPatternRoutine()
        {
            Vector3 originalPos = transform.position;
            // 1. 차징: 아래로 웅크리며 힘 모으기 (0.35유닛 밑으로 내려감)
            spriteRenderer.transform.DOMoveY(originalPos.y - 0.35f, 0.25f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.3f);

            // 부딪히는 순간 카메라 셰이크
            JuiceManager.Instance?.ShakeCamera(2);
            // 2. 솟구치기: 위쪽(플레이어 방향)으로 강하게 돌진하며 들이받기 (0.7유닛 위로 솟구침)
            spriteRenderer.transform.DOMoveY(originalPos.y + 0.7f, 0.25f)
                .SetEase(Ease.OutBack) // 충격감을 살려주는 튕김 이징
                .OnComplete(() =>
                {
              
                });
            player.Stats.TakeDamage(Data.AttackPower);
            yield return new WaitForSeconds(0.2f);

            // 3. 복귀: 부딪힌 후 원래 위치로 부드럽게 원복
            spriteRenderer.transform.DOMoveY(originalPos.y, 0.25f).SetEase(Ease.InOutQuad);
            yield return new WaitForSeconds(0.3f);

            ResetScale();
            curPattern = null;
        }
    }
}
