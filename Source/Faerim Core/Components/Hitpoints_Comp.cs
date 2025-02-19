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

			// Set HP to full on fresh spawn
			if (!respawningAfterLoad)
			{
				faeHP = GetFaeMaxHP();
			}
		}

		// Fetch dynamically instead of storing it
		public float GetFaeHP() => faeHP;
		public float GetFaeMaxHP() => FaerimHealthUtility.CalculateMaxHealth(this.parent as Pawn);

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
