namespace Upsertable.Tests.Entities;

public class Ack
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public Bar Bar { get; set; }

    public Foo Foo { get; set; }

    public int FooId { get; set; }
}