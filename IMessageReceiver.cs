//
// Copyright (C) 2022 Stuart Heath. All rights reserved.
//

using UnityEngine;

namespace TwitchIntegration
{
	public abstract class MessageReceiver : MonoBehaviour
	{
		protected virtual void OnEnable() => IRCParser.OnPRIVMSG += HandleNewTwitchMessage;

		protected virtual void OnDisable() => IRCParser.OnPRIVMSG -= HandleNewTwitchMessage;

		protected abstract void HandleNewTwitchMessage(string sender, string message);
	}
}