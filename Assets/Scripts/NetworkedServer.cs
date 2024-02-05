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
        int messageType = int.Parse(dataReceived[0]);
        switch (messageType)
        {
            case ClientToServerSignifiers.LoggingVerification:
              
                Debug.Log("Verifying user login ");
                string username = dataReceived[1];
                string password = dataReceived[2];
                bool usernameLoggedin = FindPlayerByName(userID, username);

                if (usernameLoggedin)
                {
                    NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.UserAlreadyLogged, userID, "AccesDenied");
                    break;
                }
                else
                {
                    SystemManager.Instance.LoginVerification(username, password, userID);
                }
                break;

            case ClientToServerSignifiers.CreateNewAccount:
                string newUsername = dataReceived[1];
                string newPassword = dataReceived[2];
                SystemManager.Instance.createAccount(newUsername, newPassword, userID);//Username, password, id
                break;

            case ClientToServerSignifiers.CreateORJoinGameRoom://Create GameRoom or Join GameRoom #2
                string roomName = dataReceived[2];
                string playerName = dataReceived[1];
                joinOrCreateGameRoom(userID, roomName, playerName);
                break;

            case ClientToServerSignifiers.GameisReady://Game is Ready #3
                string roomNameWhenReady = dataReceived[1];
                StartMatch(userID, roomNameWhenReady);//userID,roomName
                break;

            case ClientToServerSignifiers.PlayerMadeAMove: // PlayerMove #4

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

            case ClientToServerSignifiers.RestartMatch: //#5
                ReMatch(userID, dataReceived[1]);//userId, roomName
                break;


            case ClientToServerSignifiers.PlayerLeftGameRoom: //#6
                string GameRoomName_ = dataReceived[1];
                NotifyGameOverOnPlayerLeave(GameRoomName_);//Room Name
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

            case ClientToServerSignifiers.WatchReplay: // Get data for replay mode
                GetReplayData(userID);
                break;

            case 13: //Send files to dropdown in client
                SendReplayData(userID, dataReceived[1]);
                break;

            case ClientToServerSignifiers.PlayerLeftLobbyRoom:
                {
                    string LobbyRomName = dataReceived[1];
                    leaveLobbyRoom(LobbyRomName);
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
        #region OLD CODE
        //    bool searchisDone = false;
        //    int i = userID - 1;

        //    Debug.Log("JOINNING");
        //    while (!searchisDone)
        //    {
        //        bool isCreating = false;
        //        bool isEmpty = !GameRooms.Any();
        //        if (isEmpty)
        //        {


        //            CreateGameRoom(Players[i], roomName);
        //            notifyUser(5, userID, roomName);
        //            Debug.Log("Player 1 creating ");
        //            searchisDone = true;
        //        }
        //         if (!isEmpty) 
        //        {
        //            for (int j = 0; j < GameRooms.Count; j++)
        //            {

        //                if (GameRooms[j].GetComponent<GameRoomManager>().roomName == roomName)
        //                {
        //                    GameRooms[j].GetComponent<GameRoomManager>().Player2 = Players[i];
        //                    notifyUser(6, userID, roomName);
        //                    Debug.Log("Player2 joining");
        //                    searchisDone = true;

        //                }

        //            }

        //           isCreating = true;


        //        }
        //         if (isCreating && !searchisDone)
        //         {
        //             CreateGameRoom(Players[i], roomName);
        //             notifyUser(5, userID, roomName);
        //             Debug.Log("Player 1 creating ");
        //             searchisDone = true;
        //         }
        //         else
        //        {
        //            i++;

        //        }
        //    }
        #endregion
        bool searchIsDone = false;
        int playerIndex = userID - 1;

        Debug.Log("JOINING");

        while (!searchIsDone && playerIndex < Players.Count)
        {
            if (!GameRooms.Any(room => room.GetComponent<GameRoomManager>().roomName == roomName))
            {
                // Room with the specified name doesn't exist, create a new one
                CreateGameRoom(Players[playerIndex], roomName);
                notifyUser(ServerToClientSignifiers.RoomCreated, userID, roomName);
                Debug.Log("Player 1 creating");
                searchIsDone = true;
            }
            else
            {
                // Room with the specified name exists, join it
                var existingRoom = GameRooms.First(room => room.GetComponent<GameRoomManager>().roomName == roomName);
                existingRoom.GetComponent<GameRoomManager>().Player2 = Players[playerIndex];
                notifyUser(ServerToClientSignifiers.JoinRoomX, userID, roomName);
                Debug.Log("Player 2 joining");
                searchIsDone = true;
            }

            playerIndex++;
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
        Debug.Log("RoomName: " + roomName);
        Debug.Log("Starting Match");

        while (!searchisDone && i < GameRooms.Count)
        {
            if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                searchisDone = true;

                if (GameRooms[i].GetComponent<GameRoomManager>().Player1 != null && GameRooms[i].GetComponent<GameRoomManager>().Player2 != null)
                {
                    // Generate a random number (0 or 1) to determine the starting player
                    int startingPlayer = UnityEngine.Random.Range(0, 2);

                    // Notify both players with the randomized starting player
                    notifyUser(ServerToClientSignifiers.StartMatch, GameRooms[i].GetComponent<GameRoomManager>().Player1.GetComponent<PlayerInfo>().userID, roomName + "," + startingPlayer);
                    notifyUser(ServerToClientSignifiers.StartMatch, GameRooms[i].GetComponent<GameRoomManager>().Player2.GetComponent<PlayerInfo>().userID, roomName + "," + (1 - startingPlayer));
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
    public bool FindPlayerByName(int userID, string username)
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
    public void leaveLobbyRoom(string roomName)
    {
        Debug.Log("LEAVING LOBBY ROOM EXECUTE");
        bool searchIsDone = false;
        int i = 0;

        // Debug log to check the value of roomName parameter
        Debug.Log("Leaving room: " + roomName);

        while (!searchIsDone && i < GameRooms.Count)
        {
            // Debug logs to check room names and the target room name
            Debug.Log("Checking room: " + GameRooms[i].GetComponent<GameRoomManager>().roomName);
            Debug.Log("Target room: " + roomName);

            if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                Debug.Log("ROOM FOUND EXECUTE");

                // Notify players and spectators that a player left the room
                // ... (existing code)

                // Clear Player1 and Player2 references
                GameRooms[i].GetComponent<GameRoomManager>().Player1 = null;
                GameRooms[i].GetComponent<GameRoomManager>().Player2 = null;

                // Remove the player from the spectators list if needed
                // ...

                // Check if the room is empty and destroy it if so
                if (IsRoomEmpty(GameRooms[i].GetComponent<GameRoomManager>()))
                {
                    Debug.Log("Room to be removed: " + GameRooms[i].GetComponent<GameRoomManager>().roomName);
                    Destroy(GameRooms[i]);
                    GameRooms.RemoveAt(i);
                    Debug.Log("Room removed. GameRooms count: " + GameRooms.Count);
                }
                else
                {
                    // Move to the next room if it's not empty
                    i++;
                }

                searchIsDone = true;
                Debug.Log("COMPLETED EXECUTE");
            }
            else
            {
                i++;
            }
        }

        Debug.Log("Player removed. Players count: " + Players.Count);
    }
    public void NotifyGameOverOnPlayerLeave(string roomName)
    {
        Debug.Log("NOTIFY GAME OVER ON PLAYER LEAVE");

        int roomIndex = FindGameRoomIndex(roomName);

        if (roomIndex != -1)
        {
            HandlePlayerLeaveNotifications(GameRooms[roomIndex]);
            DestroyAndRemoveGameRoom(roomIndex);
        }
        else
        {
            Debug.LogError("Room not found: " + roomName);
        }

        Debug.Log("COMPLETED EXECUTE");
    }
    private void HandlePlayerLeaveNotifications(GameObject gameRoom)
    {
        GameObject player1 = gameRoom.GetComponent<GameRoomManager>().Player1;
        GameObject player2 = gameRoom.GetComponent<GameRoomManager>().Player2;

        NotifyPlayerGameOver(player1);
        NotifyPlayerGameOver(player2);

        List<GameObject> spectators = gameRoom.GetComponent<GameRoomManager>().spectators;
        NotifySpectatorsGameOver(spectators);

        // Clear Player1 and Player2 references
        gameRoom.GetComponent<GameRoomManager>().Player1 = null;
        gameRoom.GetComponent<GameRoomManager>().Player2 = null;

        // Clear the spectators list
        spectators.Clear();
    }

    private void NotifyPlayerGameOver(GameObject player)
    {
        if (player != null)
        {
            notifyUser(ServerToClientSignifiers.PlayerLeftGameRoom, player.GetComponent<PlayerInfo>().userID, "Player left the game");
        }
    }

    private void NotifySpectatorsGameOver(List<GameObject> spectators)
    {
        foreach (var spectator in spectators)
        {
            notifyUser(ServerToClientSignifiers.PlayerLeftGameRoom, spectator.GetComponent<PlayerInfo>().userID, "Player left the game");
        }
    }
    private void DestroyAndRemoveGameRoom(int roomIndex)
    {
        Debug.Log("Room to be removed: " + GameRooms[roomIndex].GetComponent<GameRoomManager>().roomName);
        Destroy(GameRooms[roomIndex]);
        GameRooms.RemoveAt(roomIndex);
        Debug.Log("Room removed. GameRooms count: " + GameRooms.Count);
    }
    private int FindGameRoomIndex(string roomName)
    {
        for (int i = 0; i < GameRooms.Count; i++)
        {
            if (GameRooms[i].GetComponent<GameRoomManager>().roomName == roomName)
            {
                return i;
            }
        }
        return -1;
    }
    private bool IsRoomEmpty(GameRoomManager roomManager)
    {
        return roomManager.Player1 == null && roomManager.Player2 == null && (roomManager.spectators == null || roomManager.spectators.Count == 0);
    }
   
    
    private void LogOutUser(int userID)
    {
        bool searchIsDone = false;
        Debug.Log("Logging Out");

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].GetComponent<PlayerInfo>().userID == userID)
            {
                searchIsDone = true;
                Debug.Log(Players[i].GetComponent<PlayerInfo>().userID.ToString());
                Debug.Log("Player Found");
                Debug.Log(Players[i].GetComponent<PlayerInfo>().playerName.ToString() + " Player name");

                string playerName = Players[i].GetComponent<PlayerInfo>().playerName.ToString();
                GameObject playerObject = GameObject.Find(playerName);

                if (playerObject != null)
                {
                    Players.RemoveAt(i);
                    Destroy(playerObject);
                    Debug.Log("Player found and deleted");
                }
                else
                {
                    Debug.LogError("Player GameObject not found for player name: " + playerName);
                }
            }
        }

        if (!searchIsDone)
        {
            Debug.LogError("Player not found with userID: " + userID);
        }
    }
    //private void GetReplayData(int userID)
    //{

    //    bool searchisDone = false;
    //    Debug.Log("GetReplayData");
    //    int i = 0;
    //    while (!searchisDone)
    //    {

    //        if (Players[i].GetComponent<PlayerInfo>().userID == userID)
    //        {
    //            searchisDone = true;


    //             string name = Players[i].GetComponent<PlayerInfo>().playerName.ToString();

    //            DataManager.VerifyReplayData(name, userID);
    //            Debug.Log("Verifying data");

    //        }
    //        else
    //        {
    //            Debug.Log("i++");
    //            i++;
    //        }
    //    }
    //}

    private void GetReplayData(int userID)
    {
        string playerName = GetPlayerNameByUserID(userID);

        if (playerName != null)
        {
            DataManager.VerifyReplayData(playerName, userID);
            Debug.Log("Verifying data");
        }
        else
        {
            Debug.Log("Player not found with userID: " + userID);
        }
    }

    private string GetPlayerNameByUserID(int userID)
    {
        foreach (var player in Players)
        {
            if (player.GetComponent<PlayerInfo>().userID == userID)
            {
                return player.GetComponent<PlayerInfo>().playerName.ToString();
            }
        }

        return null; // Player not found
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
                msg = ServerToClientSignifiers.PlayerLeftGameRoom + "," + message;
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
            case 20: 
                msg = ServerToClientSignifiers.InvalidAccountInformation + "," + message;
                break;
        }
        SendMessageToClient(msg, userID);
    }



    #endregion
    
}
static public class ClientToServerSignifiers
{
     public const int LoggingVerification = 0;
     public const int CreateNewAccount = 1;
     public const int CreateORJoinGameRoom = 2;
     public const int GameisReady = 3;
     public const int PlayerMadeAMove = 4;
     public const int RestartMatch = 5;
     public const int PlayerLeftGameRoom = 6;
     public const int SendMessageToOtherPlayer = 7;
     public const int SaveReplayData = 8;
     public const int SpectateRoom = 9;
     public const int LogOut = 11;
     public const int WatchReplay = 12;
     public const int PlayReplay = 13;
     public const int PlayerLeftLobbyRoom = 14;
}

static public class ServerToClientSignifiers
{

     public const int AcessGranted = 0;
     public const int AccountNameAlreadyExist = 1;
     public const int WrongUsername = 2;
     public const int WrongPassword = 3;
     public const int AccountCreatedSuccessfully = 4;
     public const int RoomCreated = 5;
     public const int JoinRoomX = 6;
     public const int StartMatch = 7;
     public const int PlayerXMadeAMove = 8;
     public const int RestartMatch = 9;
     public const int UserAlreadyLogged = 10;
     public const int PlayerLeftGameRoom = 11;
     public const int DisplayMessageInScreen = 12;
     public const int SetSpectatorMode = 13;
     public const int GetReplayData = 15;
     public const int ReplayModeOn = 16;
     public const int DataConfirmation = 17;
     public const int LeaveGameRoomLobby = 14;
     public const int InvalidAccountInformation = 20;
    public const int NoReplayDataSaved = 21;

}