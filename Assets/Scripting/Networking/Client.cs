using NetworkConnections;
using Newtonsoft.Json;
using OSCTools;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
/// <summary>
/// The client is the class that lets game code (Controller and View classes) communicate with 
/// the server, and handles network connections.
/// </summary>
public class Client : MonoBehaviour
{
	// ----- General client things:
	public string serverIP;
	public IPAddress ServerIP = IPAddress.Loopback;
	int serverPort = 58752;
	TcpNetworkConnection connection;
	OSCDispatcher dispatcher;
	bool connectionMade;

    // ----- TexasHoldem client things:
    #region Events
    // Views subscribe here, on any client:
    public delegate void UpdatePotEvent(int potMoney);
	public event UpdatePotEvent OnUpdatePot;

	public delegate void UpdatePlayerMoneyEvent(int player, int playerMoney);
	public event UpdatePlayerMoneyEvent OnUpdatePlayerMoney;

	public delegate void NextPlayerEvent(int player, int actionTaken);
	public event NextPlayerEvent OnNextPlayer;

	public delegate void ChangePlayerEvent(int actionTaken, int pot);
	public event ChangePlayerEvent OnChangePlayerOptions;

	public delegate void NextPhaseEvent(int phase);
	public event NextPhaseEvent OnNextPhase;

	public delegate void NewRoundEvent();
	public event NewRoundEvent OnNewRound;

	public delegate void DealCardsEvent(int cardRank1, int cardSuit1, int cardRank2, int cardSuit2);
	public event DealCardsEvent OnDealCards;

	public delegate void DealTableCardsEvent(Card[] cards);
	public event DealTableCardsEvent OnDealTableCards;

	public delegate void InvalidActionEvent(string error);
	public event InvalidActionEvent OnInvalidAction;

	public delegate void InvalidNewRoundEvent(string error);
	public event InvalidNewRoundEvent OnInvalidNewRound;

	public delegate void InvalidNewGameEvent(string error);
	public event InvalidNewGameEvent OnInvalidNewGame;

	public delegate void PlayerInformationEvent(List<int> playerIDs, int startingMoney);
	public event PlayerInformationEvent OnPlayerInformation;

	public delegate void RoundEndEvent(bool[] winners);
	public event RoundEndEvent OnRoundEnd;

	public delegate void GameEndEvent(int winner);
	public event GameEndEvent OnGameEnd;

	public delegate void SendHostInformationEvent();
	public event SendHostInformationEvent OnSendHostInformation;

	public delegate void PlayerCardInformationEvent(PlayerCardInfo info);
	public event PlayerCardInformationEvent OnPlayerCardInformation;

	public delegate void JoinedAsSpectatorEvent();
	public event JoinedAsSpectatorEvent OnJoinedAsSpectator;

	public delegate void PlayerIDEvent(int id);
	public event PlayerIDEvent OnPlayerID;

	public delegate void KickPlayerEvent();
	public event KickPlayerEvent onKickPlayer;

    public delegate void ValidPlayerActionEvent(int player, int action);
    public event ValidPlayerActionEvent OnValidPlayerAction;

	public delegate void PlayerDCEvent(int player);
	public event PlayerDCEvent OnPlayerDC;

	public delegate void BankRuptEvent();
	public event BankRuptEvent OnBankrupt;

	public delegate void ConnectionFailedEvent(string message);
	public event ConnectionFailedEvent OnConnectionFailed;

