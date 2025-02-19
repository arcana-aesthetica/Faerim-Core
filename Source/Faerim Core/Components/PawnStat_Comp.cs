using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Faerim_Core
{
	public class CompPawnStats : ThingComp
	{
		private Dictionary<string, float> statValues = new Dictionary<string, float>();

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref statValues, "statValues", LookMode.Value, LookMode.Value);

			if (statValues == null)
				statValues = new Dictionary<string, float>();
		}

		/// **Fetches the final stat value: Base + Dynamic Modifiers**
		public float GetTotalStat(string statDefName)
		{
			Pawn pawn = parent as Pawn;
			if (pawn == null) return 0f;

			StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
			if (stat == null) return 0f;

			// Fetch this pawn's unique base stat value
			float baseValue = GetBaseStat(statDefName);

			// Get dynamic modifiers applied by `StatDef` (StatParts, Traits, etc.)
			float dynamicModifiers = pawn.GetStatValue(stat, true);

			// **Final stat value = Base stat + Dynamic Modifiers**
			return baseValue + dynamicModifiers;
		}

		/// **Fetches the pawn's stored base value (without modifiers)**
		public float GetBaseStat(string statDefName)
		{
			if (statValues.TryGetValue(statDefName, out float storedValue))
			{
				return storedValue;
			}

			// If no stored value, default to 0 (StatDef should handle all modifications)
			return 0f;
		}

		/// **Sets a new base value for the pawn (ensuring individuality)**
		public void SetBaseStat(string statDefName, float value)
		{
			if (statValues.ContainsKey(statDefName))
			{
				statValues[statDefName] = value;
			}
			else
			{
				statValues.Add(statDefName, value);
			}

			Log.Message($"[Faerim] {parent.LabelShort} {statDefName} base stat set to {value}");
		}
	}
}
