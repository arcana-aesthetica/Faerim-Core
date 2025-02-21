using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Faerim_Core
{
	public class StatPart_FaerimFeatureBonus : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			if (!req.HasThing || !(req.Thing is Pawn pawn) || !pawn.RaceProps.Humanlike)
				return;

			CompClassSystem compClass = pawn.TryGetComp<CompClassSystem>();
			if (compClass == null)
				return;

			float bonus = 0f;

			foreach (var classDef in compClass.GetAllClasses())
			{
				if (classDef.levelFeatures == null)
					continue;

				foreach (var entry in classDef.levelFeatures)
				{
					if (entry.choices != null && entry.choices.Count > 0)
					{
						// Apply the chosen option's bonus
						FeatureChoice chosen = compClass.GetSelectedFeatureChoice(pawn, entry.featureName);
						if (chosen != null)
						{
							foreach (var modifier in chosen.statModifiers)
							{
								if (modifier.stat == this.parentStat)
								{
									bonus += modifier.value;
								}
							}
						}
					}
					else if (entry.statModifiers != null)
					{
						// Apply default stat modifiers
						foreach (var modifier in entry.statModifiers)
						{
							if (modifier.stat == this.parentStat)
							{
								bonus += modifier.value;
							}
						}
					}
				}
			}

			val += bonus;
		}


		public override string ExplanationPart(StatRequest req)
		{
			if (!req.HasThing || !(req.Thing is Pawn pawn))
				return null;

			// Get class component
			CompClassSystem compClass = pawn.TryGetComp<CompClassSystem>();
			if (compClass == null)
				return null;

			// Get breakdown of all feature bonuses
			float bonus = compClass.GetFeatureStatBonus(this.parentStat);
			if (bonus == 0) return null;

			return $"Class Features: +{bonus.ToString("+0.##;-0.##")}";
		}
	}
}
