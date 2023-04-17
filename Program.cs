using EnvironmentModule;
using Elmapi;
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

var testingObject = elmaClient.UpdateEntity("UchebnyePlany", id: 834);

testingObject.ItemObject("GroupMain").SetNull();
await testingObject.ItemObject("GroupMain").Ref("Gruppy", 4586);

await testingObject.ItemObjects("ListReferences").Ref("Gruppy", 4843);
await testingObject.ItemObjects("ListReferences").Add("Gruppy", 4840, 4841, 4842, 4843, 4843, 4843, 4843);
await testingObject.ItemObjects("ListReferences").Add("Gruppy", new long[] { 4840, 4841, 4842, 4843, 4843, 4843, 4843 });
testingObject.ItemObjects("ListReferences").SetEmpty();

await testingObject.Execute();

System.Console.WriteLine(JsonConvert.SerializeObject(testingObject.webData));

// await newArrayItem.Execute();


