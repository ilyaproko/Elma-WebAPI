using System;
using System.Collections.Generic;
using System.IO;

namespace EnvironmentModule;

class EnvModule
{
    string pathFile = Path.Combine(Environment.CurrentDirectory, ".env");

    List<EnvRecord> envsList = new List<EnvRecord>();

    public EnvModule()
    {
        if (File.Exists(this.pathFile))
        {
            string[] lines = File.ReadAllText(this.pathFile).Split("\n");

            foreach (var line in lines)
            {
                // check if line is empty or line doesn't include symbol =
                if (!String.IsNullOrEmpty(line) && line.IndexOf("=") != -1)
                {
                    string key = line.Substring(0, line.IndexOf("=")).Trim();
                    string value = line.Substring(line.IndexOf("=") + 1).Trim();

                    envsList.Add(new EnvRecord { Key = key, Value = value });
                }
            }
        }
        else
        {
            throw new Exception($"File \".env\" isn't found in directory: {Environment.CurrentDirectory}");
        }
    }

    /// <summary>
    /// Get value of certain key in environment variables. If 
    /// the won't find a record with a certain key then throw exception
    /// Warn: look carefully at letter case.
    /// </summary>
    public string getVar(string key)
    {
        var tryFind = envsList.FirstOrDefault(e => e.Key == key)?.Value;
        
        if (tryFind == null)
            throw new Exception($"key: \"{key}\" isn't found. Available"
                + $" environment variables: {String.Join(", ", envsList.Select(e => e.Key))}");

        return tryFind;
    }

}

class EnvRecord
{
    public string Key = default!;
    public string Value = default!;
}
    