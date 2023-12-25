using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using IO.Ably;
using IO.Ably.Realtime;

namespace ReactionDiffusionLibrary;

public class Grid
{
    private Species PredSpecies;
    private Species PreySpecies;
    public static int GridXSize { get; set; }
    public static int GridYSize { get; set; }
    public List<List<int[]>> Coords { get; set; } = new List<List<int[]>>();
    public List<string> Directions { get; set; } = new List<string> { "LEFT", "RIGHT", "UP", "DOWN", "STAY" };
    public List<List<List<string>>> AnimalsInGrid { get; set; }
    public List<List<List<string>>> UsersInGrid { get; set; }

    private const int MaxUsers = 10;
    private int NumUsers = 0;
    private readonly object userCountLock = new object();
    public List<User> AllUsers { get; private set; } = new List<User>();

    IRealtimeChannel channel;
    IRealtimeChannel energyChannel;

    public Grid(int max_x, int max_y)
    {
        GridXSize = max_x;
        GridYSize = max_y;
        AnimalsInGrid = new List<List<List<string>>>();
        UsersInGrid = new List<List<List<string>>>();
        AnimalsInGrid = ClearGrid(AnimalsInGrid);
        UsersInGrid = ClearGrid(UsersInGrid);

        ClientOptions options = new ClientOptions("EKxBlQ.bVI3ng:10FVabjayrOT2uTY46zlgR1_DLLDWtqvGbLVaLy_BS8") { ClientId = "grid-client" };
        AblyRealtime realtime = new AblyRealtime(options);
        channel = realtime.Channels.Get("spatial-ecology-game");
        energyChannel = realtime.Channels.Get("energy-channel");
    }

    public void SetSpecies(Species predSpecies, Species preySpecies)
    {
        PredSpecies = predSpecies;
        PreySpecies = preySpecies;
    }

    // WHY ERRROR
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


