using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using LudeonTK;

namespace Faerim_Core
{
	public class StatPart_FaerimTotalLevel : StatPart
	{
		// Ensure a parameterless constructor exists
		public StatPart_FaerimTotalLevel() { }

		public override void TransformValue(StatRequest req, ref float val)
		{
			if (!req.HasThing || !(req.Thing is Pawn pawn) || !pawn.RaceProps.Humanlike)
				return;

			// Get class component
			CompClassSystem compClass = pawn.TryGetComp<CompClassSystem>();
			if (compClass == null)
				return;

			// Calculate total level
			int totalLevel = compClass.GetAllClasses().Sum(c => compClass.GetClassLevel(c.defName));

			// Apply the calculated value
			val += totalLevel;

			// Debugging
		}

		public override string ExplanationPart(StatRequest req)
		{
			if (!req.HasThing || !(req.Thing is Pawn pawn))
				return null;

			// Get class component
			CompClassSystem compClass = pawn.TryGetComp<CompClassSystem>();
			if (compClass == null)
				return null;

			// Show breakdown of all class levels
			string breakdown = string.Join(", ", compClass.GetAllClasses()
				.Select(c => $"{c.label ?? c.defName} Lv{compClass.GetClassLevel(c.defName)}"));

			return $"Total Class Levels: {compClass.GetAllClasses().Sum(c => compClass.GetClassLevel(c.defName))}\n({breakdown})";
		}
	}

	/// <summary>
	/// Provides access to all class definitions using ThingDefs.
	/// </summary>
	public static class ClassDatabase
	{
		private static readonly ThingCategoryDef ClassCategory = DefDatabase<ThingCategoryDef>.GetNamed("Faerim_ClassDefs", false);

		/// <summary>
		/// Returns all classes that belong to the Faerim_ClassDefs category.
		/// </summary>
		public static List<ClassThingDef> AllClasses =>
			DefDatabase<ThingDef>.AllDefsListForReading
			.OfType<ClassThingDef>()
			.Where(def => def.thingCategories != null && def.thingCategories.Contains(ClassCategory))
			.ToList();

		/// <summary>
		/// Retrieves a class by defName.
		/// </summary>
		public static ClassThingDef GetClass(string className)
		{
			return AllClasses.FirstOrDefault(c => c.defName == className);
		}
	}

	/// <summary>
	/// Custom ThingDef that represents RPG classes.
	/// </summary>
	public class ClassThingDef : ThingDef
	{
		public string classLabel;
		public int hitDie;
		public List<string> requiredAttributes = new List<string>();
		public bool allPrerequisitesRequired = false;

		/// <summary>
		/// Features granted at specific levels.
		/// </summary>
		public List<LevelFeatureEntry> levelFeatures = new List<LevelFeatureEntry>();
	}

	/// <summary>
	/// Represents a feature granted at a specific level.
	/// </summary>
	public class LevelFeatureEntry
	{
		public int level;
		public string featureName; // Name of the feature
		public bool isActive; // If the feature is an active ability
		public List<StatModifier> statModifiers; // Optional stat boosts
		public List<string> abilities; // Special actions or skills
	}

	/// <summary>
	/// Component Properties for the Class System.
	/// </summary>
	public class CompProperties_ClassSystem : CompProperties
	{
		public CompProperties_ClassSystem()
		{
			this.compClass = typeof(CompClassSystem);
		}
	}

	public class CompClassSystem : ThingComp
	{
		private Dictionary<string, int> classLevels = new Dictionary<string, int>(); // Stores levels per class
		private int totalXP = 0;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref classLevels, "classLevels", LookMode.Value, LookMode.Value);
			Scribe_Values.Look(ref totalXP, "totalXP", 0);

