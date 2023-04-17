
namespace ElmaType;

// * ////////////////////////////////////////////// Main Response with Entities
public class WebData
{
    public List<WebDataItem> Items { get; set; } = default!;
    public object Value { get; set; } = default!;
}
public class WebDataItem
{
    public WebData? Data { get; set; } = default!;
    public List<WebData> DataArray { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Value { get; set; } = default!;
}
// * ///////////////////////////////////////////// Main Response with Entities

public delegate Task RefreshTokenDelegate();

public class ResponseElma 
{
    public object InnerException { get; set; } = default!;
    public string Message { get; set; } = default!;
    public int StatusCode { get; set; }
}

/// <summary>
/// every Uid is unique and can't be repeated (it's fair for Processes and Entities)
/// </summary>
public class ObjectElma 
{
    public string Name { get; set; } = default!;
    /// <summary> уникальный идентификатор типа </summary>
    public string Uid { get; set; }  = default!;
    public string NameDesc { get; set; } = default!;
    public List<string> NamesFields { get; set; } = default!;
}
/// <summary>
/// Enum from Server Elma, every Uid is unique and can't be repeated
/// </summary>
public class EnumElma
{
    public string Name { get; set; } = default!;
    /// <summary> уникальный идентификатор типа </summary>
    public string Uid { get; set; }  = default!;
    public string NameDesc { get; set; } = default!;
    /// <summary> Can be NULL !!! </summary>
    public string[]? Values { get; set; }
}
public enum TypesObj 
{
    Process, 
    Entity
}

/// <summary> Business Processes which is available in Server Elma </summary>
public class ProcessElma
{
    public string Name { get; set; } = default!;
    public int Id { get; set; }
    public int? GroupId { get; set; } = null;
}

public class ResponseAuthorization
{
    public string AuthToken { get; set; } = default!;
    public string CurrentUserId { get; set; } = default!;
    public string Lang { get; set; } = default!;
    public string SessionToken { get; set; } = default!;
}