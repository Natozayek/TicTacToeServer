using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using System;
using UnityEngine.UIElements;

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
    //Set up system manager to handle events
    static public void SetSystemManager(GameObject SystemManager)
    {
        sManager = SystemManager;
    }

    #region Send and Process messages Client-Server & Server-Client
    public void SendMessageToClient(string msg, int userID)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, userID, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int userID)
    {

        string[] dataReceived = msg.Split(',');
        switch (int.Parse(dataReceived[0]))
        {
            case 0: //LOGIN VERIFICATION
                SystemManager.Instance.LoginVerification(dataReceived[1], dataReceived[2], userID);
                break;


            case 1://CREATE ACCOUNT
                SystemManager.Instance.createAccount(dataReceived[1], dataReceived[2], userID);
                break;

            case 2://Create GameRoom or Join GameRoom

                //  CreateGameRoom(dataReceived[1], userID, dataReceived[2]);
                joinOrCreateGameRoom(userID, dataReceived[2], dataReceived[1]);//USER ID, ROOM NAME, PLAYERNAME
                break;

            case 3://Game is Ready
                StartMatch(userID, dataReceived[1]);
                break;

        }


    }
    #endregion

    #region Create player/ GameRooms/ Join Rooms/ FindPlayer/ notifyEnvets 5 & 6
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
    public void CreateGameRoom(int userID, string roomName) 
   {
            //Create room
            GameObject roomX;
            roomX = Instantiate(ObjectsPrefabs[1], this.gameObject.transform.position, Quaternion.identity) as GameObject;
            roomX.transform.parent = transform;
            roomX.GetComponent<GameRoomManager>().name = roomName;
            roomX.GetComponent<GameRoomManager>().roomName = roomName;
            roomX.GetComponent<GameRoomManager>().Player1 = Players[0];
            roomX.GetComponent<GameRoomManager>().Player1.name = Players[0].name;
            Players[0].GetComponentInParent<PlayerInfo>().playerState = PlayerStates.playerIsWaiting;
            roomX.GetComponent<GameRoomManager>().Player2 = null;
        if(userID == Players[0].GetComponentInParent<PlayerInfo>().userID)
            GameRooms.Add(roomX);
    }
    private void joinOrCreateGameRoom(int userID, string roomName, string playerName)
    {
        int playerID = FindPlayerID(userID);
        bool searchisDone = false;
        bool isPlayerFound = false;
        int i = 0;


        while (!searchisDone)
        {
            if (i == GameRooms.Count)
            {
                searchisDone = true;
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                searchisDone = true;
                isPlayerFound = true;
            }
            else
            {
                i++;
            }
        }

        if (isPlayerFound)
        {   //Join rooms
            GameRooms[i].GetComponent<GameRoomManager>().Player2 = Players[playerID];
            notifyUser(6, userID, roomName);
        }
        else
        {//Create rooms
            CreateGameRoom(Players[playerID].GetComponent<PlayerInfo>().userID, roomName);
            notifyUser(5, userID, roomName);
        }
    }
    private void StartMatch(int userID, string roomName)
    {

        bool searchisDone = false;
        int i = 0;

        Debug.Log("RoomName " + roomName);


        while (!searchisDone)
        {
            if (i == GameRooms.Count)
            {
                searchisDone = true;
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                searchisDone = true;
                Debug.Log("Search is done");
                if (searchisDone)
                {
                    if ((GameRooms[i].GetComponent<GameRoomManager>().Player2 && GameRooms[i].GetComponent<GameRoomManager>().Player1) == true)
                    {
                        Debug.Log(GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID.ToString() + " PLAYER1");
                        Debug.Log(userID + "Player 2");

                        notifyUser(7, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, roomName);
                        
                        notifyUser(7, userID, roomName);

                        Debug.Log("Notifying");

                    }

                }
            }
            else
            {
                i++;
            }
        }

     
    
    }
    public int FindPlayerID(int userID)
    {
        bool searchisDone = false;
        bool isPlayerFound = false;
        int i = 0;

        while (searchisDone == false)
        {
            Debug.Log(i);
            if (i == Players.Count)
            {
                Debug.Log("Player not found at index  :" + i);
                searchisDone = true;
            }
            else if (Players[i].GetComponent<PlayerInfo>().userID == userID)
            {
                Debug.Log(Players[i].GetComponent<PlayerInfo>().userID + " Looking for: " + userID);
                searchisDone = true;
                isPlayerFound = true;
            }
            else
            {
                Debug.Log(Players[i].GetComponent<PlayerInfo>().userID + " Player not found at index  :" + userID);
                i++;
            }
        }

        if (isPlayerFound == true)
        {
            return i;
        }

        else
        {
            return -1;
        }
    }
    public void notifyUser(int actionID, int userID, string message)
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
                break;
            case 6: // Joining Game Room
                msg = "6," + message;
                break;
            case 7:
                msg = "7," + message;
                break;
        }
        SendMessageToClient(msg, userID);
    }

    #endregion
    
}