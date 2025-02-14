using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Faerim_Core
{
	// Patch to remove random misses from projectile launch
	[HarmonyPatch(typeof(Projectile), "Launch", new Type[] {
		typeof(Thing),                // launcher
		typeof(Vector3),              // origin
		typeof(LocalTargetInfo),      // usedTarget
		typeof(LocalTargetInfo),      // intendedTarget
		typeof(ProjectileHitFlags),   // hitFlags
		typeof(bool),                 // preventFriendlyFire
		typeof(Thing),                // equipment
		typeof(ThingDef)              // targetCoverDef
	})]
	public static class Patch_Projectile_Launch
	{
		[HarmonyPrefix]
		public static void Prefix(Projectile __instance, ref LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget)
		{
			Log.Message($"######### [DEBUG] NEW ATTACK: Launching projectile {__instance.def.defName} at {(usedTarget.Thing != null ? usedTarget.Thing.Label : "UNKNOWN TARGET")}.");

			// Ensure the projectile has a valid target
			if (usedTarget.Thing == null && intendedTarget.Thing != null)
			{
				Log.Warning($"[DEBUG] usedTarget is NULL, defaulting to intendedTarget: {intendedTarget.Thing.Label}.");
				usedTarget = intendedTarget;
			}

			// Ensure we're not targeting an inanimate object unless necessary
			if (!(usedTarget.Thing is Pawn) && intendedTarget.Thing is Pawn)
			{
				Log.Warning($"[DEBUG] Target is not a pawn! Redirecting to intended target: {intendedTarget.Thing.Label}.");
				usedTarget = intendedTarget;
			}

			// Access and modify the protected 'destination' field using reflection
			FieldInfo destinationField = typeof(Projectile).GetField("destination", BindingFlags.NonPublic | BindingFlags.Instance);
			if (destinationField != null)
			{
				destinationField.SetValue(__instance, usedTarget.Cell.ToVector3Shifted());
				Log.Message($"[DEBUG] Forced projectile {__instance.def.defName} to travel directly to {usedTarget.Cell}.");
			}
			else
			{
				Log.Warning($"[DEBUG] Failed to access 'destination' field in Projectile {__instance.def.defName}.");
			}
		}
	}

	// Patch to handle projectile impact using the attack roll system
	[HarmonyPatch(typeof(Projectile), "ImpactSomething")]
	public static class Patch_Projectile_Impact
	{
		[HarmonyPrefix]
		public static bool Prefix(Projectile __instance)
		{
			Log.Message($"[Faerim] Impact detected for projectile {__instance.def.defName}.");

			// Ensure the projectile has a valid launcher
			Pawn casterPawn = __instance.Launcher as Pawn;
			if (casterPawn == null)
			{
				Log.Warning($"[Faerim] Projectile {__instance.def.defName} impacted, but the launcher is NOT a pawn. Destroying.");
				__instance.Destroy(DestroyMode.Vanish);
				return false;
			}

			Log.Message($"[Faerim] {casterPawn.Label} fired {__instance.def.defName}.");

			// Ensure the projectile has a valid target
			LocalTargetInfo target = __instance.usedTarget;
			if (target.Thing == null)
			{
				Log.Warning($"[DEBUG] Projectile {__instance.def.defName} impacted, but has no valid target. Attempting to use intended target.");
				target = __instance.intendedTarget;
			}
			if (target.Thing == null)
			{
				Log.Warning($"[Faerim] Projectile {__instance.def.defName} impacted, but still has no valid target. Destroying.");
				__instance.Destroy(DestroyMode.Vanish);
				return false;
			}

			// Check if the target is a pawn and retrieve AC
			int targetArmorClass = 0;
			Pawn targetPawn = target.Thing as Pawn;
			if (targetPawn != null)
			{
				targetArmorClass = Mathf.RoundToInt(targetPawn.GetStatValue(DefDatabaseClass.Faerim_PawnCurrentArmorClass, false));
			}

			// Retrieve modifiers for the attack roll
			float dexterityMod = casterPawn.GetStatValue(DefDatabaseClass.Faerim_DexterityMod, false);
			float proficiencyBonus = casterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_ProficiencyBonus"), false);

			// Roll attack against target's AC
			int attackRoll = Mathf.RoundToInt(Rand.RangeInclusive(1, 20));
			bool isCrit = attackRoll == 20; // Natural 20
			attackRoll += Mathf.RoundToInt(dexterityMod + proficiencyBonus);
			bool isHit = attackRoll > targetArmorClass || targetArmorClass == 0;
			

			Log.Message($"[Faerim] {casterPawn.Label}'s attack roll: {attackRoll} vs AC: {targetArmorClass}. Result: {(isHit ? "HIT" : "MISS")}{(isCrit ? " (CRITICAL HIT!)" : "")}");

			// If the attack misses, destroy the projectile and prevent damage
			if (!isHit)
			{
				Log.Message($"[Faerim] Attack missed! {casterPawn.Label}'s shot at {target.Thing.Label} did not land. Destroying projectile.");
				__instance.Destroy(DestroyMode.Vanish);
				return false;
			}

			// Retrieve weapon (if applicable)
			ThingWithComps weapon = casterPawn.equipment?.Primary;

			// Base placeholder damage to ensure ApplyToPawn gets triggered
			float baseDamage = __instance.def.projectile.GetDamageAmount(1f);


			Log.Message($"[Faerim] Projectile DamageDef: {(__instance.def.projectile.damageDef != null ? __instance.def.projectile.damageDef.defName : "NULL")}");
			// Create DamageInfo and attach a critical hit flag using the hit part
			DamageInfo dinfo = new DamageInfo(__instance.def.projectile.damageDef, baseDamage, 0f, -1f, casterPawn);

			// Store critical hit info inside the DamageInfo object using a workaround
			if (isCrit)
			{
				dinfo.SetHitPart(target.Thing.def.race.body.corePart);
				Log.Message($"[Faerim] CRITICAL HIT! {casterPawn.LabelShort} rolled a NATURAL 20 against {target.Thing.Label}!");
			}

			target.Thing.TakeDamage(dinfo);

			Log.Message($"[Faerim] {target.Thing.Label} was hit for base damage: {baseDamage}.");

			// Destroy projectile after impact
			Log.Message($"[Faerim] Destroying projectile {__instance.def.defName}.");
			__instance.Destroy(DestroyMode.Vanish);
			return false;
		}
	}

}
