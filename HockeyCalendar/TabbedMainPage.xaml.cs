using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HockeyCalendar
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedMainPage : TabbedPage
    {
        public TabbedMainPage()
        {
            InitializeComponent();

            var teamsPage = new TeamsPage();
            teamsPage.Title = "Teams";
            Children.Add(teamsPage);

            var schedPage = new SchedulePage();
            schedPage.Title = "Schedule";
            Children.Add(schedPage);
        }
    }
}