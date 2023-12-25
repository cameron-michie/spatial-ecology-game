namespace ReactionDiffusionLibrary;

public class Agent
{
    public Species ParentSpecies { get; private set; }
    public string AgentId { get; set; }
    public int AgentIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Energy { get; set; }
    public double StepSize = 1.0;

    public Agent(Species species, int agentNumber, double? energy = null) 
    {
        this.ParentSpecies = species;
        var yOrDCondition = (species.PredOrPrey == "Prey") ? 'y' : 'd';
        AgentId = $"{yOrDCondition}{agentNumber}";
        AgentIndex = agentNumber;
        X = ParentSpecies.SpeciesCoords[agentNumber][0];
        Y = ParentSpecies.SpeciesCoords[agentNumber][1];
        Random rnd = new Random();
        Energy = species.InitialEnergy * (1/2 + rnd.NextDouble());
    }

    public Agent(Species species, int agentNumber, Agent parent, double? energy = null) 
    {
        this.ParentSpecies = species;
        var yOrDCondition = (species.PredOrPrey == "Prey") ? 'y' : 'd';
        AgentId = $"{yOrDCondition}{agentNumber}";
        AgentIndex = agentNumber;
        X = parent.X;
        Y = parent.Y;
        ParentSpecies.SpeciesCoords.Add(new double[] {X, Y});
        Random rnd = new Random();
        if (energy != null) Energy = (double)energy;
        else Energy = parent.Energy/2;
    }

    public void AddToDeathList()
    {
        ParentSpecies.DeathList.Add(this);
    }

    public Agent Procreate(Agent other)
    {
        var baby = new Agent(ParentSpecies, this.AgentIndex, this, (Energy + other.Energy) / 2);
        Energy /= 3;
        other.Energy /= 3;
        ParentSpecies.Babies.Add(baby);
        ParentSpecies.SpeciesCoords.Add(new double[] {X, Y});
        return baby;
    }

    public Agent Procreate()
    {
        var baby = new Agent(ParentSpecies, this.AgentIndex, this, Energy / 2);
        Energy /= 2;
        ParentSpecies.Babies.Add(baby);
        ParentSpecies.SpeciesCoords.Add(new double[] {X, Y});
        return baby;
    }

    public void Move(string direction)
    {
        Energy -= StepSize;

        switch (direction)
        {
            case "LEFT":
                X += (X - StepSize > 0) ? -StepSize : StepSize;
                break;
            case "RIGHT":
                X += (X + StepSize < Grid.GridXSize - 1) ? StepSize : -StepSize;
                break;
            case "UP":
                Y += (Y + StepSize < Grid.GridYSize - 1) ? StepSize : -StepSize;
                break;
            case "DOWN":
                Y += (Y - StepSize > 0) ? -StepSize : StepSize;
                break;
            case "STAY":
                if (ParentSpecies.PredOrPrey == "Prey") Energy += Energy/8 + 12;
                if (ParentSpecies.PredOrPrey == "Predator") Energy += 4;
                break;
        }

        if (Energy <= 0) AddToDeathList();
    }

}