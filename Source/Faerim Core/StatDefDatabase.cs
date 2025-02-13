using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Faerim_Core
{
	[DefOf]
	public static class DefDatabaseClass
	{
		public static StatDef Faerim_PawnCurrentArmorClass;
		public static StatDef Faerim_ProficiencyBonus;
		public static StatDef Faerim_TotalLevel;
		public static StatDef Faerim_Strength;
		public static StatDef Faerim_Dexterity;
		public static StatDef Faerim_Constitution;
		public static StatDef Faerim_Wisdom;
		public static StatDef Faerim_Intelligence;
		public static StatDef Faerim_Charisma;
		public static StatDef Faerim_StrengthMod;
		public static StatDef Faerim_DexterityMod;
		public static StatDef Faerim_ConstitutionMod;
		public static StatDef Faerim_WisdomMod;
		public static StatDef Faerim_IntelligenceMod;
		public static StatDef Faerim_CharismaMod;

		static DefDatabaseClass()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(DefDatabaseClass));

			// Debug check to ensure all StatDefs exist
			foreach (var field in typeof(DefDatabaseClass).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if (field.GetValue(null) == null)
				{
					Log.Warning($"[Faerim] WARNING: StatDef '{field.Name}' is missing! Check XML definitions.");
				}
			}
		}

	}
}
