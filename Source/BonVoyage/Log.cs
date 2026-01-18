/*
	This file is part of Bon Voyage /L
		© 2024-2025 LisiasT : http://lisias.net <support@lisias.net>

	THIE FILE is licensed to you under:

	* WTFPL - http://www.wtfpl.net
		* Everyone is permitted to copy and distribute verbatim or modified
			copies of this license document, and changing it is allowed as long
			as the name is changed.

	THIS FILE is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/
using System;
using System.Diagnostics;

namespace BonVoyage
{
	public static class Log
	{
		internal static void force(string msg, params object[] @params)
		{
			UnityEngine.Debug.LogFormat("[BonVoyage] " + msg, @params);
		}

		internal static void err(string msg, params object[] @params)
		{
			UnityEngine.Debug.LogErrorFormat("[BonVoyage] " + msg, @params);
			StackTrace stacktrace = new StackTrace();
			string stackdump = stacktrace.ToString();
			UnityEngine.Debug.LogError("[BonVoyage] " + stackdump);
		}

#if DEBUG
		internal static void dbg(Exception e)
		{
			UnityEngine.Debug.LogException(e);
		}
#endif
	}
}
