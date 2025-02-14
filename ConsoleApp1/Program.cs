using Newtonsoft.Json.Linq;
using System.Xml;


string directory = "C:\\Solutions\\Apply.SundBaelt\\src\\Apply.SundBaelt.Web\\uSync\\v9\\DataTypes";
string logPath = Path.Combine("C:\\Scripts", "RemoveDuplicateKeysAndAreasObjects_Log.txt");

File.AppendAllText(logPath, $"Processing started at {DateTime.Now}\n");


if (!Directory.Exists(directory))
{
    Console.WriteLine($"The directory path '{directory}' does not exist. Exiting script.");
    Environment.Exit(0);
}

var files = Directory.GetFiles(directory);
int fileModifiedCount = 0;

if (files.Length == 0)
{
    Console.WriteLine("No files found in the directory. Exiting script.");
    Environment.Exit(0);
}

foreach (var file in files)
{
    XmlDocument xmlDoc = new XmlDocument();
    xmlDoc.Load(file);

    XmlNode? configNode = xmlDoc?.SelectSingleNode("//DataType/Config");
    if (configNode != null && configNode.FirstChild is XmlCDataSection cdataSection)
    {
        string? jsonString = cdataSection.Value;
        try
        {
            if (jsonString == null)
            {
                File.AppendAllText(logPath, $"JSON string is null in file: {file}\n");
                continue;
            }
            var jsonObject = JObject.Parse(jsonString);
            bool modified = false;
            if (jsonObject.TryGetValue("Blocks", out JToken? blocksToken) && blocksToken is JArray blocksArray)
            {
                var blocksToRemove = new List<JToken>();
                var contentElementKeys = new HashSet<string>();

                foreach (var block in blocksArray)
                {

                    if (block["areas"] is JArray areasArray && areasArray.Count > 0)
                    {
                        blocksToRemove.Add(block);
                        modified = true;
                        continue;
                    }

                    if (block["contentElementTypeKey"] != null)
                    {
                        string key = block["contentElementTypeKey"]?.ToString() ?? "";
                        if (contentElementKeys.Contains(key))
                        {
                            blocksToRemove.Add(block);
                            modified = true;
                            File.AppendAllText(logPath, $"Duplicate contentElementTypeKey found and removed in file: {file} - Key: {key}\n");
                        }
                        else
                        {
                            contentElementKeys.Add(key);
                        }
                    }
                }
                foreach (var block in blocksToRemove)
                {
                    block.Remove();
                }
            }

            if (modified)
            {
                XmlCDataSection? newCdataSection = xmlDoc?.CreateCDataSection(jsonObject.ToString());
                if (configNode != null && newCdataSection != null)
                {
                    configNode.RemoveAll();
                    configNode.AppendChild(newCdataSection);

                    xmlDoc?.Save(file);
                    fileModifiedCount++;
                }
            }
        }
        catch
        {
            Console.WriteLine("We're in the catch");
        }
    }
}
Console.WriteLine($"Processing complete. 'areas' arrays have been emptied where found. Amount of files modified: {fileModifiedCount}.");