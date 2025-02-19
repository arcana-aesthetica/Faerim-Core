using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using UnityEngine;

namespace Faerim_Core
{
	[StaticConstructorOnStartup]
	public static class Patch_AddFaerimStatsTab
	{
		static Patch_AddFaerimStatsTab()
		{
			foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d => d.race != null)) // Apply to ALL pawns, not just humanlike
			{
				if (def.inspectorTabs == null)
					def.inspectorTabs = new List<System.Type>();

				// Ensure we don't add the tab multiple times
				if (!def.inspectorTabs.Contains(typeof(ITab_Pawn_FaerimStats)))
				{
					def.inspectorTabs.Add(typeof(ITab_Pawn_FaerimStats));
				}

				// Force RimWorld to acknowledge the new tab
				def.inspectorTabsResolved = def.inspectorTabs
					.Select(InspectTabManager.GetSharedInstance)
					.ToList();
			}
		}
	}

	public class ITab_Pawn_FaerimStats : ITab
	{
		private const float TabWidth = 500f;
		private const float TabHeight = 500f;
		private Vector2 scrollPosition = Vector2.zero;

		public ITab_Pawn_FaerimStats()
		{
			this.size = new Vector2(TabWidth, TabHeight);
			this.labelKey = "FaerimCore.PawnStatsTab";
			this.tutorTag = "FaerimStatsTab";
		}

		protected override void FillTab()
		{
			Rect rect = new Rect(10f, 10f, size.x - 20f, size.y - 20f);
			Widgets.BeginScrollView(rect, ref scrollPosition, rect);

			Pawn pawn = SelPawn;
			if (pawn == null || pawn.Dead || pawn.Destroyed)
			{
				Widgets.Label(new Rect(10f, 10f, 300f, 30f), "This pawn is dead.");
				Widgets.EndScrollView();
				return;
			}

			float curY = 10f;
			float statBoxSize = 60f;
			float statBoxWidth = 90f;
			float modBoxWidth = statBoxWidth * 0.7f;
			float modBoxHeight = 25f;
			float rightColumnOffset = statBoxWidth * 2 + 30f;

			// Get pawn components safely
			CompPawnStats comp = pawn.TryGetComp<CompPawnStats>();
			CompClassSystem classComp = pawn.TryGetComp<CompClassSystem>();
			CompFaerimHP faerimHPComp = pawn.TryGetComp<CompFaerimHP>();

			// Initialize class and XP data
			string classDisplay = "None";
			int totalXP = 0;
			int requiredXP = 100;
			int totalLevel = 0;

			if (classComp != null)
			{
				var pawnClasses = classComp.GetAllClasses();
				if (pawnClasses.Count > 0)
				{
					classDisplay = string.Join(" | ", pawnClasses.Select(c => $"{c.label ?? c.defName} Lv{classComp.GetClassLevel(c.defName)}"));
				}

				totalXP = classComp.GetTotalXP();
				requiredXP = classComp.GetRequiredXP();
				totalLevel = classComp.GetTotalLevels();
			}

			// Get Faerim HP values
			float faeHP = faerimHPComp?.GetFaeHP() ?? 0f;
			float faeMaxHP = faerimHPComp?.GetFaeMaxHP() ?? 0f;

			// Draw Dead Message
			if (pawn.Dead)
			{
				Widgets.Label(new Rect(10f, 10f, 300f, 30f), "This pawn is dead.");
				Widgets.EndScrollView();
				return;
			}

			// Draw UI elements for alive pawns
			float leftColumnY = 10f;
			Rect statHeaderRect = new Rect(10f, leftColumnY, statBoxWidth, 20f);
			Widgets.Label(statHeaderRect, "Attributes");

			Rect classHeaderRect = new Rect(rightColumnOffset, curY, statBoxWidth * 2, 20f);
			Widgets.Label(classHeaderRect, "Class & XP");

			Widgets.DrawLineHorizontal(10f, leftColumnY + 18f, statBoxWidth);
			Widgets.DrawLineHorizontal(rightColumnOffset, curY + 18f, statBoxWidth * 2);
			curY += 25f;

			// Draw Character Level
			Rect charLevelBox = new Rect(rightColumnOffset, curY, statBoxWidth * 2, 25f);
			Widgets.DrawHighlight(charLevelBox);
			Widgets.Label(charLevelBox, $"Character Level: {totalLevel}");
			curY += 30f;

			// Draw Class Names & Levels
			Rect classBox = new Rect(rightColumnOffset, curY, statBoxWidth * 2, 25f);
			Widgets.DrawHighlight(classBox);
			Widgets.Label(classBox, classDisplay);
			curY += 30f;

			// Draw XP Stats
			Rect totalXPBox = new Rect(rightColumnOffset, curY, statBoxWidth * 2, 25f);
			Widgets.DrawHighlight(totalXPBox);
			Widgets.Label(totalXPBox, $"XP: {totalXP}/{requiredXP}");
			curY += 40f;

			// Draw Faerim HP
			Rect faerimHPBox = new Rect(rightColumnOffset, curY, statBoxWidth * 2, 25f);
			Widgets.DrawHighlight(faerimHPBox);
			Widgets.Label(faerimHPBox, $"Faerim HP: {faeHP}/{faeMaxHP}");
			curY += 30f;

			// Reset left column to match fixed spacing
			curY = leftColumnY + 25f;

			// Draw Attributes Column
			foreach (var (label, baseStat, modStat) in new (string, StatDef, StatDef)[]
			{
				("Strength", DefDatabaseClass.Faerim_Strength, DefDatabaseClass.Faerim_StrengthMod),
				("Dexterity", DefDatabaseClass.Faerim_Dexterity, DefDatabaseClass.Faerim_DexterityMod),
				("Constitution", DefDatabaseClass.Faerim_Constitution, DefDatabaseClass.Faerim_ConstitutionMod),
				("Intelligence", DefDatabaseClass.Faerim_Intelligence, DefDatabaseClass.Faerim_IntelligenceMod),
				("Wisdom", DefDatabaseClass.Faerim_Wisdom, DefDatabaseClass.Faerim_WisdomMod),
				("Charisma", DefDatabaseClass.Faerim_Charisma, DefDatabaseClass.Faerim_CharismaMod)
			})
			{
				// Get values
				float totalValue = comp != null ? comp.GetTotalStat(baseStat.defName) : 0; // Use total value instead of base
				float modifier = pawn.GetStatValue(modStat);

				// Draw Attribute Box
				Rect statBox = new Rect(10f, curY, statBoxWidth, statBoxSize);
				Widgets.DrawHighlight(statBox);
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(new Rect(statBox.x, curY + 5f, statBox.width, 20f), label);

				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(new Rect(statBox.x, curY + 20f, statBox.width, 30f), totalValue.ToString("F0")); // Display total stat

				// Draw Modifier Box
				Rect modBox = new Rect(statBox.x + (statBox.width / 2) - (modBoxWidth / 2), curY + statBoxSize - (modBoxHeight / 2), modBoxWidth, modBoxHeight);
				Widgets.DrawBox(modBox);
				Widgets.DrawWindowBackground(modBox);
				Widgets.Label(modBox, modifier >= 0 ? $"+{modifier:F0}" : $"{modifier:F0}");

				// Tooltip
				TooltipHandler.TipRegion(statBox, $"Total Value: {totalValue}\nModifier: {modifier}");

				curY += statBoxSize + modBoxHeight - 5f;
			}


			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.EndScrollView();
		}
	}
}