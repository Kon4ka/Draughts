using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using RelayNS;

namespace ClientApp.VM
{
    public class VMBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertiesChanged(params string[] properties_names)
        {
            foreach (var p_name in properties_names)
            {
                OnPropertyChanged(p_name);
            }
        }
        protected void OnPropertyChanged(string property_name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }

    }
}