﻿using System.Collections.Generic;

namespace Marvolo.EntityFramework.SqlMerge.Tests.Entities
{
    public class Qux
    {
        public Baz Baz { get; set; }

        public int BazId { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public ICollection<Fum> Fums { get; } = new List<Fum>();
    }
}