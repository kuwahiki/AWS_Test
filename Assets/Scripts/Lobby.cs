using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Aws.GameLift.Realtime.Types;
public class Lobby : MonoBehaviour
{
    class GameLiftConfig
    {
        public RegionEndpoint RegionEndPoint { get; set; }
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string GameLiftAliasId { get; set; }
    }
    GameLiftConfig config;
    AmazonGameLiftClient gameLiftClient;
    RealTimeClient realTimeClient;

    [SerializeField]
    LobbyUI ui;
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Debug.Log("start");
        initialize();
    }

    void initialize()
    {
        config = new GameLiftConfig
        {
            RegionEndPoint = RegionEndpoint.APNortheast1, //東京の場合
            AccessKeyId = "AKIAX5IKZ7C57ZWAT34O", // ダウンロードしたcsvのAccess key IDの値
            SecretAccessKey = "5p08FhR8uRfjmkk2nBfZtwzDtoRNw1+sGqIgdg+M", // ダウンロードしたcsvのSecret access keyの値
            GameLiftAliasId = "alias-a53d3f24-6c22-4e2c-93b8-146529f0a231" // 作成したAliasのID alias- から始まるID
        };

        // AmazonGameLiftClientクラスの初期化
        gameLiftClient = new AmazonGameLiftClient(config.AccessKeyId, config.SecretAccessKey, config.RegionEndPoint);

        ui.CreateRoomButton.onClick.AddListener(() =>
        {
            CreateRoom();
        });

        ui.SearchRoomButton.onClick.AddListener(() =>
        {
            var sessions = SearchRooms();
            ui.ClearAllPanels();
            ui.CreateSessionPanels(sessions, JoinRoom);
            //, JoinRoom
        });

        ui.SendTest1Button.onClick.AddListener(() =>
        {
            if (realTimeClient != null) realTimeClient.SendMessage(DeliveryIntent.Reliable, "test");
        });
        ui.SendTest2Button.onClick.AddListener(() =>
        {
            if (realTimeClient != null) realTimeClient.SendEvent(RealTimeClient.OpCode.SendTest2);
        });
    }

    // ルームの作成
    void CreateRoom(string roomName = "")
    {
        UnityEngine.Debug.Log("CreateRoom");
        if (string.IsNullOrEmpty(roomName)) roomName = Guid.NewGuid().ToString();
        var request = new CreateGameSessionRequest
        {
            AliasId = config.GameLiftAliasId,
            MaximumPlayerSessionCount = 2,
            Name = roomName
        };
        var response = gameLiftClient.CreateGameSession(request);
        ui.InfoText.text += "CreateRoom\n";
    }

    //ルームの検索
    public List<GameSession> SearchRooms()
    {
        UnityEngine.Debug.Log("SearchRooms");
        var response = gameLiftClient.SearchGameSessions(new SearchGameSessionsRequest
        {
            AliasId = config.GameLiftAliasId,
        });
        ui.InfoText.text += "SearchRoom\n";
        return response.GameSessions;
    }

    // ルームへの参加
    void JoinRoom(string sessionId)
    {
        UnityEngine.Debug.Log("JoinRoom");
        var response = gameLiftClient.CreatePlayerSession(new CreatePlayerSessionRequest
        {
            GameSessionId = sessionId,
            PlayerId = SystemInfo.deviceUniqueIdentifier,
        });
        var playerSession = response.PlayerSession;

        ushort DefaultUdpPort = 7777;
        var udpPort = SearchAvailableUdpPort(DefaultUdpPort, DefaultUdpPort + 100);
        realTimeClient = new RealTimeClient(
            playerSession.IpAddress,
            playerSession.Port,
            udpPort,
            ConnectionType.RT_OVER_WS_UDP_UNSECURED,
            playerSession.PlayerSessionId,
            null);

        ui.InfoText.text += "JoinRoom\n";
        realTimeClient.OnDataReceivedCallback = OnDataReceivedCallback;
    }

    public void OnDataReceivedCallback(object sender, Aws.GameLift.Realtime.Event.DataReceivedEventArgs e)
    {
        if (ui.InfoText != null)
        {
            ui.InfoText.text += $"{e.OpCode}\n";
        }
    }

    int SearchAvailableUdpPort(int from = 1024, int to = ushort.MaxValue)
    {
        from = Mathf.Clamp(from, 1, ushort.MaxValue);
        to = Mathf.Clamp(to, 1, ushort.MaxValue);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        var set = LsofUdpPorts(from, to);
#else
        var set = GetActiveUdpPorts();
#endif
        for (int port = from; port <= to; port++)
            if (!set.Contains(port))
                return port;
        return -1;
    }

    HashSet<int> LsofUdpPorts(int from, int to)
    {
        var set = new HashSet<int>();
        string command = string.Join(" | ",
            $"lsof -nP -iUDP:{from.ToString()}-{to.ToString()}",
            "sed -E 's/->[0-9.:]+$//g'",
            @"grep -Eo '\d+$'");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
        });
        if (process != null)
        {
            process.WaitForExit();
            var stream = process.StandardOutput;
            while (!stream.EndOfStream)
                if (int.TryParse(stream.ReadLine(), out int port))
                    set.Add(port);
        }
        return set;
    }

    HashSet<int> GetActiveUdpPorts()
    {
        return new HashSet<int>(IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveUdpListeners().Select(listener => listener.Port));
    }
}