			// Ensure the dictionary is always initialized and contains at least "Commoner"
			if (classLevels == null || classLevels.Count == 0)
			{
				classLevels = new Dictionary<string, int> { { "Commoner", 0 } };
			}
		}


		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			EnsureCommonerExists(); // Ensure Commoner is assigned if no other class exists
		}

		/// <summary>
		/// Ensures the pawn has at least the Commoner class if they have no other class.
		/// </summary>
		private void EnsureCommonerExists()
		{
			if (classLevels == null)
			{
				classLevels = new Dictionary<string, int>(); // Ensure dictionary is initialized
			}

			if (classLevels.Count == 0)
			{
				classLevels["Commoner"] = 0; // Default level 0 for Commoner
				Log.Warning($"[DEBUG] Assigned Commoner Lv0 to pawn {parent.LabelCap}");
			}
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Prefs.DevMode)
			{
				yield return new Command_Action
				{
					defaultLabel = "Debug: Add Class Level",
					defaultDesc = "Opens a debug menu to add levels to a class.",
					//icon = ContentFinder<Texture2D>.Get("UI/Buttons/Dev/AddLevel", true),
					action = OpenClassSelectionDialog
				};
			}
		}

		private void OpenClassSelectionDialog()
		{
			List<DebugMenuOption> options = new List<DebugMenuOption>();

			foreach (ClassThingDef classDef in ClassDatabase.AllClasses)
			{
				string classLabel = $"{classDef.label ?? classDef.defName} (Lv{GetClassLevel(classDef.defName)})";

				options.Add(new DebugMenuOption(classLabel, DebugMenuOptionMode.Action, () =>
				{
					GrantClassLevel(classDef.defName);
					Log.Message($"[DEBUG] Added level to {classDef.label} for {parent.LabelCap}");
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}

		public void GrantClassLevel(string className, int amount = 1)
		{
			EnsureCommonerExists();

			ClassThingDef classDef = ClassDatabase.GetClass(className);
			if (classDef == null)
			{
				Log.Warning($"[WARNING] Attempted to grant unknown class: {className}");
				return;
			}

			int newLevel = GetClassLevel(className) + amount;
			classLevels[className] = newLevel;

			Log.Message($"[DEBUG] {parent.LabelCap} is now {classDef.label} Lv{newLevel}");

			// Grant features for this level
			GrantClassFeatures(classDef, newLevel);

			if (parent is Pawn pawn)
			{
				// Apply HP increase
				var compHP = pawn.TryGetComp<CompFaerimHP>();
				if (compHP != null)
				{
					// Store hit die roll for the level-up
					int rolledHP = FaerimTools.RollDice(1, classDef.hitDie);
					if (!compHP.storedHitDice.ContainsKey(className))
					{
						compHP.storedHitDice[className] = new List<int>();
					}
					compHP.storedHitDice[className].Add(rolledHP);

					// **Log HP Gain (Let FaerimHealthUtility handle updating max HP naturally)**
					Log.Message($"[DEBUG] {pawn.LabelCap} gained {rolledHP} HP from {classDef.label} LvUp.");
				}
			}
		}



		private void GrantClassFeatures(ClassThingDef classDef, int level)
		{
			if (classDef.levelFeatures == null)
				return;

			foreach (var entry in classDef.levelFeatures)
			{
				if (entry.level == level)
				{
					Log.Message($"[DEBUG] {parent.LabelCap} gained feature: {entry.featureName}");
					ApplyFeatureEffects(entry);
				}
			}
		}

		/// <summary>
		/// Applies the effects of a granted feature.
		/// </summary>
		private void ApplyFeatureEffects(LevelFeatureEntry feature)
		{
			if (feature == null)
				return;

			// Ensure the parent is a pawn
			if (!(parent is Pawn pawn))
				return;

			// Apply stat modifications (Handled by StatPart)
			if (feature.statModifiers != null)
			{
				Log.Message($"[DEBUG] {pawn.LabelCap} received feature: {feature.featureName}");

				foreach (var modifier in feature.statModifiers)
				{
					Log.Message($"[DEBUG] {pawn.LabelCap} will receive {modifier.value} to {modifier.stat.defName} via StatPart.");
				}

				// The stat changes will be automatically applied by StatPart_FaerimFeatureBonus
			}

			// Add abilities for active features
			if (feature.isActive && feature.abilities != null)
			{
				Log.Message($"[DEBUG] {pawn.LabelCap} unlocked active ability: {string.Join(", ", feature.abilities)}");
				// Ability integration can be expanded later.
			}
		}

		public float GetFeatureStatBonus(StatDef stat)
		{
			float totalBonus = 0f;

			foreach (var classDef in GetAllClasses())
			{
				if (classDef.levelFeatures == null)
					continue;

				foreach (var feature in classDef.levelFeatures)
				{
					if (feature.statModifiers == null)
						continue;

					foreach (var modifier in feature.statModifiers)
					{
						if (modifier.stat == stat)
						{
							totalBonus += modifier.value;
						}
					}
				}
			}

			return totalBonus;
		}





		///////////////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Adds XP to the pawn.
		/// </summary>
		public void AddXP(int amount)
		{
			totalXP += amount;
			Log.Message($"[DEBUG] {parent.LabelCap} gained {amount} XP. Total XP: {totalXP}");
		}

		public int GetTotalXP()
		{
			return (int)totalXP;
		}

		public int GetRequiredXP()
		{
			int totalLevels = GetTotalLevels();
			return Fibonacci(totalLevels + 1) * 100;
		}

		/// <summary>
		/// Gets the total level of all classes combined.
		/// </summary>
		public int GetTotalLevels()
		{
			int totalLevels = classLevels.Values.Sum();
			return totalLevels;
		}

		/// <summary>
		/// Removes a class and restores Commoner if needed.
		/// </summary>
		public void RemoveClass(string className)
		{
			classLevels.Remove(className);
			EnsureCommonerExists();
		}

		/// <summary>
		/// Retrieves all classes assigned to this pawn.
		/// </summary>
		public List<ClassThingDef> GetAllClasses()
		{
			EnsureCommonerExists();
			return classLevels.Keys
				.Select(className => ClassDatabase.GetClass(className))
				.Where(classDef => classDef != null)
				.ToList();
		}

		public bool CanTakeClass(ClassThingDef classDef)
		{
			Pawn pawn = this.parent as Pawn;
			if (pawn == null) return false;

			// If no prerequisites, class is always available
			if (classDef.requiredAttributes == null || classDef.requiredAttributes.Count == 0)
				return true;

			// Retrieve the pawn's stats
			CompPawnStats comp = pawn.TryGetComp<CompPawnStats>();
			if (comp == null)
			{
				Log.Warning($"[DEBUG] {pawn.LabelCap} is missing CompPawnStats!");
				return false;
			}

			// Check if prerequisites are met
			bool meetsRequirement = false;
			if (classDef.allPrerequisitesRequired)
			{
				// Must meet ALL prerequisites
				meetsRequirement = classDef.requiredAttributes.All(attr => comp.GetBaseStat($"Faerim_{attr}") >= 13);
			}
			else
			{
				// Must meet AT LEAST ONE prerequisite
				meetsRequirement = classDef.requiredAttributes.Any(attr => comp.GetBaseStat($"Faerim_{attr}") >= 13);
			}

			Log.Message($"[DEBUG] {pawn.LabelCap} meets prerequisites for {classDef.classLabel}: {meetsRequirement}");
			return meetsRequirement;
		}

		/// <summary>
		/// Gets the level of a specific class.
		/// </summary>
		public int GetClassLevel(string className)
		{
			return classLevels.ContainsKey(className) ? classLevels[className] : 0;
		}

		/// <summary>
		/// Calculates the Fibonacci sequence value for a given level.
		/// </summary>
		private int Fibonacci(int n)
		{
			if (n <= 0) return 0;
			if (n == 1) return 1;
			int a = 0, b = 1, temp;

			for (int i = 2; i <= n; i++)
			{
				temp = a + b;
				a = b;
				b = temp;
			}

			return b;
		}
	}
}