using System.Web;
using System.Collections.Specialized;
using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Elmapi;
using System.ComponentModel.DataAnnotations;
using ElmaType;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExtensionElma;


public class QParamsBase
{
    public readonly Dictionary<string, string> Params = new Dictionary<string, string>();
    public QParamsBase() { }
    /// <summary> add new url parameters in storage </summary>
    public QParamsBase Add(string key, string value)
    {
        if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(value)) {
            throw new Exception($"Url parameters can't be null or empty string: key: \"{key}\", value: \"{value}\"");
        }
        Params.Add(key, value);
        return  this;
    }
    /// <summary> уникльный идентификатор типа </summary>
    protected QParamsBase TypeUid(string value) 
    {
        Params.Add("type", value);
        return  this;
    }
    /// <summary> create url parameter for Eql (elma query lanaguage) for difficult query to Elma</summary>
    protected QParamsBase Eql(string value)
    {
        this.Add("q", value);
        return  this;
    }
    /// <summary> specify how many objects need to get </summary>
    protected QParamsBase Limit(int value)
    {
        this.Add("limit", value.ToString());
        return  this;
    }
    /// <summary> specify the start (сдвиг) element </summary>
    protected QParamsBase Offset(int value)
    {
        this.Add("offset", value.ToString());
        return  this;
    }
    /// <summary> 
    /// необходимо передавать строку выборки свойств и вложенных объектов.
    /// * - универсальная подстановка для выбора всех свойств объекта на один уровень вложенности
    /// / - разделитель уровней вложенности свойств объекта
    /// , - объединяет результаты нескольких запросов
    /// Subject,Comments/* – для типа объекта Задача требуется выбрать свойство Тема и для всех объектов в свойстве Комментарии выбрать все их доступные свойства;
    /// Subject, Description, CreationAuthor/UserName, CreationAuthor/FullName - для типа объекта Задача
    /// требуется выбрать только свойства Тема, Описание и для свойства Автор (тип объекта Пользователь)
    /// выбрать свойства Логин и Полное имя;
    /// </summary>
    protected QParamsBase Select(string value)
    {
        this.Add("select", value);
        return  this;
    }

    /// <summary>
    /// Значения полей для фильтра сущности в формате: Property1:Значение1,Property2:Значение2
    /// Наименование свойства возможно задавать с точкой (.) для получения доступа к подсвойству: Property1.Property2:Значение1
    /// Для указания в значении свойства символа : (двоеточие), \ (обратный слэш) или , (запятая), его нужно экранировать черз \ (обратный слэш)
    /// </summary>
    protected QParamsBase Filter(string value)
    {
        this.Add("filter", value);
        return  this;
    }

    /// <summary> search by a certain id </summary>
    protected QParamsBase Id(long id) 
    {
        this.Add("id", id.ToString());
        return  this;
    }

    /// <summary> сортировка по указанному свойству объекта </summary>
    protected QParamsBase Sort(string value) 
    {
        this.Add("sort", value);
        return  this;
    }
}

public class PrepareHttpBase<T> : QParamsBase
{
    protected HttpClient _httpClient;
    protected NameValueCollection queryParamsUrl = HttpUtility.ParseQueryString(string.Empty);
    protected string pathUrl;
    protected HttpMethod httpMethod;
    protected RefreshTokenDelegate refToken;
    public PrepareHttpBase(HttpClient httpClient, string pathUrl, HttpMethod httpMethod, RefreshTokenDelegate refToken) : base()
    {
        this._httpClient = httpClient;
        this.pathUrl = pathUrl;
        this.httpMethod = httpMethod;
        this.refToken = refToken;
    }
    
    /// <summary>
    /// make http-request to server elma
    /// </summary>
    public async Task<T?> Execute()
    {
        // for update AuthToken and SessionToken if they'are not actual
        await refToken();

        if (this.Params.Count != 0)
        {
            foreach (var record in this.Params)
            {
                queryParamsUrl[record.Key] = record.Value;
            }
        }

        var request = new HttpRequestMessage(httpMethod,
            pathUrl + (queryParamsUrl.Count != 0 ? $"?{queryParamsUrl.ToString()}" : ""));

        var response = await _httpClient.SendAsync(request);

        // if response from server wan't equels 200 (successful result), then throw exception
        if ((int)response.StatusCode != 200)
            throw new Exception("Bad request, server's body response:> " 
                + await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<T>();
    }
}

public class PrepareHttpQuery<T> : PrepareHttpBase<T>
{
    public PrepareHttpQuery(HttpClient httpClient, string typeUid, string pathUrl, HttpMethod httpMethod, RefreshTokenDelegate refToken)
         : base(httpClient, pathUrl, httpMethod, refToken) { }

