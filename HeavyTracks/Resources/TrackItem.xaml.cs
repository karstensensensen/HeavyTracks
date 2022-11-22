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
            DataContext = this;
        }

        public int TrackNumber
        {
            get { return (int)GetValue(TrackNumberProperty); }
            set { SetValue(TrackNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrackNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackNumberProperty =
            DependencyProperty.Register("TrackNumber", typeof(int), typeof(TrackItem), new PropertyMetadata(-1));


    }
}
