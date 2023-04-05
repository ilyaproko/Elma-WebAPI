using EnvironmentModule;
using Elma;

var env = new EnvModule();

var token = env.getVar("TOKEN");
var hostaddress = env.getVar("HOSTADDR");
var username = env.getVar("USERNAME");
var password = env.getVar("PASSWORD");

var elmaClien = await new ElmaClient(token, hostaddress, username, password).Build();

