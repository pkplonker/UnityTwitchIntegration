//
// Copyright (C) 2022 Stuart Heath. All rights reserved.
//

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using StuartHeathTools;
using UnityEngine.Profiling;

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
		private Task connection;

		private void Start()
		{
			username = PlayerPrefs.GetString("username");
			password = PlayerPrefs.GetString("password");
			channelName = PlayerPrefs.GetString("channel");

			twitchClient = new TcpClient();
			AttemptConnection();
			lastPingTime = 0;
		}

		public void UpdateChannel(string c)
		{
			channelName = c;
			AttemptConnection();
		}

		public void AttemptConnection()
		{
			if (connection?.AsyncState == null)
			{
				connection = Connect();
			}
		}

		private async Task Connect()
		{
#if UNITY_EDITOR
			Profiler.BeginSample("Connect");

#endif

			ChangeConnectionState(ConnectionState.Connecting);
			twitchClient = new TcpClient(URL, 6667);
			reader = new StreamReader(twitchClient.GetStream());
			writer = new StreamWriter(twitchClient.GetStream());
			await writer.WriteLineAsync("PASS " + password);
			await writer.WriteLineAsync("NICK " + username);
			await writer.WriteLineAsync("USER " + username + " 8 *:" + username);
			await writer.WriteLineAsync("JOIN #" + channelName);
			await writer.FlushAsync();
			if (!twitchClient.Connected) return;
			Debug.Log("connecting".WithColor(Color.yellow));
			ChangeConnectionState(ConnectionState.Connecting);
			RequestCapabilities();
			awaitingPong = false;
			connection = null;
#if UNITY_EDITOR
			Profiler.EndSample();
#endif
		}

		private void RequestCapabilities() =>
			WriteToTwitch("CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands");


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
				AttemptConnection();
			}

			if (!twitchClient.Connected) return;
			pingCounter += Time.deltaTime;
			if (pingCounter > pingFrequency)
			{
				WriteToTwitch("PING " + URL);
				lastPingTime = Time.time;
				pingCounter = 0;
				awaitingPong = true;
				//	Debug.Log("Pinging");
			}

			if (awaitingPong && Time.time - lastPingTime > 10f)
			{
				Debug.Log("Pong not received - restarting connection".WithColor(Color.red));
				ChangeConnectionState(ConnectionState.ConnectionLost);
				AttemptConnection();
			}

			ReadChat();
		}

		private void WriteToTwitch(string message)
		{
			if (!twitchClient.Connected) AttemptConnection();
			Debug.Log("Writing to twitch:- " + message);

			writer.WriteLine(message);
			writer.Flush();
		}

		public void PRIVMSGTToTwitch(string message)
		{
			message = message.Replace('@', '\r');
			var w = "PRIVMSG #" + channelName + " :" + message;
			WriteToTwitch(w);
		}


		private void ReadChat()
		{
			if (twitchClient.Available <= 0) return;
			ChangeConnectionState(ConnectionState.ConnectionConfirmed);
			var message = reader.ReadLine();
			message.ToLower();
			Debug.Log("Message received = " + message);
			if (message.Contains("Welcome, GLHF!"))
			{
				Debug.Log("Connected".WithColor(Color.green));
				ChangeConnectionState(ConnectionState.ConnectionConfirmed);
				OnConnectionConfirmed?.Invoke();
			}
			else if (message.Contains(":tmi.twitch.tv PONG"))
			{
				awaitingPong = false;
			}
			else if (message.Contains(":tmi.twitch.tv CAP * ACK :twitch.tv/tags twitch.tv/commands"))
			{
			}
			else if (message.Contains("PING :tmi.twitch.tv"))
			{
				var s = message;
				Debug.Log("Received PING, Sending PONG");
				s = s.Replace("PING", "PONG");
				WriteToTwitch(s);
			}

			OnMessageReceived?.Invoke(message);
		}
	}


	public enum ConnectionState
	{
		Disconnected,
		Connecting,
		ConnectionConfirmed,
		ConnectionLost,
	}
}