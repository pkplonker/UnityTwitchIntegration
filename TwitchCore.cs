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
		public static event Action<string, string> OnMessageReceived;

		public static event Action OnConnectionConfirmed;
		
		[SerializeField] private bool extendedCapabiltiies = false;
		private TcpClient twitchClient;
		private StreamReader reader;
		private StreamWriter writer;
		private float pingCounter;
		private float lastPingTime;
		private bool awaitingPong;

		private void Start()
		{
			Connect();
			lastPingTime = 0;
		}

		public void Connect()
		{
			twitchClient = new TcpClient(URL, 6667);
			reader = new StreamReader(twitchClient.GetStream());
			writer = new StreamWriter(twitchClient.GetStream());
			writer.WriteLine("PASS " + password);
			writer.WriteLine("NICK " + username);
			writer.WriteLine("USER " + username + " 8 *:" + username);
			writer.WriteLine("JOIN #" + channelName);
			writer.Flush();
			if (twitchClient.Connected)
			{
				Debug.Log("connected".WithColor(Color.green));
			}

			if (extendedCapabiltiies) RequestCapabilities();
		}

		private void RequestCapabilities() => WriteToTwitch("CAP REQ :twitch.tv/tags twitch.tv/commands");


		private void Update()
		{
			if (!twitchClient.Connected)
			{
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
				Debug.Log("Pinging".WithColor(Color.cyan));
			}

			if (awaitingPong && Time.time - lastPingTime > 10f)
			{
				Debug.Log("Pong not received - restarting connection".WithColor(Color.red));
				Connect();
			}

//debug
			if (Input.GetKeyDown(KeyCode.A))
			{
				var message = "PRIVMSG #" + channelName + " :This is a sample message @" + Time.time;
				Debug.Log("Attempting to write message: " + message);
				WriteToTwitch(message);
			}
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
			var message = reader.ReadLine();
			//Debug.Log("Message received = " + message);
			if (message.Contains("PRIVMSG"))
			{
				ParseMessage(message);
				Debug.Log(message.WithColor(Color.green));
			}
			else if (message.Contains("Welcome, GLHF!"))
			{
				Debug.Log("Connection confirmed".WithColor(Color.green));
				OnConnectionConfirmed?.Invoke();
			}
			else if (message.Contains(":tmi.twitch.tv PONG"))
			{
				Debug.Log("Connection ponged".WithColor(Color.green));
				awaitingPong = false;
			}
		}

		private void ParseMessage(string data)
		{
			if (!data.Contains("PRIVMSG")) return;
			var messageSender = "";
			var message = "";
			//message
			var colonIndex = data.IndexOf(':',2);
			message = data.Substring(colonIndex + 1);
			Debug.Log("Message is " + message.WithColor(Color.yellow));

			var atIndex = data.IndexOf('@');
			var dotIndex = data.IndexOf('.', atIndex);
			messageSender = data.Substring(atIndex+1, dotIndex - atIndex - 1);
			
			Debug.Log("username is " + messageSender.WithColor(Color.magenta));


			OnMessageReceived?.Invoke(messageSender, message);
		}
	}
}