using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using IO.Ably;
using IO.Ably.Realtime;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;
using System;
using System.Linq;

namespace SpatialEcology;

public class Grid
{
    private Species PredSpecies;
    private Species PreySpecies;
    public static int GridXSize { get; set; }
    public static int GridYSize { get; set; }
    public List<List<int[]>> Coords { get; set; } = new List<List<int[]>>();
    public List<string> Directions { get; set; } = new List<string> { "LEFT", "RIGHT", "UP", "DOWN", "STAY" };
    public List<List<List<string>>> AnimalsInGrid { get; set; }
    IRealtimeChannel channel;
    IRealtimeChannel userDataChannel;
    private TaskCompletionSource<bool> _presenceReady = new TaskCompletionSource<bool>();
    public Grid(int max_x, int max_y)
    {
        GridXSize = max_x;
        GridYSize = max_y;
        AnimalsInGrid = new List<List<List<string>>>();
        AnimalsInGrid = ClearGrid(AnimalsInGrid);

        ClientOptions options = new ClientOptions("_iU5jA.gsQOlQ:j2PoEeg_BXZ5XteImdxyEoHv6f0rOl6J6BaDkEmPD-k") { ClientId = "grid-client" };
        AblyRealtime realtime = new AblyRealtime(options);
        channel = realtime.Channels.Get("spatial-ecology-game");
        userDataChannel = realtime.Channels.Get("user-data-channel");
    }
    public void SetSpecies(Species predSpecies, Species preySpecies)
    {
        PredSpecies = predSpecies;
        PreySpecies = preySpecies;
    }
    public (List<int>, List<int>) AgentsInGridSpace(List<string> agents) {
        
        List<int> preds = new List<int>();
        List<int> preys = new List<int>();

        foreach (string agentStr in agents)
        {
            if (agentStr[0] == 'y') preys.Add(int.Parse(agentStr.Substring(1)));
            if (agentStr[0] == 'd') preds.Add(int.Parse(agentStr.Substring(1)));
        }

        return (preys, preds);
    }
    public List<List<List<string>>> ClearGrid(List<List<List<string>>> GridObj, int MaxX=0, int MaxY=0)
    {
        if (MaxX ==0 && MaxY==0) {MaxX = GridXSize;  MaxY = GridYSize;} 
        GridObj = new List<List<List<string>>>();
        for (int i = 0; i < MaxX; i++)
                {
                    var innerList = new List<List<string>>();
                    for (int j = 0; j < MaxY; j++)
                    {
                        innerList.Add(new List<string>());
                    }
                    GridObj.Add(innerList);
                }
        return GridObj;
    }
    public void Interact()
    {
        int ySize = GridYSize;
        int xSize = GridXSize;
        for (int y_i = 0; y_i < ySize; y_i++)
        {
            for (int x_i = 0; x_i < xSize; x_i++)
            {
                List<string> agents = AnimalsInGrid[y_i][x_i];
                if (agents.Count == 0) continue;

                List<int> preds = new List<int>();
                List<int> preys = new List<int>();
                (preys, preds) = AgentsInGridSpace(agents);

                // Predators breed and feed on prey
                Procreate(preds, PredSpecies);
                for (int i = 0; i < preds.Count; i++)
                {
                    if (i < preys.Count)
                    {
                        Agent predator = PredSpecies.AgentsList[preds[i]];
                        Agent food = PreySpecies.AgentsList[preys[i]];
                        predator.Energy += food.Energy;
                        food.AddToDeathList();
                    }
                }

                // Preys procreate
                Procreate(preys, PreySpecies);

                // Check for special procreation cases
                if (PreySpecies.Dying && preys.Count > 0) SingleProcreation(preys[0], PreySpecies);
                if (PredSpecies.Dying && preds.Count > 0) SingleProcreation(preds[0], PredSpecies);
            }
        }
    }
    public static void Procreate(List<int> speciesList, Species speciesObj)
    {
        for (int i = 0; i < speciesList.Count - 1; i += 2)
        {
            Agent mum = speciesObj.AgentsList[speciesList[i]];
            Agent dad = speciesObj.AgentsList[speciesList[i + 1]];
            mum.Energy -= speciesList.Count;
            dad.Energy -= speciesList.Count;
            if (mum.Energy + dad.Energy > speciesObj.MinProcreationEnergy) mum.Procreate(dad);
        }
    }
    public static void InitialiseAgents(Species speciesObj)
    {
        for (int i = 0; i < speciesObj.NumAgents; i++)
        {
            Agent thisAgent = new Agent(speciesObj, i);
            speciesObj.AgentsList.Add(thisAgent);
            int agentX = (int)speciesObj.SpeciesCoords[i][0];
            int agentY = (int)speciesObj.SpeciesCoords[i][1];
            string yOrDCondition = (speciesObj.PredOrPrey == "Prey") ? "y" : "d";
            speciesObj.Grid.AnimalsInGrid[agentX][agentY].Add($"{yOrDCondition}{i}");
        }
    }
    public static void SingleProcreation(int agentId, Species speciesObj)
    {
        Agent mum = speciesObj.AgentsList[agentId];
        if (mum.Energy > speciesObj.MinProcreationEnergy) mum.Procreate();
    }
    public async Task BroadcastGameState()
    {
        string base64payload = VisualizeMap(this.AnimalsInGrid);
        var result = await channel.PublishAsync("image-message", base64payload);

    }
    public void DisplayGridState(List<List<List<string>>> grid)
    {
        for (int x = 0; x < grid.Count; x++)
        {
            for (int y = 0; y < grid[x].Count; y++)
            {
                if (grid[x][y].Count > 0)
                {
                    Console.Write($"[{string.Join(",", grid[x][y])}] ");
                }
                else
                {
                    Console.Write("[ ] ");
                }
            }
            Console.WriteLine();
        }
    }
    public string VisualizeMap(List<List<List<string>>> map)
    {
        int width = map.Count;
        int height = map[0].Count;
        int rowPadding = (4 - (width * 3) % 4) % 4;
        int imageSize = (width * 3 + rowPadding) * height;
        int fileSize = 54 + imageSize; // 54 bytes for headers

        using (MemoryStream ms = new MemoryStream())
        {
            BinaryWriter writer = new BinaryWriter(ms);

            // BMP File Header
            writer.Write(new char[] { 'B', 'M' }); // BM
            writer.Write(fileSize);
            writer.Write((int)0); // Reserved
            writer.Write(54); // Offset to start of pixel data

            // BMP Info Header
            writer.Write(40); // Info header size
            writer.Write(width);
            writer.Write(height);
            writer.Write((short)1); // Planes
            writer.Write((short)24); // Bits per pixel
            writer.Write((int)0); // Compression (none)
            writer.Write(imageSize);
            writer.Write(0); // X pixels per meter (not specified)
            writer.Write(0); // Y pixels per meter (not specified)
            writer.Write(0); // Total colors (default)
            writer.Write(0); // Important colors (default)

            // Pixel Data
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    var agents = map[x][y];
                    var greenCount = agents.Count(a => a.StartsWith("y"));
                    var redCount = agents.Count(a => a.StartsWith("d"));

                    Color color = DetermineColor(greenCount, redCount);

                    writer.Write(color.B);
                    writer.Write(color.G);
                    writer.Write(color.R);
                }
                for (int p = 0; p < rowPadding; p++) writer.Write((byte)0); // Padding
            }

