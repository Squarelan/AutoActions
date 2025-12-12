using CodectoryCore.UI.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;
using AutoActions.UWP;
using AutoActions.ProjectResources;

namespace AutoActions
{
    public class ExeApplicationAdder : ApplicationAdderBase
    {

        public override RelayCommand GetApplicationCommand { get; protected set; }

        public ExeApplicationAdder() : base()
        {
            CreateRelayCommands();

        }

        public ExeApplicationAdder(IApplicationItem application) : base(application)
        {

            CreateRelayCommands();
        }

        private void CreateRelayCommands()
        {
            GetApplicationCommand = new RelayCommand(GetApplication);
        }

        public void GetApplication()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".exe";
            fileDialog.Filter = "Executables (.exe)|*.exe";
            Nullable<bool> result = fileDialog.ShowDialog();
            string filePath = string.Empty;
            if (result == true)
                filePath = fileDialog.FileName;
            else
                return;
            if (!File.Exists(filePath))
                throw new Exception("Invalid file path.");
            FilePath = filePath;
            if (string.IsNullOrEmpty(DisplayName))
                DisplayName = new FileInfo(FilePath).Name.Replace(".exe", "");
            ApplicationItem = new ExeApplicationItem(DisplayName, FilePath);
            if (ApplicationItem != null)
                ApplicationItem.DisplayName = DisplayName;

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
