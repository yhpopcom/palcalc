﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalCalc.UI.Model;
using PalCalc.UI.View;
using PalCalc.UI.ViewModel.Inspector.Search;
using PalCalc.UI.ViewModel.Inspector.Search.Container;
using PalCalc.UI.ViewModel.Inspector.Search.Grid;
using PalCalc.UI.ViewModel.Mapped;
using QuickGraph.Graphviz;
using QuickGraph.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PalCalc.UI.ViewModel.Inspector
{
    public partial class SearchViewModel : ObservableObject
    {
        private static SearchViewModel designerInstance = null;
        public static SearchViewModel DesignerInstance => designerInstance ??= new SearchViewModel(SaveGameViewModel.DesignerInstance);

        [ObservableProperty]
        private OwnerTreeViewModel ownerTree;

        public SearchSettingsViewModel SearchSettings { get; }

        private ICommand newCustomContainerCommand;
        public IRelayCommand<ISearchableContainerViewModel> DeleteContainerCommand { get; }

        public IRelayCommand<ISearchableContainerViewModel> RenameContainerCommand { get; }

        private static bool IsValidCustomLabel(SaveGameViewModel context, string label) =>
            label.Length > 0 && !context.Customizations.CustomContainers.Any(c => c.Label == label);

        public SearchViewModel(SaveGameViewModel sgvm)
        {
            RenameContainerCommand = new RelayCommand<ISearchableContainerViewModel>(
                container =>
                {
                    var customContainer = container as CustomSearchableContainerViewModel;
                    if (customContainer == null) return;

                    var nameModal = new SimpleTextInputWindow()
                    {
                        // TODO - Itl
                        Title = "Rename Custom Container",
                        InputLabel = "Name",
                        Validator = label => IsValidCustomLabel(sgvm, label),
                        Result = customContainer.Label,
                    };
                    nameModal.Owner = App.ActiveWindow;
                    if (nameModal.ShowDialog() == true)
                    {
                        customContainer.Value.Label = nameModal.Result;
                    }
                }
            );

            DeleteContainerCommand = new RelayCommand<ISearchableContainerViewModel>(
                container =>
                {
                    var customContainer = container as CustomSearchableContainerViewModel;
                    if (customContainer == null) return;

                    sgvm.Customizations.CustomContainers.Remove(customContainer.Value);
                }
            );

            newCustomContainerCommand = new RelayCommand(() =>
            {
                var nameModal = new SimpleTextInputWindow()
                {
                    // TODO - Itl
                    Title = "New Custom Container",
                    InputLabel = "Name",
                    Validator = label => IsValidCustomLabel(sgvm, label),
                };
                nameModal.Owner = App.ActiveWindow;
                if (nameModal.ShowDialog() == true)
                {
                    var container = new CustomContainer() { Label = nameModal.Result };
                    sgvm.Customizations.CustomContainers.Add(new CustomContainerViewModel(container));
                }
            });

            sgvm.Customizations.CustomContainers.CollectionChanged += (_, _) => BuildContainerTree(sgvm);

            BuildContainerTree(sgvm);
            SearchSettings = new SearchSettingsViewModel();

            SearchSettings.PropertyChanged += SearchSettings_PropertyChanged;
        }

        private void BuildContainerTree(SaveGameViewModel sgvm)
        {
            if (OwnerTree != null)
            {
                OwnerTree.PropertyChanging -= OwnerTree_PropertyChanging;
                OwnerTree.PropertyChanged -= OwnerTree_PropertyChanged;
            }

            var csg = sgvm.CachedValue;
            var palsByContainerId = csg.OwnedPals.GroupBy(p => p.Location.ContainerId).ToDictionary(g => g.Key, g => g.ToList());

            var containers = palsByContainerId
                .Select(kvp => (ISearchableContainerViewModel)new DefaultSearchableContainerViewModel(kvp.Key, kvp.Value.First().Location.Type, kvp.Value))
                .Concat(
                    sgvm.Customizations.CustomContainers.Select(c =>
                        new CustomSearchableContainerViewModel(c)
                        {
                            RenameCommand = RenameContainerCommand,
                            DeleteCommand = DeleteContainerCommand,
                        }
                    )
                );

            OwnerTree = new OwnerTreeViewModel(csg, containers.ToList())
            {
                CreateCustomContainerCommand = newCustomContainerCommand
            };

            OwnerTree.PropertyChanging += OwnerTree_PropertyChanging;
            OwnerTree.PropertyChanged += OwnerTree_PropertyChanged;
        }

        private void ApplySearchSettings()
        {
            foreach (var source in OwnerTree.AllContainerSources)
                source.SearchCriteria = SearchSettings.AsCriteria;
        }

        private void OwnerTree_PropertyChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(OwnerTree.SelectedSource))
            {
                if (OwnerTree.SelectedSource != null)
                    OwnerTree.SelectedSource.Container.PropertyChanged -= SelectedContainer_PropertyChanged;
            }
        }

        private void OwnerTree_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OwnerTree.SelectedSource))
            {
                OnPropertyChanged(nameof(SlotDetailsVisibility));
                OnPropertyChanged(nameof(FocusedGrid));

                if (OwnerTree.SelectedSource != null)
                {
                    OwnerTree.SelectedSource.Container.PropertyChanged += SelectedContainer_PropertyChanged;
                }
            }
        }

        private void SearchSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchSettings.AsCriteria))
            {
                ApplySearchSettings();
            }
        }

        public Visibility SlotDetailsVisibility => OwnerTree.HasValidSource && OwnerTree.SelectedSource.Container.SelectedSlot != null ? Visibility.Visible : Visibility.Collapsed;

        private void SelectedContainer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DefaultSearchableContainerViewModel.SelectedSlot))
            {
                OnPropertyChanged(nameof(SlotDetailsVisibility));
                OnPropertyChanged(nameof(FocusedGrid));
            }
        }

        public IContainerGridViewModel FocusedGrid
        {
            get
            {
                var res = OwnerTree.SelectedSource?.Container?.Grids?.FirstOrDefault(g => g.SelectedSlot != null);
                return res;
            }
        }
    }
}
