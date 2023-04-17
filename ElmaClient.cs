using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EnvironmentModule;
using HtmlAgilityPack;
using Newtonsoft.Json;
using ExtensionElma;
using ElmaType;

namespace Elmapi;

public class ElmaClient 
{
    private string ElmaTokenApi;
    private readonly HttpClient _httpClient;
    private readonly string Username;
    private readonly string Password;
    public ResponseAuthorization? AuthorizationData;
    private List<ObjectElma> Objects = new List<ObjectElma>(); // all available entities in elma
    private List<ObjectElma> Processes = new List<ObjectElma>(); // all processes in elma
    private List<EnumElma> Enums = new List<EnumElma>(); // all available enums in server elma
    private List<ProcessElma> StartableProcesses = new List<ProcessElma>(); // all avaialbe processes to start in elma
    // url to get authorization token from elma server for requests
    private readonly string UrlAuthorization = "/API/REST/Authorization/LoginWith";
    // url to get entities from elma server
    private readonly string UrlEntityQueryTree = "/API/REST/Entity/QueryTree";
    // url to get a certain one entity using TypeUID and its id
    private readonly string UrlEntityLoadTree = "/API/REST/Entity/LoadTree";
    // example insert full url /API/REST/Entity/Insert/<TYPEUID_ELMA_ENTITY>
    // after /Insert/ should be typeuid which need to insert to server elma
    private readonly string UrlEntityInsert = "/API/REST/Entity/Insert/";
    // url to count entities in elma server
    private readonly string UrlEntityCount = "/API/REST/Entity/Count";
    // url to update entity in serlve elma, where 0 - it's TypeUidElma entity, 1 - it's Id entity elma to update
    private readonly string UrlEntiityUpdate = "/API/REST/Entity/Update/{0}/{1}";
    // url to launch process by http
    private readonly string UrlStartProcess = "/API/REST/Workflow/StartProcess";
    // url to get all starable processes 
    private readonly string UrlStarableProcesses = "/API/REST/Workflow/StartableProcesses";
    // url to get all starable processes from external apps
    private readonly string UrlStarableProcessesExternalApps = "/API/REST/Workflow/StartableProcessesFromExternalApps";
    // url to html page with all elma acccessable elma entities
    private readonly string UrlPageTypes = "/API/Help/Types";
    // url to html page with specific Object's information (also will need UrlParameter 'uid')
    private readonly string UrlPageType = "/API/Help/Type";
    // url to html page with all available Enums in Server Elma
    private readonly string UrlPageEnums = "/API/Help/Enums";
    // url to html page with specifit Enum's information (also will need UrlParameter 'uid')
    private readonly string UrlPageEnum = "/API/Help/Enum";

    public ElmaClient(string token, string hostaddress, string username, string password)
    {
        this.ElmaTokenApi = token;
        this.Username = username;
        this.Password = password;
        this._httpClient = new() { BaseAddress = new Uri($"http://{hostaddress}") };
    }

    /// <summary>
    /// assamble instance of ElmaClient, must be call before any operations with instance of clss ElmaClient
    /// </summary>
    public async Task<ElmaClient> Build()
    {
        await GetAuthorization();
        await GetTypesUid();
        await GetNamesItemsForObjects();
        await GetEnumsElma();
        await GetStarableProcesses();
        return this;
    }

    /// <summary>
    /// check if authToken and sessionToken is no more active. Then get new tokens by IServiceAuthorization
    /// </summary>
    public async Task RefreshToken() 
    {
        var request = new HttpRequestMessage(HttpMethod.Get, " /API/REST/Authorization/ServerTimeUTC");

        var response = await _httpClient.SendAsync(request);

        if ((int)response.StatusCode == 400)
        {
            var convertJson = await response.Content.ReadFromJsonAsync<ResponseElma>();
            await GetAuthorization();
        }
    }

