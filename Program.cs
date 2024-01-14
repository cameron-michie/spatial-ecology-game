// // // File: Program.cs

// /*
// Average Time (ms) - 
// Prey.Move: 15.612478, 
// Pred.Move: 4.2246476, 
// MoveUsers: 2.7307867999999997, 
// Interact: 4.269799, 
// BroadcastGameState: 840.2903634, 
// NewDayPrey: 13.031925, 
// NewDayPred: 2.747202, 
// ClearGrid: 3.6080398
// */

//  Did another set of testing
// Average Time (ms) - BroadcastGameState: 529.6970558139535
// Average Time (ms) - ClearGrid: 2.823074418604651
// Average Time (ms) - Prey.Move: 0.9749139534883722
// Average Time (ms) - Pred.Move: 1.7045279069767443
// Average Time (ms) - Interact: 3.2855279069767445
// Average Time (ms) - MoveUsers: 9.320623255813954
// Average Time (ms) - NewDayPrey: 0.31241162790697674
// Average Time (ms) - NewDayPred: 0.11546046511627908

namespace ReactionDiffusionLibrary;

class Program
{
    static async Task Main(string[] args)
    {

        int gridxsize = 120; // set to 120
        int gridysize = 120;

        int Prey_E0 = 50; 
        int Prey_EP = 10;
        int Prey_N = 1000; // 1000

        int Pred_E0 = 200;
        int Pred_EP = 40;
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
            TheGrid.KillUsers();

            Console.WriteLine($"{Prey.AgentsList.Count}  +{Prey.Babies.Count-Prey.DeathList.Count}   {Pred.AgentsList.Count}  +{Pred.Babies.Count-Pred.DeathList.Count}");
            Prey.NewDay();
            Pred.NewDay();
            // Thread.Sleep(5000);
        }
    }
}  

// using System;
// using System.Threading.Tasks;
// using System.Collections.Generic;
// using System.Diagnostics;

// namespace ReactionDiffusionLibrary
// {
//     class Program
//     {
//         static async Task Main(string[] args)
//         {
//             int gridxsize = 120; // set to 120
//             int gridysize = 120;

//             int Prey_E0 = 50; 
//             int Prey_EP = 1;
//             int Prey_N = 1000; // 1000

//             int Pred_E0 = 200;
//             int Pred_EP = 3;
//             int Pred_N = 1000;// 1000

//             Grid TheGrid = new Grid(gridxsize, gridysize);
//             await TheGrid.PresenceMembers();
//             TheGrid.SubscribeToUserMoves();

//             Species Prey = new Species("Prey", Prey_E0, Prey_EP, Prey_N, TheGrid);
//             Species Pred = new Species("Predator", Pred_E0, Pred_EP, Pred_N, TheGrid);
//             TheGrid.SetSpecies(Pred, Prey);

//             // Dictionary to hold cumulative time for each function
//             Dictionary<string, TimeSpan> totalTime = new Dictionary<string, TimeSpan>();
//             Dictionary<string, int> callCount = new Dictionary<string, int>();

//             int numIterations = 100;
//             for (int i = 0; i < numIterations; i++)
//             {
//                 // Measure time for BroadcastGameState
//                 Stopwatch sw = Stopwatch.StartNew();
//                 await TheGrid.BroadcastGameState();
//                 sw.Stop();
//                 UpdateTime("BroadcastGameState", sw.Elapsed, totalTime, callCount);

//                 // Clear Grid
//                 sw.Restart();
//                 TheGrid.AnimalsInGrid = TheGrid.ClearGrid(TheGrid.AnimalsInGrid);
//                 sw.Stop();
//                 UpdateTime("ClearGrid", sw.Elapsed, totalTime, callCount);

//                 // Prey Move
//                 sw.Restart();
//                 Prey.Move();
//                 sw.Stop();
//                 UpdateTime("Prey.Move", sw.Elapsed, totalTime, callCount);

//                 // Pred Move
//                 sw.Restart();
//                 Pred.Move();
//                 sw.Stop();
//                 UpdateTime("Pred.Move", sw.Elapsed, totalTime, callCount);

//                 // Interact
//                 sw.Restart();
//                 TheGrid.Interact();
//                 sw.Stop();
//                 UpdateTime("Interact", sw.Elapsed, totalTime, callCount);

//                 // MoveUsers
//                 sw.Restart();
//                 TheGrid.MoveUsers();
//                 sw.Stop();
//                 UpdateTime("MoveUsers", sw.Elapsed, totalTime, callCount);

//                 // NewDay for Prey
//                 sw.Restart();
//                 Prey.NewDay();
//                 sw.Stop();
//                 UpdateTime("NewDayPrey", sw.Elapsed, totalTime, callCount);

//                 // NewDay for Pred
//                 sw.Restart();
//                 Pred.NewDay();
//                 sw.Stop();
//                 UpdateTime("NewDayPred", sw.Elapsed, totalTime, callCount);

//                 // Print average times
//             foreach (var entry in totalTime)
//             {
//                 Console.WriteLine($"Average Time (ms) - {entry.Key}: {entry.Value.TotalMilliseconds / callCount[entry.Key]}");
//             }

//             }

            
//             static void UpdateTime(string functionName, TimeSpan elapsed, Dictionary<string, TimeSpan> totalTime, Dictionary<string, int> callCount)
//             {
//                 if (!totalTime.ContainsKey(functionName))
//                 {
//                     totalTime.Add(functionName, new TimeSpan());
//                     callCount.Add(functionName, 0);
//                 }

//                 totalTime[functionName] += elapsed;
//                 callCount[functionName]++;
//             }
//         }

//     }

    
// }
