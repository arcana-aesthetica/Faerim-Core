using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using HarmonyLib;

namespace Faerim_Core
{
	/// <summary>
	/// This is the main Mod entry point.
	/// It ensures all Defs are loaded and logs any issues.
	/// </summary>
	public class FaerimMod : Mod
	{
		public FaerimMod(ModContentPack content) : base(content)
		{
			Log.Message("[DEBUG] Faerim Core Mod Initialized!");

			// Ensure Defs are loaded properly AFTER all mods are initialized
			LongEventHandler.ExecuteWhenFinished(() =>
			{
				Log.Message($"[DEBUG] Checking ClassThingDef load... Total ClassThingDefs found: {ClassDatabase.AllClasses.Count}");

				// Log all loaded ClassThingDefs
				foreach (var def in ClassDatabase.AllClasses)
				{
					Log.Message($"[DEBUG] Loaded ClassThingDef: {def.defName}");
				}

				// If no ClassThingDefs are loaded, log an error
				if (!ClassDatabase.AllClasses.Any())
				{
					Log.Error("[ERROR] No ClassThingDefs found! The XML may not be loading correctly.");
				}
			});
		}
	}

	/// <summary>
	/// This ensures Harmony patches are applied and logs core initialization.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class FaerimCore_Init
	{
		static FaerimCore_Init()
		{
			Log.Message("[DEBUG] Loading Faerim Core...");
			Log.Message("Quick test");

			Harmony harmony = new Harmony("Faerim.Core");
			foreach (var method in Harmony.GetAllPatchedMethods())
			{
				Log.Message($"[Faerim] Patched Method: {method.DeclaringType}.{method.Name}");
			}
			harmony.PatchAll();

			Log.Message("[DEBUG] Harmony patches applied!");
			Log.Message("[DEBUG] Faerim Core Fully Loaded!");
		}
	}

	/// <summary>
	/// Utility class for dice rolling.
	/// </summary>
	public static class FaerimTools
	{
		public static int RollDice(int diceNum, int diceVal)
		{
			int result = 0;
			for (int i = 0; i < diceNum; i++)
			{
				result += Rand.RangeInclusive(1, diceVal);
			}
			return result;
		}
	}
}