    public List<List<List<string>>> ClearGrid(List<List<List<string>>> GridObj)
    {
        GridObj = new List<List<List<string>>>();
        for (int i = 0; i < GridXSize; i++)
                {
                    var innerList = new List<List<string>>();
                    for (int j = 0; j < GridYSize; j++)
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
        Stopwatch interactTimer = new Stopwatch();
        interactTimer.Start();
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
        interactTimer.Stop();
        Console.WriteLine($"Interact Time: {interactTimer.ElapsedMilliseconds} ms");

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
    Stopwatch publishStopwatch = new Stopwatch();
    string base64payload = VisualizeMap(this.AnimalsInGrid, this.UsersInGrid);

    // Measure Network Call
    publishStopwatch.Start();
    var result = await channel.PublishAsync("image-message", base64payload);
    publishStopwatch.Stop();

    if (result.IsFailure)
    {
        var error = result.Error;
        Console.WriteLine("Publish error: " + error);
    }

    // Output times
    Console.WriteLine($"PublishAsync Time: {publishStopwatch.ElapsedMilliseconds} ms");
}

     public async Task PresenceMembers()
    {
        var presenceMembers = await channel.Presence.GetAsync(true);
        Console.WriteLine($"There are {presenceMembers.Count()} members on this channel");
        foreach (var user in presenceMembers)
        {
            lock (userCountLock)
            {
                if (NumUsers < 10)  // Assuming you want the first 10 users
                {
                    Console.WriteLine("Member " + user.ClientId + " is already in the channel.");
                    AllUsers.Add(new User(user.ClientId));
                    NumUsers++;
                }
            }
        }

        channel.Presence.Subscribe(PresenceAction.Enter, member =>
        {
            lock (userCountLock)
            {
                if (NumUsers < MaxUsers)
                {
                    Console.WriteLine("Member " + member.ClientId + " entered.");
                    User thisUser = new User(member.ClientId);
                    AllUsers.Add(thisUser);
                    NumUsers++;
                }
            }
        });

        channel.Presence.Subscribe(PresenceAction.Leave, member =>
        {
            lock (userCountLock)
            {
                Console.WriteLine("Member " + member.ClientId + " left.");
                AllUsers.RemoveAll(user => user.ID == member.ClientId);
                NumUsers--;
            }
        });
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

    public void UpdateUsersInGrid()
    {
        UsersInGrid = ClearGrid(UsersInGrid);
        foreach (User user in AllUsers) {
            foreach (int x in user.Xs) {
                foreach (int y in user.Ys) {
                    if (x < GridXSize && y < GridYSize && x >= 0 && y >= 0) {
                        UsersInGrid[x][y].Add(user.ID);

                        List<string> agents = AnimalsInGrid[y][x];
                        if (agents.Count == 0) continue;
                        
                        List<int> preds = new List<int>();
                        List<int> preys = new List<int>();
                        (preys, preds) = AgentsInGridSpace(agents);

                        foreach (int predID in preds){
                            Agent pred = PredSpecies.AgentsList[predID];
                            user.Energy -= pred.Energy;
                            pred.AddToDeathList();
                        }

                        foreach (int preyID in preys){
                            Agent prey = PreySpecies.AgentsList[preyID];
                            user.Energy += prey.Energy;
                            prey.AddToDeathList();
                        }
                    } 
                }
            }
        }
        // DisplayGridState(UsersInGrid);
    }

    public void SubscribeToUserMoves()
    {
        foreach (User user in AllUsers)
        {
            channel.Subscribe($"{user.ID}-moves", (message) =>
            {
                string? direction = message.Data as string;
                if (direction != null)
                {
                    user.MoveQueue.Add(direction);
                }
            });
        }
        
    }

    public void MoveUsers()
    {
        foreach (User user in AllUsers) {
            if (user.MoveQueue.Count == 0) { continue; }
            user.Move(user.MoveQueue[0]);
            user.MoveQueue.RemoveAt(0);
        }
        UpdateUsersInGrid();
        PublishUserEnergyLevels();
    }

    private void PublishUserEnergyLevels()
    {
        foreach (User user in AllUsers)
        {
            var energyData = new { clientId = user.ID, energy = user.Energy };
            energyChannel.Publish("energy-update", energyData);
        } 
    }                  


    private string GetRandomDirection()
    {
        Random random = new Random();
        int index = random.Next(Directions.Count);
        return Directions[index];
    }

    public static string VisualizeAnimals(List<List<List<string>>> map)
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

    public static string VisualizeMap(List<List<List<string>>> AnimalGrid, List<List<List<string>>> UserGrid)
    {
        int width = AnimalGrid.Count;
        int height = AnimalGrid[0].Count;
        int rowPadding = (4 - (width * 3) % 4) % 4;
        int imageSize = (width * 3 + rowPadding) * height;
        int fileSize = 54 + imageSize;

        using (MemoryStream ms = new MemoryStream(fileSize))
        {
            BinaryWriter writer = new BinaryWriter(ms);

            // BMP File Header
            writer.Write(new char[] { 'B', 'M' });
            writer.Write(fileSize);
            writer.Write(0); // Reserved
            writer.Write(54); // Offset to pixel data

            // BMP Info Header
            writer.Write(40);
            writer.Write(width);
            writer.Write(height);
            writer.Write((short)1);
            writer.Write((short)24);
            writer.Write(0); // Compression
            writer.Write(imageSize);
            writer.Write(0); // X pixels per meter
            writer.Write(0); // Y pixels per meter
            writer.Write(0); // Total colors
            writer.Write(0); // Important colors

            // Pixel Data
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    var agents = AnimalGrid[x][y];
                    int greenCount = 0;
                    int redCount = 0;
                    foreach (var agent in agents)
                    {
                        if (agent.StartsWith("y")) greenCount++;
                        if (agent.StartsWith("d")) redCount++;
                    }

                    Color color = DetermineColor(greenCount, redCount, UserGrid[x][y].Count > 0);

                    writer.Write(color.B);
                    writer.Write(color.G);
                    writer.Write(color.R);
                }
                for (int p = 0; p < rowPadding; p++) writer.Write((byte)0);
            }

            writer.Flush();
            return Convert.ToBase64String(ms.ToArray());
        }
    }

    private static Color DetermineColor(int greenCount, int redCount, bool userPresent = false)
    {
        if (userPresent) {
            return new Color(33,10,10);
        }
        else if (greenCount >= redCount && greenCount > 0)
        {
            return greenCount > 1 ? new Color(0, 128, 0) : new Color(0, 204, 0); // Darker or lighter green
        }
        else if (redCount > greenCount)
        {
            return redCount > 1 ? new Color(128, 0, 0) : new Color(204, 0, 0); // Darker or lighter red
        }
        return new Color(255, 255, 255); // White for no agents
    }

    struct Color
    {
        public byte R, G, B;
        public Color(byte r, byte g, byte b) { R = r; G = g; B = b; }
    }
}
