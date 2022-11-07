using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using System;

public class NetworkedServer : MonoBehaviour
{
    public static NetworkedServer Instance;
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 3333;
    static GameObject sManager;

    public List<GameObject> ObjectsPrefabs = new List<GameObject>();
    public List<GameObject> Players = new List<GameObject>();
    public List<GameObject> GameRooms = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

    }
    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {

        string[] dataReceived = msg.Split(',');
        switch (int.Parse(dataReceived[0]))
        {
            case 0: //LOGIN VERIFICATION
                Debug.Log("VERIFYING");
                SystemManager.Instance.LoginVerification(dataReceived[1], dataReceived[2], id);
                break;


            case 1://CREATE ACCOUNT
                SystemManager.Instance.createAccount(dataReceived[1], dataReceived[2], id);
                break;

            case 2://Create GameRoom;

                CreateRoomorJoinRoom(dataReceived[1], id, dataReceived[2]);
              
                

                break;

        }

      
    }


    public void CretePlayer(string playerName, int userID)
    {
        GameObject playerX;
        playerX = Instantiate(ObjectsPrefabs[0]);
        playerX.transform.parent = transform;
        playerX.GetComponent<PlayerInfo>().name = playerName;
        playerX.GetComponent<PlayerInfo>().playerName = playerName;
        playerX.GetComponent<PlayerInfo>().userID = userID;
        playerX.GetComponent<PlayerInfo>().playerState = PlayerStates.playerIsInLobby;
        Players.Add(playerX);

    }
    public void CreateRoomorJoinRoom(string playerName,int userID, string roomName) 
   {
        if(GameRooms.Count == 0)
        {
            //Createroom


            GameObject roomX;
            roomX = Instantiate(ObjectsPrefabs[1], this.gameObject.transform.position, Quaternion.identity) as GameObject;
            roomX.transform.parent = transform;
            roomX.GetComponent<GameRoomManager>().name = roomName;
            roomX.GetComponent<GameRoomManager>().roomName = roomName;
            roomX.GetComponent<GameRoomManager>().Player1 = Players[0];
            roomX.GetComponent<GameRoomManager>().Player1.name = Players[0].name;
            roomX.GetComponent<GameRoomManager>().playerID1 = userID;
            Players[0].GetComponentInParent<PlayerInfo>().playerState = PlayerStates.playerIsWaiting;
            // roomX.GetComponent<GameRoomManager>().Player1.name = playerName;

            //foreach (var player in Players)
            //{
            //    if(player.name == playerName)
            //    {
            //        roomX.GetComponent<GameRoomManager>().Player1.name = player.GetComponent<PlayerInfo>().playerName;
            //        roomX.GetComponent<GameRoomManager>().playerID1 = player.GetComponent<PlayerInfo>().userID;

            //        roomX.GetComponent<GameRoomManager>().Player1 = player;

            //    }
            //}

            roomX.GetComponent<GameRoomManager>().Player2 = null;
            roomX.GetComponent<GameRoomManager>().playerID2 = 0;
   


            GameRooms.Add(roomX);
         
            notifyUser(5, userID, roomName);


        }
        else if(GameRooms.Count >0)
        {
            foreach (var rooms in GameRooms)
            {
                if(rooms.name == roomName)
                {
                    rooms.GetComponent<GameRoomManager>().playerID2 = userID;
                    
                    notifyUser(6, userID, "");
                }
                else
                {
                    //Create room



                    GameObject roomX;
                    roomX = Instantiate(ObjectsPrefabs[1], this.gameObject.transform.position, Quaternion.identity) as GameObject;
                    roomX.transform.parent = transform;
                    roomX.GetComponent<GameRoomManager>().name = roomName;
                    roomX.GetComponent<GameRoomManager>().roomName = roomName;

                    // roomX.GetComponent<GameRoomManager>().Player1.name = playerName;

                    foreach (var player in Players)
                    {
                        if (player.name == playerName)
                        {
                            roomX.GetComponent<GameRoomManager>().Player1.name = player.GetComponent<PlayerInfo>().playerName;
                            roomX.GetComponent<GameRoomManager>().playerID1 = player.GetComponent<PlayerInfo>().userID;
                            roomX.GetComponent<GameRoomManager>().Player1 = player;

                        }
                    }


                    roomX.GetComponent<GameRoomManager>().Player2.name = "";
                    roomX.GetComponent<GameRoomManager>().playerID2 = 0;
                  


                    GameRooms.Add(roomX);
                    notifyUser(5, userID, roomName);


                }
            }
  
            
        }


    }
    static public void SetSystemManager(GameObject SystemManager)
    {
        sManager = SystemManager;
    }
    public void notifyUser(int actionID, int userID, string message )
    {
        string msg = "";

        switch (actionID)
        {

            case 0:
                msg = "0";// ACCESS GRANTED - GOOD USERNAME AND PASSWORD
                break;

            case 1:
                msg = "1"; // ERROR-  Account name already exist
                break;

            case 2:
                msg = "2"; // ACCESS DENIED - Wrong username
                break;
            case 3:// ACCESS DENIED -Wrong password
                msg = "3";
                break;
            case 4:
                msg = "4," + message; //Account created successfully 
                break;
            case 5://GameRoom Creation/ Joining Game Room
                msg = "5," + message;//Nameofroom;
                break ;
            case 6: // Joining Game Room
                msg = "6";
                break;
        }
        SendMessageToClient(msg, userID);
    }
}