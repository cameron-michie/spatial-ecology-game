using System.Runtime.Serialization;
using Microsoft.VisualBasic;

namespace ReactionDiffusionLibrary;

public class Species
{
    public string PredOrPrey { get; set; }
    public double InitialEnergy { get; set; }
    public double MinProcreationEnergy { get; set; }
    public int NumAgents { get; set; }
    public Grid Grid { get; set; }
    public List<Agent> AgentsList { get; set; } = new List<Agent>();
    public List<Agent> DeathList { get; set; } = new List<Agent>();
    public List<Agent> Babies { get; set; } = new List<Agent>();
    public List<double[]> SpeciesCoords { get; set; } = new List<double[]>();
    public bool Dying { get; set; } = new bool();

    public Species(string predOrPrey, double initialEnergy, double minProcreationEnergy, int InputNumAgents, Grid grid)
    {
        PredOrPrey = predOrPrey;
        InitialEnergy = initialEnergy;
        MinProcreationEnergy = minProcreationEnergy;
        NumAgents = InputNumAgents;
        Grid = grid;
        InitialiseAgents();
    }

    public void Move()
    {
        if (AgentsList.Count < 1000) Dying = true;
        else Dying = false;

        List<double[]> SpeciesCoords = new List<double[]>(AgentsList.Count);
        Random random = new Random();

        for (int i = 0; i < AgentsList.Count; i++)
        {
            var agent = AgentsList[i];
            int dirIndex = random.Next(0, 5);
            agent.Move(Grid.Directions[dirIndex]);

            SpeciesCoords.Add(new double[] {agent.X, agent.Y});
            char yOrDCondition = PredOrPrey == "Prey" ? 'y' : 'd';
            agent.AgentId = $"{yOrDCondition}{i}";
            agent.AgentIndex = i; 

            Grid.AnimalsInGrid[(int)agent.X][(int)agent.Y].Add(agent.AgentId);
        }
    }

    public void InitialiseAgents()
    {
        for (int i = 0; i < this.NumAgents; i++)
        {
            Random rand = new Random();
            double x = Math.Truncate(rand.NextDouble() * (Grid.GridXSize - 1));
            double y = Math.Truncate(rand.NextDouble() * (Grid.GridYSize - 1));
            SpeciesCoords.Add(new double[] { x, y });

            Agent thisAgent = new Agent(this, i, InitialEnergy);
            this.AgentsList.Add(thisAgent);
            int agentX = (int)x;
            int agentY = (int)y;
            string yOrDCondition = (this.PredOrPrey == "Prey") ? "y" : "d";
            this.Grid.AnimalsInGrid[agentX][agentY].Add($"{yOrDCondition}{i}");
        }
    }

    // This overload for making killed players explode into agents
    public void InitialiseAgents(List<int> Xs, List<int> Ys)
    {
        for (int i = 0; i < 30; i++)
        {
            Random random = new Random();
            int x = Xs[random.Next(Xs.Count)];
            int y = Ys[random.Next(Ys.Count)];
            SpeciesCoords.Add(new double[] { x,y });

            Agent thisAgent = new Agent(this, i, InitialEnergy);
            this.AgentsList.Add(thisAgent);
            int agentX = (int)x;
            int agentY = (int)y;
            string yOrDCondition = (this.PredOrPrey == "Prey") ? "y" : "d";
            this.Grid.AnimalsInGrid[agentX][agentY].Add($"{yOrDCondition}{i}");
        }
    }

    public void NewDay()
    {

        foreach (var baby in Babies) AgentsList.Add(baby);
        Babies.Clear();

        List<int> deathIndices = DeathList.Select(slain => slain.AgentIndex).ToList();
        foreach (var index in deathIndices.Distinct().OrderByDescending(i => i)) AgentsList.RemoveAt(index);
        DeathList.Clear();
        
    }
}