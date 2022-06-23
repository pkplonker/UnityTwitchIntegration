//
// Copyright (C) 2022 Stuart Heath. All rights reserved.
//

using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using StuartHeathTools;

namespace TwitchIntegration
{
	/// <summary>
	///TwitchCore - Connection to twitch to receive chat data.
	/// </summary>
	public class TwitchCore : GenericUnitySingleton<TwitchCore>
	{
		[SerializeField] private string username;
		[SerializeField] private string password;
		[SerializeField] private string channelName;
		[SerializeField] private string URL => "irc.chat.twitch.tv";
		[SerializeField] private float pingFrequency = 30f;
		public static event Action<string> OnMessageReceived;
		public static event Action<ConnectionState> OnConnectionStatusChange;

		public static event Action OnConnectionConfirmed;

		private ConnectionState connectionState = ConnectionState.Disconnected;
		private TcpClient twitchClient;
		private StreamReader reader;
		private StreamWriter writer;
		private float pingCounter;
		private float lastPingTime;
		private bool awaitingPong;
		private bool firstResponse = false;
		private bool extendedAck;

		private void Start()
		{
			Connect();
			lastPingTime = 0;
		}

		public void Connect()
		{
			firstResponse = false;
			ChangeConnectionState(ConnectionState.Connecting);

			twitchClient = new TcpClient(URL, 6667);
			reader = new StreamReader(twitchClient.GetStream());
			writer = new StreamWriter(twitchClient.GetStream());
			writer.WriteLine("PASS " + password);
			writer.WriteLine("NICK " + username);
			writer.WriteLine("USER " + username + " 8 *:" + username);
			writer.WriteLine("JOIN #" + channelName);
			writer.Flush();
			if (!twitchClient.Connected) return;
			Debug.Log("connected".WithColor(Color.green));
			ChangeConnectionState(ConnectionState.Connected);
			RequestCapabilities();
		}

		private void RequestCapabilities()=>WriteToTwitch("CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands");
		

		private void ChangeConnectionState(ConnectionState state)
		{
			connectionState = state;
			OnConnectionStatusChange?.Invoke(connectionState);
		}

		private void Update()
		{
			if (!twitchClient.Connected)
			{
				ChangeConnectionState(ConnectionState.Disconnected);
				Connect();
			}

			if (!twitchClient.Connected) return;
			pingCounter += Time.deltaTime;
			if (pingCounter > pingFrequency)
			{
				WriteToTwitch("PING " + URL);
				lastPingTime = Time.time;
				pingCounter = 0;
				awaitingPong = true;
				Debug.Log("Pinging");
			}

			if (awaitingPong && Time.time - lastPingTime > 10f)
			{
				Debug.Log("Pong not received - restarting connection".WithColor(Color.red));
				ChangeConnectionState(ConnectionState.ConnectionLost);
				Connect();
			}

//debug
			if (Input.GetKeyDown(KeyCode.A))
			{
				var message = "PRIVMSG #" + channelName + " :This is a sample message " + Time.time;
				Debug.Log("Attempting to write message: " + message);
				WriteToTwitch(message);
			}

			//if (firstResponse && !extendedAck) RequestCapabilities();


//debug
			ReadChat();
		}

		private void WriteToTwitch(string message)
		{
			if (!twitchClient.Connected) Connect();
			writer.WriteLine(message);
			writer.Flush();
		}

		private void ReadChat()
		{
			if (twitchClient.Available <= 0) return;
			firstResponse = true;
			ChangeConnectionState(ConnectionState.ConnectionConfirmed);
			var message = reader.ReadLine();
			Debug.Log("Message received = " + message);
			if (message.Contains("Welcome, GLHF!"))
			{
				Debug.Log("Connection confirmed".WithColor(Color.green));
				ChangeConnectionState(ConnectionState.ConnectionConfirmed);
				OnConnectionConfirmed?.Invoke();
			}
			else if (message.Contains(":tmi.twitch.tv PONG"))
			{
				awaitingPong = false;
			}
			else if (message.Contains(":tmi.twitch.tv CAP * ACK :twitch.tv/tags twitch.tv/commands"))
				extendedAck = true;
			else if (message.Contains("PING :tmi.twitch.tv"))
			{
				var s = message;
				Debug.Log("Received PING, Sending PONG");

				s.Replace("PING", "PONG");
				WriteToTwitch(s);
			}

			OnMessageReceived?.Invoke(message);
		}
	}


	public enum ConnectionState
	{
		Disconnected,
		Connected,
		ConnectionConfirmed,
		ConnectionLost,
		Connecting
	}
}