    public new PrepareHttpQuery<T> TypeUid(string value) 
    {
        base.TypeUid(value);
        return this;
    }
    /// <summary> create url parameter for Eql (elma query lanaguage) for difficult query to Elma</summary>
    public new PrepareHttpQuery<T> Eql(string value) 
    {
        base.Eql(value);
        return this;
    }
    /// <summary> 
    /// необходимо передавать строку выборки свойств и вложенных объектов.
    /// * - универсальная подстановка для выбора всех свойств объекта на один уровень вложенности
    /// / - разделитель уровней вложенности свойств объекта
    /// , - объединяет результаты нескольких запросов
    /// Subject,Comments/* – для типа объекта Задача требуется выбрать свойство Тема и для всех объектов в свойстве Комментарии выбрать все их доступные свойства;
    /// Subject, Description, CreationAuthor/UserName, CreationAuthor/FullName - для типа объекта Задача
    /// требуется выбрать только свойства Тема, Описание и для свойства Автор (тип объекта Пользователь)
    /// выбрать свойства Логин и Полное имя;
    /// </summary>
    public new PrepareHttpQuery<T> Select(string value) 
    {
        base.Select(value);
        return this;
    }
    /// <summary> specify how many objects need to get </summary>
    public new PrepareHttpQuery<T> Limit(int value) 
    {
        base.Limit(value);
        return this;
    }
    /// <summary> specify the start (сдвиг) element </summary>
    public new PrepareHttpQuery<T> Offset(int value) 
    {
        base.Offset(value);
        return this;
    }
    public new PrepareHttpQuery<T> Sort(string value) 
    {
        base.Sort(value);
        return this;
    }
    /// <summary>
    /// Значения полей для фильтра сущности в формате: Property1:Значение1,Property2:Значение2
    /// Наименование свойства возможно задавать с точкой (.) для получения доступа к подсвойству: Property1.Property2:Значение1
    /// Для указания в значении свойства символа : (двоеточие), \ (обратный слэш) или , (запятая), его нужно экранировать черз \ (обратный слэш)
    /// </summary>
    public new PrepareHttpQuery<T> Filter(string value) 
    {
        base.Filter(value);
        return this;
    }
}
public class PrepareHttpLoad<T> : PrepareHttpBase<T>
{
    public PrepareHttpLoad(HttpClient httpClient, string pathUrl, HttpMethod httpMethod, long id, RefreshTokenDelegate refToken)
        : base(httpClient, pathUrl, httpMethod, refToken) 
    {
        this.Id(id);
    }
    public new PrepareHttpLoad<T> TypeUid(string value)
    {
        base.TypeUid(value);
        return this;
    }

    /// <summary> 
    /// необходимо передавать строку выборки свойств и вложенных объектов.
    /// * - универсальная подстановка для выбора всех свойств объекта на один уровень вложенности
    /// / - разделитель уровней вложенности свойств объекта
    /// , - объединяет результаты нескольких запросов
    /// Subject,Comments/* – для типа объекта Задача требуется выбрать свойство Тема и для всех объектов в свойстве Комментарии выбрать все их доступные свойства;
    /// Subject, Description, CreationAuthor/UserName, CreationAuthor/FullName - для типа объекта Задача
    /// требуется выбрать только свойства Тема, Описание и для свойства Автор (тип объекта Пользователь)
    /// выбрать свойства Логин и Полное имя;
    /// </summary>
    public new PrepareHttpLoad<T> Select(string value) 
    {
        base.Select(value);
        return this;
    }
}

public class MakeWebData : IMakersWebData
{
    public WebData webData = new WebData();
    private List<ObjectElma> _availableElmaObjects;
    private ElmaClient _elmaClient;
    private List<EnumElma> _enums;