    public PrepareHttpStartProcess StartProcess(string nameProcess)
    {
        var tryFindProcessElma = this.StartableProcesses?
            .FirstOrDefault(process => process.Name == nameProcess);

        if (tryFindProcessElma == null)
            throw new Exception($"Process '{nameProcess}' isn't found. "
                + $"All available process:> {String.Join(", ", StartableProcesses!.Select(elm => elm.Name))}");


        var prepareRequest = new PrepareHttpStartProcess(this._httpClient, UrlStartProcess, RefreshToken);
        prepareRequest.WebItem("ProcessHeaderId", tryFindProcessElma.Id.ToString());
        prepareRequest.WebItem("ProcessName", tryFindProcessElma.Name);

        return prepareRequest;

        // var request = new HttpRequestMessage(HttpMethod.Post, UrlStartProcess);

        // request.Content = new StringContent(JsonConvert.SerializeObject(
        //     new WebData 
        //     {
        //         Items = new List<WebDataItem>() 
        //         {
        //             new WebDataItem { Name = "ProcessHeaderId", Value = tryFindProcessElma.Id.ToString()},
        //             new WebDataItem { Name = "ProcessName", Value = tryFindProcessElma.Name}
        //         }
        //     }
        // ), Encoding.UTF8, "application/json");

        // request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        // {
        //     CharSet = "utf-8"
        // };

        // var response = await _httpClient.SendAsync(request);

        // // if response from server wan't equels 200 (successful result), then throw exception
        // if ((int)response.StatusCode != 200)
        //     throw new Exception("Bad request, server's body response:> " 
        //         + await response.Content.ReadAsStringAsync());

        // var body = await response.Content.ReadAsStringAsync();

        // System.Console.WriteLine(JsonConvert.SerializeObject(body));
    }

