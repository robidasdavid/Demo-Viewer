using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rotateSpeed = 10;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
    }
}
