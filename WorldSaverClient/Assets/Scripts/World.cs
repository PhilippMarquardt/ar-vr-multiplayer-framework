using System.Collections;
using NetLib.Script.Rpc;
using NetLib.Script;
using UnityEngine;
using UnityEngine.SceneManagement;
using NetLib.XR;
using Random = UnityEngine.Random;

public class World : NetworkBehaviour
{
    public float asteroidSpawnInterval = 2;
    public GameObject[] asteroidObjects;
    public bool debugMode;

    private NetworkManager networkManager;
    private Anchor earthAnchor;
    private Transform cachedTransform;


    public void StartGame()
    {
        if(IsServer)
            StartCoroutine(AsteroidSpawn());
        if(IsClient)
            InvokeServerRpc(Test, 42);
    }

    protected override void OnNetworkStart()
    {
        if (debugMode)
            StartGame();
    }

    [ServerRpc]
    public void Test(int i)
    {
        Debug.Log("RPC Test: " + i);
    }

    public void LockAnchor()
    {
        earthAnchor.LockAnchor();
    }

    public void UnlockAnchor()
    {
        earthAnchor.UnlockAnchor();
    }


    private void Awake()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        earthAnchor = GameObject.Find("EarthAnchor").GetComponent<Anchor>();
        cachedTransform = transform;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * 5f, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (debugMode)
        {
            if (IsServer)
                networkManager.DestroyNetworkObject(collision.gameObject);
        }
        else
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }
    }

    private IEnumerator AsteroidSpawn()
    {
        while (true)
        {
            var asteroidPrefab = asteroidObjects[Random.Range(0, asteroidObjects.Length - 1)];
            var asteroidObject = Instantiate(
                asteroidPrefab, 
                cachedTransform.position + Random.onUnitSphere * 2, 
                Random.rotation, 
                cachedTransform.parent);

            networkManager.SpawnLocalObject(asteroidObject);

            yield return new WaitForSeconds(asteroidSpawnInterval);
        }
    }
}