	public delegate void DisconnectedEvent(string reason);
	public event DisconnectedEvent OnDisconnected;
    #endregion
    void Start()
	{
        TryConnect();
    }
    void TryConnect()
    {
        try
        {
            ServerIP = Dns.GetHostAddresses(serverIP)[0];

            TcpClient client = new TcpClient();

            client.Connect(new IPEndPoint(ServerIP, serverPort));

            connection = new TcpNetworkConnection(client);

            Debug.Log("Connected to server: " + ServerIP);

            dispatcher = new OSCDispatcher();
            dispatcher.ShowIncomingMessages = true;

            Initialize();
        }
        catch (SocketException ex)
        {
            Debug.LogError("Socket error: " + ex.Message);

            OnConnectionFailed?.Invoke(ex.Message);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Connection failed: " + ex.Message);

            OnConnectionFailed?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// Called from NetworkConnection callback (connection.Update), when a packet arrives:
    /// </summary>
    void HandlePacket(byte[] packet, IPEndPoint remote)
    {
        try
        {
            OSCMessageIn mess = new OSCMessageIn(packet);

            Debug.Log("Message arrives on client: " + mess);

            dispatcher.HandlePacket(packet, remote);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Invalid packet: " + ex.Message);
        }
    }

    /// <summary>
    /// Disconnects the client from the server
    /// </summary>
    public void Disconnect()
	{
        HandleDisconnect("Client disconnected.");
    }
	void Update()
	{
		if (!connectionMade || connection == null) return;
        // Check for incoming packets, and deal with them:
        try
        {
            while (connection.Available() > 0)
            {
                HandlePacket(connection.GetPacket(), connection.Remote);
            }

            CheckConnectionAlive();
        }
        catch (SocketException ex)
        {
            Debug.LogError("Lost connection: " + ex.Message);

            HandleDisconnect("Lost connection to server.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Network error: " + ex.Message);

            HandleDisconnect("Network error.");
        }
    }
	void CheckConnectionAlive()
	{
        if (connection == null)
            return;

        Socket socket = connection.socket.Client;

        bool disconnected =
            socket.Poll(1, SelectMode.SelectRead) &&
            socket.Available == 0;

        if (disconnected)
        {
            HandleDisconnect("Server disconnected.");
        }
    }
    void HandleDisconnect(string reason)
    {
        if (!connectionMade)
            return;

        connectionMade = false;

        Debug.Log("Disconnected: " + reason);

        try
        {
            connection?.Close();
        }
        catch { }

        connection = null;

        OnDisconnected?.Invoke(reason);

		onKickPlayer?.Invoke();
    }

    void Initialize() {
		// The (optional) list of parameter types (OSCUtil.INT) lets the dispatcher filter
		//  messages that do not satisfy the expected signature (=parameter list):
		dispatcher.AddListener("/UpdatePot", UpdatePotRpc, OSCUtil.INT);
		dispatcher.AddListener("/UpdatePlayerMoney", UpdatePlayerMoneyRpc, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/NextPlayer", NextPlayerRpc, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/ChangePlayer", ChangePlayerOptionsRpc, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/NextPhase", NextPhaseRpc, OSCUtil.INT);
		dispatcher.AddListener("/NewRound", NewRoundRpc);
		dispatcher.AddListener("/DealPlayerCards", DealCardsRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/DealTableCards", DealTableCardsRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/InvalidAction", InvalidActionRpc, OSCUtil.STRING);
		dispatcher.AddListener("/InvalidNewRound", InvalidNewRoundRpc, OSCUtil.STRING);
		dispatcher.AddListener("/InvalidNewGame", InvalidNewGameRpc, OSCUtil.STRING);
		dispatcher.AddListener("/PlayerInformation", PlayerInformationRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/RoundEnd", RoundEndRpc, OSCUtil.BOOL, OSCUtil.BOOL, OSCUtil.BOOL, OSCUtil.BOOL, OSCUtil.BOOL, OSCUtil.BOOL);
		dispatcher.AddListener("/GameEnd", GameEndRpc, OSCUtil.INT);
		dispatcher.AddListener("/SendHostInformation", SendHostInformationRpc);
		dispatcher.AddListener("/PlayerCardInfo", PlayerCardInformationRpc, OSCUtil.STRING);
		dispatcher.AddListener("/Spectator", JoinedAsSpectatorRpc);
		dispatcher.AddListener("/PlayerID", PlayerIDRpc, OSCUtil.INT);
		dispatcher.AddListener("/KickPlayer", KickPlayerRpc);
		dispatcher.AddListener("/ValidPlayerAction", ValidPlayerActionRpc, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/PlayerDC", PlayerDCRpc, OSCUtil.INT);
		dispatcher.AddListener("/Bankrupt", BankruptRpc);
		
		connectionMade = true;
	}
    void OnApplicationQuit()
    {
        Disconnect();
    }
	void HandleHeartBeat()
	{
		timeSinceHeartBeat = 0;
	}
    // ----- Incoming RPCs (events are triggered, and View classes subscribe):
    #region Incoming
    void UpdatePotRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int potMoney = message.ReadInt();
		OnUpdatePot?.Invoke(potMoney);
	}
	void UpdatePlayerMoneyRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int player = message.ReadInt();
		int playerMoney = message.ReadInt();
		OnUpdatePlayerMoney?.Invoke(player, playerMoney);
	}
	void NextPlayerRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int player = message.ReadInt();
		int actionTaken = message.ReadInt();
		OnNextPlayer?.Invoke(player, actionTaken);
	}
	void ChangePlayerOptionsRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int actionTaken = message.ReadInt();
		int pot = message.ReadInt();
		OnChangePlayerOptions?.Invoke(actionTaken, pot);
	}
	void NextPhaseRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int phase = message.ReadInt();
		OnNextPhase?.Invoke(phase);
	}
	void NewRoundRpc(OSCMessageIn message, IPEndPoint remote)
	{
		OnNewRound?.Invoke();
	}
	void DealCardsRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int cardRank1 = message.ReadInt();
		int cardSuit1 = message.ReadInt();
		int cardRank2 = message.ReadInt();
		int cardSuit2 = message.ReadInt();

