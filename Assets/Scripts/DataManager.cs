using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;
using System.Runtime.InteropServices.ComTypes;

public class DataManager : MonoBehaviour
{
    static  GameObject sManager, Network;
    static public void SetSystemManager(GameObject SystemManager)
    {
        sManager = SystemManager;
    }
    #region Save Data for username creation/ Verify Data to handle loging access
    static public void SaveData(string username, string password)
    {
            using (StreamWriter sw = File.AppendText(@"..\TicTacToeGameServer\Users\"+ username + ".txt"))
            {
                sw.WriteLine(username + "," + password);
            }
    }
    static public void VerifyData(string username, string password, int userID)
    {
        // Construct the file path based on the username
        string filePath = Path.Combine(@"..\TicTacToeGameServer\Users", $"{username}.txt");
        try
        {
            // Check if the file exists
            if (File.Exists(filePath))
            {
                Debug.Log("File Exists");

                // Read the content of the file
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Read the first line of the file
                    string line = sr.ReadLine();

                    // Split the line into an array of strings using ',' as the delimiter,
                    // and remove any empty entries from the result.
                    string[] lineData = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // Check if the split resulted in two non-empty parts
                    if (lineData.Length == 2 && lineData[0] == username && lineData[1] == password)
                    {
                        Debug.Log("Correct username & password");
                        NetworkedServer.Instance.CreatePlayer(lineData[0], userID);
                        NetworkedServer.Instance.NotifyUser(ServerToClientSignifiers.AcessGranted, userID, "Access Granted");
                    }
                    else
                    {
                        Debug.Log("Access Denied - Wrong Password");
                        NetworkedServer.Instance.NotifyUser(ServerToClientSignifiers.WrongPassword, userID, "Access Denied - Wrong password");
                    }
                }
            }
            else
            {
                Debug.Log("Access Denied - Wrong username");
                NetworkedServer.Instance.NotifyUser(ServerToClientSignifiers.WrongUsername, userID, "Access Denied - Wrong username");
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that might occur during file operations
            Debug.LogError($"Error while verifying user data: {ex.Message}");
        }
     
    }

    static public void SaveReplayData(string username, int turnofPlayer, string replayName, string usedButtons)
    {
        if (!File.Exists(@"..\TicTacToeGameServer\ReplayData\" + username + @"\" + replayName + ".txt"))
        {

            Directory.CreateDirectory(@"..\TicTacToeGameServer\ReplayData\" + username);

            using (StreamWriter streamWriter = new StreamWriter(@"..\TicTacToeGameServer\ReplayData\" + username + @"\" + replayName + ".txt"))
            {
                streamWriter.WriteLine(turnofPlayer + "," + usedButtons);
            }
        }
        if (File.Exists(@"..\TicTacToeGameServer\ReplayData\" + username + @"\" + replayName + ".txt"))
        {

            using (StreamWriter streamWriter = new StreamWriter(@"..\TicTacToeGameServer\ReplayData\" + username + @"\" + replayName + ".txt"))
            {
                streamWriter.WriteLine(turnofPlayer + "," + usedButtons);
            }
        }
    }

    static public void VerifyReplayData(string username, int userID)
    {
        try
        {
            string directoryPath = Path.Combine("..", "TicTacToeGameServer", "ReplayData", username);

            if (Directory.Exists(directoryPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

                foreach (var currentFile in directoryInfo.GetFiles("*.txt"))
                {
                    string filename = currentFile.Name;
                    Debug.Log(filename);

                    // Notify the client about each replay file
                    NetworkedServer.Instance.NotifyUser(ServerToClientSignifiers.GetReplayData, userID, filename);
                }

                // Notify the client that the data is confirmed
                NetworkedServer.Instance.NotifyUser(ServerToClientSignifiers.DataConfirmation, userID, "");
            }
            else
            {
                // Notify the client about the absence of replay data
                NetworkedServer.Instance.NotifyUser(ServerToClientSignifiers.NoReplayDataSaved, userID, "No data");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error processing replay data: " + ex.Message);
        }
    }

    static public void SendReplayData(string username, int userID, string replayName)
    {
        if (File.Exists(@"..\TicTacToeGameServer\ReplayData\" + username + @"\" + replayName))
        {
            Debug.Log("file Exist");

            using (StreamReader sr = new StreamReader(@"..\TicTacToeGameServer\ReplayData\" + username + @"\" + replayName))
             {
                            string line = sr.ReadLine();
                            NetworkedServer.Instance.NotifyUser(16, userID, line);
                            sr.Close();
             }
        }

    }

    #endregion


}