    public MakeWebData(List<ObjectElma> availableElmaObjects, ElmaClient elmaClient, List<EnumElma> enums)
    {
        _availableElmaObjects = availableElmaObjects;
        _elmaClient = elmaClient;
        _enums = enums;
    }

    public void WebItem(string name, string? value)
    {
        CreatePayloadHttpElma.WebItem(name, value, ref webData);
    }
    public void ItemDateOnly(string name, DateOnly dateOnly)
    {
        var parseDate = $"{dateOnly.Year}-{dateOnly.Month}-{dateOnly.Day}";
        WebItem(name, parseDate);
    }
    public void ItemDateTime(string nameItem, DateTime dateTime)
    {
        var parseDateTime = $"{dateTime.Month}/{dateTime.Day}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}:00";

        WebItem(nameItem, parseDateTime);
    }
    public void ItemInteger(string nameItem, long value) => WebItem(nameItem, value.ToString());
    public void ItemDouble(string nameItem, double value) => 
        WebItem(nameItem, Math.Round(value, 2).ToString().Replace(",", "."));
    public void ItemMoney(string nameItem, double value) => 
        WebItem(nameItem, Math.Round(value, 2).ToString().Replace(",", "."));
    public void ItemMoneySetNull(string nameItem) => WebItem(nameItem, "");
    public void ItemTimeInterval(string nameItem, TimeInterval timeInterval) =>
        WebItem(nameItem, $"{timeInterval.Days}.{timeInterval.Hours}:{timeInterval.Minutes}:00");
    public void ItemSetNull(string nameItem) => WebItem(nameItem, null);
    public void ItemBoolean(string nameItem, bool value) => WebItem(nameItem, value.ToString());
    public void ItemUrl(string nameItem, Uri url) => WebItem(nameItem, url.ToString());
    public void ItemLine(string nameItem, string value)
    {
        int searchLines = Regex.Matches(value, "\n").Count + 1;

        if (searchLines != 1)
            throw new Exception("For WebItem with type Line there should be only one Line of text. Value: " + value);

        WebItem(nameItem, value);
    }
    public void ItemText(string nameItem, string value) => WebItem(nameItem, value);
    public void ItemHtml(string nameItem, string value) => WebItem(nameItem, value);
    public WebItemEnum ItemEnum(string nameItem)
    {
        return new WebItemEnum(nameItem, ref webData, _enums);
    }
    public WebItemObject ItemObject(string nameItem) 
    {
        return new WebItemObject(nameItem, ref webData, _availableElmaObjects, _elmaClient);
    }

    public WebItemObjects ItemObjects(string nameItem)
    {
        return new WebItemObjects(
            nameItem,
            ref webData,
            _availableElmaObjects,
            _elmaClient,
            null,
            null);
    }
}

public class WebDataUpdateInsert : IMakersWebData
{
    public WebData webData = new WebData();
    public ObjectElma TypeObject;
    protected List<ObjectElma> AvailableElmaObjects;
    protected ElmaClient _elmaClient;
    protected long? CurrentIdObject;
    protected List<EnumElma> _enums;
    public WebDataUpdateInsert(
        ObjectElma objectElma,
        List<ObjectElma> availableElmaObjects,
        ElmaClient elmaClient,
        long? currentIdObject,
        List<EnumElma> enums)
    {
        TypeObject = objectElma;
        AvailableElmaObjects = availableElmaObjects;
        _elmaClient = elmaClient;
        CurrentIdObject = currentIdObject;
        _enums = enums;
    }

    /// <summary>
    /// Создать новый WebItem с названием name и значением value, Если такой WebItem уже есть 
    /// тогда заменит значение в данном WebItem с названием name. Перед созданием происходит проверка
    /// названия WebItem name, есть ли похожее поле в Объекте Elma, если нет тогда выбросит ошибку
    /// </summary>
    public void WebItem(string name, string? value)
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.TypeObject.NamesFields.Contains(name))
        {
            throw new Exception(
                $"Elma Object \"{TypeObject.Name}\", don't have field \"{name}\". "
                + $"Available fileds: {String.Join(", ", TypeObject.NamesFields)}"
            );
        }

