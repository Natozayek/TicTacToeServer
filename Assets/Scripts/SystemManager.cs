using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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


    public void CheckAccoutnt(string user, int id)
    {
        DataManager.SaveData(user, id);  
    }


    

 

}
