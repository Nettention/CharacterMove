using UnityEngine;
using System.Collections;
using Nettention.Proud;

// #SHOW02 About NetManager GameObject and here
public class NetManager : MonoBehaviour
{
    // #SHOW19  Prepare RMI module on client 
    Simple.Proxy m_proxy = new Simple.Proxy();
    Simple.Stub m_stub = new Simple.Stub();

    // #SHOW10 Server connection info
    public static System.Guid Version = new System.Guid("{0x107b3b66,0xb7de,0x4091,{0xa5,0xba,0x72,0xca,0x1a,0xf5,0x1a,0xbc}}");
    public static ushort ServerPort = 35475;

    // #SHOW05 Network Client definition 
    NetClient m_netClient;

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
        //#SHOW06 Init
        m_netClient = new NetClient();

        //#SHOW11 If the server connection is complete...
        m_netClient.JoinServerCompleteHandler = 
            (ErrorInfo info, ByteArray replyFromServer) =>
        {
            if (info.errorType == ErrorType.Ok)
            {
                //#SHOW20 Now, remote call aka. send message!
                m_proxy.RequestLogin(HostID.HostID_Server, 
                    RmiContext.SecureReliableSend, m_userID, m_password);
                m_state = MyState.Connecting;
            }
            else
            {
                Debug.Log(info.ToString());
                m_state = MyState.Disconnected;
            }
        };

        m_netClient.LeaveServerHandler = (ErrorInfo info) =>
        {
            Debug.Log(info.ToString());
            m_state = MyState.Disconnected;
        }; // SHOW 11 BY HERE

        //#SHOW11_2 Now we enable NetManager GameObject in Unity Editor.

        //#SHOW19_2 Prepare RMI module on client 
        InitRMI();

        m_netClient.AttachProxy(m_proxy);
        m_netClient.AttachStub(m_stub);



        //#SHOW39 On client side, get the p2p group info of local player.  
        m_netClient.P2PMemberJoinHandler = 
            (HostID memberHostID, HostID groupHostID,
            int memberCount, ByteArray customField) =>
        {
            m_playerP2PGroup = groupHostID;
        };

    }


    // #SHOW40 Define RMI. Uncomment and introduce Player_Move function.

    float m_lastSendTime = -1;

     HostID m_playerP2PGroup = HostID.HostID_None;

    // Update is called once per frame
    void Update()
    {
        if (m_netClient != null)
        {
//#SHOW41 Send the movement to all players. Let's know about Unreliable and MaxMulticastCount.
            // If connection online
            if (m_netClient.GetLocalHostID() != HostID.HostID_None)
            {
                // send player move message
                if (m_lastSendTime < 0 || Time.time - m_lastSendTime > 0.1)
                {
                    var sendOption = new RmiContext();
                    sendOption.reliability = MessageReliability.MessageReliability_Unreliable;
                    sendOption.maxDirectP2PMulticastCount = 30;
                    sendOption.enableLoopback = false;

                    var pc = m_localPlayer.GetComponent<PlayControl>();
                    m_proxy.Player_Move(m_playerP2PGroup, sendOption,
                        m_localPlayer.transform.position.x,
                        m_localPlayer.transform.position.y,
                        m_localPlayer.transform.position.z,
                        pc.velocity.x, // for demo, 0,0,0 first. then use pc.velocity.
                        pc.velocity.y,
                        pc.velocity.z,
                        pc.carAngle);

                    m_lastSendTime = Time.time;
                }
            }
            // #SHOW07 Receive messages and events
            m_netClient.FrameMove();
        }

        if (m_disconnectNow)
        {
            m_netClient.Disconnect();
            m_state = MyState.Disconnected;
            m_disconnectNow = false;
        }
    }