        CreatePayloadHttpElma.WebItem(name, value, ref webData);
    }
    public void ItemDateOnly(string name, DateOnly dateOnly)
    {
        var parseDate = $"{dateOnly.Year}-{dateOnly.Month}-{dateOnly.Day}";
        WebItem(name, parseDate);
    }
    public void ItemDateTime(string nameItem, DateTime dateTime)
    {
        var parseDateTime = $"{dateTime.Month}/{dateTime.Day}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}:00";

        WebItem(nameItem, parseDateTime);
    }
    public void ItemInteger(string nameItem, long value) => WebItem(nameItem, value.ToString());
    public void ItemDouble(string nameItem, double value) => 
        WebItem(nameItem, Math.Round(value, 2).ToString().Replace(",", "."));
    public void ItemMoney(string nameItem, double value) => 
        WebItem(nameItem, Math.Round(value, 2).ToString().Replace(",", "."));
    public void ItemMoneySetNull(string nameItem) => WebItem(nameItem, "");
    public void ItemTimeInterval(string nameItem, TimeInterval timeInterval) =>
        WebItem(nameItem, $"{timeInterval.Days}.{timeInterval.Hours}:{timeInterval.Minutes}:00");
    public void ItemSetNull(string nameItem) => WebItem(nameItem, null);
    public void ItemBoolean(string nameItem, bool value) => WebItem(nameItem, value.ToString());
    public void ItemUrl(string nameItem, Uri url) => WebItem(nameItem, url.ToString());
    public void ItemLine(string nameItem, string value)
    {
        int searchLines = Regex.Matches(value, "\n").Count + 1;

        if (searchLines != 1)
            throw new Exception("For WebItem with type Line there should be only one Line of text. Value: " + value);

        WebItem(nameItem, value);
    }
    public void ItemText(string nameItem, string value) => WebItem(nameItem, value);
    public void ItemHtml(string nameItem, string value) => WebItem(nameItem, value);
    public WebItemEnum ItemEnum(string nameItem)
    {
        return new WebItemEnum(nameItem, ref webData, _enums);
    }
    public WebItemBlock ItemBlock(string nameItem)
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.TypeObject.NamesFields.Contains(nameItem))
            throw new Exception(
                $"Elma Object \"{TypeObject.Name}\", don't have field \"{nameItem}\". "
                + $"Available fileds: {String.Join(", ", TypeObject.NamesFields)}"
            );

        return new WebItemBlock(nameItem,
            ref webData,
            AvailableElmaObjects,
            _elmaClient,
            (long)CurrentIdObject!,
            this.TypeObject.Name);
    }

    /// <summary>
    /// Создать новый WebItem ссылка на завизимый объект другой сущности,
    /// в параметре нужно указать имя поля (Item) для которой создатеся ссылка на объект,
    /// вторым аргументов указывается уникальный идентификатор сущности на которую будет 
    /// ссылвться данное поле. Чтобы указать что поле не ссылается ни на один объект нужно
    /// вторым аргументом передать значение null
    /// </summary>
    public WebItemObject ItemObject(string nameItem) 
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.TypeObject.NamesFields.Contains(nameItem))
        {
            throw new Exception(
                $"Elma Object \"{TypeObject.Name}\", don't have field \"{nameItem}\". "
                + $"Available fileds: {String.Join(", ", TypeObject.NamesFields)}"
            );
        }

        return new WebItemObject(nameItem, ref webData, AvailableElmaObjects, _elmaClient);
    }

    /// <summary>
    /// Создание WebItem с массивом ссылок на другие сущнсоти (аналог foreign key в базе данных где свзять 1-N или N-N)
    /// </summary>
    /// <param name="nameItem">Item's name</param>
    public WebItemObjects ItemObjects(string nameItem)
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.TypeObject.NamesFields.Contains(nameItem))
        {
            throw new Exception(
                $"Elma Object \"{TypeObject.Name}\", don't have field \"{nameItem}\". "
                + $"Available fileds: {String.Join(", ", TypeObject.NamesFields)}"
            );
        }

        return new WebItemObjects(
            nameItem,
            ref webData,
            AvailableElmaObjects,
            _elmaClient,
            (long)CurrentIdObject!,
            this.TypeObject.Name);
    }
}

public class PrepareHttpInsertUpdate : WebDataUpdateInsert
{
    protected HttpClient _httpClient;
    protected NameValueCollection queryParamsUrl = HttpUtility.ParseQueryString(string.Empty);
    protected string PathUrl;
    protected HttpMethod HttpMethod;
    protected RefreshTokenDelegate RefToken;

