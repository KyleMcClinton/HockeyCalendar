using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HockeyCalendar
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SchedulePage : ContentPage
    {
        public SchedulePage()
        {
            InitializeComponent();

            ScheduleView.ItemsSource = HockeyData.GamesSchedule;
            ScheduleView.SelectionMode = ListViewSelectionMode.None;
        }
    }
}