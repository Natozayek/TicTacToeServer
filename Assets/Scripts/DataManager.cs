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

    static public void SaveData(string user, int id)
    {
        
        {
            using (StreamWriter sw = File.AppendText(user + ".txt"))
            {
                sw.WriteLine(id);
            }
        }
       

    }

    //static public void VerifyData()
    //{
    //    if (File.Exists(GetNameForLogin() + ".txt"))
    //    {
    //        using (StreamReader sr = new StreamReader(GetNameForLogin() + ".txt"))
    //        {
    //            string line = sr.ReadLine();

    //            Debug.Log(line);
    //            Debug.Log(GetPassWordForLogin());

    //            if ((line == GetPassWordForLogin()))
    //            {
    //                Debug.Log(line);
    //                Debug.Log("True");
    //                indexOf = 1;
    //                Debug.Log(GetLoginIndex());
    //                sManager.GetComponent<SystemManager>().ShowMessage();
                

    //            }
    //            else
    //            {
    //                Debug.Log(line);
    //                Debug.Log("WrongPassword");
    //                indexOf = 0;
    //                Debug.Log(GetLoginIndex());
    //                sManager.GetComponent<SystemManager>().ShowMessage();
    //            }
    //            sr.Close();
    //        }
            
    //    }
    //    else
    //    {
    //        indexOf = 3;
    //        sManager.GetComponent<SystemManager>().ShowMessage();
    //    }

        
    //}

    static public void AddConnectionID(int connetionID)
    {
        //Debug.Log(() + ".txt");

        //if (File.Exists(GetNameForLogin() + ".txt"))
        //{

        //    using (StreamWriter sw = File.AppendText(GetNameForLogin() + ".txt"))
        //    {
        //        Debug.Log("INSIDE");
        //        sw.WriteLine(GetPassWordForLogin().ToString());

        //        sw.WriteLine(connetionID);

        //        sw.Close();
        //    }
        //}
    }
   
}
