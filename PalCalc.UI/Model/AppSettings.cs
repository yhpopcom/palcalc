﻿using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.Model
{
    public class SolverSettings
    {
        public int MaxBreedingSteps { get; set; } = 6;
        public int MaxWildPals { get; set; } = 1;
        public int MaxInputIrrelevantPassives { get; set; } = 3;
        public int MaxBredIrrelevantPassives { get; set; } = 1;
        public int MaxThreads { get; set; } = 0;

        public List<string> BannedBredPalInternalNames { get; set; } = [];
        public List<string> BannedWildPalInternalNames { get; set; } = [
            "HerculesBeetle_Ground", // warsect terra, not released yet
            "PlantSlime_Flower", // flower gumoss
        ];

        public List<Pal> BannedBredPals(PalDB db) => BannedBredPalInternalNames.Select(n => n.InternalToPal(db)).ToList();
        public List<Pal> BannedWildPals(PalDB db) => BannedWildPalInternalNames.Select(n => n.InternalToPal(db)).ToList();
    }

    internal class AppSettings
    {
        public List<string> ExtraSaveLocations { get; set; } = new List<string>();

        public SolverSettings SolverSettings { get; set; } = new SolverSettings();

        public string SelectedGameIdentifier { get; set; } = null;

        public TranslationLocale Locale { get; set; } = TranslationLocale.en;
    }
}
