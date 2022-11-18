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
            using (StreamWriter sw = File.AppendText(@"..\TicTacToeServer\Users\"+ username + ".txt"))
            {
                sw.WriteLine(username + "," + password);
            }
    }
    static public void VerifyData(string username, string password, int userID)
    {
        if (File.Exists(@"..\TicTacToeServer\Users\" + username+ ".txt"))
        {
            Debug.Log("file Exist");
            using (StreamReader sr = new StreamReader(@"..\TicTacToeServer\Users\" +username + ".txt"))
            {
                string line = sr.ReadLine();
                string[] lineData = line.Split(',');
                

                if (lineData[0] == username && lineData[1] == password)
                {
                    Debug.Log("True username & passoword");
                    NetworkedServer.Instance.CreatePlayer(lineData[0], userID);
                    NetworkedServer.Instance.notifyUser(0, userID, "AccesGranted"); // ACCESS GRANTED 

                }
                else
                {
                    Debug.Log("WrongPassword"); // ACCESS DENIED -Wrong password
                    NetworkedServer.Instance.notifyUser(3, userID, " AccesDenied -Wrong password");
                }
                sr.Close();
            }
        }
        else
        {
            //AccesDenied -Wrong username
            NetworkedServer.Instance.notifyUser(2, userID, " AccesDenied -Wrong username");
        }
    }

    static public void SaveReplayData(string username, int turnofPlayer, string replayName, string usedButtons)
    {
        if (!File.Exists(@"..\TicTacToeServer\ReplayData\" + username + @"\" + replayName + ".txt"))
        {

            Directory.CreateDirectory(@"..\TicTacToeServer\ReplayData\" + username);

            using (StreamWriter streamWriter = new StreamWriter(@"..\TicTacToeServer\ReplayData\" + username + @"\" + replayName + ".txt"))
            {
                streamWriter.WriteLine(turnofPlayer + "," + usedButtons);
            }
        }
        if (File.Exists(@"..\TicTacToeServer\ReplayData\" + username + @"\" + replayName + ".txt"))
        {

            using (StreamWriter streamWriter = new StreamWriter(@"..\TicTacToeServer\ReplayData\" + username + @"\" + replayName + ".txt"))
            {
                streamWriter.WriteLine(turnofPlayer + "," + usedButtons);
            }
        }
    }

    static public void VerifyReplayData(string username, int userID)
    {
        if (Directory.Exists((@"..\TicTacToeServer\ReplayData\" + username )))
        {

            DirectoryInfo directoriy = new DirectoryInfo(@"..\TicTacToeServer\ReplayData\" + username);
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
        if (File.Exists(@"..\TicTacToeServer\ReplayData\" + username + @"\" + replayName))
        {
            Debug.Log("file Exist");

            using (StreamReader sr = new StreamReader(@"..\TicTacToeServer\ReplayData\" + username + @"\" + replayName))
             {
                            string line = sr.ReadLine();
                            NetworkedServer.Instance.notifyUser(16, userID, line);
                            sr.Close();
             }
        }

    }

    #endregion


}
