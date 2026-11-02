namespace SwiftDrop.Identity.Data.Entities;

public class Role
{
    private Role() { }   // required by EF Core

    public Role(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
}
