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
			float healthScale = pawn.RaceProps.baseHealthScale;
			float wildness = pawn.RaceProps.wildness;
			bool isPredator = pawn.RaceProps.predator;
			float moveSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);

			// **Assign Core Attributes Based on Universal Scaling**
			SetStatBaseIfMissing(comp, "Faerim_Strength", Mathf.RoundToInt(8 + (bodySize * 4) + (isPredator ? 2 : 0)));
			SetStatBaseIfMissing(comp, "Faerim_Dexterity", Mathf.RoundToInt(10 + (moveSpeed * 1.5f) - (wildness * 3)));
			SetStatBaseIfMissing(comp, "Faerim_Constitution", Mathf.RoundToInt(8 + (bodySize * 5) + (healthScale * 3)));
			SetStatBaseIfMissing(comp, "Faerim_Intelligence", Mathf.RoundToInt(2 + ((1 - wildness) * 6)));
			SetStatBaseIfMissing(comp, "Faerim_Wisdom", Mathf.RoundToInt(8 + (wildness * 6)));
			SetStatBaseIfMissing(comp, "Faerim_Charisma", Mathf.RoundToInt(6 + ((1 - wildness) * 6)));

			// **Assign Attribute Modifiers**
			SetStatBaseIfMissing(comp, "Faerim_StrengthMod", Mathf.FloorToInt((comp.GetBaseStat("Faerim_Strength") - 10) / 2));
			SetStatBaseIfMissing(comp, "Faerim_DexterityMod", Mathf.FloorToInt((comp.GetBaseStat("Faerim_Dexterity") - 10) / 2));
			SetStatBaseIfMissing(comp, "Faerim_ConstitutionMod", Mathf.FloorToInt((comp.GetBaseStat("Faerim_Constitution") - 10) / 2));
			SetStatBaseIfMissing(comp, "Faerim_WisdomMod", Mathf.FloorToInt((comp.GetBaseStat("Faerim_Wisdom") - 10) / 2));
			SetStatBaseIfMissing(comp, "Faerim_IntelligenceMod", Mathf.FloorToInt((comp.GetBaseStat("Faerim_Intelligence") - 10) / 2));
			SetStatBaseIfMissing(comp, "Faerim_CharismaMod", Mathf.FloorToInt((comp.GetBaseStat("Faerim_Charisma") - 10) / 2));

			// **Assign Combat Stats**
			SetStatBaseIfMissing(comp, "Faerim_PawnBaseArmorClass", Mathf.RoundToInt(10 + (bodySize * 2) + (isPredator ? 1 : 0)));
			SetStatBaseIfMissing(comp, "Faerim_PawnCurrentArmorClass", comp.GetBaseStat("Faerim_PawnBaseArmorClass"));
			SetStatBaseIfMissing(comp, "Faerim_ProficiencyBonus", Mathf.RoundToInt(1 + (bodySize * 0.2f) + ((1 - wildness) * 1)));

			// **Force Faerim HP Recalculation AFTER Stats Are Set**
			hpComp.faeHP = hpComp.GetFaeMaxHP(); // Set to full health

			Log.Message($"[Faerim] {pawn.Label} assigned Faerim HP: {hpComp.faeHP}/{hpComp.GetFaeMaxHP()}");
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