		OnDealCards?.Invoke(cardRank1, cardSuit1, cardRank2, cardSuit2);
	}
	void DealTableCardsRpc(OSCMessageIn message, IPEndPoint remote)
	{
		List<int> cardInts = new();

		for (int i = 0; i < 10; i++) cardInts.Add(message.ReadInt());

		Card[] cards = new Card[5];

		for (int i = 0; i < 5; i++)
		{
			if (cardInts[i*2] == -1) continue;

			cards[i] = (new Card((Suits)cardInts[(i * 2) + 1], (Ranks)cardInts[(i * 2)]));
		}

		OnDealTableCards?.Invoke(cards);
	}
	void InvalidActionRpc(OSCMessageIn message, IPEndPoint remote)
	{
		string error = message.ReadString();
		OnInvalidAction?.Invoke(error);
	}
	void InvalidNewRoundRpc(OSCMessageIn message, IPEndPoint remote)
	{
		string error = message.ReadString();
		OnInvalidNewRound?.Invoke(error);
	}
	void InvalidNewGameRpc(OSCMessageIn message, IPEndPoint remote)
	{
		string error = message.ReadString();
		OnInvalidNewGame?.Invoke(error);
	}
	void PlayerInformationRpc(OSCMessageIn message, IPEndPoint remote)
	{
		List<int> ids = new();

		for (int i = 0; i < 6; i++)
		{
            int id = message.ReadInt();
			if (id > 0) ids.Add(id);
        }

		int startingMoney = message.ReadInt();
		OnPlayerInformation?.Invoke(ids, startingMoney);
	}
	void RoundEndRpc(OSCMessageIn message, IPEndPoint remote)
	{
		bool[] winners = new bool[6];
		for (int i = 0; i < 5; i++)
		{
			winners[i] = message.ReadBool();
        }
		OnRoundEnd?.Invoke(winners);
	}
	void GameEndRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int winner = message.ReadInt();
		OnGameEnd?.Invoke(winner);
	}
	void SendHostInformationRpc(OSCMessageIn message, IPEndPoint remote)
	{
		OnSendHostInformation?.Invoke();
	}
	void PlayerCardInformationRpc(OSCMessageIn message, IPEndPoint remote)
	{
		string json = message.ReadString();
        PlayerCardInfo info =
			JsonConvert.DeserializeObject<PlayerCardInfo>(json);
        OnPlayerCardInformation?.Invoke(info);
	}
	void JoinedAsSpectatorRpc(OSCMessageIn message, IPEndPoint remote)
	{
		OnJoinedAsSpectator?.Invoke();
	}
	void PlayerIDRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int id = message.ReadInt();
		OnPlayerID?.Invoke(id);
	}
	void KickPlayerRpc(OSCMessageIn message, IPEndPoint remote)
	{
		onKickPlayer?.Invoke();
	}
	void ValidPlayerActionRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int player = message.ReadInt();
		int action = message.ReadInt();
		OnValidPlayerAction?.Invoke(player, action);
	}
	void PlayerDCRpc(OSCMessageIn message, IPEndPoint remote)
	{
		int player = message.ReadInt();
		OnPlayerDC?.Invoke(player);
	}
    public void BankruptRpc(OSCMessageIn message, IPEndPoint remote)
	{
		OnBankrupt?.Invoke();
	}
    #endregion

    // ----- Outgoing RPCs (called from Controller):
    #region Outgoing
    public void CheckRequest()
	{
		OSCMessageOut message = new OSCMessageOut("/Check");
		connection.Send(message.GetBytes());
	}
    public void BetRequest(int money)
    {
        OSCMessageOut message = new OSCMessageOut("/Bet").AddInt(money);
        connection.Send(message.GetBytes());
    }
    public void CallRequest()
    {
        OSCMessageOut message = new OSCMessageOut("/Call");
        connection.Send(message.GetBytes());
    }
    public void RaiseRequest(int money)
    {
        OSCMessageOut message = new OSCMessageOut("/Raise").AddInt(money);
        connection.Send(message.GetBytes());
    }
    public void FoldRequest()
    {
        OSCMessageOut message = new OSCMessageOut("/Fold");
        connection.Send(message.GetBytes());
    }
    public void NewRoundRequest()
    {
        OSCMessageOut message = new OSCMessageOut("/NewRound");
        connection.Send(message.GetBytes());
    }
	public void NewGameRequest(int startingMoney)
	{
        OSCMessageOut message = new OSCMessageOut("/NewGame").AddInt(startingMoney);
        connection.Send(message.GetBytes());
    }
    #endregion
}