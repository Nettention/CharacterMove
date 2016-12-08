#pragma once

#include "..\SimpleCommon\SimpleCommon.h"
#include "../SimpleCommon/SimpleCharacterMove_proxy.h"
#include "../SimpleCommon/SimpleCharacterMove_stub.h"

// #SHOW26 server side memory definition
class RemoteClient
{
public:
	std::wstring m_userID;
	float m_x, m_y, m_z;
	float m_vx, m_vy, m_vz;
	float m_angle;
};

class SimpleServer
	//#SHOW17 Let's add the PIDL file.
	:public Simple::Stub
{
public:
//#SHOW17 and here too.
 	Simple::Proxy m_proxy;

	// #SHOW12 Network server instance
 	shared_ptr<CNetServer> m_netServer;

// #SHOW27 As we are doing the multithreaded way...
 	CriticalSection m_critSec; // needed because blahblah

 	// key: client HostID
 	unordered_map<int, shared_ptr<RemoteClient> > m_remoteClients;

	// #SHOW36 Now we group all players into a P2P group and multicast the movement!
 	// player P2P group
 	HostID m_playerGroup = HostID_None;

	SimpleServer()
	{
	}

	~SimpleServer()
	{
	}

	void Run();
protected:


private:
	//#SHOW21 Server receives these messages. Decleration.
 	DECRMI_Simple_RequestLogin;



 	DECRMI_Simple_JoinGameScene;
 
 	DECRMI_Simple_Player_Move;

};


