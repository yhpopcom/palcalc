﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel.Mapped;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.ViewModel.Inspector.Search
{
    public class GenderOption(PalGender value)
    {
        public PalGender Value => value;
        public ILocalizedText Label { get; } = value.Label();

        public static GenderOption AnyGender { get; } = new GenderOption(PalGender.WILDCARD);
        public static GenderOption Female { get; } = new GenderOption(PalGender.FEMALE);
        public static GenderOption Male { get; } = new GenderOption(PalGender.MALE);
    }

    public partial class SearchSettingsViewModel : ObservableObject
    {
        public SearchSettingsViewModel()
        {
            ResetCommand = new RelayCommand(
                execute: () =>
                {
                    SearchedPal = null;
                    SearchedGender = GenderOption.AnyGender;
                    SearchedPassive1 = null;
                    SearchedPassive2 = null;
                    SearchedPassive3 = null;
                    SearchedPassive4 = null;
                    MinIVHP = 0;
                    MinIVAttack = 0;
                    MinIVDefense = 0;
                }
            );
        }

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private PalViewModel searchedPal;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private GenderOption searchedGender = GenderOption.AnyGender;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private PassiveSkillViewModel searchedPassive1;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private PassiveSkillViewModel searchedPassive2;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private PassiveSkillViewModel searchedPassive3;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private PassiveSkillViewModel searchedPassive4;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private int minIVHP = 0;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private int minIVAttack = 0;

        [NotifyPropertyChangedFor(nameof(AsCriteria))]
        [ObservableProperty]
        private int minIVDefense = 0;

        public List<PalViewModel> PalOptions { get; } = PalDB.LoadEmbedded().Pals.Select(PalViewModel.Make).ToList();

        public List<GenderOption> GenderOptions { get; } = [
            GenderOption.AnyGender,
            GenderOption.Male,
            GenderOption.Female,
        ];

        public List<PassiveSkillViewModel> PassiveSkillOptions { get; } = PalDB.LoadEmbedded().PassiveSkills.Select(PassiveSkillViewModel.Make).ToList();

        public ISearchCriteria AsCriteria
        {
            get
            {
                var criteria = new List<ISearchCriteria>();
                if (SearchedPal != null) criteria.Add(new PalSearchCriteria(SearchedPal.ModelObject));
                if (SearchedGender.Value != PalGender.WILDCARD) criteria.Add(new GenderSearchCriteria(SearchedGender.Value));

                if (SearchedPassive1 != null) criteria.Add(new PassiveSkillSearchCriteria(SearchedPassive1.ModelObject));
                if (SearchedPassive2 != null) criteria.Add(new PassiveSkillSearchCriteria(SearchedPassive2.ModelObject));
                if (SearchedPassive3 != null) criteria.Add(new PassiveSkillSearchCriteria(SearchedPassive3.ModelObject));
                if (SearchedPassive4 != null) criteria.Add(new PassiveSkillSearchCriteria(SearchedPassive4.ModelObject));

                criteria.Add(new CustomSearchCriteria(p => p.IV_HP >= MinIVHP));
                criteria.Add(new CustomSearchCriteria(p => p.IV_Shot >= MinIVAttack));
                criteria.Add(new CustomSearchCriteria(p => p.IV_Defense >= MinIVDefense));

                return new AllOfSearchCriteria(criteria);
            }
        }

        public IRelayCommand ResetCommand { get; }
    }
}
