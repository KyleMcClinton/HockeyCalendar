using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HockeyCalendar
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TeamsPage : ContentPage
    {
        public TeamsPage()
        {
            InitializeComponent();
            HockeyData.UpdateTeamNames();

            TeamsView.ItemsSource = HockeyData.AllTeams;
            TeamsView.SelectionMode = ListViewSelectionMode.None;
        }

        void OnTeamTapped(object sender, ItemTappedEventArgs args)
        {
            var SelectedTeamInfo = (HockeyData.TeamInfo)args.Item;
            SelectedTeamInfo.Selected = !SelectedTeamInfo.Selected;
            HockeyData.UpdateGameSchedule();

            Console.WriteLine("Tapped: {0}", SelectedTeamInfo.Name);
        }

        void OnTeamSelected(object sender, SelectedItemChangedEventArgs args)
        {
        }
    }
}