            writer.Flush();
            return Convert.ToBase64String(ms.ToArray());
        }
    }
    private static Color DetermineColor(int greenCount, int redCount)
    {
        if (greenCount >= redCount && greenCount > 0)
        {
            return greenCount > 1 ? new Color(0, 128, 0) : new Color(0, 204, 0); // Darker or lighter green
        }
        else if (redCount > greenCount)
        {
            return redCount > 1 ? new Color(128, 0, 0) : new Color(204, 0, 0); // Darker or lighter red
        }
        return new Color(255, 255, 255); // White for no agents
    }
    public struct Color
    {
        public byte R, G, B;
        public Color(byte r, byte g, byte b) { R = r; G = g; B = b; }

        // Method to convert Color to string
        public override string ToString()
        {
            return $"{R},{G},{B}";
        }

        // Static method to parse a string into a Color object
        public static Color Parse(string colorString)
        {
            var parts = colorString.Split(',');
            if (parts.Length != 3) throw new FormatException("Invalid color format");

            byte r = byte.Parse(parts[0]);
            byte g = byte.Parse(parts[1]);
            byte b = byte.Parse(parts[2]);

            return new Color(r, g, b);
        }
    }
    public async Task UpdateGame(string update) {
        await userDataChannel.PublishAsync("game-update", update);
    }
}