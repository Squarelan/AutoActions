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
    public class UWPApplicationAdder : ApplicationAdderBase
    {

        public override RelayCommand GetApplicationCommand { get; protected set; }

        public UWPApplicationAdder() : base()
        {
            CreateRelayCommands();

        }

        public UWPApplicationAdder(IApplicationItem application) : base(application)
        {

            CreateRelayCommands();
        }

        private void CreateRelayCommands()
        {
            GetApplicationCommand = new RelayCommand(GetApplication);
        }

        public void GetApplication()
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
                DialogService.ShowDialogModal(uwpDialog, new System.Drawing.Size(800, 600));

        }
    }
}
