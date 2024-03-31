using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleProjectile : UdonSharpBehaviour {
    [SerializeField] float speed = 1f;

    private float liveTime = 5f;

    public void Init(MazeController controller, Quaternion look, bool vrMode) {
        Vector3 e = look.eulerAngles;
        if (vrMode) {
            e.x += controller.MazeUI.GetHandXOffset();
            //e.x = (e.x + 360f) % 360f;
        } else {
            // спавн под глазами
            transform.position -= new Vector3(0, 0.143f, 0);
        }
        controller.MazeUI.Log("e=" + e.ToString());
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

    private void OnTriggerEnter(Collider other) {
        liveTime = 0f;
    }
}
