﻿using DeviceControl.Wpf.ViewModels;
using GenICam;
using GigeVision.Core.Interfaces;
using Prism.Mvvm;

namespace DeviceControl.Test.Wpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "DeviceControl Test";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public DeviceControlViewModel DeviceControl { get; set; }
        public ICamera Camera { get; }

        public MainWindowViewModel()
        {
            DeviceControl = new DeviceControlViewModel("192.168.10.89");
        }
    }
}