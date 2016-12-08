using UnityEngine;
using System.Collections;

public class PlayControl : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    public float carSpeed = 10;
    public float carAngle = 0;
    public float carRotateSpeed = 200;

     public Vector3 velocity = new Vector3(0, 0, 0);

    // Update is called once per frame
    void Update()
    {
        // #SHOW01 LocalPlayer GameObject and here
        if (Input.GetKey("a"))
        {
            carAngle -= carRotateSpeed * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            carAngle += carRotateSpeed * Time.deltaTime;
        }
        var rot = Quaternion.AngleAxis(carAngle, new Vector3(0, 1, 0));
        transform.rotation = rot;

        var forwardSpeed = new Vector3(0, 0, 0);

        if (Input.GetKey("w"))
            forwardSpeed = new Vector3(0, 0, carSpeed);

        if (Input.GetKey("s"))
            forwardSpeed = new Vector3(0, 0, -carSpeed);

        // move player 
        transform.Translate(forwardSpeed*Time.deltaTime);

        //#SHOW44 Calculate the velocity. We are already sending the velocity.
        // set velocity for sending message
        velocity = rot * forwardSpeed * 0.3f;
    }
    
}