    /// <summary>
    /// Get all business processes in elma server which is availble to start (launch)
    /// </summary>
    private async Task GetStarableProcesses()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, UrlStarableProcesses);

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<WebData>();

        var findProcesses = body?.Items?.FirstOrDefault(elem => elem.Name == "Processes");

        // if didn't find any availbale to start process then break the method
        if (findProcesses == null) return;

        this.StartableProcesses = findProcesses!.DataArray!.Select(webData =>
        {
            var nameProcess = webData.Items.First(webItem => webItem.Name == "Name");
            var idProcess = webData.Items.First(webItem => webItem.Name == "Id");
            var groupIdProcess = webData.Items.FirstOrDefault(webItem => webItem.Name == "GroupId");
            return new ProcessElma
            {
                Name = nameProcess.Value!,
                Id = int.Parse(idProcess.Value!),
                GroupId = groupIdProcess == null ? null : int.Parse(groupIdProcess.Value!)
            };
        }).ToList();
    }

    /// <summary>
    /// get all enums with their values in server elma and add them to storage Enums
    /// </summary>
    private async Task GetEnumsElma() 
    {
        var request = new HttpRequestMessage(HttpMethod.Get, UrlPageEnums);

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(body);

        var nodesHtml = htmlDoc.DocumentNode.SelectNodes("//body/table/tr")
            .Select(node => $"<body>{node.InnerHtml.Trim()}</body>").ToList();
        
        foreach (var node in nodesHtml)
        {
            htmlDoc.LoadHtml(node);
            var tdNodes = htmlDoc.DocumentNode.SelectNodes("body/td");

            // if the tdNode doesn't have 2 elements then skip it iteration
            if (tdNodes.Count != 2) continue;
            // if first element doesn't have attribute href with start value href="/API/Help/Enum?uid=
            // then skip it iteration
            if (!tdNodes[0].InnerHtml.Contains("href=\"/API/Help/Enum?uid=")) continue;

            var nameEnum = tdNodes[0].InnerText.Trim();
            var descEnum = tdNodes[1].InnerText.Trim();
            var uidEnum = htmlDoc.DocumentNode.SelectSingleNode("//@href")
                .GetAttributeValue("href", null).Substring(19);

            var requestEnum = new HttpRequestMessage(HttpMethod.Get, $"{UrlPageEnum}?uid={uidEnum}");

            var responseEnum = await _httpClient.SendAsync(requestEnum);
            var bodyEnum = await responseEnum.Content.ReadAsStringAsync();

            htmlDoc.LoadHtml(bodyEnum);
            var nodesValueEnum = htmlDoc.DocumentNode.SelectNodes("//body/table/tr/td[1]");
            
            var getEnumValues = nodesValueEnum != null 
                ? nodesValueEnum.Select(node => node.InnerText.Trim()).ToArray()
                : null;

            Enums.Add(
                new EnumElma 
                {
                    Name = nameEnum,
                    Uid = uidEnum,
                    NameDesc = descEnum,
                    Values = getEnumValues
                }
            );
        }
    }

    /// <summary>
    /// for every elma object add for them their fileds name
    /// </summary>
    private async Task GetNamesItemsForObjects()
    {
        // for all elma objects add for them fields' name for every one
        var allObject = this.Processes.Concat(Objects);

        foreach (var obj in allObject)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{UrlPageType}?uid={obj.Uid}");

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(body);
            
            var nodesHtml = htmlDoc.DocumentNode.SelectNodes("//body/table/tr/td[1]")
                .Select(node => node.InnerText.Trim()).ToList();

            obj.NamesFields ??= nodesHtml;
        }
    }

    /// <summary>
    /// получение authorization token и session token from elma server. These tokens 
    /// need for workflow with elma resp api
    /// </summary>
    private async Task GetAuthorization()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{UrlAuthorization}?username={Username}");
        request.Headers.Add("ApplicationToken", ElmaTokenApi);
        request.Content = new StringContent($"\"{Password}\"", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        // if server return response with 'bad request' then throw exception about the error
        if ((int)response.StatusCode != 200)
            throw new Exception($"Get authorization was unsuccessful. "
                + "Check parameters authorization: hostaddress, password, token, userlogin");

        AuthorizationData = await response.Content.ReadFromJsonAsync<ResponseAuthorization>();

        // automatically add tokens to every request's headers form this client to server http
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("AuthToken", AuthorizationData?.AuthToken);
        _httpClient.DefaultRequestHeaders.Add("SessionToken", AuthorizationData?.SessionToken);
    }

    /// <summary> получение сущностей (объект-справочник) от сервера elma </summary>
    /// <param name="type">имя униклього идентификтора типа сущности elma</param>
    public PrepareHttpQuery<List<WebData>> QueryEntity(string type)
    {
        // получаем тип обьекта по его наименованию и его TypeUID для запросов
        var getTypeObj = this.GetTypeObj(type, TypesObj.Entity);

        var prepareQuery = new PrepareHttpQuery<List<WebData>>(
            _httpClient, 
            getTypeObj.Uid, 
            UrlEntityQueryTree, 
            HttpMethod.Get,
            RefreshToken);

        prepareQuery.TypeUid(getTypeObj.Uid);

        return prepareQuery;
    }

    /// <summary> get a certain entity by Its id </summary>
    /// <param name="nameEntity">имя униклього идентификтора типа сущности elma</param>
    /// <param name="id">entity's id which will be updated</param>
    public PrepareHttpLoad<WebData> LoadEntity(string nameEntity, long id)
    {
        // получаем тип обьекта по его наименованию и его TypeUID для запросов
        var getTypeObj = this.GetTypeObj(nameEntity, TypesObj.Entity);

        var prepareLoad = new PrepareHttpLoad<WebData>(_httpClient,  UrlEntityLoadTree, HttpMethod.Get, id, RefreshToken);
        prepareLoad.TypeUid(getTypeObj.Uid);

        return prepareLoad;
    }

    /// <summary> Count all entities elma by name of type </summary>
    /// <param name="type">имя униклього идентификтора типа сущности elma</param>
    public async Task<int> CountEntity(string type)
    {
        // for update AuthToken and SessionToken if they'are not actual
        await RefreshToken();

        // получаем тип обьекта по его наименованию и его TypeUID для запросов
        var getTypeObj = this.GetTypeObj(type, TypesObj.Entity);

        var request = new HttpRequestMessage(HttpMethod.Get, UrlEntityCount + $"?type={getTypeObj.Uid}");
        var response = await _httpClient.SendAsync(request);
        return int.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary> inserted new entity to server elma </summary>
    /// <param name="type">имя униклього идентификтора типа сущности elma</param>
    public PrepareHttpInsertUpdate InsertEntity(string type)
    {
        // получаем тип обьекта по его наименованию и его TypeUID для запросов
        var getTypeObj = this.GetTypeObj(type, TypesObj.Entity);

        return new PrepareHttpInsertUpdate(
            _httpClient, 
            getTypeObj, 
            UrlEntityInsert + getTypeObj.Uid, 
            HttpMethod.Post,
            RefreshToken,
            Objects,
            this);
    }

    /// <summary> update entity via id with new data </summary>
    /// <param name="type">имя униклього идентификтора типа сущности elma</param>
    /// <param name="id">entity's id which will be updated</param>
    public PrepareHttpInsertUpdate UpdateEntity(string type, long id)
    {
        // получаем тип обьекта по его наименованию и его TypeUID для запросов
        var getTypeObj = this.GetTypeObj(type, TypesObj.Entity);

        return new PrepareHttpInsertUpdate(
            _httpClient, 
            getTypeObj, 
            String.Format(UrlEntiityUpdate, getTypeObj.Uid, id),
            HttpMethod.Post,
            RefreshToken,
            Objects,
            this,
            id);
    }

    /// <summary>
    /// get all types uid for accessable entities and processes in elma server
    /// </summary>
    private async Task GetTypesUid()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, UrlPageTypes);

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(body);

        var nodesHtml = htmlDoc.DocumentNode.SelectNodes("html/body/table/tr")
            .ToList()
            .Select(node => node.InnerHtml.Replace("\t", "").Replace("\r", "").Replace("\n", "")).ToList()
            .FindAll(node => new Regex("href=\"/API/Help/Type\\?uid=(.*)\">.*</a>", RegexOptions.IgnoreCase)
                    .Match(node).Success);

        foreach (var node in nodesHtml)
        {
            List<string> wrapNode = new() { 
                "<body>",
                node.Replace("\t", "").Replace("\r", "").Replace("\n", ""),
                "</body>" };

            string nodeConvertStr = String.Join("", wrapNode);

            htmlDoc.LoadHtml(nodeConvertStr);
            var nodeTypesInfo = htmlDoc.DocumentNode.SelectNodes("/body/td").ToList();

            // if didn't find two nodes structured together in one node
            if (nodeTypesInfo.Count != 2) continue;

            var nodeNameAndTypeUid = nodeTypesInfo.First().InnerHtml.Trim();
            var nodeNameDesc = nodeTypesInfo.Last().InnerHtml.Trim();

            var typeUid = nodeNameAndTypeUid.Substring(28, 36);
            var nameTypeUid = nodeNameAndTypeUid.Substring(66).Replace("</a>", "");

            // if name type uid starts with "P_" then it's a processe and then pass it 
            // to storage List TypesUidStorage
            if (nameTypeUid.StartsWith("P_"))
            {
                this.Processes.Add(
                    new ObjectElma
                    {
                        Name = nameTypeUid,
                        Uid = typeUid,
                        NameDesc = nodeNameDesc
                    });
            }
            // if not then it's a elma entity
            else {
                this.Objects.Add(
                    new ObjectElma
                    {
                        Name = nameTypeUid,
                        Uid = typeUid,
                        NameDesc = nodeNameDesc
                    });
            }

        }
    }


    /// <summary> Method for searching object's unique elma type </summary>
    /// <param name="name">Unique name of the object's type</param>
    /// <param name="type">enums can be only Entity or Process</param>
    /// <exception cref="Exception">If won't find the object's unique type then throw excepiton</exception>
    public ObjectElma GetTypeObj(string name, TypesObj type)
    {
        var tryFind = TypesObj.Process.Equals(type)
            ? this.Processes.Find(typeUid => typeUid.Name == name)
            : this.Objects.Find(typeUid => typeUid.Name == name);

        string entityOrProcess = TypesObj.Process.Equals(type) ? "Process" : "Entity";

        if (tryFind == null)
        {
            throw new Exception(
                $"{entityOrProcess} with name : \"{(String.IsNullOrEmpty(name) ? "null" : name)}\" "
                + $"isn't found. Check please: Letter Case, the {entityOrProcess} is published "
                + $"to the server, access to the {entityOrProcess}");
        }

        return tryFind;
    }

    public ObjectElma GetTypeObj(string name)
    {
        var processesAndEntities = this.Processes.Concat(Objects).ToList();

        var tryFind = processesAndEntities.Find(obj => obj.Name == name);

        if (tryFind == null)
        {
            throw new Exception(
                $"With name : \"{(String.IsNullOrEmpty(name) ? "null" : name)}\" "
                + $"isn't found any process and enitties. Check please: Letter Case, is the one published? "
                + $"to the server, access to the one");
        }
        return tryFind;
    }

    public string GetEnumValue(string nameEnum, string valueEnum)
    {
        var tryFindEnum = Enums.FirstOrDefault(enumElma =>
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

        return tryFindIndexOfValue.ToString();
    }

    /// <summary>
    /// method for comfortable get needed data from List of Items 
    /// if pass nestedNameItem that mean that main Object has dependency
    /// that It has pair Name/Value that we want to get
    /// </summary>
    /// <param name="items">Список Item</param>
    /// <param name="nameItem">
    /// Принимает наименование Item или если это вложенный Item (т.е. зависимость)
    /// указывается наименование данной зависимости и обезательно параметр nestedItemName
    /// который и будет вытаскивать необходимый Item в этой вложенной завимимости
    /// </param>
    /// <param name="nestedItemName"></param>
    /// <returns>
    /// При условии что наименование Item указано правильно вернет значение 
    /// данного Item или если не найдет тогда null
    /// </returns>
    static public string? getValueItem(
        List<WebDataItem> items,
        string nameItem,
        string? nestedItemName = null)
    {
        foreach (var item in items)
        {
            // if nestedNameItem wasn't passed in 
            if (nestedItemName == null)
            { // GET OBJECT DATA WITH ITEMS WHICH IS QUALS nameItem
                if (item.Name == nameItem) return item.Value; // RETURN !!!
            }
            else
            {
                if (item.Name == nameItem && item.Data != null)
                { // GET OBJECT DATA WITH ITEMS WHICH IS QUALS nameItem
                    foreach (var itemNested in item.Data.Items)
                    { // HAS NESTED DEPENDENCY
                        if (itemNested.Name == nestedItemName) return itemNested.Value; // RETURN !!!
                    }
                }
            }
        }
        return null;
    }

}