    public PrepareHttpInsertUpdate(
        HttpClient httpClient,
        ObjectElma typeObj,
        string pathUrl,
        HttpMethod httpMethod,
        RefreshTokenDelegate refToken,
        List<ObjectElma> availableObjects,
        ElmaClient elmaClient,
        List<EnumElma> enums,
        long? currentIdObject = null)
        : base(typeObj, availableObjects, elmaClient, currentIdObject, enums)
    {
        _httpClient = httpClient;
        PathUrl = pathUrl;
        HttpMethod = httpMethod;
        RefToken = refToken;
        TypeObject = typeObj;
    }

    public async Task<int> Execute()
    {
        // for update AuthToken and SessionToken if they'are not actual
        await RefToken();

        var request = new HttpRequestMessage(HttpMethod, PathUrl);

        request.Content = new StringContent(JsonConvert.SerializeObject(webData), Encoding.UTF8, "application/json");

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };

        var response = await _httpClient.SendAsync(request);

        // if response from server wan't equels 200 (successful result), then throw exception
        if ((int)response.StatusCode != 200)
            throw new Exception("Bad request, server's body response:> " 
                + await response.Content.ReadAsStringAsync());

        var body = await response.Content.ReadAsStringAsync();

        return int.Parse(body.Replace("\"", String.Empty));
    }
}

public class PrepareHttpStartProcess
{
    protected HttpClient _httpClient;
    public WebData webData = new WebData();
    private string pathUrl;
    protected RefreshTokenDelegate refToken;

    public PrepareHttpStartProcess(HttpClient httpClient, string pathUrl, RefreshTokenDelegate refToken)
    {
        this._httpClient = httpClient;
        this.pathUrl = pathUrl;
        this.refToken = refToken;
    }

    public async Task<WebData?> Execute()
    {
        // for update AuthToken and SessionToken if they'are not actual
        await refToken();

        System.Console.WriteLine(JsonConvert.SerializeObject(webData));
        // if data wasn't provided then throw an exception
        if (webData == null) throw new Exception("Field webData is null. Need data to upload to server");

        var request = new HttpRequestMessage(HttpMethod.Post, pathUrl);

        request.Content = new StringContent(JsonConvert.SerializeObject(webData), Encoding.UTF8, "application/json");

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };

        var response = await _httpClient.SendAsync(request);

        // if response from server wan't equels 200 (successful result), then throw exception
        if ((int)response.StatusCode != 200)
            throw new Exception("Bad request, server's body response:> " 
                + await response.Content.ReadAsStringAsync());

        var body = await response.Content.ReadAsStringAsync();

        return await response.Content.ReadFromJsonAsync<WebData>();
    }

    /// <summary>
    /// Создать новый WebItem с названием name и значением value, Если такой WebItem уже есть 
    /// тогда заменит значение в данном WebItem с названием name. 
    /// </summary>
    public PrepareHttpStartProcess WebItem(string name, string value)
    {
        CreatePayloadHttpElma.WebItem(name, value, ref webData);
        return this;
    }
    
    /// <summary>
    /// Создать новый WebItem ссылка на завизимый объект другой сущности,
    /// в параметре нужно указать имя поля (Item) для которой создатеся ссылка на объект,
    /// вторым аргументов указывается уникальный идентификатор сущности на которую будет 
    /// ссылвться данное поле. Чтобы указать что поле не ссылается ни на один объект нужно
    /// вторым аргументом передать значение null
    /// </summary>
    public PrepareHttpStartProcess WebItemRefObject(string nameItem, int? id) 
    {
        CreatePayloadHttpElma.WebItemRefObject(nameItem, id, ref webData);
        return this;
    }
}

static class CreatePayloadHttpElma
{
    static public void WebItem(string name, string? value, ref WebData webData)
    {
        webData ??= new WebData();
        webData.Items ??= new List<WebDataItem>();

        var tryFindItemByName = webData.Items.FirstOrDefault(item => item.Name == name);

        if (tryFindItemByName == null)
            webData.Items.Add(new WebDataItem { Name = name, Value = value });
        else
            tryFindItemByName.Value = value;
    }
    
