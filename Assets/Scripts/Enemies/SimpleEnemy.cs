
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleEnemy : UdonSharpBehaviour {
    [SerializeField] float health = 5f;
    [SerializeField] float speed = 0.1f;

    private VRCPlayerApi localPlayer;

    private void Start() {
        speed += Random.Range(0f, 0.2f);
    }

    void Update() {
        if (localPlayer == null)
            localPlayer = Networking.LocalPlayer;

        var playerPos = localPlayer.GetPosition();
        transform.position = Vector3.Lerp(transform.position, playerPos, speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other) {
        health--;
        if (health <= 0f) {
            Destroy(gameObject);
        }
    }
}
