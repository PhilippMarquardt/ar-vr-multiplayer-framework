using NetLib.Script;
using NetLib.XR;
using UnityEngine;

public class Asteroid : NetworkBehaviour
{
    public GameObject explosion;

    private NewNetworkManager networkManager;

    private Rigidbody rb;
    private float rotationRate;
    private Vector3 rotationAxis;

    private void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NewNetworkManager>();


        rb = gameObject.GetComponent<Rigidbody>();
        var target = GameObject.Find("World").transform.position;

        if (IsServer)
            rb.AddForce((target - transform.position).normalized * 7f);

        rotationRate = Random.Range(30f, 100f);
        rotationAxis = Random.onUnitSphere;
    }

    private void Update()
    {
        transform.Rotate(rotationAxis, Time.deltaTime * rotationRate, Space.World);
    }

    private void OnTargeted()
    {
        gameObject.SetActive(false);
        networkManager.DestroyNetworkObject(gameObject);
        Instantiate(explosion, transform.position, Random.rotation);
    }
}
