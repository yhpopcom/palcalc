﻿using CommunityToolkit.Mvvm.ComponentModel;
using PalCalc.Model;
using PalCalc.Solver;
using PalCalc.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.ViewModel
{
    public partial class SolverControlsViewModel : ObservableObject
    {
        public SolverControlsViewModel()
        {
            MaxBreedingSteps = 6;
            MaxWildPals = 1;
            MaxIrrelevantTraits = 1;
        }

        private int maxBreedingSteps;
        public int MaxBreedingSteps
        {
            get => maxBreedingSteps;
            set => SetProperty(ref maxBreedingSteps, Math.Max(1, value));
        }

        private int maxWildPals;
        public int MaxWildPals
        {
            get => maxWildPals;
            set => SetProperty(ref maxWildPals, Math.Max(0, value));
        }

        private int maxIrrelevantTraits;
        public int MaxIrrelevantTraits
        {
            get => maxIrrelevantTraits;
            set => SetProperty(ref maxIrrelevantTraits, Math.Clamp(value, 0, GameConstants.MaxTotalTraits));
        }

        [NotifyPropertyChangedFor(nameof(CanCancelSolver))]
        [ObservableProperty]
        private bool canRunSolver = true;

        public bool CanCancelSolver => !CanRunSolver;

        public BreedingSolver ConfiguredSolver(GameSettings gameSettings, List<PalInstance> pals) => new BreedingSolver(
            gameSettings,
            PalDB.LoadEmbedded(),
            pals,
            MaxBreedingSteps,
            MaxWildPals,
            MaxIrrelevantTraits,
            TimeSpan.MaxValue
        );

        public SolverSettings AsModel => new SolverSettings()
        {
            MaxBreedingSteps = MaxBreedingSteps,
            MaxWildPals = MaxWildPals,
            MaxIrrelevantTraits = MaxIrrelevantTraits
        };

        public static SolverControlsViewModel FromModel(SolverSettings model) => new SolverControlsViewModel()
        {
            MaxBreedingSteps = model.MaxBreedingSteps,
            MaxWildPals = model.MaxWildPals,
            MaxIrrelevantTraits = model.MaxIrrelevantTraits
        };
    }
}
