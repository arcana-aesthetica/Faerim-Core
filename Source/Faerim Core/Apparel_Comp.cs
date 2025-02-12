using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Faerim_Core
{
	public class Faerim_ArmorTypeComp : ThingComp
	{
		public Faerim_ArmorTypeProps Props
		{
			get
			{
				return (Faerim_ArmorTypeProps)this.props;  // Ensure it casts to Properties (CompProperties)
			}
		}
	}

	public class Faerim_ArmorTypeProps : CompProperties
	{
		public string ArmorType;
		public int ResistantTo;
		public int VulenrableTo;

		public Faerim_ArmorTypeProps()
		{
			this.compClass = typeof(Faerim_ArmorTypeComp);
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			foreach (StatDrawEntry s in base.SpecialDisplayStats(req))  // Call the base method to include other stats
			{
				yield return s;  // Yield any stats from the base method first
			}

			string armorTypeDescription = "This is simple clothing with no combat benefits.";  // Default description
			switch (ArmorType)
			{
				case "Heavy":
					armorTypeDescription = "This is heavy armor.";
					break;

				case "Medium":
					armorTypeDescription = "This is medium armor";
					break;

				case "Light":
					armorTypeDescription = "This is light armor";
					break;

				default:
					ArmorType = "Clothing";
					armorTypeDescription = "This is simple clothing with no combat benefits.";
					break;
			}

			// Add custom stat for apparel type
			yield return new StatDrawEntry(
				StatCategoryDefOf.Apparel,  // Category for the stat
				"Armor Type",
				ArmorType,
				armorTypeDescription,
				0,
				null,
				null
				);

			yield break;  // End of the method
		}

	}

}
