using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Faerim_Core
{
	public static class DamageHandlers
	{
		public static int ConvertDamageToDiceRoll(float originalDamage, ThingWithComps weapon, Pawn casterPawn, bool isCrit = false)
		{

			bool hasFinesse = false;
			float strengthMod = 0;
			float dexterityMod = 0;
			float proficiencyBonus = 1;
			float statBonus = 0;

			// If the damage comes from a pawn's attack, use specialized logic
			if (casterPawn != null)
			{
				// Check for finesse property
				hasFinesse = weapon?.def?.comps?.Any(c => c is Faerim_WeaponProps props && props.weaponProperties.Contains("Finesse")) ?? false;

				// Retrieve modifiers
				strengthMod = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_StrengthMod"), false);
				dexterityMod = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_DexterityMod"), false);
				proficiencyBonus = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_ProficiencyBonus"), false);

				// **Check if weapon is ranged**
				bool isRangedWeapon = weapon?.def?.Verbs?.Any(v => v.defaultProjectile != null) ?? false;

				// **Force Dexterity for ranged weapons, else use Finesse logic**
				statBonus = isRangedWeapon ? dexterityMod : (hasFinesse ? Mathf.Max(strengthMod, dexterityMod) : strengthMod);
				Log.Message($"[Faerim] Weapon Type: {(isRangedWeapon ? "Ranged (Using DEX)" : "Melee (Using STR or Finesse)")} | StatBonus: {statBonus}");

				// If the weapon has custom dice, use them
				if (weapon != null)
				{
					Faerim_DamageDiceComp diceComp = weapon.GetComp<Faerim_DamageDiceComp>();
					if (diceComp != null)
					{
						// Roll the weapon's custom dice
						int weaponDamage = FaerimTools.RollDice(diceComp.Props.dice_num, diceComp.Props.dice_val);
						Log.Message($"[Faerim] Using weapon dice: {diceComp.Props.dice_num}d{diceComp.Props.dice_val}, rolled {weaponDamage}.");

						// Apply crit bonus
						if (isCrit)
						{
							weaponDamage += FaerimTools.RollDice(diceComp.Props.dice_num, diceComp.Props.dice_val);
							Log.Message($"[Faerim] After crit: {weaponDamage} damage.");
						}

						// Apply stat and proficiency bonuses
						weaponDamage += Mathf.RoundToInt(statBonus + proficiencyBonus);
						Log.Message($"[Faerim] Final damage after modifiers: {weaponDamage}");

						return weaponDamage;
					}
				}

				float baseDamage = originalDamage;

				if (isRangedWeapon)
				{
					// Ensure we always use the projectile's damage for ranged attacks
					ThingDef projectile = weapon.def.Verbs?
						.Where(v => v.defaultProjectile != null)
						.Select(v => v.defaultProjectile)
						.FirstOrDefault();

					if (projectile != null)
					{
						baseDamage = projectile.projectile?.GetDamageAmount(1f) ?? originalDamage;
						Log.Message($"[Faerim] Ranged attack detected. Using projectile damage: {baseDamage}");
					}
					else
					{
						Log.Warning($"[Faerim] WARNING: Ranged weapon {weapon.def.label} has no valid projectile! Falling back to default.");
					}
				}
				else
				{
					// Use melee tool damage normally
					baseDamage = weapon?.def.tools?.OrderByDescending(t => t.power).FirstOrDefault()?.power ?? originalDamage;
					Log.Message($"[Faerim] Melee attack detected. Using highest power tool. Base damage: {baseDamage}");
				}

				// Ensure Dexterity is used for ranged weapons
				statBonus = isRangedWeapon ? dexterityMod : (hasFinesse ? Mathf.Max(strengthMod, dexterityMod) : strengthMod);
				originalDamage = baseDamage;

			}

			// **For non-pawn damage sources (traps, explosions, etc.), still convert to dice**
			float targetAverage = originalDamage * 0.35f;
			int[] validDice = { 4, 6, 8, 10, 12, 20, 100 };
			int bestDie = 4;
			int bestNumDice = 1;
			float bestDifference = float.MaxValue;
			int finalDamage = 0;

			if (originalDamage > 1)
			{
				foreach (int die in validDice)
				{
					for (int numDice = 1; numDice <= 20; numDice++)
					{
						float average = (numDice * die / 2f) + 1;
						float difference = Math.Abs(average - targetAverage);

						if (difference < bestDifference)
						{
							bestDifference = difference;
							bestDie = die;
							bestNumDice = numDice;
						}
					}
				}

				Log.Message($"[Faerim] Generated Dice: {bestNumDice}d{bestDie}.");
				for (int i = 0; i < bestNumDice; i++)
				{
					finalDamage += FaerimTools.RollDice(1, bestDie);
					Log.Message($"[Faerim] Rolled a die, current damage: {finalDamage}.");

					if (isCrit)
					{
						finalDamage += FaerimTools.RollDice(1, bestDie);
						Log.Message($"[Faerim] After crit: {finalDamage} damage.");
					}
				}
			}
			else
			{
				finalDamage = 1;
			}

			// Apply stat and proficiency bonuses only if a pawn was involved
			if (casterPawn != null)
			{
				finalDamage += Mathf.RoundToInt(statBonus + proficiencyBonus);
				Log.Message($"[Faerim] Final damage after modifiers: {finalDamage}");
			}

			Log.Message($"[Faerim] Damage converted to dice. Final damage: {finalDamage}.");
			return finalDamage;
		}
	}


	[HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyToPawn")]
	public static class Patch_Faerim_AdjustDamageSeverity
	{
		[HarmonyPrefix]
		public static void Prefix(ref DamageInfo dinfo, Pawn pawn)
		{
			Log.Message($"[Faerim] ApplyToPawn is running: {pawn?.LabelShort ?? "NULL"} | Damage: {dinfo.Amount}");

			// Ensure valid pawn and prevent processing dead pawns
			if (pawn == null || pawn.Dead) return;

			// Get Faerim HP component
			var comp = pawn.TryGetComp<CompFaerimHP>();
			if (comp == null) return;

			// Extract the attacker (Instigator)
			Pawn casterPawn = dinfo.Instigator as Pawn;

			// If the damage comes from a pawn's attack, apply special logic
			if (casterPawn != null)
			{
				// Extract weapon from the attacker (if applicable)
				ThingWithComps weapon = casterPawn.equipment?.Primary;

				// **Check if this was a Critical Hit** (we use the core part as an indicator)
				bool isCrit = dinfo.HitPart == pawn.def.race.body.corePart;
				if (isCrit)
				{
					Log.Message($"[Faerim] CRITICAL HIT confirmed in ApplyToPawn for {casterPawn.LabelShort}!");
				}

				// Adjust damage using dice-based conversion
				int baseDamage = DamageHandlers.ConvertDamageToDiceRoll(dinfo.Amount, weapon, casterPawn, isCrit);
				dinfo.SetAmount(baseDamage);
			}
			else
			{
				// Non-pawn sources still get converted to dice, but without weapon or crit considerations
				dinfo.SetAmount(DamageHandlers.ConvertDamageToDiceRoll(dinfo.Amount, null, null, false));
			}

			// Get Faerim HP stats
			float faeHP = comp.GetFaeHP();
			float faeMaxHP = comp.GetFaeMaxHP();

			// Estimate downing injury threshold
			float lethalThreshold = pawn.health.LethalDamageThreshold;
			float downingThreshold = lethalThreshold * 0.2f; 

			// Calculate proportional injury severity
			float damageScale = downingThreshold / faeMaxHP;
			float adjustedDamage = dinfo.Amount * damageScale;

			// Apply Faerim HP loss
			comp.ModifyFaeHP(dinfo.Amount);

			// **Modify `dinfo.Amount` so vanilla handles injury correctly**
			dinfo.SetAmount(adjustedDamage);

			// Debug Log
			Log.Message($"[Faerim] {pawn.LabelShort} took {dinfo.Amount} adjusted damage from {(casterPawn != null ? casterPawn.LabelShort : "non-pawn source")}. Faerim HP: {comp.GetFaeHP()}/{faeMaxHP}. Injury Severity Adjusted: {adjustedDamage}.");
		}
	}


}