    static public void WebItemRefObject(string nameItem, Int64? id, ref WebData webData) 
    {
        webData ??= new WebData();
        webData.Items ??= new List<WebDataItem>();

        var tryFindDependency = webData.Items.FirstOrDefault(item => 
            item.Name == nameItem);

        // if didn't create before, then create new
        if (tryFindDependency == null)
        {
            if (id == null)
            {
                // create new item
                webData.Items.Add(
                    new WebDataItem { Name = nameItem, Data = null }
                );
                return;
            }

            // create new item
            webData.Items.Add(
                new WebDataItem { Name = nameItem, Data = new WebData { Items = new List<WebDataItem>() } }
            );
            
            var findNewItem = webData.Items.First(item => item.Name == nameItem);

            // add new item for referenced object
            findNewItem.Data?.Items?.Add(new WebDataItem { Name = "Id", Value = id.ToString()! });
        }
        else 
        {
            // check if item with 'nameItem' for referenced object has already created before 
            var tryFindItemRefObj = tryFindDependency.Data?.Items?.FirstOrDefault(item => item.Name == "Id");

            if (id == null)
            {
                tryFindDependency.Data = null;
                return;
            }

            if (tryFindItemRefObj == null)
            {
                tryFindDependency.Data = new WebData { 
                    Items = new List<WebDataItem> { 
                        new WebDataItem { Name = "Id", Value = id.ToString() } 
                    } 
                };
            }
            else
            {
                tryFindItemRefObj.Value = id.ToString();
            }  
        }
    }

    static public void WebItemRefObjects(string nameItem, List<long>? refsObjects, ref WebData webData)
    {
        webData ??= new WebData();
        webData.Items ??= new List<WebDataItem>();

        var tryFindItemDependencies = webData.Items.FirstOrDefault(item => item.Name == nameItem);

        // if didn't create before, then create new
        if (tryFindItemDependencies == null)
        {
            // temporary storage
            List<WebData> arrayRefObjects = new List<WebData>();

            refsObjects?.ForEach(refId =>
            {
                WebData newRefObj = new WebData
                {
                    Items = new List<WebDataItem> { new WebDataItem { Name = "Id", Value = refId.ToString() } }
                };

                arrayRefObjects.Add(newRefObj);
            });

            // create new item with reference objects
            webData.Items.Add(
                new WebDataItem { Name = nameItem, DataArray = arrayRefObjects }
            );
        }
        else 
        {
            // for delete all dependencies objects
            if (refsObjects == null) 
            {
                tryFindItemDependencies.DataArray = new List<WebData>();
                return;
            }

            refsObjects.ForEach(refId =>
            {
                var existOrNotItemInArray = tryFindItemDependencies.DataArray.FirstOrDefault(webData =>
                {
                    var tempItem = webData.Items.First(item => item.Name == "Id");
                    return int.Parse(tempItem.Value!) == refId;
                });

                if (existOrNotItemInArray == null)
                    tryFindItemDependencies.DataArray.Add(
                        new WebData { 
                            Items = new List<WebDataItem> { 
                                new WebDataItem { Name = "Id", Value = refId.ToString()}
                            }
                        }
                    );

            });

        }
    }

    static public void WebItemBlock(string nameItem, List<WebData> webDatas, ref WebData webData)
    {
        webData ??= new WebData();
        webData.Items ??= new List<WebDataItem>();

        var tryFindItemBlock = webData.Items.FirstOrDefault(item => item.Name == nameItem);

        // if didn't create before, then create new
        if (tryFindItemBlock == null)
        {
            // create new item with reference objects
            webData.Items.Add(
                new WebDataItem { Name = nameItem, DataArray = webDatas }
            );
        }
        else 
        {
            tryFindItemBlock.DataArray = webDatas;
        }
    }
}

public class BaseRefItem
{
    public WebData WebData;
    public string NameItem;
    public List<ObjectElma> AvailableElmaObjects;
    protected ElmaClient _elmaClient;
    public BaseRefItem(string nameItem,
                         ref WebData webData,
                         List<ObjectElma> availableElmaObjects,
                         ElmaClient elmaClient)
    {
        WebData = webData;
        NameItem = nameItem;
        AvailableElmaObjects = availableElmaObjects;
        _elmaClient = elmaClient;
    }
}


