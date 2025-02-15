using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;

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
			if (pawn.def.statBases == null)
				pawn.def.statBases = new List<StatModifier>(); // Ensure statBases exists

			float bodySize = pawn.RaceProps.baseBodySize;
			float wildness = pawn.RaceProps.wildness;
			bool isPredator = pawn.RaceProps.predator;
			float healthScale = pawn.RaceProps.baseHealthScale;

			// **Assign Core Attributes Based on Pawn Traits**
			SetStatBaseIfMissing(pawn, "Faerim_Strength", 5 + (bodySize * 4) + (isPredator ? 2 : 0));
			SetStatBaseIfMissing(pawn, "Faerim_Dexterity", 5 + (bodySize * 2) - (wildness * 3));
			SetStatBaseIfMissing(pawn, "Faerim_Constitution", 10 + (bodySize * 5) + (healthScale * 2));
			SetStatBaseIfMissing(pawn, "Faerim_Wisdom", 5 + (wildness * 3));
			SetStatBaseIfMissing(pawn, "Faerim_Intelligence", 5 + ((1 - wildness) * 4));
			SetStatBaseIfMissing(pawn, "Faerim_Charisma", 5 + ((1 - wildness) * 3));

			// **Assign Attribute Modifiers**
			SetStatBaseIfMissing(pawn, "Faerim_StrengthMod", (GetStatBase(pawn, "Faerim_Strength") - 10) / 2);
			SetStatBaseIfMissing(pawn, "Faerim_DexterityMod", (GetStatBase(pawn, "Faerim_Dexterity") - 10) / 2);
			SetStatBaseIfMissing(pawn, "Faerim_ConstitutionMod", (GetStatBase(pawn, "Faerim_Constitution") - 10) / 2);
			SetStatBaseIfMissing(pawn, "Faerim_WisdomMod", (GetStatBase(pawn, "Faerim_Wisdom") - 10) / 2);
			SetStatBaseIfMissing(pawn, "Faerim_IntelligenceMod", (GetStatBase(pawn, "Faerim_Intelligence") - 10) / 2);
			SetStatBaseIfMissing(pawn, "Faerim_CharismaMod", (GetStatBase(pawn, "Faerim_Charisma") - 10) / 2);

			// **Assign Combat Stats**
			SetStatBaseIfMissing(pawn, "Faerim_PawnBaseArmorClass", 10 + (bodySize * 2) + (isPredator ? 1 : 0));
			SetStatBaseIfMissing(pawn, "Faerim_PawnCurrentArmorClass", GetStatBase(pawn, "Faerim_PawnBaseArmorClass"));
			SetStatBaseIfMissing(pawn, "Faerim_ProficiencyBonus", 1 + (bodySize * 0.2f) + ((1 - wildness) * 1));

			Log.Message($"[Faerim] Stats assigned to {pawn.Label}");
		}

		private static void SetStatBaseIfMissing(Pawn pawn, string statDefName, float defaultValue)
		{
			StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
			if (stat == null)
				return; // StatDef not found, avoid errors.

			if (!pawn.def.statBases.Exists(s => s.stat == stat)) // If the stat isn't already defined, set it
			{
				pawn.def.statBases.Add(new StatModifier { stat = stat, value = defaultValue });
				Log.Message($"[Faerim] Assigned default {statDefName} = {defaultValue} to {pawn.Label}");
			}
		}

		private static float GetStatBase(Pawn pawn, string statDefName)
		{
			StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
			if (stat == null)
				return 0f; // If the stat isn't found, return 0.

			StatModifier existingStat = pawn.def.statBases.Find(s => s.stat == stat);
			return existingStat != null ? existingStat.value : 0f;
		}
	}
}
