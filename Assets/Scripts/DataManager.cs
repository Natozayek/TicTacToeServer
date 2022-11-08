using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

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
                    Debug.Log("True");
                    NetworkedServer.Instance.notifyUser(0, userID, " AccesGranted"); // ACCESS GRANTED 

                    //Create player in game 
                    //CreatePlayer(nameofUser, userID)
                    NetworkedServer.Instance.CretePlayer(lineData[0], userID);
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
    #endregion


}
