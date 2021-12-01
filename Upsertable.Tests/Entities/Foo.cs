using System.Collections.Generic;

namespace Upsertable.Tests.Entities
{
    public class Foo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public Zot Zot { get; set; }

        public ICollection<Fub> Fubs { get; } = new List<Fub>();

        public ICollection<Ack> Acks { get; } = new List<Ack>();
    }
}