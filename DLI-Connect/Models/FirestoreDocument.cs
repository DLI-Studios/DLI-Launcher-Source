using System.Collections.Generic;

namespace DLI.Connect.Models;

public class FirestoreDocument
{
    public string Name { get; set; } = "";
    public Dictionary<string, FirestoreValue> Fields { get; set; } = new();
    public string CreateTime { get; set; } = "";
    public string UpdateTime { get; set; } = "";
}

public class FirestoreValue
{
    public string? StringValue { get; set; }
    public long? IntegerValue { get; set; }
    public bool? BooleanValue { get; set; }
    public double? DoubleValue { get; set; }
    public FirestoreValue? MapValue { get; set; }

    public object? RawValue =>
        StringValue ?? (object?)IntegerValue ?? (object?)BooleanValue ?? (object?)DoubleValue;
}
