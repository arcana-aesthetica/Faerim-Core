using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace Faerim_Core
{
	[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
	public static class Patch_Pawn_SpawnSetup
	{
		static void Postfix(Pawn __instance, bool respawningAfterLoad)
		{
			if (!__instance.RaceProps.Humanlike) // Apply to ALL non-humanlike pawns
			{
				AssignNonHumanlikeStats(__instance);
			}
		}

		private static void AssignNonHumanlikeStats(Pawn pawn)
		{
			CompPawnStats comp = pawn.TryGetComp<CompPawnStats>();
			CompFaerimHP hpComp = pawn.TryGetComp<CompFaerimHP>();

			if (comp == null)
			{
				Log.Warning($"[Faerim] {pawn.Label} is missing CompPawnStats!");
				return;
			}

			if (hpComp == null)
			{
				Log.Warning($"[Faerim] {pawn.Label} is missing CompFaerimHP!");
				return;
			}

			float bodySize = pawn.RaceProps.baseBodySize;
			float wildness = pawn.RaceProps.wildness;
			bool isPredator = pawn.RaceProps.predator;
			float healthScale = pawn.RaceProps.baseHealthScale;

			// **Assign Core Attributes Based on Pawn Traits (Now Floored)**
			SetStatBaseIfMissing(comp, "Faerim_Strength", Mathf.FloorToInt(5 + (bodySize * 4) + (isPredator ? 2 : 0)));
			SetStatBaseIfMissing(comp, "Faerim_Dexterity", Mathf.FloorToInt(5 + (bodySize * 2) - (wildness * 3)));
			SetStatBaseIfMissing(comp, "Faerim_Constitution", Mathf.FloorToInt(10 + (bodySize * 5) + (healthScale * 2)));
			SetStatBaseIfMissing(comp, "Faerim_Wisdom", Mathf.FloorToInt(5 + (wildness * 3)));
			SetStatBaseIfMissing(comp, "Faerim_Intelligence", Mathf.FloorToInt(5 + ((1 - wildness) * 4)));
			SetStatBaseIfMissing(comp, "Faerim_Charisma", Mathf.FloorToInt(5 + ((1 - wildness) * 3)));


			// **Assign Combat Stats**
			SetStatBaseIfMissing(comp, "Faerim_PawnBaseArmorClass", 10 + (bodySize * 2) + (isPredator ? 1 : 0));
			SetStatBaseIfMissing(comp, "Faerim_PawnCurrentArmorClass", comp.GetBaseStat("Faerim_PawnBaseArmorClass"));
			SetStatBaseIfMissing(comp, "Faerim_ProficiencyBonus", 1 + (bodySize * 0.2f) + ((1 - wildness) * 1));

			// **Force Faerim HP Recalculation AFTER Stats Are Set**
			hpComp.faeMaxHP = FaerimHealthUtility.CalculateMaxHealth(pawn);
			hpComp.faeHP = hpComp.faeMaxHP; // Set to full health

			Log.Message($"[Faerim] {pawn.Label} assigned Faerim HP: {hpComp.faeHP}/{hpComp.faeMaxHP}");
			Log.Message($"[Faerim] Stats assigned to {pawn.Label}");
		}

		private static void SetStatBaseIfMissing(CompPawnStats comp, string statDefName, float defaultValue)
		{
			if (comp.GetBaseStat(statDefName) == 0f) // If the stat isn't set, assign it
			{
				comp.SetBaseStat(statDefName, defaultValue);
				Log.Message($"[Faerim] Assigned {statDefName} = {defaultValue}");
			}
		}
	}
}
