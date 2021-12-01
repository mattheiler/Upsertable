namespace Upsertable.Tests.Entities
{
    public class Baz
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public Qux Qux { get; set; }
    }
}