public class WebItemBlock : BaseRefItem
{
    public long CurrentIdObject;
    public string NameObject;
    public WebItemBlock(
        string nameItem,
        ref WebData webData,
        List<ObjectElma> availableElmaObjects,
        ElmaClient elmaClient,
        long currentIdObject,
        string nameObject
        ) 
        : base(
            nameItem,
            ref webData,
            availableElmaObjects,
            elmaClient) 
    {
        CurrentIdObject = currentIdObject;
        NameObject = nameObject;
    }

    public void Ref(params MakeWebData[] webDatas) => 
        CreatePayloadHttpElma.WebItemBlock(NameItem, webDatas.Select(elem => elem.webData).ToList(), ref WebData);

    async public Task Add(params MakeWebData[] webDatas)
    {     
        var getCurrentObject = await _elmaClient.LoadEntity(NameObject!, (long)CurrentIdObject!).Execute();

        var findItemBlock = getCurrentObject!.Items.First(item => item.Name == NameItem);

        var JoinedCurrentAndNew = findItemBlock.DataArray.Concat(webDatas.Select(elme => elme.webData).ToList());

        CreatePayloadHttpElma.WebItemBlock(NameItem, JoinedCurrentAndNew.ToList(), ref WebData);
    }
}

public class WebItemObject : BaseRefItem
{
    public WebItemObject(
        string nameItem,
        ref WebData webData,
        List<ObjectElma> availableElmaObjects,
        ElmaClient elmaClient) 
        : base(
            nameItem,
            ref webData,
            availableElmaObjects,
            elmaClient) { }

    /// <summary>
    /// Make reference to object with name parameter "nameObject" and its id "entityId".
    /// Validate the object with name "nameObject" exist, if so check there is entity with id "entityId"
    /// </summary>
    async public Task Ref(string nameObject, Int64 id)
    {
        if (AvailableElmaObjects.FirstOrDefault(obj => obj.Name == nameObject) == null)
            throw new Exception($"Elma entity with name \"{nameObject}\" isn't exist in server.");

        try
            { await _elmaClient.LoadEntity(nameObject, id).Execute(); }
        catch (Exception ex)
            { throw new Exception($"Elma entity with id \"{id}\" isn't exist in Object \"{nameObject}\". " + ex.Message); }

        CreatePayloadHttpElma.WebItemRefObject(NameItem, id, ref WebData);
    }

    /// <summary>
    /// Clear the reference to object
    /// </summary>
    public void SetNull()
    {
        CreatePayloadHttpElma.WebItemRefObject(NameItem, null, ref WebData);
    }
}

public class WebItemObjects : BaseRefItem
{
    public long? CurrentIdObject;
    public string? NameObject;
    public WebItemObjects(
        string nameItem,
        ref WebData webData,
        List<ObjectElma> availableElmaObjects,
        ElmaClient elmaClient,
        long? currentIdObject,
        string? nameObject
        ) 
        : base(
            nameItem,
            ref webData,
            availableElmaObjects,
            elmaClient) 
    {
        CurrentIdObject = currentIdObject;
        NameObject = nameObject;
    }

    /// <summary>
    /// Make reference to objects with name parameter "nameObject" and its id "entityId".
    /// Validate the object with name "nameObject" exist, if so check there is entity with id "entityId"
    /// </summary>
    async public Task Ref(string nameObject, params long[] ids)
    {
        if (AvailableElmaObjects.FirstOrDefault(obj => obj.Name == nameObject) == null)
            throw new Exception($"Elma entity with name \"{nameObject}\" isn't exist in server.");

        foreach (var id in ids)
        {
            try
                { await _elmaClient.LoadEntity(nameObject, id).Execute(); }
            catch (Exception ex)
                { throw new Exception($"Elma entity with id \"{id}\" isn't exist in Object \"{nameObject}\". " + ex.Message); }
        }

        CreatePayloadHttpElma.WebItemRefObjects(NameItem, ids.ToList(), ref WebData);
    }

