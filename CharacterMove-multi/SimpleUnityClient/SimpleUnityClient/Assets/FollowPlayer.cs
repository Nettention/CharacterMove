using UnityEngine;
using System.Collections;

public class FollowPlayer : MonoBehaviour
{

    public GameObject player;

    public Vector3 offset = new Vector3(10, 20, 10);

    // Use this for initialization
    void Start()
    {

    }

    // LateUpdate is called after Update each frame
    void LateUpdate()
    {
        // Set the position of the camera's transform to be the same as the player's, but offset by the calculated offset distance.
        transform.position = player.transform.position + offset;
        transform.LookAt(player.transform);
    }
}
