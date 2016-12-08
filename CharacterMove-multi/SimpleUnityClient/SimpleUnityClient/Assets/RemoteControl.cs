using UnityEngine;
using System.Collections;
using Nettention.Proud;

public class RemoteControl : MonoBehaviour {
    public PositionFollower m_positionFollower = new PositionFollower();

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        // #SHOW45 We adopt position smoother function here.
        m_positionFollower.FrameMove(Time.deltaTime);

        var p = new Nettention.Proud.Vector3();
        var v = new Nettention.Proud.Vector3();
        m_positionFollower.GetFollower(ref p, ref v);

        transform.position = new UnityEngine.Vector3((float)p.x, (float)p.y, (float)p.z);
    }
}
