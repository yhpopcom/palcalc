﻿
using PalCalc.Model;
using PalCalc.SaveReader;
using PalCalc.Solver;
using PalCalc.Solver.ResultPruning;
using PalCalc.Solver.Tree;
using System.Diagnostics;

internal class Program
{
    static void Main(string[] args)
    {
        Logging.InitCommonFull();

        var sw = Stopwatch.StartNew();

        var db = PalDB.LoadEmbedded();
        Console.WriteLine("Loaded Pal DB");

        var saveGame = DirectSavesLocation.AllLocal.SelectMany(l => l.ValidSaveGames).MaxBy(g => g.LastModified);
        Console.WriteLine("Using {0}", saveGame);

        var savedInstances = saveGame.Level.ReadCharacterData(db, []).Pals;
        Console.WriteLine("Loaded save game");

        var solver = new BreedingSolver(
            gameSettings: new GameSettings(),
            db: db,
            pruningBuilder: PruningRulesBuilder.Default,
            ownedPals: savedInstances,
            maxBreedingSteps: 20,
            maxWildPals: 1,
            allowedWildPals: db.Pals.ToList(),
            bannedBredPals: new List<Pal>(),
            maxBredIrrelevantTraits: 0,
            maxInputIrrelevantTraits: 2,
            maxEffort: TimeSpan.FromDays(7),
            maxThreads: 0
        );

        var targetInstance = new PalSpecifier
        {
            Pal = "Ragnahawk".ToPal(db),
            RequiredTraits = new List<Trait> { "Swift".ToTrait(db), "Runner".ToTrait(db), "Nimble".ToTrait(db) },
        };

        var matches = solver.SolveFor(targetInstance, CancellationToken.None);

        Console.WriteLine("Took {0}", TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));

        Console.WriteLine("\n\nRESULTS:");
        foreach (var match in matches)
        {
            var tree = new BreedingTree(match);
            tree.Print();
            Console.WriteLine("Should take: {0}\n", match.BreedingEffort);
        }
    }
}