namespace DLI.Connect.Firebase;

public class FieldTransform
{
    public string FieldPath { get; init; } = "";
    public long? Increment { get; init; }
    public string? SetToServerValue { get; init; }
}

public class CommitWrite
{
    public string Name { get; init; } = "";
    public Dictionary<string, object>? Fields { get; init; }
    public List<string>? FieldPaths { get; init; }
    public List<FieldTransform>? Transforms { get; init; }
}