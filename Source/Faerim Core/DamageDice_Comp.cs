using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Faerim_Core
{
	public class Faerim_DamageDiceComp : ThingComp
	{
		public Faerim_DamageDiceProps Props
		{
			get
			{
				return (Faerim_DamageDiceProps)this.props;  // Ensure it casts to Properties (CompProperties)
			}
		}
	}

	public class Faerim_DamageDiceProps : CompProperties
	{
		public int dice_num;
		public int dice_val;

		public Faerim_DamageDiceProps()
		{
			this.compClass = typeof(Faerim_DamageDiceComp);
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			foreach (StatDrawEntry s in base.SpecialDisplayStats(req))  // Call the base method to include other stats
			{
				yield return s;  // Yield any stats from the base method first
			}

			string DamageDice = $"{dice_num}d{dice_val}";  // Default description
			string DamageDiceDescription = $"The damage will be determined by rolling a dice. The dice will have {dice_val} sides and it will be rolled {dice_num} time.";

			// Add custom stat for apparel type
			yield return new StatDrawEntry(
				StatCategoryDefOf.Weapon,  // Category for the stat
				"Damage:",
				DamageDice,
				DamageDiceDescription,
				0,
				null,
				null
				);

			yield break;  // End of the method
		}
	}
}
