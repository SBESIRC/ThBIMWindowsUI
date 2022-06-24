using System;
using System.Windows;
using System.Windows.Forms.Integration;
using THBimEngine.Presention;

namespace THBimXplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void formHost_Initialized(object sender, EventArgs e)
        {
            var glControl = new GLControl();
            (sender as WindowsFormsHost).Child = glControl;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var childConrol = formHost.Child as GLControl;
            childConrol.EnableNativeInput();
            childConrol.MakeCurrent();
            ExampleScene.Init(childConrol.Handle, childConrol.Width, childConrol.Height, ".\\temp3.midfile");
            ExampleScene.Render();
        }
    }
}
