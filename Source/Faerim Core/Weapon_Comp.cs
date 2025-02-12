using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Faerim_Core
{
	public class Faerim_WeaponPropsComp : ThingComp
	{
		public Faerim_WeaponProps Props => (Faerim_WeaponProps)props;

		// 🔥 Checks if weapon has a specific property
		public bool HasProperty(string propertyName)
		{
			return Props.weaponProperties.Contains(propertyName);
		}
	}

	public class Faerim_WeaponProps : CompProperties
	{
		// 🔥 Store weapon properties as a list of strings
		public List<string> weaponProperties = new List<string>();

		public Faerim_WeaponProps()
		{
			this.compClass = typeof(Faerim_WeaponPropsComp);
		}

		// 🔥 Display properties in the stats window
		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			foreach (StatDrawEntry s in base.SpecialDisplayStats(req))
			{
				yield return s;
			}

			// 🔥 Only display if weapon has properties
			if (req.HasThing && req.Thing.TryGetComp<Faerim_WeaponPropsComp>() is Faerim_WeaponPropsComp comp && comp.Props.weaponProperties.Count > 0)
			{
				string propertiesList = string.Join(", ", comp.Props.weaponProperties);

				yield return new StatDrawEntry(
					StatCategoryDefOf.Weapon,
					"Weapon Properties",
					propertiesList,
					"Special properties of this weapon.",
					100);
			}
		}
	}
}
