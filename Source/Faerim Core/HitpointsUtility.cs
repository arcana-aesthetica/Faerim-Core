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
	public class CompFaerimHP : ThingComp
	{
		public float faeMaxHP = 10;
		public float faeHP = 10;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			Pawn pawn = this.parent as Pawn;
			if (pawn == null)
			{
				Log.Error("[Faerim] ERROR: CompFaerimHP found no valid Pawn!");
				return;
			}

			// Only recalculate HP on new spawn, not on load
			if (!respawningAfterLoad)
			{
				faeMaxHP = FaerimHealthUtility.CalculateMaxHealth(pawn);
				faeHP = faeMaxHP; // Ensure full health on fresh spawn
			}
		}

		public float GetFaeHP() => faeHP;
		public float GetFaeMaxHP() => faeMaxHP;

		public void ModifyFaeHP(float amount)
		{
			faeHP = Mathf.Clamp(faeHP - amount, 0, faeMaxHP);
			if (faeHP <= 0)
			{
				Log.Message($"[Faerim] {parent.LabelShort} has reached 0 HP and may be downed.");
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref faeMaxHP, "faeMaxHP", 10f);
			Scribe_Values.Look(ref faeHP, "faeHP", 10f);
		}

		public override string CompInspectStringExtra()
		{
			return $"FaeHP: {faeHP}/{faeMaxHP}";
		}
	}

	public class CompProperties_FaerimHP : CompProperties
	{
		public CompProperties_FaerimHP()
		{
			this.compClass = typeof(CompFaerimHP);
		}
	}

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

			// Retrieve Constitution modifier
			int constitutionMod = GetConstitutionModifier(pawn);

			// Retrieve total class levels (Pawns start at Level 0)
			int totalCharacterLevel = (int)pawn.GetStatValue(DefDatabaseClass.Faerim_TotalLevel, true);

			// Retrieve class levels from CompClassSystem
			CompClassSystem compClass = pawn.TryGetComp<CompClassSystem>();

			if (compClass != null)
			{
				foreach (ClassThingDef classDef in compClass.GetAllClasses())
				{
					if (classDef == null) continue;

					// Get actual class level
					int classLevel = compClass.GetClassLevel(classDef.defName);
					int classHitDie = classDef.hitDie;

					// Roll hit dice for each level in this class
					totalHP += RollHitDie(classHitDie, classLevel);
				}
			}

			// Apply Constitution Modifier Scaling
			totalHP += constitutionMod * (totalCharacterLevel + 1);

			// Allow Constitution to lower HP but prevent zero or negative HP
			totalHP = Mathf.Max(totalHP, 1);

			return totalHP;
		}

		private static int RollHitDie(int hitDie, int levels)
		{
			int total = 0;
			for (int i = 0; i < levels; i++)
			{
				total += FaerimTools.RollDice(1, hitDie);
			}
			return total;
		}

		private static int GetConstitutionModifier(Pawn pawn)
		{
			return (int)pawn.GetStatValue(DefDatabaseClass.Faerim_ConstitutionMod, true);
		}
	}

}
