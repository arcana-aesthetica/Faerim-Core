using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using UnityEngine;

namespace Faerim_Core
{
	public class StatPart_AddModToAC : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			if (req.HasThing && req.Thing is Pawn pawn)
			{
				// Fetch Dexterity Modifier (Make sure the name matches the StatDef in XML)
				float dexMod = pawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_DexterityMod"), true);
				val += GetAdjustedDexMod(pawn, dexMod);
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			if (req.HasThing && req.Thing is Pawn pawn)
			{
				float dexMod = pawn.GetStatValue(DefDatabase<StatDef>.GetNamed("Faerim_DexterityMod"), true);
				float adjustedDexMod = GetAdjustedDexMod(pawn, dexMod);

				if (adjustedDexMod == 0)
				{
					return "Dexterity Modifier is negated by Heavy Armor.";
				}
				if (adjustedDexMod < dexMod)
				{
					return $"Dexterity Modifier: {adjustedDexMod:F2} (Capped by Medium Armor)";
				}
				return $"Dexterity Modifier: {adjustedDexMod:F2}";
			}
			return null;
		}



		/// <summary>
		/// Determines how much Dexterity applies based on armor type.
		/// </summary>
		private float GetAdjustedDexMod(Pawn pawn, float dexMod)
		{
			string armorType = GetWornArmorType(pawn);

			if (armorType == "Heavy") return 0;             // Heavy Armor negates Dexterity
			if (armorType == "Medium") return Mathf.Min(dexMod, 2); // Medium Armor caps Dex Mod at +2
			return dexMod;                                  // Light Armor or No Armor allows full Dex Mod
		}

		/// <summary>
		/// Checks equipped armor and determines the highest priority armor type on the body.
		/// </summary>
		private string GetWornArmorType(Pawn pawn)
		{
			if (pawn.apparel == null || pawn.apparel.WornApparel == null) return "None"; // Prevent null reference error

			foreach (Apparel apparel in pawn.apparel.WornApparel)
			{
				if (apparel?.def?.apparel?.bodyPartGroups?.Contains(BodyPartGroupDefOf.Torso) == true) // Null check
				{
					Faerim_ArmorTypeComp armorComp = apparel.GetComp<Faerim_ArmorTypeComp>();
					if (armorComp != null)
					{

						if (armorComp.Props.ArmorType == "Heavy") return "Heavy";  // **If any Heavy armor is found, return immediately**
						if (armorComp.Props.ArmorType == "Medium") return "Medium";
					}
				}
			}

			return "Light"; // Default to Light if no Medium or Heavy armor is found
		}
	}
}