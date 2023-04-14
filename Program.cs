﻿using EnvironmentModule;
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

// var newArrayItem = elmaClient.UpdateEntity("UchebnyePlany", id: 834);

// newArrayItem.WebItemRefObject("GroupMain", entityId: 2139);
// newArrayItem.WebItemRefObjects("ListReferences", new List<int> { 2139, 2140 });
// System.Console.WriteLine(JsonConvert.SerializeObject(newArrayItem.webData));

// await newArrayItem.Execute();


