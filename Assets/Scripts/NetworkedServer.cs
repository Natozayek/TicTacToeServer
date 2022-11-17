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

    public string usedButtons = "";

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
                bool usernameLoggedin = false;

               usernameLoggedin =  FindPlayerByName(userID, dataReceived[1]);


                if (usernameLoggedin)
                {
                    NetworkedServer.Instance.notifyUser(10, userID, "AccesDenied"); // ACCESS Denied 
                    break;
                }
                else
                {

                    SystemManager.Instance.LoginVerification(dataReceived[1], dataReceived[2], userID);
                }

                break;


            case 1://CREATE ACCOUNT
                SystemManager.Instance.createAccount(dataReceived[1], dataReceived[2], userID);
                break;

            case 2://Create GameRoom or Join GameRoom

                joinOrCreateGameRoom(userID, dataReceived[2], dataReceived[1]);//USER ID, ROOM NAME, PLAYERNAME
                break;

            case 3://Game is Ready
                StartMatch(userID, dataReceived[1]);
                break;

            case 4: // PlayerMove

                if(usedButtons == "")
                {
                    usedButtons = dataReceived[1];
                }
                else
                {
                    usedButtons = usedButtons + "," + (dataReceived[1]);
                }
                
                PlayerXMadeMove(userID, int.Parse(dataReceived[1]), int.Parse(dataReceived[2]));//UserID, ButtonIndex, PlayerOnTurn
                break;

            case 5: //Reset game
                ReMatch(userID, dataReceived[1]);
                break;


            case 6: //Leave game notification
                Debug.Log("Find room by name and delete it");
                leaveRoomName(userID, dataReceived[1]);
                break;

            case 7://Message received in the server now send it to X player to show it in their screen
                displayMessage(userID, dataReceived[1]);
                break;

            case 8://Save replay data - Username (for folder name ), UserID, turnofPlayer, replayName, usedButtons 
               SaveReplay(userID, int.Parse(dataReceived[1]), dataReceived[2].ToString(), usedButtons);
                break;


        }


    }




    #endregion

    #region Create player/ GameRooms/ Join Rooms/ FindPlayer/ notifyEnvets 5 & 6
    public void CreatePlayer(string playerName, int userID)
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

        Debug.Log("JOINNING");
        while (!searchisDone)
        {
            if (i == GameRooms.Count)
            {
                searchisDone = true;
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {

              searchisDone=true;


            }
            else
            {
                i++;
                
            }
        }

      
        if(searchisDone && GameRooms.Count > 0)
        {
        
             if (GameRooms[i].GetComponent<GameRoomManager>().Player2 == null)
            {   //Join rooms
                GameRooms[i].GetComponent<GameRoomManager>().Player2 = Players[playerID];
                notifyUser(6, userID, roomName);
                Debug.Log("Player2 joining");

            }
            else
            {
                    //Join as spectator
                    GameRooms[i].GetComponent<GameRoomManager>().spectators.Add(Players[playerID]);
                    notifyUser(13, userID, "");
                    notifyUser(6, userID, roomName);
                    Debug.Log("Spectator joining");

                
            }


        }
        else
        {
            //Create rooms
            CreateGameRoom(Players[playerID].GetComponent<PlayerInfo>().userID, roomName);
            notifyUser(5, userID, roomName);
            Debug.Log("Player 1 creating ");
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

                        notifyUser(7, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, roomName + ",0");
                        
                        notifyUser(7, userID, roomName + ",1");

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
    private void ReMatch(int userID, string roomName)
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
                       if(userID== GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID)
                        {
                            notifyUser(9,userID, roomName + ",0");
                            notifyUser(9, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, roomName + ",1");

                            for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                            {
                                notifyUser(9, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, roomName + ",0");

                            }
                        }
                        else
                        {
                               notifyUser(9, userID, roomName + ",0");
                               notifyUser(9, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, roomName + ",1");

                            for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                            {
                                notifyUser(9, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, roomName + ",0");

                            }
                        }

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
    public bool FindPlayerByName(int userID, string name)
    {
        bool searchisDone = false;
        bool isPlayerFound = false;
        int i = 0;

        while (searchisDone == false)
        {

            if (i == Players.Count)
            {
                Debug.Log("Player not found :" + i);
                searchisDone = true;
            }
            else if (Players[i].GetComponent<PlayerInfo>().playerName == name)
            {
                Debug.Log(Players[i].GetComponent<PlayerInfo>().playerName + " <---  Name  tringying to join ->: " + name);
                searchisDone = true;
                isPlayerFound = true;
            }
            else
            {
                Debug.Log(name + "  not found " );
                i++;
            }
        }

        if (isPlayerFound == true)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    public void  leaveRoomName(int userID, string roomName)
    {
        bool searchisDone = false;
        bool isRoomFound = false;
        int i = 0;
        while (!searchisDone)
        {
            if (i == GameRooms.Count)
            {
                searchisDone = true;
                Debug.Log("no rooms available");
            }

            else if(GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                Debug.Log("room found");


                if (GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID == userID)
                 {
                    searchisDone = true;
                      if (searchisDone)
                      {
                          notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, "Player left the game") ;
                          notifyUser(11, userID, "Player left the game");

                        for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                        {
                            notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, "Player left the game");

                        }

                        isRoomFound = true;


                        if (isRoomFound)

                        {
                            GameRooms.RemoveAt(i);
                            GameObject roomObject = GameObject.Find(roomName.ToString());
                            Destroy(roomObject);
                            Debug.Log("Room found and deleted");
                        }

                      }
                 }
                else if (GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID == userID)
                {
                    searchisDone = true;
                    if (searchisDone)
                    {
                        notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, "Player left the game");
                        notifyUser(11, userID, "Player left the game");

                        for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                        {
                            notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, "Player left the game");

                        }

                        isRoomFound = true;
                        if(isRoomFound)
                        {
                            GameRooms.RemoveAt(i);
                            GameObject roomObject = GameObject.Find(roomName.ToString());
                            Destroy(roomObject);
                            Debug.Log("Room found and deleted");

                        }
                    }
                }
            }
           
            else
            {
                i++;
            }  
        }
    }

    public void SaveReplay(int userID, int PlayerInTurn, string replayname, string usedButtons)
    {
        bool searchisDone = false;
        int i = 0;


        while (!searchisDone)
        {
            if (i == Players.Count)
            {
                searchisDone = true;
            }
            else if (Players[i].GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
            }
            else
            {
                i++;

            }
        }

        if (searchisDone)
        {

           string playerName = Players[i].GetComponent<PlayerInfo>().gameObject.name;
            SystemManager.Instance.SavingDataInServer(playerName, PlayerInTurn, replayname, usedButtons);  
        }
    }

    private void PlayerXMadeMove(int userID, int ButtonIndex, int turnOfPlayerX)
    {
        bool searchisDone = false;
        int i = 0;
    

        while (!searchisDone)
        {
            if (i == GameRooms.Count)
            {
                searchisDone = true;
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID == userID )
            {
                searchisDone = true;
                if (searchisDone)
                {
                    notifyUser(8, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, ButtonIndex.ToString() + "," + turnOfPlayerX.ToString());

                    for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                    {
                        notifyUser(8, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, ButtonIndex.ToString() + "," + turnOfPlayerX.ToString());

                    }

                }
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
                if (searchisDone)
                {
                    notifyUser(8, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, ButtonIndex.ToString() + "," + turnOfPlayerX.ToString());

                    for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                    {
                        notifyUser(8, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, ButtonIndex.ToString() + "," + turnOfPlayerX.ToString());

                    }

                }
            }
           
            else
            {
                i++;
            }
        }


    }
    public void displayMessage(int userID, string message)
    {
        bool searchisDone = false;
        int i = 0;


        while (!searchisDone)
        {
            if (i == GameRooms.Count)
            {
                searchisDone = true;
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
                if (searchisDone)
                {
                    notifyUser(12, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, message);

                }
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
                if (searchisDone)
                {
                    notifyUser(12, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, message);

                }
            }
            else
            {
                i++;
            }
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
            case 7://Start Game
                msg = "7," + message;
                break;

            case 8: // SEND MOVE TO OTHER PLAYER
                msg = "8," + message;
                break;
            
            case 9: // SEND MOVE TO OTHER PLAYER
                msg = "9," + message;
                break;
            case 10: // Error - Player already connected
                msg = "10," + message;
                break;

            case 11: // Error - Player left the game room
                msg = "11," + message;
                break;

            case 12: //Message to display
                msg = "12," + message;
                break;
            case 13: // Case spectator mode
                msg = "13," + message;
                break;
        }
        SendMessageToClient(msg, userID);
    }



    #endregion
    
}