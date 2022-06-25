//
// Copyright (C) 2022 Stuart Heath. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using StuartHeathTools;
using TwitchIntegration;
using UnityEngine;

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
		if (data.Contains("PRIVMSG"))
		{
			HandlePrivMessage(data);
		}
		else if (data.Contains("JOIN #"))
		{
			HandleUserConnection(data, true);
		}
		else if (data.Contains("PART #"))
		{
			Debug.LogWarning("Part detected");
			HandleUserConnection(data, false);
		}
		else if (data.Contains("tmi.twitch.tv 353"))
		{
			ParseExistingMemberList(data);
		}
	}

	private void ParseExistingMemberList(string data)
	{
		var fpos = data.IndexOf("353");
		if (fpos == -1) return;
		var startPos = data.IndexOf(':', fpos);
		var userNames = data.Substring(startPos + 1).Split(' ');
		foreach (var user in userNames)
		{
			OnActiveMemberChange?.Invoke(user, true);
		}
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
		Debug.Log((isJoiner ? "Joiner = " : "Leaver = ") + messageSender.WithColor(Color.yellow));
	}

	private static void HandlePrivMessage(string data)
	{
		var messageSender = "";
		var message = "";
		messageSender = GetSender(data);
		message = GetMessage(data);
		Debug.Log("username is: " + messageSender.WithColor(Color.magenta) + " message is: " +
		          message.WithColor(Color.yellow));
		OnPRIVMSG?.Invoke(messageSender, message);
	}

	private static string GetMessage(string data)
	{
		int fPos;
		string message;
		fPos = data.IndexOf(':', data.IndexOf("PRIVMSG"));
		message = data.Substring(fPos + 1);
		return message;
	}

	private static string GetSender(string data)
	{
		string messageSender;
		var fPos = data.IndexOf("display-name=") + 13;
		var lPos = data.IndexOf(';', fPos);
		if (lPos - fPos < 0)
			Debug.LogError("length cannot be less than zero. Data:" + data + ". lpos= " + lPos + ". fpos= " + fPos);
		messageSender = data.Substring(fPos,
			lPos - fPos);
		return messageSender;
	}
#if UNITY_EDITOR

	public static void JoinTesters()
	{
		OnPRIVMSG?.Invoke("pkplonker", "!join");
		OnPRIVMSG?.Invoke("pkplonkertest", "!join");
	}

	public static void FightTesters()
	{
		OnPRIVMSG?.Invoke("pkplonker", "!join");
		OnPRIVMSG?.Invoke("pkplonkertest", "!join");
		OnPRIVMSG?.Invoke("pkplonker", "!fight pkplonkertest");
	}

#endif
}