﻿namespace OpenBve
{
	/// <summary>The TrainManager is the root class containing functions to load and manage trains within the simulation world.</summary>
	public static partial class TrainManager
	{
		/// <summary>Defines the behaviour of the other handles (Power & brake) when the EB is activated</summary>
		internal enum EbHandleBehaviour
		{
			/// <summary>No action is taken</summary>
			NoAction = 0,
			/// <summary>The power handle returns to the neutral position</summary>
			PowerNeutral = 1,
			/// <summary>The reverser handle returns to the neutral position</summary>
			ReverserNeutral = 2,
			/// <summary>Both the power handle and the reverser handle return to the neutral position</summary>
			PowerReverserNeutral = 3
		}
	}
}
