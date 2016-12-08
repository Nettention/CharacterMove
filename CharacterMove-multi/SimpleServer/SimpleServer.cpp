// SimpleServer.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "SimpleServer.h"
#include "../SimpleCommon/SimpleCharacterMove_proxy.cpp"
#include "../SimpleCommon/SimpleCharacterMove_stub.cpp"
#include "../SimpleCommon/SimpleCharacterMove_common.cpp"
#include <AdoWrap.h>

// #SHOW03 Server base code
int main()
{
	SimpleServer server;
	server.Run();

    return 0;
}

// #SHOW04 Server main code
void SimpleServer::Run()
{

	// #SHOW13 Init
 	m_netServer = shared_ptr<CNetServer>(CNetServer::Create());

//#SHOW16 On server, process the client join and leave. FINISHED. TEST IT!
	m_netServer->OnClientJoin = [](CNetClientInfo* info)
	{
		cout << "Client " << info->m_HostID << " joined.\n";
	};

	m_netServer->OnClientLeave = [this](CNetClientInfo* info, ErrorInfo*, const ByteArray&)
	{
		cout << "Client " << info->m_HostID << " went out.\n";
// 
	// #SHOW28_2 Remove the player info if client leaves 
 		CriticalSectionLock lock(m_critSec, true);
//// 	
	//#SHOW34 let others know the disappear
		// let others know the disappear
		vector<int> others;
		for (auto otherIter : m_remoteClients)
		{
			others.push_back(otherIter.first);
		}
		m_proxy.Player_Disappear((HostID*)others.data(), others.size(),
			RmiContext::ReliableSend,
			info->m_HostID);


//// 		
		// delete player
		m_remoteClients.erase(info->m_HostID);
 	}; // SHOW 16 END HERE

	//#SHOW18 Prepare RMI module on server
	m_netServer->AttachProxy(&m_proxy);
	m_netServer->AttachStub(this);

//#SHOW14 Starts server listening
 	CStartServerParameter startConfig;
 	startConfig.m_protocolVersion = g_protocolVersion;
 	startConfig.m_tcpPorts.Add(g_serverPort);
 	startConfig.m_allowServerAsP2PGroupMember = true; // needed for receiving P2P message on the server side
 
 	m_netServer->Start(startConfig);


// #SHOW37 Prepare an P2P group and server joins it.
	// Prepare an empty P2P group
	m_playerGroup = m_netServer->CreateP2PGroup();
	m_netServer->JoinP2PGroup(HostID_Server, m_playerGroup);

	cout << "Server started. Hit return to exit.\n";

	string line;
	getline(std::cin, line);

// 	m_netServer->Stop();

}

//#SHOW29 After login, we enter the game scene. 
/* Uncomment and introduce JoinGameScene RMI definition in PIDL file.
*/


//#SHOW24 Let's do the real auth. Create DB instance.
/* Create DB instance SimpleCharacterMove.
Create table GameUser { UserID:nvarchar(50), Password:nvarchar(50) } 

Create stored procedure below

CREATE PROCEDURE [dbo].[GetGameUser]
 @UserID nvarchar(50)
 AS
 SELECT * from GameUser where UserID = @UserID
 RETURN 0

... and some records. john / 123, alice / 123.
*/



