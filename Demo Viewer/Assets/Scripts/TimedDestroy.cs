/**
 * Zzenith 2020
 * Date: 16 April 2020
 * Purpose: Destroy or deactivate objects
 */

using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    public double destoryTime = 10f;
    public bool destroy = true;
    private double startTime;

    // Start is called before the first frame update
    void Awake()
    {
        startTime = Time.timeAsDouble;
    }

    // Update is called once per frame
    void Update()
    {

        if (Time.timeAsDouble >= startTime + destoryTime)
        {
            if (destroy) Destroy(gameObject);
            else gameObject.SetActive(false);
        }

    }
}
