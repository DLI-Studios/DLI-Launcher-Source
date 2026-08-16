using System.Collections.Generic;

namespace DLI.Connect.Models;

public class FirebaseRestError
{
    public FirebaseRestErrorBody? Error { get; set; }
}

public class FirebaseRestErrorBody
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
}
