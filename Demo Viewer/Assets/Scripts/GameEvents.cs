﻿using System;

namespace Spark
{
	public static class GameEvents
	{
		public static Action<EchoVRAPI.LastScore> Goal;
		public static Action<EchoVRAPI.LastThrow> LocalThrow;
	}
}