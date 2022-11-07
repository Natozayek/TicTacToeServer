using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class SystemManager : MonoBehaviour
{
    public static SystemManager Instance;
    protected int connectionID;

    public int ConnectionID { get { return connectionID; } set { connectionID = value; } }

    // Start is called before the first frame update
    void Start()
    {
        DataManager.SetSystemManager(gameObject);
        NetworkedServer.SetSystemManager(gameObject);
    }
    private void Awake()
    {
        Instance = this;
    }

    public void createAccount(string username, string password, int id)
    {
        if (File.Exists(@"..\TicTacToeServer\Users\" + username + ".txt"))
        {
            NetworkedServer.Instance.notifyUser(1, id, "");
        }
        else
        {
            DataManager.SaveData(username, password);
            NetworkedServer.Instance.notifyUser(4, id, "");
        }

    }
    public void LoginVerification(string username, string password, int userID)
    {

        DataManager.VerifyData(username, password, userID);
    }

}
