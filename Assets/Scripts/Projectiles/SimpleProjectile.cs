using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleProjectile : UdonSharpBehaviour {
    [SerializeField] float speed = 1f;

    private float liveTime = 5f;

    public void Init(Quaternion look, bool vrMode) {
        Vector3 e = look.eulerAngles;
        if (vrMode) {
            // -35 X right hand?
            //e.x += 35f;
        } else {
            transform.position -= new Vector3(0, 0.143f, 0);
        }
        transform.rotation = Quaternion.Euler(e);
    }

    private void Update() {
        liveTime -= Time.deltaTime;
        if (liveTime <= 0f) {
            Destroy(gameObject);
            return;
        }

        transform.position += speed * Time.deltaTime * transform.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        liveTime = 0f;
    }
}
