// [Test]
// public void UpdateUsersInGrid_ShouldKillAgents()
// {
//     // Arrange
//     Grid testGrid = new Grid(10, 10); // Smaller grid for testing
//     Species testPrey = new Species("Prey", 50, 1, 10, testGrid); // Fewer agents
//     Species testPred = new Species("Predator", 200, 3, 10, testGrid);

//     testGrid.SetSpecies(testPred, testPrey);

//     // Set up the grid with predictable agent positions and interactions
//     // ...

//     int initialPreyCount = testPrey.AgentsList.Count;
//     int initialPredCount = testPred.AgentsList.Count;

//     // Act
//     testGrid.UpdateUsersInGrid();

//     // Assert
//     Assert.Less(testPrey.AgentsList.Count, initialPreyCount, "Prey count should decrease.");
//     Assert.Less(testPred.AgentsList.Count, initialPredCount, "Predator count should decrease.");
// }
