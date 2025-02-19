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

			// Fetch HP component
			CompFaerimHP hpComp = pawn.TryGetComp<CompFaerimHP>();
			if (hpComp == null) return totalHP;

			// **Store old max HP separately**
			float oldMaxHP = hpComp.faeMaxHP; // Directly accessing the stored value to avoid infinite loops

			// Calculate Hit Dice HP
			int hitDiceHP = 0;
			foreach (var hitDice in hpComp.storedHitDice)
			{
				int classTotal = hitDice.Value.Sum();
				hitDiceHP += classTotal;
				Log.Message($"[Faerim] {pawn.LabelCap} - Class {hitDice.Key}: {string.Join(", ", hitDice.Value)} (Total: {classTotal})");
			}

			// Apply Constitution Modifier scaling dynamically
			int conBonus = constitutionMod * (totalCharacterLevel + 1);

			// Compute final HP
			totalHP += hitDiceHP + conBonus;

			// Prevent negative HP
			int newMaxHP = Mathf.Max(totalHP, 1);

			// **Proportionally adjust current HP based on the change in max HP**
			if (oldMaxHP > 0) // Prevent division by zero
			{
				hpComp.faeHP = Mathf.RoundToInt((hpComp.faeHP / (float)oldMaxHP) * newMaxHP);
			}

			// **Directly store the new max HP instead of using GetFaeMaxHP()**
			hpComp.faeMaxHP = newMaxHP;

			// **Debug Log Breakdown**
			//Log.Message($"[Faerim] HP Calculation Breakdown for {pawn.LabelCap}: Base HP: {baseHP} + ((Total Levels: {totalCharacterLevel} + 1) * Constitution Modifier: {constitutionMod})): {conBonus} + HitDice: {hitDiceHP}");
			//Log.Message($"Hitpoints adjusted: Old Max HP: {oldMaxHP} | New Max HP: {newMaxHP} | Adjusted Current HP: {hpComp.faeHP}/{newMaxHP}");

			// Return new max HP
			return newMaxHP;
		}
	}
}
