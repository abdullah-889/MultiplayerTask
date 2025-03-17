using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private NetworkRunner runner;

    public NetworkObject cube;

    void Awake()
    {
        runner = FindObjectOfType<NetworkRunner>();
    }
    void Start()
    {
        if (runner.IsServer)
        {
            StartCoroutine(SpawnCubesAsync());
        }
    }

    private IEnumerator SpawnCubesAsync()
    {
        for (int i = 0; i < runner.SessionInfo.PlayerCount; i++)
        {
            Vector3 spawnPosition = new Vector3(i * 1.5f, 0, 0);
            yield return new WaitForSeconds(0.1f); 
            runner.Spawn(cube, spawnPosition, Quaternion.identity);
        }
    }


}
