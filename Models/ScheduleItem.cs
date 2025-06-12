using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScheduleApp.Models
{
    public class ScheduleItem(string _time, string _subjectType, string _subject, string _room, string _address, string _teacher, int _id = 0) : INotifyPropertyChanged
    {
        private string time = _time;
        public string Time
        {
            get => time;
            set
            {
                if (time == value) return;
                time = value;
                OnPropertyChanged();
            }
        }
        private string subjectType = _subjectType;
        public string SubjectType
        {
            get => subjectType;
            set
            {
                if (subjectType == value) return;
                subjectType = value;
                OnPropertyChanged();
            }
        }

        private string subject = _subject;
        public string Subject
        {
            get => subject;
            set
            {
                if (subject == value) return;
                subject = value;
                OnPropertyChanged();
            }
        }

        private string room = _room;
        public string Room
        {
            get => room;
            set
            {
                if (room == value) return;
                room = value;
                OnPropertyChanged();
            }
        }

        private string address = _address;
        public string Address
        {
            get => address;
            set
            {
                if (address == value) return;
                address = value;
                OnPropertyChanged();
            }
        }

        private string teacher = _teacher;
        public string Teacher
        {
            get => teacher;
            set
            {
                if (teacher == value) return;
                teacher = value;
                OnPropertyChanged();
            }
        }

        public int Id { get; } = _id;
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}