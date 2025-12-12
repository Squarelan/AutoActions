using AutoActions.ProjectResources;
using AutoActions.UWP;
using CodectoryCore.UI.Wpf;
using System;
using System.Windows;

namespace AutoActions
{
    public abstract class ApplicationAdderBase : DialogViewModelBase
    {
        private bool _canCreate = false;

        private string _displayName = string.Empty;
        private string _filePath = string.Empty;
        private IApplicationItem applicationItem = null;
        private bool _editMode = false;

        public bool EditMode { get => _editMode; set { _editMode = value; OnPropertyChanged(); } }
        public IApplicationItem ApplicationItem { get => applicationItem; protected set { applicationItem = value; OnPropertyChanged(); } }

        public abstract RelayCommand GetApplicationCommand { get; protected set; }
        public RelayCommand GetUWPAppCommand { get; private set; }

        public RelayCommand<object>  OKClickCommand { get; private set; }

        public event EventHandler OKClicked;

        public ApplicationAdderBase()
        {
            EditMode = false;

            Title = ProjectLocales.Add;
            _CreateRelayCommands();
        }

        public ApplicationAdderBase(IApplicationItem application)
        {
            EditMode = true;
            Title = ProjectLocales.Edit;
            DisplayName = application.DisplayName;
            FilePath = application.ApplicationFilePath;
            ApplicationItem = application;

            _CreateRelayCommands();
        }

        private void _CreateRelayCommands()
        {
            GetUWPAppCommand = new RelayCommand(GetUWPAplication);
            OKClickCommand = new RelayCommand<object>(Close);
        }


        public string DisplayName { get => _displayName; set { _displayName = value; UpdateCanCreate(); OnPropertyChanged(); } }

        public string FilePath { get => _filePath; set { _filePath = value; UpdateCanCreate(); OnPropertyChanged(); } }

        private void UpdateCanCreate()
        {
            CanCreate = !String.IsNullOrEmpty(FilePath) && !String.IsNullOrEmpty(DisplayName);
        }

        public bool CanCreate { get => _canCreate; set { _canCreate = value; OnPropertyChanged(); } }



        public void Close(object parameter)
        {
           

            OKClicked?.Invoke(this, EventArgs.Empty);
            CloseDialog(parameter as Window);
        }

        private void GetUWPAplication()
        {
            UIServices.SetBusyState();
            UWPApplicationDialog uwpDialog = new UWPApplicationDialog();
            uwpDialog.OKClicked += (o, e) =>
            {
                if (uwpDialog.ApplicationItem != null)
                {
                    ApplicationItem = uwpDialog.ApplicationItem;
                    DisplayName = ApplicationItem.DisplayName;
                    FilePath = ApplicationItem.ApplicationFilePath;
                }
            };
            if (DialogService != null)
                DialogService.ShowDialogModal(uwpDialog, new System.Drawing.Size(800,600));
        }

    }
}
