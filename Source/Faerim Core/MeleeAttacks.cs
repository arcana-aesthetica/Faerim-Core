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
	[HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
	public static class Patch_TryCastShot
	{
		[HarmonyPrefix]
		public static bool Prefix(Verb_MeleeAttack __instance, ref bool __result)
		{
			Log.Message("[Faerim] TryCastShot patch executing...");

			Pawn casterPawn = __instance.CasterPawn;

			LocalTargetInfo target = null;
			try
			{
				target = Traverse.Create(__instance).Field("currentTarget").GetValue<LocalTargetInfo>();
			}
			catch (Exception ex)
			{
				Log.Error($"[Faerim] ERROR: Failed to access currentTarget via reflection! {ex}");
				return false;
			}


			// Ensure the caster and target are valid
			if (casterPawn == null || target.Thing == null)
			{
				Log.Warning("[Faerim] Custom melee system: Missing caster or target.");
				__result = false;
				return false;
			}

			// Log attack initiation
			Log.Message($"[Faerim] {casterPawn.Label} attacks {target.Thing.Label}.");

			// Get selected tool and weapon
			ThingWithComps weapon = __instance.EquipmentSource ?? casterPawn.equipment?.Primary;
			Tool selectedTool = __instance.tool;

			// Ensure weapon tool is used if available
			if (weapon != null && weapon.def.tools != null && weapon.def.tools.Count > 0)
			{
				selectedTool = weapon.def.tools[0];
				Log.Message($"[Faerim] Weapon detected: {weapon.def.label}. Assigned Tool: {selectedTool.label}");
			}

			// Determine DamageDef from the selected tool
			DamageDef damageDef = DamageDefOf.Blunt; // Default fallback
			if (selectedTool?.capacities != null && selectedTool.capacities.Count > 0)
			{
				DamageDef foundDamageDef = DefDatabase<DamageDef>.GetNamedSilentFail(selectedTool.capacities[0].defName);
				if (foundDamageDef != null)
				{
					damageDef = foundDamageDef;
				}
			}
			Log.Message($"[Faerim] Using DamageDef: {damageDef.defName} for attack.");

			// Retrieve target's Armor Class if a pawn
			int targetArmorClass = 0;
			Pawn targetPawn = target.Thing as Pawn;
			if (targetPawn != null)
			{
				targetArmorClass = Mathf.RoundToInt(targetPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_PawnCurrentArmorClass"), false));
			}

			// Check for finesse property
			bool hasFinesse = weapon?.def?.comps?.Any(c => c is Faerim_WeaponProps props && props.weaponProperties.Contains("Finesse")) ?? false;

			// Retrieve modifiers
			float strengthMod = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_StrengthMod"), false);
			float dexterityMod = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_DexterityMod"), false);
			float statBonus = hasFinesse ? Mathf.Max(strengthMod, dexterityMod) : strengthMod;
			float proficiencyBonus = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_ProficiencyBonus"), false);

			// Roll for hit determination
			int attackRoll = Mathf.RoundToInt(Rand.RangeInclusive(1, 20));
			bool isCrit = attackRoll == 20;
			attackRoll += Mathf.RoundToInt(statBonus + proficiencyBonus);

			bool isHit = attackRoll > targetArmorClass || targetArmorClass == 0;
			

			// Apply damage if the attack hits
			if (isHit)
			{
				// Base placeholder damage to ensure ApplyToPawn gets triggered
				float baseDamage = 10;

				// Create DamageInfo and attach a critical hit flag
				DamageInfo dinfo = new DamageInfo(damageDef, baseDamage, 999f, -1f, casterPawn);

				// Store critical hit info inside the DamageInfo object using custom hit part (a workaround)
				if (isCrit)
				{
					dinfo.SetHitPart(target.Thing.def.race.body.corePart); // Use the core part as a placeholder
					Log.Message($"[Faerim] CRITICAL HIT! {casterPawn.LabelShort} rolled a NATURAL 20 against {target.Thing.Label}!");
				}

				target.Thing.TakeDamage(dinfo);
				Log.Message($"[Faerim] {casterPawn.Label}'s attack hit {target.Thing.Label}. (Roll: {attackRoll} vs AC: {targetArmorClass})");
			}
			else
			{
				Log.Message($"[Faerim] {casterPawn.Label}'s attack missed {target.Thing.Label}. (Roll: {attackRoll} vs AC: {targetArmorClass})");
			}

			__result = true;
			return false;
		}
	}

}