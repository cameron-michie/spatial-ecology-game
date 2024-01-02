using System.Security;
using System.Security.Cryptography.X509Certificates;
using IO.Ably;
using IO.Ably.Realtime;

namespace ReactionDiffusionLibrary;

public class User
{

    private double _energy = 50000;
    private double _energySinceLastDot = 0;
    public Grid.Color userColour = new Grid.Color();
    public double Energy
    {
        get { return _energy; }
        set
        {
            double energyChange = value - _energy;
            _energy = value;
            _energySinceLastDot += energyChange;
            CheckAndAddDot();
        }
    }

    public List<string> MoveQueue = new List<string>();
    public List<int> Xs = new List<int>();
    public List<int> Ys = new List<int>();
    public HashSet<(int,int)> Coords = new HashSet<(int, int)>();
    public List<(int,int)> PossibleCoords = new List<(int, int)>();
    public List<(int,int)> AddedCoords = new List<(int,int)>();
    public string ID;
    public User(string ClientID, Grid.Color color)
    {
        ID = ClientID;
        userColour = color;
        InitializePositionAndSize();
    }

    private void CheckAndAddDot()
    {
        if (_energySinceLastDot >= 500)
        {
            _energySinceLastDot -= 500;
            AddDot();
        }

        if (_energySinceLastDot <= -500)
        {
            _energySinceLastDot += 500;
            TakeDot();
        }
    }

    private void AddDot()
    {
        // Pick from PossibleCoords
        int x, y;
        (x, y) = SelectAndRemoveRandom(PossibleCoords);
        while (Coords.Contains((x,y))) 
        {
            if (PossibleCoords.Count > 1) (x, y) = SelectAndRemoveRandom(PossibleCoords);
            else return;
        }
            

        // Add (x,y) to Coords
        Coords.Add((x,y));
        Xs.Add(x);
        Ys.Add(y);

        // Update PossibleCoords
        PossibleCoords.AddRange(GetAdjacentPossibleCoords((x,y)));
        AddedCoords.Add((x,y));
    }
    private void TakeDot() 
    {
        int x = Xs[Xs.Count - 1];
        int y = Ys[Ys.Count - 1];
        Coords.Remove((x,y));
        Xs.RemoveAt(Xs.Count - 1);
        Ys.RemoveAt(Ys.Count - 1);

    }
    private void InitializePositionAndSize()
    {
        Random rand = new Random();
        int numberOfDots = (int)(Energy / 500) - 4; // One dot per 500 energy points

        int x = (int)Math.Truncate(rand.NextDouble() * (Grid.GridXSize - 1));
        int y = (int)Math.Truncate(rand.NextDouble() * (Grid.GridYSize - 1));

        Xs = new List<int> {x, x, x+1, x+1};
        Ys = new List<int> {y, y+1, y, y+1};
        UpdateCoords();

        for (int i=0; i < Xs.Count; i++) PossibleCoords.AddRange(GetAdjacentPossibleCoords((Xs[i], Ys[i])));
        for (int i = 0; i < numberOfDots; i++) AddDot();
    }

    public void Move(string direction)
    {
        // Energy -= Xs.Count * Ys.Count;

        // Calculate proposed new positions
        List<int> newXs = new List<int>(Xs);
        List<int> newYs = new List<int>(Ys);
        List<(int,int)> newPossibleCoords = new List<(int, int)>(PossibleCoords);

        switch (direction)
        {
            case "LEFT":
                if (Xs.All(x => x - 1 >= 0))
                    newXs = Xs.Select(x => x - 1).ToList();
                    newPossibleCoords = PossibleCoords.Select(coord => (coord.Item1 - 1, coord.Item2) ).ToList();
                break;
            case "RIGHT":
                if (Xs.All(x => x + 1 < Grid.GridXSize))
                    newXs = Xs.Select(x => x + 1).ToList();
                    newPossibleCoords = PossibleCoords.Select(coord => (coord.Item1 + 1, coord.Item2) ).ToList();
                break;
            case "UP":
                if (Ys.All(y => y - 1 >= 0))
                    newYs = Ys.Select(y => y - 1).ToList();
                    newPossibleCoords = PossibleCoords.Select(coord => (coord.Item1, coord.Item2 - 1) ).ToList();
                break;
            case "DOWN":
                if (Ys.All(y => y + 1 < Grid.GridYSize))
                    newYs = Ys.Select(y => y + 1).ToList();
                    newPossibleCoords = PossibleCoords.Select(coord => (coord.Item1, coord.Item2 + 1) ).ToList();
                break;
            case "STAY":
                break;
        }

        // Update positions if within boundaries
        if (IsWithinGrid((newXs.Min(), newYs.Min())) 
            && IsWithinGrid((newXs.Max(), newYs.Max())))
        {
            Xs = newXs;
            Ys = newYs;
            UpdateCoords();
            PossibleCoords = newPossibleCoords;
        }
        CheckAndAddDot();
    }

    public void UpdateCoords() {
        Coords = new HashSet<(int, int)>{};
        for (int i=0; i<Xs.Count; i++) Coords.Add((Xs[i], Ys[i]));        
    }
    public List<(int, int)> GetAdjacentPossibleCoords((int, int) coord)
    {
        var adjacentCoords = new List<(int, int)>();
        var (x, y) = coord;
        var positions = new List<(int, int)>
        {
            (x + 1, y),
            (x - 1, y),
            (x, y + 1),
            (x, y - 1)
        };

        foreach (var pos in positions) if (IsWithinGrid(pos) && !Coords.Contains(pos)) {
            adjacentCoords.Add(pos);
        }

        return adjacentCoords;
    }

    private bool IsWithinGrid((int, int) coord)
    {
        var (x, y) = coord;
        return x >= 0 && x < Grid.GridXSize && y >= 0 && y < Grid.GridYSize;
    }

    public static (int, int) SelectAndRemoveRandom(List<(int, int)> coords)
    {
        Random rand = new Random();
        int index = rand.Next(coords.Count);
        var selectedCoord = coords[index];
        coords.RemoveAt(index);
        return selectedCoord;
    }
    public struct Color
    {
        public byte R, G, B;
        public Color(byte r, byte g, byte b) { R = r; G = g; B = b; }
    }   
}