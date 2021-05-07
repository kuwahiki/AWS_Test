using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Aws.GameLift.Realtime.Types;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] public Button CreateRoomButton, SearchRoomButton, SendTest1Button, SendTest2Button;
    [SerializeField] public Text InfoText,RoomInfo;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClearAllPanels()
    {
        InfoText.text = "";
    }

    public void CreateSessionPanels(List<GameSession> gameSessions,Action<string> action)
    {
        //for (int i = 0; i < gameSessions.Count; i++)
        //{
        //    string RoomName = gameSessions[i].Name;
        //    UnityEngine.Debug.Log(RoomName);

        //}
        RoomInfo.text = "Name:" + gameSessions[0].Name + "\n" + "Status:" + gameSessions[0].Status;
        action(gameSessions[0].GameSessionId);


    }
}
