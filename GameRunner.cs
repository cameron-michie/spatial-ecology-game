using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SpatialEcology
{
    public static class GameRunner
    {
        [FunctionName("GameRunner")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. The game should now begin playing. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. The game should now begin playing This HTTP triggered function executed successfully.";
            
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

            bool gameReady = true;
            while (gameReady)
            {

                await TheGrid.BroadcastGameState();
                TheGrid.AnimalsInGrid = TheGrid.ClearGrid(TheGrid.AnimalsInGrid);

                Prey.Move();
                Pred.Move();
                TheGrid.Interact();
                TheGrid.MoveUsers();
                gameReady = TheGrid.KillUsers();
                Console.WriteLine($"{Prey.AgentsList.Count}  +{Prey.Babies.Count-Prey.DeathList.Count}   {Pred.AgentsList.Count}  +{Pred.Babies.Count-Pred.DeathList.Count}");

                Prey.NewDay();
                Pred.NewDay();
            }

            return new OkObjectResult(responseMessage);
        }
    }
}