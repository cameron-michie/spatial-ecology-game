// namespace ReactionDiffusionLibrary;

// class ProgramTest
// {
//     static void MainTest(string[] args)
//     {
//         Grid theGrid = new Grid(10, 10); // Example grid size

//         // Simulate adding users
//         for (int i = 0; i < 10; i++)
//         {
//             theGrid.AllUsers.Add(new User(i));
//         }

//         // Simulate user movements and grid updates
//         for (int i = 0; i < 5; i++) // Simulate 5 iterations
//         {
//             theGrid.MoveUsers();
//             theGrid.UpdateUsersInGrid();

//             // Display the grid state
//             Console.WriteLine($"Iteration {i + 1}:");
//             DisplayGridState(theGrid.UsersInGrid);
//         }
//     }

//     static void DisplayGridState(List<List<List<string>>> grid)
//     {
//         for (int x = 0; x < grid.Count; x++)
//         {
//             for (int y = 0; y < grid[x].Count; y++)
//             {
//                 if (grid[x][y].Count > 0)
//                 {
//                     Console.Write($"[{string.Join(",", grid[x][y])}] ");
//                 }
//                 else
//                 {
//                     Console.Write("[ ] ");
//                 }
//             }
//             Console.WriteLine();
//         }
//     }
// }
