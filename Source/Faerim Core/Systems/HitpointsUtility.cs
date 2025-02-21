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
			}

			// Apply Constitution Modifier scaling dynamically
			int conBonus = constitutionMod * (totalCharacterLevel + 1);

			// Compute max HP before scars
			totalHP += hitDiceHP + conBonus;

			// Count permanent injuries (scars)
			int permanentInjuries = pawn.health.hediffSet.hediffs
				.Count(h => h.IsPermanent());

			// Subtract 1 max HP per scar, ensuring it never goes below 1
			int adjustedMaxHP = Mathf.Max(1, totalHP - permanentInjuries);

			// **Proportionally adjust current HP based on the change in max HP**
			if (oldMaxHP > 0) // Prevent division by zero
			{
				hpComp.faeHP = Mathf.RoundToInt((hpComp.faeHP / (float)oldMaxHP) * adjustedMaxHP);
			}

			// **Directly store the new max HP**
			hpComp.faeMaxHP = adjustedMaxHP;

			// Return new max HP
			return adjustedMaxHP;
		}


		public static float GetTotalLostHealth(Pawn pawn)
		{
			float totalLostHealth = 0f;

			foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
			{
				float maxHealth = part.def.GetMaxHealth(pawn);
				float partHealth = pawn.health.hediffSet.GetPartHealth(part);

				// Ignore fully destroyed parts
				if (partHealth <= 0) continue;

				// Ignore permanent scars (Hediffs that never heal)
				if (pawn.health.hediffSet.hediffs.Any(h => h.Part == part && h.IsPermanent()))
				{
					continue;
				}

				// Add missing health for this part
				totalLostHealth += (maxHealth - partHealth);
			}

			return totalLostHealth;
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker), "HealthTick")]
	public static class Patch_Faerim_TrackHealing
	{
		[HarmonyPrefix]
		public static void Prefix(Pawn_HealthTracker __instance, out Dictionary<Hediff_Injury, float> __state)
		{
			// Get the pawn reference
			Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
			if (pawn == null || pawn.Dead)
			{
				__state = null;
				return;
			}

			// Store current injury severity before healing
			__state = __instance.hediffSet.hediffs
				.OfType<Hediff_Injury>()
				.Where(h => h.CanHealNaturally())
				.ToDictionary(h => h, h => h.Severity);
		}

		[HarmonyPostfix]
		public static void Postfix(Pawn_HealthTracker __instance, Dictionary<Hediff_Injury, float> __state)
		{
			// Ensure valid state
			if (__state == null) return;

			// Get the pawn reference
			Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
			if (pawn == null || pawn.Dead) return;

			// Get the Faerim HP component
			var comp = pawn.TryGetComp<CompFaerimHP>();
			if (comp == null) return;

			// Track the total healing done
			float totalHealed = 0f;

			// Iterate through injuries and calculate healed amount
			foreach (var injury in __state.Keys)
			{
				float beforeHealing = __state[injury];
				float afterHealing = injury.Severity;
				float healedAmount = beforeHealing - afterHealing;

				if (healedAmount > 0)
				{
					totalHealed += healedAmount;
				}
			}

			// Store healing in faeHealed
			comp.faeHealed += totalHealed;

			// Calculate damageScale dynamically based on missing health
			float totalLostHealth = FaerimHealthUtility.GetTotalLostHealth(pawn);
			float missingFaeHP = comp.GetFaeMaxHP() - comp.faeHP;

			float damageScale = 0;

			// Ensure Faerim HP aligns exactly when fully healed
			if (totalLostHealth > 0 && missingFaeHP > 0)
			{
				damageScale = totalLostHealth / missingFaeHP;
			}
			else
			{
				damageScale = 1f;  // Prevent divide-by-zero
			}

			// Log the accumulated healing
			if (totalHealed > 0)
			{
				Log.Message($"[Faerim] {pawn.LabelShort} healed {totalHealed} this tick. Total stored healing: {comp.faeHealed}. Target: {damageScale}");
			}

			// Convert healing to Faerim HP in whole numbers
			while (comp.faeHealed >= damageScale && comp.faeHP < comp.GetFaeMaxHP())
			{
				comp.faeHP += 1;
				comp.faeHealed -= damageScale;
				Log.Message($"[Faerim] {pawn.LabelShort} HEALED A HITPOINT. Total stored healing: {comp.faeHealed}");
			}

			// Ensure Faerim HP does NOT exceed max HP
			if (comp.faeHP > comp.GetFaeMaxHP())
			{
				comp.faeHP = comp.GetFaeMaxHP();
				comp.faeHealed = 0; // Reset extra stored healing since we are at full HP
				Log.Message($"[Faerim] {pawn.LabelShort} reached max Faerim HP: {comp.faeHP}");
			}
		}
	}
}
