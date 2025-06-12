using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ScheduleApp.Styles
{
    public static class CalendarHelper
    {
        public static readonly DependencyProperty HighlightDatesProperty = DependencyProperty.RegisterAttached(
            "HighlightDates",
            typeof(ObservableCollection<DateTime>),
            typeof(CalendarHelper),
            new PropertyMetadata(null, OnHighlightDatesChanged));

        public static ObservableCollection<DateTime> GetHighlightDates(DependencyObject obj)
            => (ObservableCollection<DateTime>)obj.GetValue(HighlightDatesProperty);
        public static void SetHighlightDates(DependencyObject obj, ObservableCollection<DateTime> value)
            => obj.SetValue(HighlightDatesProperty, value);
        private static void OnHighlightDatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Calendar calendar)
            {
                if (e.OldValue is ObservableCollection<DateTime> oldDates)
                    oldDates.CollectionChanged -= (_, _) => HighlightDates(calendar, oldDates);

                if (e.NewValue is ObservableCollection<DateTime> newDates)
                {
                    newDates.CollectionChanged += (_, _) => HighlightDates(calendar, newDates);
                    calendar.DisplayDateChanged += (_, _) => HighlightDates(calendar, newDates);
                    calendar.Loaded += (_, _) => HighlightDates(calendar, newDates);
                    HighlightDates(calendar, newDates);
                }
            }
        }


        private static void HighlightDates(Calendar calendar, ObservableCollection<DateTime> dates)
        {
            foreach (var child in GetChildrenRecursively(calendar))
            {
                if (child is CalendarDayButton button)
                {
                    button.ClearValue(Control.BackgroundProperty);
                }
            }
            foreach (var date in dates)
            {
                var button = FindCalendarDayButton(calendar, date);
                if (button != null)
                {
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#44944A"));
                }
            }
        }
        private static CalendarDayButton FindCalendarDayButton(Calendar calendar, DateTime date)
        {
            foreach (var child in GetChildrenRecursively(calendar))
            {
                if (child is CalendarDayButton button && button.DataContext is DateTime buttonDate && buttonDate.Date == date.Date)
                {
                    return button;
                }
            }
            return null;
        }

        private static IEnumerable<DependencyObject> GetChildrenRecursively(DependencyObject parent)
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;

                foreach (var grandChild in GetChildrenRecursively(child))
                {
                    yield return grandChild;
                }
            }
        }
    }
}