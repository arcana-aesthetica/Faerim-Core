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

			// Get class component
			CompClassSystem compClass = pawn.TryGetComp<CompClassSystem>();
			if (compClass == null)
				return;

			// Get total stat bonus from class features
			float bonus = compClass.GetFeatureStatBonus(this.parentStat);

			// Apply the calculated bonus
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
