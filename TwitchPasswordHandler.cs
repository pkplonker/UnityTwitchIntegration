using System;
using System.IO;
using TwitchIntegration;
using UnityEngine;

public static class TwitchPasswordHandler
{
	private static string password;
	private static string path = "S:/Users/pkplo/OneDrive/Desktoppass.txt";

	public static string GetPassword()
	{
		Load();
		return password;
	}


	public static void Load()
	{
		if (!File.Exists(path))
		{
			Debug.Log("pass file not found");
			return;
		}
		var json = File.ReadAllText(path);
		var p = JsonUtility.FromJson<TwitchPass>(json);

		password = p.password;
	}
}