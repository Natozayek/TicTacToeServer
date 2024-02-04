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
                        NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.AcessGranted, userID, "Access Granted");
                    }
                    else
                    {
                        Debug.Log("Access Denied - Wrong Password");
                        NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.WrongPassword, userID, "Access Denied - Wrong password");
                    }
                }
            }
            else
            {
                Debug.Log("Access Denied - Wrong username");
                NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.WrongUsername, userID, "Access Denied - Wrong username");
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that might occur during file operations
            Debug.LogError($"Error while verifying user data: {ex.Message}");
        }
        #region OLD CODE
        //if (File.Exists(@"..\TicTacToeGameServer\Users\" + username+ ".txt"))
        //{
        //    Debug.Log("file Exist");
        //    using (StreamReader sr = new StreamReader(@"..\TicTacToeGameServer\Users\" +username + ".txt"))
        //    {
        //        string line = sr.ReadLine();
        //        string[] lineData = line.Split(',');


        //        if (lineData[0] == username && lineData[1] == password)
        //        {
        //            Debug.Log("True username & passoword");
        //            NetworkedServer.Instance.CreatePlayer(lineData[0], userID);
        //            NetworkedServer.Instance.notifyUser(0, userID, "AccesGranted"); // ACCESS GRANTED 

        //        }
        //        else
        //        {
        //            Debug.Log("WrongPassword"); // ACCESS DENIED -Wrong password
        //            NetworkedServer.Instance.notifyUser(3, userID, " AccesDenied -Wrong password");
        //        }
        //        sr.Close();
        //    }
        //}
        //else
        //{
        //    //AccesDenied -Wrong username
        //    NetworkedServer.Instance.notifyUser(2, userID, " AccesDenied -Wrong username");
        //}

        #endregion
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
        if (Directory.Exists((@"..\TicTacToeGameServer\ReplayData\" + username )))
        {

            DirectoryInfo directoriy = new DirectoryInfo(@"..\TicTacToeGameServer\ReplayData\" + username);
            foreach (var currentFile in directoriy.GetFiles("*.txt"))
            {
                string filename = "";
                filename = currentFile.Name;
                Debug.Log(filename);
                NetworkedServer.Instance.notifyUser(15, userID, filename);
            }
            NetworkedServer.Instance.notifyUser(17, userID, "");
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
                            NetworkedServer.Instance.notifyUser(16, userID, line);
                            sr.Close();
             }
        }

    }

    #endregion


}
