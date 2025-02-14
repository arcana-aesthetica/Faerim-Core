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
	public class StatPart_Modifier : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			if (req.HasThing && req.Thing is Pawn pawn)
			{
				// Get stat storage component
				CompPawnStats comp = pawn.TryGetComp<CompPawnStats>();
				if (comp == null) return;

				// Derive related stat name (remove "Mod" suffix)
				string relatedStatDefName = GetRelatedStatDefName();

				// Get stored base stat
				float baseValue = comp.GetBaseStat(relatedStatDefName);

				// Calculate and apply modifier
				float modifier = Mathf.FloorToInt((baseValue - 10) / 2);
				val += modifier;
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			if (req.HasThing && req.Thing is Pawn pawn)
			{
				CompPawnStats comp = pawn.TryGetComp<CompPawnStats>();
				if (comp == null) return null;

				string relatedStatDefName = GetRelatedStatDefName();
				float baseValue = comp.GetBaseStat(relatedStatDefName);
				float modifier = Mathf.FloorToInt((baseValue - 10) / 2);

				return $"{parentStat.label.CapitalizeFirst()}: (Base {relatedStatDefName}: {baseValue} - 10) / 2 = {modifier.ToStringWithSign()}";
			}
			return null;
		}

		private string GetRelatedStatDefName()
		{
			return parentStat.defName.Replace("Mod", "");
		}
	}


	[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
	public static class PawnGeneratorPatch
	{
		[HarmonyPostfix]
		public static void GenerateStats(Pawn __result)
		{
			// Ensure we're modifying a valid humanlike pawn
			if (__result != null && __result.RaceProps.Humanlike)
			{
				// Generate and store stats using a Comp (per-pawn storage)
				CompPawnStats comp = __result.TryGetComp<CompPawnStats>();
				if (comp != null)
				{
					comp.SetBaseStat("Faerim_Strength", FaerimTools.RollDice(3, 6));
					comp.SetBaseStat("Faerim_Dexterity", FaerimTools.RollDice(3, 6));
					comp.SetBaseStat("Faerim_Constitution", FaerimTools.RollDice(3, 6));
					comp.SetBaseStat("Faerim_Wisdom", FaerimTools.RollDice(3, 6));
					comp.SetBaseStat("Faerim_Intelligence", FaerimTools.RollDice(3, 6));
					comp.SetBaseStat("Faerim_Charisma", FaerimTools.RollDice(3, 6));
				}
			}
		}

		private static void SetPawnStat(Pawn pawn, string statDefName, float value)
		{
			StatDef stat = StatDef.Named(statDefName);

			// Apply the stat value using traits
			if (pawn.story?.traits != null)
			{
				pawn.story.traits.GainTrait(new Trait(DefDatabase<TraitDef>.GetNamedSilentFail(statDefName), Mathf.RoundToInt(value), false));
			}
		}
	}
}
