using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public int userID;
    public string playerName;
    public PlayerStates playerState;

  
}
public enum PlayerStates
{
    playerIsInLobby,
    playerIsWaiting,
    playerIsPlaying
}