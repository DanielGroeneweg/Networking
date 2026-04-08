using UnityEngine;
using System.Net;
using System.Net.Sockets;
using NetworkConnections;
using OSCTools;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
/// <summary>
/// The client is the class that lets game code (Controller and View classes) communicate with 
/// the server, and handles network connections.
/// </summary>
public class Client : MonoBehaviour
{
	// ----- General client things:
	public IPAddress ServerIP = IPAddress.Loopback;
	TcpNetworkConnection connection;
	OSCDispatcher dispatcher;

	// ----- TexasHoldem client things:

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

    void Start()
    {
		TcpClient client = new TcpClient();
		client.Connect(new IPEndPoint(ServerIP, 50006));
		connection = new TcpNetworkConnection(client);
		// TODO: error handling

		Debug.Log("Starting client, connecting to " + ServerIP);

		// Initialize the dispatcher and callbacks for incoming OSC messages:
		dispatcher = new OSCDispatcher();
		dispatcher.ShowIncomingMessages = true;
		Initialize();
    }

	/// <summary>
	/// Called from NetworkConnection callback (connection.Update), when a packet arrives:
	/// </summary>
	void HandlePacket(byte[] packet, IPEndPoint remote) {
		OSCMessageIn mess = new OSCMessageIn(packet);
		Debug.Log("Message arrives on client: " + mess);
		dispatcher.HandlePacket(packet, remote);
	}

	void Update()
    {
		// Check for incoming packets, and deal with them:
		while (connection.Available()>0) {
			HandlePacket(connection.GetPacket(), connection.Remote);
		}
		// TODO: disconnect handling
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
		dispatcher.AddListener("/DealCards", DealCardsRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/DealTableCards", DealTableCardsRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT);
		dispatcher.AddListener("/InvalidAction", InvalidActionRpc, OSCUtil.STRING);
		dispatcher.AddListener("/InvalidNewRound", InvalidNewRoundRpc, OSCUtil.STRING);
    }

	// ----- Incoming RPCs (events are triggered, and View classes subscribe):
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

		for(int i = 0; i < 5; i++)
		{
			if (cardInts[i] == -1) continue;

			cards[i] = (new Card((Suits)cardInts[(i *2 ) + 1], (Ranks)cardInts[(i * 2)]));
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

	// ----- Outgoing RPCs (called from Controller):
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
}
