using System.Web;
using System.Collections.Specialized;
using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Elma;
using System.ComponentModel.DataAnnotations;

namespace TypesElma;


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

class ResponseAuthorization
{
    public string AuthToken { get; set; } = default!;
    public string CurrentUserId { get; set; } = default!;
    public string Lang { get; set; } = default!;
    public string SessionToken { get; set; } = default!;
}

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
    protected QParamsBase Id(int id) 
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

interface IPrepareHttpBase<T>
{
    public Task<T?> Execute();
}

public class PrepareHttpBase<T> : QParamsBase, IPrepareHttpBase<T> 
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
    public PrepareHttpLoad(HttpClient httpClient, string pathUrl, HttpMethod httpMethod, int id, RefreshTokenDelegate refToken)
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

public class PrepareHttpInsertOrUpdate : PrepareHttpBase<int>
{
    public WebData webData = new WebData();
    public ObjectElma typeObj;
    public PrepareHttpInsertOrUpdate(HttpClient httpClient, ObjectElma typeObj, string pathUrl, HttpMethod httpMethod, RefreshTokenDelegate refToken)
        : base(httpClient, pathUrl, httpMethod, refToken) 
    {
        this.typeObj = typeObj;
    }

    public async new Task<int> Execute()
    {
        // for update AuthToken and SessionToken if they'are not actual
        await refToken();

        var request = new HttpRequestMessage(httpMethod, pathUrl);

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

    /// <summary>
    /// Создать новый WebItem с названием name и значением value, Если такой WebItem уже есть 
    /// тогда заменит значение в данном WebItem с названием name. Перед созданием происходит проверка
    /// названия WebItem name, есть ли похожее поле в Объекте Elma, если нет тогда выбросит ошибку
    /// </summary>
    public PrepareHttpInsertOrUpdate WebItem(string name, string value)
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.typeObj.NamesFields.Contains(name))
        {
            throw new Exception(
                $"Elma Object \"{typeObj.Name}\", don't have field \"{name}\". "
                + $"Available fileds: {String.Join(", ", typeObj.NamesFields)}"
            );
        }

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
    public PrepareHttpInsertOrUpdate WebItemRefObject(string nameObject, int? entityId) 
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.typeObj.NamesFields.Contains(nameObject))
        {
            throw new Exception(
                $"Elma Object \"{typeObj.Name}\", don't have field \"{nameObject}\". "
                + $"Available fileds: {String.Join(", ", typeObj.NamesFields)}"
            );
        }

        CreatePayloadHttpElma.WebItemRefObject(nameObject, entityId, ref webData);
        return this;
    }

    /// <summary>
    /// Создание WebItem с массивом ссылок на другие сущнсоти (аналог foreign key в базе данных где свзять 1-N или N-N)
    /// </summary>
    /// <param name="nameItem">Item's name</param>
    /// <param name="entitisId">Lifst of unique Objects' ids which is referenced</param>
    public PrepareHttpInsertOrUpdate WebItemRefObjects(string nameItem, List<int>? entitisId)
    {
        // check if the name exists for certain object elma which the WebItem creating
        // if the Name Of creating Item don't specified then throw Exception
        if (!this.typeObj.NamesFields.Contains(nameItem))
        {
            throw new Exception(
                $"Elma Object \"{typeObj.Name}\", don't have field \"{nameItem}\". "
                + $"Available fileds: {String.Join(", ", typeObj.NamesFields)}"
            );
        }

        CreatePayloadHttpElma.WebItemRefObjects(nameItem, entitisId?.Distinct().ToList(), ref webData);
        return this;
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
    static public void WebItem(string name, string value, ref WebData webData)
    {
        webData ??= new WebData();
        webData.Items ??= new List<WebDataItem>();

        var tryFindItemByName = webData.Items.FirstOrDefault(item => item.Name == name);

        if (tryFindItemByName == null)
            webData.Items.Add(new WebDataItem { Name = name, Value = value });
        else
            tryFindItemByName.Value = value;
    }
    
    static public void WebItemRefObject(string nameItem, int? id, ref WebData webData) 
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

    static public void WebItemRefObjects(string nameItem, List<int>? refsObjects, ref WebData webData)
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
}