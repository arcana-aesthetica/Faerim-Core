using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Faerim_Core
{
	public class CompFaerimHP : ThingComp
	{
		public float faeHP = 10;
		public float faeMaxHP = 10;
		public float faeHealed = 0;
		public Dictionary<string, List<int>> storedHitDice = new Dictionary<string, List<int>>();

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			Pawn pawn = this.parent as Pawn;
			if (pawn == null)
			{
				Log.Error("[Faerim] ERROR: CompFaerimHP found no valid Pawn!");
				return;
			}

			// Prevent resetting HP unless it's a fresh spawn (not just going to bed)
			if (!respawningAfterLoad && faeHP == 0)
			{
				faeHP = GetFaeMaxHP();
				Log.Message($"[Faerim] {pawn.LabelShort} has spawned with full Faerim HP: {faeHP}");
			}
		}

		// Fetch dynamically instead of storing it
		public float GetFaeHP() => faeHP;
		public float GetFaeMaxHP() => FaerimHealthUtility.CalculateMaxHealth(this.parent as Pawn);

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Prefs.DevMode) // Only show in Developer Mode
			{
				yield return new Command_Action
				{
					defaultLabel = "Test Heal FaeHP",
					defaultDesc = "Click to heal 5 Faerim HP and heal injuries proportionally.",
					action = () => HealFaerimHP(5),
				};
			}
		}

		public void HealFaerimHP(int amount)
		{
			if (amount <= 0) return; // No healing if the amount is zero or negative

			Pawn pawn = this.parent as Pawn;
			if (pawn == null || pawn.Dead) return;

			// Calculate how much Faerim HP is actually restored
			float oldFaeHP = faeHP;
			faeHP = Mathf.Clamp(faeHP + amount, 0, GetFaeMaxHP());
			float restoredFaeHP = faeHP - oldFaeHP; // Actual amount restored

			// If no Faerim HP was restored, exit early
			if (restoredFaeHP <= 0) return;

			// Calculate the healing ratio (percentage of Faerim Max HP restored as a decimal)
			float healingRatio = (1 / (float)GetFaeMaxHP()) * restoredFaeHP;

			float damageScale = DamageHandlers.GetDamageScale(pawn);
			float damageThreshold = damageScale * faeMaxHP;

			// Find all non-permanent, healable injuries
			List<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
				.OfType<Hediff_Injury>()
				.Where(h => h.CanHealNaturally() && !h.IsPermanent())
				.OrderByDescending(h => h.Severity) // Heal most severe injuries first
				.ToList();

			// Calculate total injury severity (to distribute healing proportionally)
			float totalInjurySeverity = injuries.Sum(h => h.Severity);

			int numInjuries = 0;

			foreach (var injury in injuries)
			{
				numInjuries++;
			}

			// Apply healing to injuries proportional to the ratio
			if (faeHP != faeMaxHP)
			{
				foreach (var injury in injuries)
				{
					float injuryHealing = (damageThreshold * healingRatio) / numInjuries; // Apply % of the injury's severity
					injury.Heal(injuryHealing);
				}
			}
			else
			{
				foreach (var injury in injuries)
				{
					injury.Heal(totalInjurySeverity);
				}
			}




			// Log to verify healing applied correctly
			Log.Message($"[Faerim] {pawn.LabelShort} actively healed {restoredFaeHP} Faerim HP. Applied {healingRatio:P} healing to injuries.");
		}
		public void ModifyFaeHP(float amount)
		{
			faeHP = Mathf.Clamp(faeHP - amount, 0, GetFaeMaxHP());
			if (faeHP <= 0)
			{
				Log.Message($"[Faerim] {parent.LabelShort} has reached 0 HP and may be downed.");
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref faeHP, "faeHP", 10f);
			Scribe_Collections.Look(ref storedHitDice, "storedHitDice", LookMode.Value, LookMode.Deep);
		}

		public override string CompInspectStringExtra()
		{
			return $"FaeHP: {faeHP}/{GetFaeMaxHP()}";
		}
	}

}
