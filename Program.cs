using EnvironmentModule;
using Elma;
using Newtonsoft.Json;

var env = new EnvModule(".env");

var token = env.GetVar("TOKEN");
var hostaddress = env.GetVar("HOSTADDR");
var username = env.GetVar("USERNAME");
var password = env.GetVar("PASSWORD");

var elmaClient = await new ElmaClient(token, hostaddress, username, password).Build();


// var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(60));
// int counter = 60;
// while (await periodicTimer.WaitForNextTickAsync())
// {
//     var updated = elmaClient.UpdateEntity("UchebnyePlany", id: 831);
//     updated.WebItem("Naimenovanie", "new");

//     System.Console.WriteLine(
//         (await updated.Execute()) + 
//         $" {elmaClient!.AuthorizationData!.AuthToken} " + 
//         " Time :> " + counter / 60
//     );

//     counter += 60;
// }

// var test = await elmaClient.QueryEntity("User").Eql("FirstName LIKE 'В%'").Execute();

// System.Console.WriteLine(
//     JsonConvert.SerializeObject(
//         await elmaClient.LoadEntity("UchebnyePlany", id: 103).Execute()
//     )
// );



