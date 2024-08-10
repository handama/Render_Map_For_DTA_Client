using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace Render_Map_For_DTA_Client
{
    static class Program
    {
        static void Main(string[] args)
        {
            var setting = new IniFile("settings.ini");

            string RENDERERPATH = setting.GetStringValue("settings", "RendererPath", @"..\Map Renderer\CNCMaps.Renderer.exe");
            string GAMEPATH = setting.GetStringValue("settings", "GamePath", @"..");
            bool RENDERERMAP = setting.GetBooleanValue("settings", "RenderMap", true);

            DirectoryInfo maps = new DirectoryInfo(System.Environment.CurrentDirectory);
            var directorys = maps.GetDirectories();

            if (File.Exists("output.txt"))
                File.Delete("output.txt");
            var output = new IniFile("output.txt");
            output.AddSection("MultiMaps");

            var MultiMaps = new List<string>();
            var MultiMapsOrder = new List<int>();

            foreach (var dir in directorys)
            {
                var files = dir.GetFiles();
                foreach(var file in files)
                {
                    if (file.Extension != ".map")// && file.Extension != ".yrm" && file.Extension != ".mpr")
                        continue;

                    var mapPath = file.FullName;
                    var mapName = file.Name.Split('.')[0];
                    if (RENDERERMAP)
                    {
                        Process MapRenderer = new Process();
                        MapRenderer.StartInfo.FileName = RENDERERPATH;
                        MapRenderer.StartInfo.Arguments = $@"-i ""{mapPath}"" -p -o ""{mapName}"" -m ""{GAMEPATH}"" -r ";
                        MapRenderer.Start();
                    }

                    var mapFile = new IniFile(mapPath);

                    var titleSplit = mapPath.Split('\\');
                    var mapSectionString = titleSplit[titleSplit.Length - 3] + "\\" + titleSplit[titleSplit.Length - 2] + "\\" + mapName;
                    output.AddSection(mapSectionString);
                    MultiMaps.Add(mapSectionString);

                    var mapSection = output.GetSection(mapSectionString);
                    var maxPlayer = mapFile.GetIntValue("Header", "NumberStartingPoints", 2);
                    MultiMapsOrder.Add(maxPlayer);

                    

                    mapSection.AddKey("Author", mapFile.GetStringValue("Basic", "Author", titleSplit[titleSplit.Length - 2]));
                    mapSection.AddKey("Size", mapFile.GetStringValue("Map", "Size", "0,0,50,50"));
                    mapSection.AddKey("LocalSize", mapFile.GetStringValue("Map", "LocalSize", "0,0,50,50"));
                    mapSection.AddKey("Description", $"[{maxPlayer}]{mapName}");

                    for (int i = 0; i < maxPlayer; i++)
                    {
                        string[] waypointLocation = mapFile.GetStringValue("Header", $"Waypoint{i + 1}", "0,0").Split(',');
                        double x = double.Parse(waypointLocation[0]);
                        double y = double.Parse(waypointLocation[1]);

                        if (x == 0 && y == 0)
                            continue;

                        x *= Math.Sqrt(2);
                        y *= Math.Sqrt(2);
                        x -= 256 * Math.Sqrt(2);

                        var temp = x;
                        x = y;
                        y = temp;

                        var length = Math.Sqrt(x * x + y * y);
                        var alpha = Math.Atan(y / x);
                        var beta = Math.PI / 4 + alpha;

                        int newX = (int)(Math.Cos(beta) * length);
                        int newY = (int)(Math.Sin(beta) * length);

                        mapSection.AddKey($"Waypoint{i}", string.Format("{0:D3}", newX) + string.Format("{0:D3}", newY));
                    }

                    mapSection.AddKey("MinPlayers", "1");
                    mapSection.AddKey("MaxPlayers", maxPlayer.ToString());
                    mapSection.AddKey("EnforceMaxPlayers", "True");
                    mapSection.AddKey("GameModes", "Standard");
                }
            }

            if (MultiMaps.Count > 1)
            {
                for (int i = 0; i < MultiMapsOrder.Count - 1; i++)
                {
                    for (int j = 0; j < MultiMapsOrder.Count - i - 1; j++)
                    {
                        if (MultiMapsOrder[j] > MultiMapsOrder[j + 1])
                        {
                            var tmp1 = MultiMapsOrder[j];
                            var tmp2 = MultiMaps[j];
                            MultiMapsOrder[j] = MultiMapsOrder[j + 1];
                            MultiMapsOrder[j + 1] = tmp1;

                            MultiMaps[j] = MultiMaps[j + 1];
                            MultiMaps[j + 1] = tmp2;
                        }
                    }
                }
            }

            var MultiMapsSetcion = output.GetSection("MultiMaps");
            for (int i = 0; i < MultiMaps.Count; i++)
            {
                MultiMapsSetcion.AddKey(i.ToString(), MultiMaps[i]);
            }

            output.WriteIniFile();
        }
    }
}
