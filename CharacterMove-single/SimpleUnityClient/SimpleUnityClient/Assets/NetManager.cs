using UnityEngine;
using System.Collections;

public class NetManager : MonoBehaviour
{


    string m_userID = "john";
    string m_password = "123";

    public GameObject m_localPlayer;

    public GameObject m_remotePlayerPrefab;

    enum MyState
    {
        Disconnected,
        Connecting,
        Connected,
    }
    MyState m_state = MyState.Disconnected;

    // Use this for initialization
    void Start()
    {
    }


    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI()
    {
        if (m_state == MyState.Disconnected || m_state == MyState.Connecting)
        {
            GUI.Label(new Rect(10, 10, 300, 30), "## ProudNet Sample ##");

            GUI.Label(new Rect(10, 30, 300, 30), "Enter your ID, password here.");
            m_userID = GUI.TextField(new Rect(10, 60, 200, 30), "john");
            m_password = GUI.TextField(new Rect(10, 90, 200, 30), "123");

            if (m_state == MyState.Disconnected)
            {
                if (GUI.Button(new Rect(10, 130, 100, 20), "Login!"))
                {
                }
            }
            if (m_state == MyState.Connecting)
            {
                GUI.Button(new Rect(10, 130, 100, 20), "Connecting...");
            }
        }
    }

    void OnApplicationQuit()
    {
    }


}
