# Upsertable
 SQL Server Entity Framework merge extensions.

```
await _context
  .Merge(Enumerable.Empty<Foo>())
  .On(foo => foo.Code)
  .Insert()
  .Update(foo => foo.Name)
  .MergeMany(foo => foo.Fubs, fubs =>
  {
    fubs
      .Insert()
      .Merge(fub => fub.Baz, bazs =>
      {
        bazs.On(baz => baz.Code)
          .Insert()
          .Merge(baz => baz.Qux, quxs =>
          {
            quxs.On(fum => fum.Code)
              .Insert()
              .MergeMany(qux => qux.Fums, fums =>
              {
                fums.On(fum => fum.Code)
                  .Insert();
              });
          });
      });
  })
  .MergeMany(foo => foo.Acks, acks =>
  {
    acks.On(bar => bar.Code)
      .Insert()
      .Update(ack => ack.Name)
      .Merge(ack => ack.Bar, bars =>
      {
        bars.On(bar => bar.Code)
          .Insert();
      });
  })
  .ExecuteAsync()
```
