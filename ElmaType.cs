using ExtensionElma;

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

public class TimeInterval
{
    public int Days = 0;
    public byte Hours = 0;
    public byte Minutes = 0;

    /// <summary>
    /// Hours between 0 and 23 (23 included). Minutes should be between 0 and 59 (59 included).
    /// If hours or minutes has incorrect value then program will throw exception about it.
    /// </summary>
    public TimeInterval(int days = 0, byte hours = 0, byte minutes = 0)
    {
        if (hours >= 24) 
            throw new Exception($"Hours should be between 0 and 23 (23 included) but got hours: {hours}");
        if (minutes >= 60) 
            throw new Exception($"Minutes should be between 0 and 59 (59 included) but got minutes: {minutes}");

        Days = days;
        Hours = hours;
        Minutes = minutes;
    }
}

interface IMakersWebData
{
    void WebItem(string name, string? value);
    void ItemDateOnly(string name, DateOnly dateOnly);
    void ItemDateTime(string nameItem, DateTime dateTime);
    void ItemInteger(string nameItem, long value);
    void ItemDouble(string nameItem, double value);
    void ItemMoney(string nameItem, double value);
    void ItemMoneySetNull(string nameItem);
    void ItemTimeInterval(string nameItem, TimeInterval timeInterval);
    void ItemSetNull(string nameItem);
    void ItemBoolean(string nameItem, bool value);
    void ItemUrl(string nameItem, Uri url);
    void ItemLine(string nameItem, string value);
    void ItemText(string nameItem, string value);
    void ItemHtml(string nameItem, string value);
    WebItemObject ItemObject(string nameItem);
    WebItemObjects ItemObjects(string nameItem);
}
