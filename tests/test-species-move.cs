// using ReactionDiffusionLibrary;
// public class TestSpeciesMove
// {
//     public static void MainTest(string[] args)
//     {
//         // Create a grid
//         int gridSizeX = 10;
//         int gridSizeY = 10;
//         Grid testGrid = new Grid(gridSizeX, gridSizeY);

//         // Create a species object
//         int initialEnergy = 50;
//         int energyPerStep = 1;
//         int numberOfAgents = 5;
//         Species testSpecies = new Species("Prey", initialEnergy, energyPerStep, numberOfAgents, testGrid);

//         // Initialize agents in the species
//         Grid.InitialiseAgents(testSpecies);

//         // Display initial grid state
//         Console.WriteLine("Initial Grid State:");
//         testGrid.DisplayGridState(testGrid.AnimalsInGrid);

//         // Perform move operations
//         int numIterations = 3;
//         for (int i = 0; i < numIterations; i++)
//         {
//             testSpecies.Move();

//             // Display grid state after each move
//             Console.WriteLine($"Grid State after iteration {i + 1}:");
//             testGrid.DisplayGridState(testGrid.AnimalsInGrid);

//             // Clear grid for the next iteration
//             testGrid.AnimalsInGrid = testGrid.ClearGrid(testGrid.AnimalsInGrid);
//         }
//     }
// }