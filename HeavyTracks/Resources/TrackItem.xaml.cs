using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HeavyTracks.Resources
{
    public partial class TrackItem : UserControl
    {
        public TrackItem()
        {
            InitializeComponent();
        }

        public int TrackNumber
        {
            get { return (int)GetValue(TrackNumberProperty); }
            set { SetValue(TrackNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrackNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackNumberProperty =
            DependencyProperty.Register("TrackNumber", typeof(int), typeof(TrackItem), new PropertyMetadata(-1));



        public string TrackName
        {
            get { return (string)GetValue(TrackNameProperty); }
            set { SetValue(TrackNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrackName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackNameProperty =
            DependencyProperty.Register("TrackName", typeof(string), typeof(TrackItem), new PropertyMetadata(""));




        public int TrackWeight
        {
            get { return (int)GetValue(TrackWeightProperty); }
            set { SetValue(TrackWeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrackWeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackWeightProperty =
            DependencyProperty.Register("TrackWeight", typeof(int), typeof(TrackItem), new PropertyMetadata(0));



        public string TrackAlbum
        {
            get { return (string)GetValue(TrackAlbumProperty); }
            set { SetValue(TrackAlbumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrackAlbum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackAlbumProperty =
            DependencyProperty.Register("TrackAlbum", typeof(string), typeof(TrackItem), new PropertyMetadata(""));



        public int TrackDuration
        {
            get { return (int)GetValue(TrackDurationProperty); }
            set { SetValue(TrackDurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrackDuration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackDurationProperty =
            DependencyProperty.Register("TrackDuration", typeof(int), typeof(TrackItem), new PropertyMetadata(0));


    }
}
