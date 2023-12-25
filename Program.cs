// // File: Program.cs

/*
Average Time (ms) - 
Prey.Move: 15.612478, 
Pred.Move: 4.2246476, 
MoveUsers: 2.7307867999999997, 
Interact: 4.269799, 
BroadcastGameState: 840.2903634, 
NewDayPrey: 13.031925, 
NewDayPred: 2.747202, 
ClearGrid: 3.6080398
*/


namespace ReactionDiffusionLibrary;

class Program
{
    static async Task Main(string[] args)
    {

        int gridxsize = 50; // set to 120
        int gridysize = 50;

        int Prey_E0 = 50; 
        int Prey_EP = 1;
        int Prey_N = 1000; // 1000

        int Pred_E0 = 200;
        int Pred_EP = 3;
        int Pred_N = 1000;// 1000

        Grid TheGrid = new Grid(gridxsize, gridysize);
        await TheGrid.PresenceMembers();
        TheGrid.SubscribeToUserMoves();

        Species Prey = new Species("Prey", Prey_E0, Prey_EP, Prey_N, TheGrid);
        Species Pred = new Species("Predator", Pred_E0, Pred_EP, Pred_N, TheGrid);
        TheGrid.SetSpecies(Pred, Prey);
  
        int numIterations = 5000;
        for (int i = 0; i < numIterations; i++)
        {
            await TheGrid.BroadcastGameState();
            TheGrid.AnimalsInGrid = TheGrid.ClearGrid(TheGrid.AnimalsInGrid);

            Prey.Move();
            Pred.Move();
            TheGrid.Interact();
            TheGrid.MoveUsers();

            // At the end of the turn, make babies into adults and handle deaths
            Console.WriteLine($"{Prey.AgentsList.Count}  +{Prey.Babies.Count-Prey.DeathList.Count}   {Pred.AgentsList.Count}  +{Pred.Babies.Count-Pred.DeathList.Count}");
            Prey.NewDay();
            Pred.NewDay();
            }
        Thread.Sleep(5000);
    }
}  

