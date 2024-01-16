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
    public List<List<List<string>>> UsersInGrid { get; set; }
    List<User> KillList = new List<User>(){};
    private const int MaxUsers = 10;
    private int NumUsers = 0;
    private readonly object userCountLock = new object();
    public List<User> AllUsers { get; set; } = new List<User>();
    IRealtimeChannel channel;
    IRealtimeChannel userDataChannel;
    private TaskCompletionSource<bool> _presenceReady = new TaskCompletionSource<bool>();
    public Grid(int max_x, int max_y)
    {
        GridXSize = max_x;
        GridYSize = max_y;
        AnimalsInGrid = new List<List<List<string>>>();
        UsersInGrid = new List<List<List<string>>>();
        AnimalsInGrid = ClearGrid(AnimalsInGrid);
        UsersInGrid = ClearGrid(UsersInGrid);

        ClientOptions options = new ClientOptions("_iU5jA.gsQOlQ:j2PoEeg_BXZ5XteImdxyEoHv6f0rOl6J6BaDkEmPD-k") { ClientId = "grid-client" };
        // options.LogLevel = LogLevel.Debug;
        // options.LogHandler = new CustomLogHandler();
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
        Stopwatch publishStopwatch = new Stopwatch();
        string base64payload = VisualizeMap(this.AnimalsInGrid, this.UsersInGrid);

        publishStopwatch.Start();
        var result = await channel.PublishAsync("image-message", base64payload);
        publishStopwatch.Stop();

    }
    public async Task PresenceMembers()
    {
        var presenceMembers = await userDataChannel.Presence.GetAsync(true);
        Console.WriteLine($"There are {presenceMembers.Count()} members on this channel");
        List<Color> AvailableColors = new List<Color> {
            new Color(34,56,200),
            new Color(100, 170, 200),
            new Color(30,100,230),
            new Color(150,100,255),
            new Color(200, 200, 200),
            new Color(143,87,100)
            //  Some more
        };
        foreach (var user in presenceMembers)
        {
            lock (userCountLock)
            {
                if (NumUsers < AvailableColors.Count)  // Assuming you want the first 10 users
                {
                    Console.WriteLine("Member " + user.ClientId + " is already in the channel.");
                    User thisUser = new User(user.ClientId, AvailableColors[0]);
                    AllUsers.Add(thisUser);
                    AvailableColors.RemoveAt(0);
                    NumUsers++;
                }
            }
        }

        if (NumUsers >= 2) _presenceReady.TrySetResult(true);

        userDataChannel.Presence.Subscribe(PresenceAction.Enter, member =>
        {
            lock (userCountLock)
            {
                if (NumUsers < AvailableColors.Count)
                {
                    Console.WriteLine("Member " + member.ClientId + " entered.");
                    User thisUser = new User(member.ClientId, AvailableColors[0]);
                    AllUsers.Add(thisUser);
                    AvailableColors.RemoveAt(0);
                    NumUsers++;
                }

                if (NumUsers >= 2) _presenceReady.TrySetResult(true);
            }
        });

        userDataChannel.Presence.Subscribe(PresenceAction.Leave, member =>
        {
            lock (userCountLock)
            {
                Console.WriteLine("Member " + member.ClientId + " left.");
                User thisUser = AllUsers.FirstOrDefault(user => user.ID == member.ClientId);

                if (thisUser != null)
                {
                    AvailableColors.Add(thisUser.userColour);
                    AllUsers.RemoveAll(user => user.ID == thisUser.ID);
                    NumUsers--;
                }
                else
                {
                    Console.WriteLine("User not found: " + member.ClientId);
                }
            }
        });
        // Await two presence members on channel
        await _presenceReady.Task;
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
    // Create a copy of AllUsers for safe iteration
    var usersCopy = new List<User>(AllUsers);

    foreach (User user in usersCopy)
    {
        var Xs_temp = new List<int>(user.Xs);
        var Ys_temp = new List<int>(user.Ys);
        foreach (var coord in Xs_temp.Zip(Ys_temp, Tuple.Create))
        {
            int x = coord.Item1;
            int y = coord.Item2;

            if (x < GridXSize && y < GridYSize && x >= 0 && y >= 0)
            {
                UsersInGrid[x][y].Add(user.userColour.ToString());
                var numUsers = UsersInGrid[x][y].Count;
                var numAgents = AnimalsInGrid[x][y].Count;
                if (numUsers > 0 && numAgents > 0) InteractUsersAgents(x, y, user);
                if (numUsers > 1) InteractUsersUsers(x, y, user);
            }
        }
    }
    // Optionally, update AllUsers with changes if needed
    AllUsers = usersCopy;
    }
    public void InteractUsersAgents(int x, int y, User thisUser)
    {
        List<string> agents = AnimalsInGrid[x][y];
        
        List<int> preds = new List<int>();
        List<int> preys = new List<int>();
        (preys, preds) = AgentsInGridSpace(agents);

        foreach (int predID in preds){
            Agent pred = PredSpecies.AgentsList[predID];
            thisUser.Energy -= pred.Energy;
            pred.AddToDeathList();
        }

        foreach (int preyID in preys){
            Agent prey = PreySpecies.AgentsList[preyID];
            thisUser.Energy += prey.Energy;
            prey.AddToDeathList();
        }
    }
    public void InteractUsersUsers(int x, int y, User thisUser)
    {
        var usersInSpace = UsersInGrid[x][y];
        usersInSpace.Remove(thisUser.userColour.ToString());
        string otherUserColor = usersInSpace[0];
        // Get User otherUser from its ID
        User otherUser = AllUsers.FirstOrDefault(user => user.userColour.ToString() == otherUserColor);
        
        if (KillList.Contains(thisUser) || KillList.Contains(otherUser)) return;
        if (otherUser.Energy >= thisUser.Energy) {
            Console.WriteLine(otherUser.ID + " kills " + thisUser.ID);
            this.PreySpecies.InitialiseAgents(thisUser.Coords.Count, thisUser.Xs, thisUser.Ys);
            otherUser.Energy += thisUser.Energy;
            KillList.Add(thisUser);
        } else {
            Console.WriteLine(thisUser.ID + " kills " + otherUser.ID);
            this.PreySpecies.InitialiseAgents(otherUser.Coords.Count, otherUser.Xs, otherUser.Ys);
            thisUser.Energy += otherUser.Energy;
            KillList.Add(otherUser);
        }
    }
    public void SubscribeToUserMoves()
    {
        foreach (User user in AllUsers)
        {
            userDataChannel.Subscribe($"{user.ID}-moves", (message) =>
            {
                
                string? direction = message.Data as string;
                // Console.WriteLine($"Published direction {direction} to {user.ID}");
                if (direction != null)
                {
                    user.MoveQueue.Add(direction);
                }
            });
        }
        
    }
    public void MoveUsers()
    {
        var usersCopy = new List<User>(AllUsers);
        foreach (User user in usersCopy) 
        {
            int size = user.Xs.Count * user.Ys.Count;
            UpdateUsersInGrid();
            var allMoves = new List<string> (user.MoveQueue);
            user.MoveQueue.Clear();
            if (allMoves.Count == 0) { PublishUserData(user); continue; }
            int maxMoves = Math.Max(1, 300000 / size);
            for (int i=0; i < Math.Min(allMoves.Count, maxMoves); i++)
            {
                user.Move(allMoves[i]);
                UpdateUsersInGrid();
            }
            
            PublishUserData(user);
            
        }
    }
    public void PublishUserData(User user) 
    {
        int minX = user.Xs.Min();
        List<int> Xs_scaled = user.Xs.Select(x => x - minX).ToList();

        int minY = user.Ys.Min();
        List<int> Ys_scaled = user.Ys.Select(y => y - minY).ToList();

        int maxX = Xs_scaled.Max()+1;
        int maxY = Ys_scaled.Max()+1;

        List<List<List<string>>> littleGrid = new List<List<List<string>>>();
        littleGrid = ClearGrid(littleGrid, maxX, maxY);
        foreach (var coord in Xs_scaled.Zip(Ys_scaled, Tuple.Create))
        {
            int x = coord.Item1;
            int y = coord.Item2;
            littleGrid[x][y].Add(user.userColour.ToString());
        }

        string littleBinary64EncodedPayload = VisualizeMap(littleGrid);

        var data = new 
        { 
            clientId = user.ID, 
            energy = Math.Round(user.Energy),
            characterMacro = littleBinary64EncodedPayload,
            numPrey = PreySpecies.AgentsList.Count.ToString(),
            numPred = PredSpecies.AgentsList.Count.ToString()
        };

        userDataChannel.Publish("user-data-update", data);
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
                    var userCount = agents.Count(a => Char.IsDigit(a[0]));

                    Color color = DetermineColor(greenCount, redCount);
                    if (userCount > 0) color = Color.Parse(agents[0]);

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
    public string VisualizeMap(List<List<List<string>>> AnimalGrid, List<List<List<string>>> UserGrid)
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
                    var users = UsersInGrid[x][y];
                    int greenCount = 0;
                    int redCount = 0;
                    foreach (var agent in agents)
                    {
                        if (agent.StartsWith("y")) greenCount++;
                        if (agent.StartsWith("d")) redCount++;
                    }
                    
                    Color color = DetermineColor(greenCount, redCount);
                    if (users.Count > 0) { 
                        color = Color.Parse(users[0]); }

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
    public bool KillUsers() 
    {
        foreach (User user in KillList) {
            AllUsers.Remove(user);
            RemoveUserFromGrid(user);
        }
        KillList.Clear();
        return AllUsers.Count > 1;
    }
    private void RemoveUserFromGrid(User user)
    {
        foreach (var x in user.Xs)
        {
            foreach (var y in user.Ys)
            {
                if (x < GridXSize && y < GridYSize && x >= 0 && y >= 0)
                {
                    UsersInGrid[x][y].Remove(user.userColour.ToString());
                }
            }
        }
    }
}