using Quorum.FieldVisor.Mobile.Enums;
using Quorum.FieldVisor.Mobile.Extensions;
using Quorum.FieldVisor.Mobile.ViewModels;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Syncfusion.SfCalendar.XForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace Quorum.FieldVisor.Mobile.Pages.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DatePickerPopup : PopupPage
    {
        private TaskCompletionSource<DateTime> taskCompletion;
        public DateTime CurrentDateTime;
        private bool isMonthView;

        public bool IsMonthView
        {
            get { return isMonthView; }
            set { isMonthView = value; OnPropertyChanged(); }
        }


        public DatePickerPopup(DateTime dt)
        {
            InitializeComponent();
            IsMonthView = true;
            //set start and selected dates for calendar
            //initialize values if not already set

            CurrentDateTime = dt;

            CalendarHeader.Text = new DateTimeFormatInfo().GetMonthName(CurrentDateTime.Month).ToString().Truncate(3).ToUpper()
                + " " + (CurrentDateTime).Year.ToString();

            BuildDateGrid();
            BuildMonthGrid();

        }

        private void BuildMonthGrid()
        {
            int month = 1;
            foreach (var item in MonthGrid.Children)
            {
                item.AutomationId = "CalendarMonth" + month.ToString();
                month++;
            }
        }

        public static async Task<DateTime> GetSelectedDateTime(object obj = null)
        {
            DateTime dt;
            if (obj == null)
            {
                dt = DateTime.Now;
            }
            else
            {
                var success = DateTime.TryParse(obj.ToString(), out dt);
                dt = success ? dt : DateTime.Now;
            }
            
            DatePickerPopup pop = new DatePickerPopup(dt);

            var result = await pop.GetSelectedDateTimeInternal(dt);

            return result;
        }

        private async Task<DateTime> GetSelectedDateTimeInternal(DateTime dt)
        {
            taskCompletion = new TaskCompletionSource<DateTime>();

            await PopupNavigation.Instance.PushAsync(this);

            return await taskCompletion.Task;
        }

        private void BuildDateGrid()
        {
            //remove all previous children when building/rebuilding date grid
            foreach (var item in DateGrid.Children.Reverse())
            {
                DateGrid.Children.Remove(item);
            }

            //the first day of the month serves as the reference point for generating all the other dates
            var firstDay = new DateTime(CurrentDateTime.Year, CurrentDateTime.Month, 1).DayOfWeek.GetHashCode();

            //populates the date grid
            var dateTracker = new DateTime(CurrentDateTime.Year, CurrentDateTime.Month, 1);
            int day = 1;
            int row = 0;
            int column = firstDay;
            while (dateTracker.Month.Equals(CurrentDateTime.Month))
            {
                DateGrid.Children.Add(new Label()
                {
                    Text = (day).ToString(),
                    HorizontalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 24,
                    AutomationId = "CalendarDate" + (day).ToString(),
                }, column, row);
                foreach (var item in DateGrid.Children)
                {
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += DateTapped;
                    item.GestureRecognizers.Add(tap);
                }

                day++;
                dateTracker = dateTracker.AddDays(1);

                if (column < 6)
                {
                    column++;
                }
                else
                {
                    column = 0;
                    row++;
                }
            }
            //set initial date highlight
            DateGrid.Children.Where(x => (x as Label).Text.Equals(CurrentDateTime.Day.ToString())).FirstOrDefault().BackgroundColor =
                (Color)Application.Current.Resources["QuorumBlue"];
        }

        private async void DateTapped(object sender, EventArgs e)
        {
            //remove previous highlight before setting new highlight
            foreach (var item in DateGrid.Children)
            {
                item.BackgroundColor = Color.White;
            }
            var label = sender as Label;

            if (label != null && !string.IsNullOrEmpty(label.Text))
            {
                label.BackgroundColor = (Color)Application.Current.Resources["QuorumBlue"];
                CurrentDateTime = new DateTime(CurrentDateTime.Year, CurrentDateTime.Month, Convert.ToInt32(label.Text));

                if (taskCompletion != null)
                {
                    taskCompletion.TrySetResult(CurrentDateTime);

                    taskCompletion = null;
                }

                await PopupNavigation.Instance.PopAsync();
            }
        }


        private void PopupPage_Disappearing(object sender, EventArgs e)
        {
            if (taskCompletion != null)
            {
                taskCompletion.TrySetResult(CurrentDateTime);

                taskCompletion = null;
            }
        }

        private async void BackgroundTapped(object sender, EventArgs e)
        {
            if (taskCompletion != null)
            {
                taskCompletion.TrySetResult(CurrentDateTime);

                taskCompletion = null;
            }

            await PopupNavigation.Instance.PopAsync();
        }

        private void MovePrevious(object sender, EventArgs e)
        {
            //move between months/years depending on which view is visible
            if(IsMonthView)
            {
                CurrentDateTime = CurrentDateTime.AddMonths(-1);
                CalendarHeader.Text = new DateTimeFormatInfo().GetMonthName(CurrentDateTime.Month).ToString().Truncate(3).ToUpper()
                    + " " + (CurrentDateTime).Year.ToString();
                BuildDateGrid();
            }
            else
            {
                CurrentDateTime = CurrentDateTime.AddYears(-1);
                CalendarHeader.Text = CurrentDateTime.Year.ToString();
            }
            
        }

        private void MoveNext(object sender, EventArgs e)
        {
            //move between months/years depending on which view is visible
            if (IsMonthView)
            {
                CurrentDateTime = CurrentDateTime.AddMonths(1);
                CalendarHeader.Text = new DateTimeFormatInfo().GetMonthName(CurrentDateTime.Month).ToString().Truncate(3).ToUpper()
                    + " " + (CurrentDateTime).Year.ToString();
                BuildDateGrid();
            }
            else
            {
                CurrentDateTime = CurrentDateTime.AddYears(1);
                CalendarHeader.Text = CurrentDateTime.Year.ToString();
            }
           
        }

        private void HeaderTapped(object sender, EventArgs e)
        {
            //toggle between month/year navigation views when header is tapped
            if (IsMonthView)
            {
                IsMonthView = false;
                CalendarHeader.Text = CurrentDateTime.Year.ToString();
            }
            else
            {
                BuildDateGrid();
                IsMonthView = true;
                CalendarHeader.Text = new DateTimeFormatInfo().GetMonthName(CurrentDateTime.Month).ToString().Truncate(3).ToUpper()
                + " " + (CurrentDateTime).Year.ToString(); 
            }
        }

        private void MonthTapped(object sender, EventArgs e)
        {
            //switch back to month view when a month is tapped in year view
            CurrentDateTime = new DateTime(CurrentDateTime.Year, Convert.ToInt32((sender as Button).StyleId), 1);
            CalendarHeader.Text = new DateTimeFormatInfo().GetMonthName(CurrentDateTime.Month).ToString().Truncate(3).ToUpper()
                + " " + (CurrentDateTime).Year.ToString();
            BuildDateGrid();
            IsMonthView = true;
        }

        private void CalendarSwipedLeft(object sender, SwipedEventArgs e)
        {
            MoveNext(sender, e);
        }

        private void CalendarSwipedRight(object sender, SwipedEventArgs e)
        {
            MovePrevious(sender, e);
        }
    }
}