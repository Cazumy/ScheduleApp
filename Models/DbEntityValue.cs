namespace ScheduleApp.Models
{
    public class DbEntityValue (int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}