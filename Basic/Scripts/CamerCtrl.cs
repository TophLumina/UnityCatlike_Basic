using UnityEngine;

public class CamerCtrl : MonoBehaviour
{
    [SerializeField]
    Transform target;

    Transform transform;

    private void OnEnable() {
        transform = this.GetComponent<Transform>();
    }

    private void Update() {
        if (target != null)
        {
            transform.LookAt(target);
        }
    }
}
