﻿using Microsoft.OneDrive.Sdk;
using System.ComponentModel;

namespace Showcase
{
    public class OneDriveItemModel : INotifyPropertyChanged
    {
        public OneDriveItemModel(Item item)
        {
            this.Item = item;
        }

        public string Id
        {
            get
            {
                return Item == null ? null : this.Item.Id;
            }
        }

        public Item Item { get; private set; }

        public string Name
        {
            get
            {
                return this.Item.Name;
            }
        }

        //INotifyPropertyChanged members
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
