using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Annotations : MonoBehaviour
{
    public int maxTrailsTest;
    //For less future headaches, these are defined in the viewer in the inspector smh
    public Color trailColor = new Color(255, 255, 255);
    public float distanceFromCamera = 5;
    public float startWidth = 0.1f;
    public float endWidth = 0f;
    public float trailTime = 0.24f;

    private GameObject[] allTrails = new GameObject[] { };

    Transform trailTransform;
    Camera thisCamera;

    // Start is called before the first frame update

    private void Start()
    {
        maxTrailsTest = 30;
    }
    void InitiateTrail()
    {
        thisCamera = GetComponent<Camera>();

        GameObject trailObj = new GameObject("Mouse Trail");
        trailTransform = trailObj.transform;
        //setup trail object
        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        //TimedDestroy destroyScript = trailObj.AddComponent<TimedDestroy>();
        //destroyScript.destoryTime = 45f;
        trailObj.tag = "Trail";

        MoveTrailToCursor(Input.mousePosition);
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.numCapVertices = 2;
        trail.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
        trail.sharedMaterial.color = trailColor;
    }

    // Update is called once per frame
    void Update()
    {
        allTrails = GameObject.FindGameObjectsWithTag("Trail");
        //instantiate trail when you start holding down lft mouse
        if (Input.GetMouseButtonDown(0) && EventSystem.current.currentSelectedGameObject == null) 
        {
            InitiateTrail();
        } else if (Input.GetMouseButtonUp(0))
        {
            //focus off of trail when you let go of mouse
            trailTransform = null;
        }
        if(Input.GetMouseButtonDown(2))
        {
            
            foreach(GameObject trail in allTrails)
            {
                Destroy(trail);
            }
        }
        //Debug.Log("Amount: " + allTrails.Length.ToString() + ". And: " + (allTrails.Length > this.maxTrailsTest) + " and maxTrails = " + this.maxTrailsTest.ToString() );
        
        if (allTrails.Length > this.maxTrailsTest)
        {
            for (int i = 0; i < allTrails.Length - this.maxTrailsTest; i++)
            {
                Destroy(allTrails[0]);
                allTrails = allTrails.Skip(1).ToArray();
            }
        }

        //move focused trail position to cursor position
        MoveTrailToCursor(Input.mousePosition);


    }

    void MoveTrailToCursor(Vector3 screenPosition)
    {
        if(trailTransform != null)
        {
            trailTransform.position = thisCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
        }
    }
}