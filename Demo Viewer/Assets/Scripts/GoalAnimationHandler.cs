using Spark;
using UnityEngine;

public class GoalAnimationHandler : MonoBehaviour
{
    public Animation[] animations;
    
    // Start is called before the first frame update
    private void Start()
    {
        GameEvents.Goal += (_) =>
        {
            Debug.Log("Goal");
            foreach (Animation a in animations)
            {
                a.gameObject.SetActive(true);
                a.Play();
            }
        };
    }
}
