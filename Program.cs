using EnvironmentModule;
using Elma;
using Newtonsoft.Json;

var env = new EnvModule();

var token = env.getVar("TOKEN");
var hostaddress = env.getVar("HOSTADDR");
var username = env.getVar("USERNAME");
var password = env.getVar("PASSWORD");

var elmaClient = await new ElmaClient(token, hostaddress, username, password).Build();



// var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(60));
// int counter = 60;
// while (await periodicTimer.WaitForNextTickAsync())
// {
//     System.Console.WriteLine(
//         JsonConvert.SerializeObject(await elmaClient.LoadEntity("UchebnyePlany", id: 103).Execute()) + 
//         $"\n{elmaClient!.AuthorizationData!.AuthToken} " + 
//         " Time :> " + counter / 60
//     );

//     counter += 60;
// }

// var test = await elmaClient.QueryEntity("User").Eql("FirstName LIKE 'В%'").Execute();

// System.Console.WriteLine(JsonConvert.SerializeObject(test));

// System.Console.WriteLine("-------------------------");
// System.Console.WriteLine(";;;;;;;;;;;;---------------------------");

// System.Console.WriteLine(
//     JsonConvert.SerializeObject(await elmaClient.LoadEntity("User", id: 230).Select("ReplacementUser").Execute())
// );




