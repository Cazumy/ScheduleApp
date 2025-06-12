namespace ScheduleApp.Models
{
    public class DbEntity (int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}
