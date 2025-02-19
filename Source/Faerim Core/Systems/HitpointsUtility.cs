using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace Faerim_Core
{
	[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
	public static class Patch_Faerim_AutoDowning
	{
		[HarmonyPostfix]
		public static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
		{
			Pawn pawn = __instance.hediffSet.pawn;
			if (pawn == null || pawn.Dead) return;

			var comp = pawn.TryGetComp<CompFaerimHP>();
			if (comp == null) return;

			if (comp.GetFaeHP() <= 0)
			{
				__result = true; // Pawn is downed when Faerim HP reaches 0
			}
		}
	}

	public static class FaerimHealthUtility
	{
		public static int CalculateMaxHealth(Pawn pawn)
		{
			if (pawn == null || pawn.Dead || pawn.health == null)
			{
				Log.Warning($"[Faerim] ERROR: {pawn?.LabelShort} is invalid or dead. Defaulting HP to 10.");
				return 10;
			}

			int baseHP = 10;
			int totalHP = baseHP;

			// Get Constitution modifier dynamically
			int constitutionMod = (int)pawn.GetStatValue(DefDatabaseClass.Faerim_ConstitutionMod, true);

			// Get total character level
			int totalCharacterLevel = (int)pawn.GetStatValue(DefDatabaseClass.Faerim_TotalLevel, true);

			// Fetch stored hit dice
			CompFaerimHP hpComp = pawn.TryGetComp<CompFaerimHP>();
			if (hpComp == null) return totalHP;

			foreach (var hitDice in hpComp.storedHitDice.Values)
			{
				totalHP += hitDice.Sum();
			}

			// Apply Constitution Modifier scaling dynamically
			totalHP += constitutionMod * (totalCharacterLevel + 1);

			// Prevent negative HP
			return Mathf.Max(totalHP, 1);
		}

		public static int GetConstitutionModifier(Pawn pawn)
		{
			return (int)pawn.GetStatValue(DefDatabaseClass.Faerim_ConstitutionMod, true);
		}
	}
}