    /// <summary>
    /// add new references to objects. Existing references to objects will be still in current object
    /// </summary>
    async public Task Add(string nameObject, params long[] ids)
    {
        if (AvailableElmaObjects.FirstOrDefault(obj => obj.Name == nameObject) == null)
            throw new Exception($"Elma entity with name \"{nameObject}\" isn't exist in server.");

        foreach (var id in ids)
        {
            try
                { await _elmaClient.LoadEntity(nameObject, id).Execute(); }
            catch (Exception ex)
                { throw new Exception($"Elma entity with id \"{id}\" isn't exist in Object \"{nameObject}\". " + ex.Message); }
        }

        // ! If don't need to check existing object, its WebItem with ids referenced to another object. Or It's a Block WebItem
        if (String.IsNullOrEmpty(NameObject) && CurrentIdObject == null)
        {
            CreatePayloadHttpElma.WebItemRefObjects(NameItem, ids.ToList(), ref WebData);
            return;
        }
            
        var getCurrentObject = await _elmaClient.LoadEntity(NameObject!, (long)CurrentIdObject!).Execute();
        var findItemArray = getCurrentObject!.Items.First(item => item.Name == NameItem);
        var currIds = findItemArray.DataArray.Select(webData => webData.Items.First(item => item.Name == "Id").Value)
            .Select(id => long.Parse(id!)).ToList();

        CreatePayloadHttpElma.WebItemRefObjects(NameItem, currIds.Union(ids).ToList(), ref WebData);
    }

    async public Task Remove(string nameObject, params long[] ids)
    {
        if (AvailableElmaObjects.FirstOrDefault(obj => obj.Name == nameObject) == null)
            throw new Exception($"Elma entity with name \"{nameObject}\" isn't exist in server.");

        foreach (var id in ids)
        {
            try
                { await _elmaClient.LoadEntity(nameObject, id).Execute(); }
            catch (Exception ex)
                { throw new Exception($"Elma entity with id \"{id}\" isn't exist in Object \"{nameObject}\". " + ex.Message); }
        }

        // ! If don't need to check existing object, its WebItem with ids referenced to another object. Or It's a Block WebItem
        if (String.IsNullOrEmpty(NameObject) && CurrentIdObject == null)
        {
            CreatePayloadHttpElma.WebItemRefObjects(NameItem, ids.ToList(), ref WebData);
            return;
        }

        var getCurrentObject = await _elmaClient.LoadEntity(NameObject!, (long)CurrentIdObject!).Execute();
        var findItemArray = getCurrentObject!.Items.First(item => item.Name == NameItem);
        var currIds = findItemArray.DataArray.Select(webData => webData.Items.First(item => item.Name == "Id").Value)
            .Select(id => long.Parse(id!)).ToList();

        CreatePayloadHttpElma.WebItemRefObjects(NameItem, currIds.Except(ids).ToList(), ref WebData);
    }

    /// <summary>
    /// Clear WebItem the reference to objects
    /// </summary>
    public void SetEmpty()
    {
        CreatePayloadHttpElma.WebItemRefObjects(NameItem, null, ref WebData);
    }
}

public class WebItemEnum
{
    private List<EnumElma> _enums;
    public WebData WebData;
    private string _nameItem;
    public WebItemEnum(string nameItem, ref WebData webData,List<EnumElma> enums)
    {
        _nameItem = nameItem;
        WebData = webData;
        _enums = enums;
    }
    
    public void Value(string nameEnum, string valueEnum)
    {
        var tryFindEnum = _enums.FirstOrDefault(enumElma =>
            enumElma.Name == nameEnum);

        // if enum with name 'nameEnum' is not existed in storage then throw exception
        if (tryFindEnum == null)
            throw new Exception($"Enum with name: '{nameEnum}' isn't existed in storage");

        // if the found enum don't have any Values then throw exception
        if (tryFindEnum.Values == null)
            throw new Exception($"The enum '{nameEnum}' doesn't have any values");


        var tryFindIndexOfValue = Array.IndexOf(tryFindEnum.Values, valueEnum);

        // if the value 'valueEnum' doesn't exist in storage the found enum, throw exception
        if (tryFindIndexOfValue == -1)
            throw new Exception($"The enum '{nameEnum}' doesn't have value '{valueEnum}'."
                + $" All values: {String.Join(",", tryFindEnum.Values)}");

        CreatePayloadHttpElma.WebItem(_nameItem, tryFindIndexOfValue.ToString(), ref WebData);
    }

    public void SetNull()
    {
        CreatePayloadHttpElma.WebItem(_nameItem, "", ref WebData);
    }
}