using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class DataManager : MonoBehaviour
{
    static  GameObject sManager, Network;
    public static int indexOf = 0;


    static public void SetSystemManager(GameObject SystemManager)
    {
        sManager = SystemManager;
    }

    static public void SaveData(string username, string password)
    {
        
        {
            using (StreamWriter sw = File.AppendText(@"..\TicTacToeServer\Users\"+ username + ".txt"))
            {
                sw.WriteLine(username + "," + password);
            }
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

    static public string GetNameForLogin()
    {
        //return sManager.GetComponent<SystemManager>().GetUsername();
        return "";
    }
    static public string GetPassWordForLogin()
    {
        //return sManager.GetComponent<SystemManager>().GetPassWord();
        return "";
    }
    static public string GetNewNameFromInput()
    {
        // return sManager.GetComponent<SystemManager>().GetNewUsername();
        return "";
    }

    static public string GetNewPassWordFromInput()
    {
        //  return sManager.GetComponent<SystemManager>().GetNewPassWord();
        return "";
    }



    //static public void AddConnectionID(int connetionID)
    //{
    //    Debug.Log(GetNameForLogin() + ".txt");

    //    if (File.Exists(GetNameForLogin() + ".txt"))
    //    {

    //        using (StreamWriter sw = File.AppendText(GetNameForLogin() + ".txt"))
    //        {
    //            Debug.Log("INSIDE");
    //            //sw.WriteLine(GetPassWordForLogin().ToString());

    //            sw.WriteLine(connetionID);

    //            sw.Close();
    //        }
    //    }
    //}
    static public int GetLoginIndex()
    {
        return indexOf;
    }
    static public int SetLoginIndex(int data)
    {
        return data;
    }   
}
