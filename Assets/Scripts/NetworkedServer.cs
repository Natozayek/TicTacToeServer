using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using System;
using System.Linq;
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
                Debug.Log("Connecting, Player ID: " + recConnectionID + " attempting to establish network event.");
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Client ID : " + recConnectionID + " disconnected.");
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
                SystemManager.Instance.createAccount(dataReceived[1], dataReceived[2], userID);//Username, password, id
                break;

            case 2://Create GameRoom or Join GameRoom

                joinOrCreateGameRoom(userID, dataReceived[2], dataReceived[1]);//USER ID, ROOM NAME, PLAYERNAME
                break;

            case 3://Game is Ready
                StartMatch(userID, dataReceived[1]);//userID,roomName
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
                ReMatch(userID, dataReceived[1]);//userId, roomName
                break;


            case 6: //Leave game notification
                leaveRoomName(dataReceived[1]);//Room Name
                break;

            case 7://Message received in the server now send it to X player to show it in their screen
                displayMessage(userID, dataReceived[1]);// userId, message
                break;

            case 8://Save replay data - Username (for folder name ), UserID, turnofPlayer, replayName, usedButtons 
               SaveReplay(userID, int.Parse(dataReceived[1]), dataReceived[2].ToString(), usedButtons);
                break;

            case 9://Create GameRoom or Join GameRoom

                SpectateGameRoom(userID, dataReceived[2], dataReceived[1]);//USER ID, ROOM NAME, PLAYERNAME
                break;

            case 10:
                break;


            case 11: // Player Log out - Deletes the player from the list of Players
                LogOutUser(userID);
                break;

            case 12: // Get data for replay mode
                GetReplayData(userID);
                break;

            case 13: //Send files to dropdown in client
                SendReplayData(userID, dataReceived[1]);
                break;

            case 14:
            {
                    leaveLobbyRoomName(dataReceived[1]);
                    break;
            }
        }


    }

 


    #endregion

    #region Create player/ GameRooms/ Join Rooms/SpectateGame/StartMatch/Rematch/ FindPlayer/leavegameroom/logout/ notifyEnvets / Player move/ SaveReplayData
    public void CreatePlayer(string playerName, int userID)
    {
        GameObject playerX;
        playerX = Instantiate(ObjectsPrefabs[0]);
        playerX.transform.parent = transform;

        
        playerX.GetComponent<PlayerInfo>().name = playerName;
        playerX.GetComponent<PlayerInfo>().playerName = playerName;
        playerX.GetComponent<PlayerInfo>().userID = userID;
        Players.Add(playerX);

    }
    public void CreateGameRoom(GameObject player, string roomName) 
   {
            //Create room
            GameObject roomX;
            roomX = Instantiate(ObjectsPrefabs[1], this.gameObject.transform.position, Quaternion.identity) as GameObject;
            roomX.transform.parent = transform;
            roomX.GetComponent<GameRoomManager>().name = roomName;
            roomX.GetComponent<GameRoomManager>().roomName = roomName;
        
            roomX.GetComponent<GameRoomManager>().Player1 = player;
            roomX.GetComponent<GameRoomManager>().Player1.name = player.name;
            roomX.GetComponent<GameRoomManager>().Player2 = null;
            GameRooms.Add(roomX);
    }
    private void joinOrCreateGameRoom(int userID, string roomName, string playerName)
    {
        bool searchisDone = false;
        int i = userID - 1;

        Debug.Log("JOINNING");
        while (!searchisDone)
        {
            bool isCreating = false;
            bool isEmpty = !GameRooms.Any();
            if (isEmpty)
            {
               
                
                CreateGameRoom(Players[i], roomName);
                notifyUser(5, userID, roomName);
                Debug.Log("Player 1 creating ");
                searchisDone = true;
            }
             if (!isEmpty) 
            {
                for (int j = 0; j < GameRooms.Count; j++)
                {
              
                    if (GameRooms[j].GetComponent<GameRoomManager>().roomName == roomName)
                    {
                        GameRooms[j].GetComponent<GameRoomManager>().Player2 = Players[i];
                        notifyUser(6, userID, roomName);
                        Debug.Log("Player2 joining");
                        searchisDone = true;
        
                    }

                }

               isCreating = true;

                 
            }
             if (isCreating && !searchisDone)
             {
                 CreateGameRoom(Players[i], roomName);
                 notifyUser(5, userID, roomName);
                 Debug.Log("Player 1 creating ");
                 searchisDone = true;
             }
             else
            {
                i++;
                
            }
        }

   

    }
    private void SpectateGameRoom(int userID, string roomName, string playerName)
    {
        int playerID = FindPlayerID(userID);
        bool searchisDone = false;
        int i = 0;

        Debug.Log("JOINNING");
        while (!searchisDone)
        {
            if (GameRooms.Any())
            {
                searchisDone = true;
            }
            else if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {

                GameRooms[i].GetComponent<GameRoomManager>().spectators.Add(Players[playerID]);
                notifyUser(13, userID, "");
                notifyUser(6, userID, roomName);
                Debug.Log("Spectator joining");


            }
            else
            {
                i++;

            }
        }

    
    }
        private void StartMatch(int userID, string roomName)
    {

        bool searchisDone = false;
        int i = 0;

        Debug.Log("RoomName " + roomName);


        while (!searchisDone)
        {
          
             if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
             {
                 searchisDone = true;

                if (searchisDone)
                {

                    if ((GameRooms[i].GetComponent<GameRoomManager>().Player2 && GameRooms[i].GetComponent<GameRoomManager>().Player1) != null)
                    {
                        
                        notifyUser(7, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, roomName + ",0");
                        notifyUser(7, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, roomName + ",1");


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
                            notifyUser(9,userID, roomName + ",1");
                            notifyUser(9, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, roomName + ",0");

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

                    usedButtons = "";
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
    public void  leaveRoomName(string roomName)
    {
        Debug.Log("LEAVING  GAME EXECUTE");
        bool searchisDone = false;
  
        int i = 0;
        while (!searchisDone)
        {
  
             if(GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                Debug.Log("ROOM FOUND EXECUTE");
                if (GameRooms[i].GetComponent<GameRoomManager>().Player1 != null )
                {
                    notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, "Player left the game");
                    
                }
                if (GameRooms[i].GetComponent<GameRoomManager>().Player2 != null)
                {
                    notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, "Player left the lobby");
                }

                if (GameRooms[i].GetComponent<GameRoomManager>().spectators != null)
                {
                    for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                    {
                        notifyUser(11, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, "Player left the game");
                    }
                }
         

                GameRooms.RemoveAt(i);
                GameObject roomObject = GameObject.Find(roomName);
                Destroy(roomObject);
                searchisDone = true;
                Debug.Log("COMPLETED EXECUTE");

            }

            else
            {
                i++;
            }  
        }
    }
    public void leaveLobbyRoomName(string roomName)
    {
        Debug.Log("LEAVING LOBBY EXECUTE");
        bool searchisDone = false;
        bool isRoomFound = false;
        int i = 0;
        while (!searchisDone)
        {

            if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                Debug.Log("ROOM FOUND EXECUTE");
                if (GameRooms[i].GetComponent<GameRoomManager>().Player1 != null)
                {
                    notifyUser(14, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, "Player left the lobby");

                }
                if (GameRooms[i].GetComponent<GameRoomManager>().Player2 != null)
                {
                    notifyUser(14, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, "Player left the lobby");
                }

                 if (GameRooms[i].GetComponent<GameRoomManager>().spectators != null)
                {
                    for (int j = 0; j < GameRooms[i].GetComponent<GameRoomManager>().spectators.Count; j++)
                    {
                        notifyUser(14, GameRooms[i].GetComponent<GameRoomManager>().spectators[j].GetComponent<PlayerInfo>().userID, "Player left the lobby");
                    }
                }


                GameRooms.RemoveAt(i);
                GameObject roomObject = GameObject.Find(roomName.ToString());
                Destroy(roomObject);
                searchisDone = true;
                Debug.Log("COMPLETED EXECUTE");
      
            }

            else
            {
                i++;
            }
        }
    }
    private void LogOutUser(int userID)
    {
        bool searchisDone = false;
        Debug.Log("logOUt");
    
        int i = 0;
        while (!searchisDone)
        {
          
            if (Players[i].GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
                Debug.Log(Players[i].GetComponent<PlayerInfo>().userID.ToString());
                Debug.Log("PlayerFound");
                Debug.Log(Players[i].GetComponent<PlayerInfo>().playerName.ToString() + " Player name");
                {
                    string name = Players[i].GetComponent<PlayerInfo>().playerName.ToString();

                    
                    GameObject playerObject = GameObject.Find(name);
                    Players.RemoveAt(i);
                    Destroy(playerObject);
                    Debug.Log("Player found and deleted");

                }
            }
            else
            {
                Debug.Log("i++");
                i++;
            }
        }
    }

    private void GetReplayData(int userID)
    {

        bool searchisDone = false;
        Debug.Log("GetReplayData");
        int i = 0;
        while (!searchisDone)
        {

            if (Players[i].GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
            
                
                 string name = Players[i].GetComponent<PlayerInfo>().playerName.ToString();

                DataManager.VerifyReplayData(name, userID);
                Debug.Log("Verifying data");
                
            }
            else
            {
                Debug.Log("i++");
                i++;
            }
        }
    }
    private void SendReplayData(int userID, string replayName)
    {
        bool searchisDone = false;
        Debug.Log("SendReplayData");
        int i = 0;
        while (!searchisDone)
        {
            if (Players[i].GetComponent<PlayerInfo>().userID == userID)
            {
                searchisDone = true;
                string name = Players[i].GetComponent<PlayerInfo>().playerName.ToString();
                DataManager.SendReplayData(name, userID, replayName);

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
                msg = ServerToClientSignifiers.AcessGranted.ToString();// ACCESS GRANTED - GOOD USERNAME AND PASSWORD
                break;

            case 1:
                msg = ServerToClientSignifiers.AccountNameAlreadyExist.ToString(); // ERROR-  Account name already exist
                break;

            case 2:
                msg = ServerToClientSignifiers.WrongUsername.ToString(); // ACCESS DENIED - Wrong username
                break;
            case 3:// ACCESS DENIED -Wrong password

                msg = ServerToClientSignifiers.WrongPassword.ToString();
                break;
            case 4:
                msg = ServerToClientSignifiers.AccountCreatedSuccessfully + "," + message; //Account created successfully 
                break;
            case 5://GameRoom Creation/ Joining Game Room
                msg = ServerToClientSignifiers.RoomCreated + "," + message;//Nameofroom;
                break;
            case 6: // Joining Game Room
                msg = ServerToClientSignifiers.JoinRoomX + "," + message;
                break;
            case 7://Start Game
                msg =  ServerToClientSignifiers.StartMatch + "," + message;
                break;

            case 8: // SEND MOVE TO OTHER PLAYER
                msg =  ServerToClientSignifiers.PlayerXMadeAMove +"," + message;
                break;
            
            case 9: //RESTART MATCH
                msg = ServerToClientSignifiers.RestartMatch + "," + message;
                break;
            case 10: // Error - Player already connected
                msg = ServerToClientSignifiers.UserAlreadyLogged + "," + message;
                break;

            case 11: // Error - Player left the game room
                msg = ServerToClientSignifiers.LeaveGameRoom + "," + message;
                break;

            case 12: //Message to display
                msg =  ServerToClientSignifiers.DisplayMessageInScreen + "," + message;
                break;
            case 13: // Case spectator mode
                msg = ServerToClientSignifiers.SetSpectatorMode + "," + message;
                break;
            case 14:
                msg = ServerToClientSignifiers.LeaveGameRoomLobby +"," + message;
                break;

            case 15: // Get replay data
                msg = ServerToClientSignifiers.GetReplayData + "," + message;
                break;

            case 16: // Send replay data
                msg = ServerToClientSignifiers.ReplayModeOn + "," + message;
                break;
            case 17: // Send confirmation that data is already received
                msg = ServerToClientSignifiers.DataConfirmation + "," + message;
                break;
        }
        SendMessageToClient(msg, userID);
    }



    #endregion
    
}
static public class ClientToServerSignifiers
{
    static public int AccessVerification = 0;
    static public int CreateNewAccount = 1;
    static public int CreateORJoinGameRoom = 2;
    static public int GameisReady = 3;
    static public int playerMoved = 4;
    static public int RestartMatch = 5;
    static public int LeaveGameNotification = 6;
    static public int SendMessageToOtherPlayer = 7;
    static public int SaveReplayData = 8;
    static public int SpectateRoom = 9;
    static public int LogOut = 11;
    static public int WatchReplay = 12;
    static public int PlayReplay = 13;
    static public int LeaveGameRoomLobby = 14;
}

static public class ServerToClientSignifiers
{

    static public int AcessGranted = 0;
    static public int AccountNameAlreadyExist = 1;
    static public int WrongUsername = 2;
    static public int WrongPassword = 3;
    static public int AccountCreatedSuccessfully = 4;
    static public int RoomCreated = 5;
    static public int JoinRoomX = 6;
    static public int StartMatch = 7;
    static public int PlayerXMadeAMove = 8;
    static public int RestartMatch = 9;
    static public int UserAlreadyLogged = 10;
    static public int LeaveGameRoom = 11;
    static public int DisplayMessageInScreen = 12;
    static public int SetSpectatorMode = 13;
    static public int GetReplayData = 15;
    static public int ReplayModeOn = 16;
    static public int DataConfirmation = 17;
    static public int LeaveGameRoomLobby = 14;


}