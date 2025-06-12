namespace ScheduleApp.Models
{
    public class Group(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}