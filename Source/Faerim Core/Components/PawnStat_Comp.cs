using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Faerim_Core
{
	public class CompPawnStats : ThingComp
	{
		private Dictionary<string, float> statValues = new Dictionary<string, float>();

		public void SetBaseStat(string statName, float value)
		{
			statValues[statName] = value;
		}

		public float GetBaseStat(string statName)
		{
			return statValues.TryGetValue(statName, out float value) ? value : 0f;
		}

		// 🔹 **Ensure stats persist between saves**
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref statValues, "statValues", LookMode.Value, LookMode.Value);

			// Ensure the dictionary is always initialized after loading
			if (statValues == null)
				statValues = new Dictionary<string, float>();
		}
	}
}
