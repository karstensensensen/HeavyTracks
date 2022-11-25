using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeavyTracks.Resources
{
    /// <summary>
    /// Interaction logic for NumericInput.xaml
    /// </summary>
    public partial class NumericInput : UserControl
    {
        public NumericInput()
        {
            InitializeComponent();
        }

        public int Number
        {
            get { return (int)GetValue(NumberProperty); }
            set { SetValue(NumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NumberProperty =
            DependencyProperty.Register("Number", typeof(int), typeof(NumericInput), new PropertyMetadata(0));


    }
}