//#SHOW22 Definition of it.
DEFRMI_Simple_RequestLogin(SimpleServer)
{
	cout << "RequestLogin " << StringT2A(id.c_str()) << " "
		<< StringT2A(password.c_str()) << endl;

//#SHOW25 Call DB stored procedure.
 	try
 	{
 		CAdoConnection conn;
 
 		conn.Open(L"server=.;database=SimpleCharacterMove;trusted_connection=yes", DbmsType::MsSql);
 
 		CAdoCommand cmd;
 		
 		cmd.Prepare(conn, L"GetGameUser");
 		cmd.AppendParameter(L"UserID", ADODB::adVarWChar, ADODB::adParamInput, id.c_str());
 		CAdoRecordset rs;
 		cmd.Execute(rs);
 
 		String password2;
 		if (!rs.GetFieldValue(L"Password", password2) || password != password2.GetString())
 		{
 			m_proxy.NotifyLoginFailed(remote, RmiContext::ReliableSend, L"Invalid user ID or password");
 			return true;
 		}// SHOW 25 BY HERE


// #SHOW28 Add the player info on server
 		CriticalSectionLock lock(m_critSec, true);
 
 		// already logged in?
 		auto it = m_remoteClients.find(remote);
 		if (it != m_remoteClients.end())
 		{
 			m_proxy.NotifyLoginFailed(remote, RmiContext::ReliableSend, L"Already logged in.");
 			return true;
 		}
 
 		// add new player
 		// NOTE: we don't care duplicated login for now. We are in tutorial.
 		auto newRC = make_shared<RemoteClient>();
 		newRC->m_userID = id;
 		m_remoteClients[remote] = newRC; 
// 
 		// success!
 		m_proxy.NotifyLoginSuccess(remote, RmiContext::ReliableSend);
//

//#SHOW25 also, we must catch the exception too. FINISHED. LET'S TEST!
 	}
 	catch (AdoException& e)
 	{
 		m_proxy.NotifyLoginFailed(remote, RmiContext::ReliableSend, StringA2T(e.what()).GetString());
 	}
 
 	return true;
 } // SHOW 22 BY HERE


//#SHOW31 Server receives the message and let the player join the world 
DEFRMI_Simple_JoinGameScene(SimpleServer)
{
 cout << "JoinGameScene is called.\n";
 	CriticalSectionLock lock(m_critSec, true);
 
 	// validation check
 	auto it = m_remoteClients.find(remote);
 	if (it == m_remoteClients.end())
 	{
 		return true;
 	}
 	auto& rc = it->second;
 
 	// update player info
 	rc->m_x = x;
 	rc->m_y = y;
 	rc->m_z = z;
 	rc->m_angle = angle;
// 
// #SHOW38 On client join, we add it to the p2p group.
 	// P2P group join
 	m_netServer->JoinP2PGroup(remote, m_playerGroup);
//// 
// #SHOW33 Let incomer know others, let others know incomers.
 	assert(it->first == remote);
 
 	// Let incomer know others, let others know incomers.
 	for (auto otherIter : m_remoteClients)
 	{
 		if (otherIter.first != it->first)
 		{
 			auto& otherRC = otherIter.second;
 			m_proxy.Player_Appear((HostID)it->first, RmiContext::ReliableSend,
 				(HostID)otherIter.first,
 				otherRC->m_userID,
 				otherRC->m_x, otherRC->m_y, otherRC->m_z, 
 				otherRC->m_vx, otherRC->m_vy, otherRC->m_vz,
 				otherRC->m_angle);
 
 			m_proxy.Player_Appear((HostID)otherIter.first, RmiContext::ReliableSend,
 				(HostID)it->first,
 				rc->m_userID, 
 				rc->m_x, rc->m_y, rc->m_z,
 				rc->m_vx, rc->m_vy, rc->m_vz,
 				rc->m_angle);
 		}
 	}
 
 	return true;
 } // show 31 ends here


// #SHOW32 Uncomment and introduce Player_Appear,Disappear.

// #SHOW43 Server must know the movement too. For Appear message. 
//         Now test the result. We see the players move stuttering.
 DEFRMI_Simple_Player_Move(SimpleServer)
 {
 	CriticalSectionLock lock(m_critSec, true);
 
 	// validation check
 	auto it = m_remoteClients.find(remote);
 	if (it == m_remoteClients.end())
 	{
 		return true;
 	}
 	auto& rc = it->second;
 
 	// update player info
 	rc->m_x = x;
 	rc->m_y = y;
 	rc->m_z = z;
 	rc->m_vx = vx;
 	rc->m_vy = vy;
 	rc->m_vz = vz;
 	rc->m_angle = angle;
 
 	return true;
 }
