using EnvironmentModule;
using Elmapi;
using Newtonsoft.Json;

var env = new EnvModule(".env");

var token = env.GetVar("TOKEN");
var hostaddress = env.GetVar("HOSTADDR");
var username = env.GetVar("USERNAME");
var password = env.GetVar("PASSWORD");

var elmaClient = await new ElmaClient(token, hostaddress, username, password).Build();




