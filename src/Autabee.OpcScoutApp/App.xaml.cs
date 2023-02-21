
namespace Autabee.OpcScout
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new OpcScout.MainPage();
        }
    }
}