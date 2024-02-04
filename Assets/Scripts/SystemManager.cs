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

    #region Functions to handle CreateAccount or data serialization/ Loging verification accessing DataManager to Verify the username and password
    public void createAccount(string username, string password, int id)
    {
        try
        {
            string filePath = Path.Combine(@"..\TicTacToeServer\Users", $"{username}.txt");

            if (File.Exists(filePath))
            {
                Debug.Log($"Account with username '{username}' already exists");
                NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.AccountNameAlreadyExist, id, "Account name already exists");
            }
            else
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    Debug.Log("Invalid username or password");
                    NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.InvalidAccountInformation, id, "Invalid username or password");
                    return;
                }

                DataManager.SaveData(username, password);
                Debug.Log($"User account '{username}' created successfully");
                NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.AccountCreatedSuccessfully, id, "Account created successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while creating user account: {ex.Message}");
            // Handle the exception or log accordingly
        }

        #region OLD CODE

        //if (File.Exists(@"..\TicTacToeServer\Users\" + username + ".txt"))
        //{
        //    NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.AccountNameAlreadyExist, id, "");
        //}
        //else
        //{
        //    DataManager.SaveData(username, password);// Save Username and password + notification of Account created
        //    NetworkedServer.Instance.notifyUser(ServerToClientSignifiers.AccountCreatedSuccessfully, id, "");
        //}
        #endregion

    }
    public void LoginVerification(string username, string password, int userID)
    {
        DataManager.VerifyData(username, password, userID);
    }
    #endregion

    public void SavingDataInServer(string username, int turnofPlayer, string replayName,  string usedButtons)
    {
        DataManager.SaveReplayData(username, turnofPlayer, replayName, usedButtons);

    }

}
