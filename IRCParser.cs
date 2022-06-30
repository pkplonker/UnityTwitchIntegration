//
// Copyright (C) 2022 Stuart Heath. All rights reserved.
//

using System;
using StuartHeathTools;
using UnityEngine;

namespace TwitchIntegration
{
	/// <summary>
	///IRCParser full description
	/// </summary>
	public class IRCParser : MonoBehaviour
	{
		public static event Action<string, string> OnPRIVMSG;
		public static event Action<string, bool> OnActiveMemberChange;

		private void OnEnable() => TwitchCore.OnMessageReceived += ParseMessage;

		private void OnDisable() => TwitchCore.OnMessageReceived -= ParseMessage;

		private void ParseMessage(string data)
		{
			if (data.Contains("PRIVMSG")) HandlePrivMessage(data);
			else if (data.Contains("JOIN #")) HandleUserConnection(data, true);
			else if (data.Contains("PART #")) HandleUserConnection(data, false);
			else if (data.Contains("tmi.twitch.tv 353")) ParseExistingMemberList(data);
		}


		private void ParseExistingMemberList(string data)
		{
			var fpos = data.IndexOf("353");
			if (fpos == -1) return;
			var startPos = data.IndexOf(':', fpos);
			var userNames = data.Substring(startPos + 1).Split(' ');
			foreach (var user in userNames) OnActiveMemberChange?.Invoke(user, true);
		}

		private void HandleUserConnection(string data, bool isJoiner)
		{
			var messageSender = "";
			var fPos = data.IndexOf('!');
			var lPos = data.IndexOf('@', fPos);
			if (fPos == -1) throw new Exception("-1");
			messageSender = data.Substring(fPos + 1,
				lPos - fPos - 1);
			OnActiveMemberChange?.Invoke(messageSender, isJoiner);
		}

		private static void HandlePrivMessage(string data)
		{
			var messageSender = "";
			var message = "";
			messageSender = GetSender(data);
			message = GetMessage(data);
			OnPRIVMSG?.Invoke(messageSender, message);
		}

		private static string GetMessage(string data)
		{
			var fPos = data.IndexOf(':', data.IndexOf("PRIVMSG"));
			var message = data.Substring(fPos + 1);
			return message;
		}

		private static string GetSender(string data)
		{
			var fPos = data.IndexOf("display-name=") + 13;
			var lPos = data.IndexOf(';', fPos);
			if (lPos - fPos < 0)
				Debug.LogError("length cannot be less than zero. Data:" + data + ". lpos= " + lPos + ". fpos= " + fPos);
			var messageSender = data.Substring(fPos,
				lPos - fPos);
			return messageSender;
		}
#if UNITY_EDITOR
		public static void JoinTesters(string one, string two)
		{
			OnPRIVMSG?.Invoke(one, "!join");
			OnPRIVMSG?.Invoke(two, "!join");
		}


		public static void FightTesters(string one, string two)
		{
			OnPRIVMSG?.Invoke(one, "!join");
			OnPRIVMSG?.Invoke(two, "!join");
			OnPRIVMSG?.Invoke(one, "!fight " + two);
		}
		public static void JoinTestersMass()
		{

			for (int i = 0; i < 20; i++)
			{
				OnPRIVMSG?.Invoke(i.ToString(), "!join");
			}
		}

#endif
	}
}