//#SHOW02_1 Logon GUI code here. No work yet.
    void OnGUI()
    {
        if (m_state == MyState.Disconnected || m_state == MyState.Connecting)
        {
            GUI.Label(new Rect(10, 10, 300, 30), "## ProudNet Sample ##");

            GUI.Label(new Rect(10, 30, 300, 30), "Enter your ID, password here.");
            m_userID = GUI.TextField(new Rect(10, 60, 200, 30), m_userID);
            m_password = GUI.TextField(new Rect(10, 90, 200, 30), m_password);

            if (m_state == MyState.Disconnected)
            {
                if (GUI.Button(new Rect(10, 130, 100, 20), "Login!"))
                {
                    // #SHOW09 Connection request
                    var connectParam = new NetConnectionParam();
                    connectParam.protocolVersion = new Guid();
                    connectParam.protocolVersion.Set(Version);
                    connectParam.serverIP = "localhost";
                    connectParam.serverPort = ServerPort;

                    m_netClient.Connect(connectParam);
                    m_state = MyState.Connecting;
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
        // #SHOW08 Stop
        m_netClient.Disconnect();
    }

    bool m_disconnectNow = false;

    // #SHOW23 Client receives the login result. Finished. NOW, TEST IT!
    private void InitRMI()
    {
        m_stub.NotifyLoginSuccess = (HostID remote, RmiContext rmiContext) =>
        {
            m_state = MyState.Connected;


            //#SHOW30 call to the server!
            // join the world!
            m_proxy.JoinGameScene(HostID.HostID_Server, RmiContext.ReliableSend,
                m_localPlayer.transform.position.x,
                m_localPlayer.transform.position.y,
                m_localPlayer.transform.position.z,
                m_localPlayer.GetComponent<PlayControl>().carAngle); // show 30 ends here


            return true;
        };
        m_stub.NotifyLoginFailed = (HostID remote, RmiContext rmiContext, System.String reason) =>
        {
            Debug.Log(reason);
            m_disconnectNow = true;
            return true;
        };


        //#SHOW35 On cient, let the other player appear on Unity scene. FINISHED. TEST IT!
        m_stub.Player_Appear = (HostID remote, RmiContext rmiContext, int hostID, System.String userID, float x, float y, float z, float vx, float vy, float vz, float angle) =>
        {
            if (hostID != (int)m_netClient.GetLocalHostID())
            {
                var rot = Quaternion.AngleAxis(angle, new UnityEngine.Vector3(0, 1, 0));

                var remotePlayerCharacter = (GameObject)Instantiate(m_remotePlayerPrefab, new UnityEngine.Vector3(x, y, z), rot);
                remotePlayerCharacter.name = "RemotePlayer/" + hostID.ToString();

                var control = remotePlayerCharacter.GetComponent<RemoteControl>();
                var p = new Nettention.Proud.Vector3();
                p.x = x;
                p.y = y;
                p.z = z;
                var v = new Nettention.Proud.Vector3();
                v.x = vx;
                v.y = vy;
                v.z = vz;
                control.m_positionFollower.SetTarget(p, v);
                control.m_positionFollower.SetFollower(p, v);
            }
            return true;
        };
        m_stub.Player_Disappear = (Nettention.Proud.HostID remote,
            Nettention.Proud.RmiContext rmiContext, int hostID) =>
        {
            var g = GameObject.Find("RemotePlayer/" + hostID.ToString());
            if (g != null)
            {
                Destroy(g);
            }
            return true;
        };



        // #SHOW42 On client, receive movement message.
        m_stub.Player_Move = (Nettention.Proud.HostID remote, 
            Nettention.Proud.RmiContext rmiContext,
            float x, float y, float z, float vx, float vy, float vz, float angle) =>
        {
            var g = GameObject.Find("RemotePlayer/" + remote.ToString());
            if (g != null)
            {
                //g.transform.rotation = new UnityEngine.Vector3(x, y, z);

                //#SHOW46 we don't move GameObject directly here. But we let positionFollower know it. FINISHED. DO IT!
                var control = g.GetComponent<RemoteControl>();
                var p = new Nettention.Proud.Vector3();
                p.x = x;
                p.y = y;
                p.z = z;
                var v = new Nettention.Proud.Vector3();
                v.x = vx;
                v.y = vy;
                v.z = vz;
                control.m_positionFollower.SetTarget(p, v);

                var rot = Quaternion.AngleAxis(angle, new UnityEngine.Vector3(0, 1, 0));
                g.transform.rotation = rot;
            }

            return true;
        };
    }

}
