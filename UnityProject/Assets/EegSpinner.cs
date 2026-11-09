using UnityEngine;

public class EegSpinner : MonoBehaviour {
    public float eegspinning = -90f;
    void Update() {
        transform.Rotate(new Vector3(0f, eegspinning*Time.deltaTime, 0f));
    }
}
