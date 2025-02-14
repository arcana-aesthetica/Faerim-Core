using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Verse;
using RimWorld;

namespace Faerim_Core
{
	internal class AttackHandlers
	{
		public static bool HasAdvantage(Pawn attacker, Pawn target, Thing weapon)
		{
			Log.Message($"[AttackHandlers] Checking advantage for {attacker.LabelShort} attacking {target.LabelShort}.");

			if (attacker.Position.DistanceTo(target.Position) < 2 && !target.Awake()) // Attacking a sleeping target
			{
				Log.Message($"[AttackHandlers] {attacker.LabelShort} has advantage: Target is asleep.");
				return true;
			}
			// Additional advantage checks can be added here

			Log.Message($"[AttackHandlers] {attacker.LabelShort} has NO advantage.");
			return false;
		}

		public static bool HasDisadvantage(Pawn attacker, Pawn target, Thing weapon)
		{
			Log.Message($"[AttackHandlers] Checking disadvantage for {attacker.LabelShort} attacking {target.LabelShort}.");

			if (weapon != null && weapon.def.IsRangedWeapon && attacker.Position.DistanceTo(target.Position) < 2) // Shooting in melee range
			{
				Log.Message($"[AttackHandlers] {attacker.LabelShort} has disadvantage: Using ranged weapon in melee.");
				return true;
			}
			// Additional disadvantage checks can be added here

			Log.Message($"[AttackHandlers] {attacker.LabelShort} has NO disadvantage.");
			return false;
		}

		public static int RollAttack(Pawn attacker, Pawn target, Thing weapon)
		{
			bool advantage = HasAdvantage(attacker, target, weapon);
			bool disadvantage = HasDisadvantage(attacker, target, weapon);

			Log.Message($"[AttackHandlers] Rolling attack for {attacker.LabelShort} against {target.LabelShort}. Advantage: {advantage}, Disadvantage: {disadvantage}");

			if (advantage && disadvantage)
			{
				// They cancel out, roll normally
				int roll = Rand.RangeInclusive(1, 20);
				Log.Message($"[AttackHandlers] Normal roll (Advantage & Disadvantage cancel out): {roll}");
				return roll;
			}
			else if (advantage)
			{
				// Roll twice and take the highest
				int roll1 = Rand.RangeInclusive(1, 20);
				int roll2 = Rand.RangeInclusive(1, 20);
				int result = Mathf.Max(roll1, roll2);
				Log.Message($"[AttackHandlers] Advantage roll: {roll1}, {roll2} (Highest: {result})");
				return result;
			}
			else if (disadvantage)
			{
				// Roll twice and take the lowest
				int roll1 = Rand.RangeInclusive(1, 20);
				int roll2 = Rand.RangeInclusive(1, 20);
				int result = Mathf.Min(roll1, roll2);
				Log.Message($"[AttackHandlers] Disadvantage roll: {roll1}, {roll2} (Lowest: {result})");
				return result;
			}
			else
			{
				// Normal roll
				int roll = Rand.RangeInclusive(1, 20);
				Log.Message($"[AttackHandlers] Normal roll: {roll}");
				return roll;
			}
		}
	}
}


