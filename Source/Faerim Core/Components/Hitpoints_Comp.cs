using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

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
}
