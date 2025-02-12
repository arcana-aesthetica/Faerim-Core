using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace Faerim_Core
{
	// Patch for Explosion.AffectCell
	[HarmonyPatch(typeof(Explosion), "AffectCell")]
	public static class Patch_Explosion_AffectCell
	{
		[HarmonyPrefix]
		public static void Prefix(IntVec3 c, Explosion __instance)
		{
			if (__instance.instigator == null)
			{
				Log.Warning($"Explosion at {c} has no instigator.");
			}
			else
			{
				Log.Message($"Explosion caused by {__instance.instigator.Label} is affecting cell {c}.");
			}

			// Log damage amount for the cell
			int damageAmount = __instance.GetDamageAmountAt(c);
			Log.Message($"Explosion at {c} has damage amount: {damageAmount}.");
		}
	}

}
