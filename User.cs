using IO.Ably;
using IO.Ably.Realtime;

namespace ReactionDiffusionLibrary;

public class User
{

    public double Energy=10000;

    public List<string> MoveQueue = new List<string>();
    public List<int> Xs;
    public List<int> Ys;
    public string ID;
    public User(string ClientID)
    {
        ID = ClientID;
        Random rand = new Random();
        int x = (int)Math.Truncate(rand.NextDouble() * (Grid.GridXSize - 2));
        int y = (int)Math.Truncate(rand.NextDouble() * (Grid.GridYSize - 2));
        Xs = new List<int>() {x, x+1, x+2, x+3, x+4};
        Ys = new List<int>() {y, y+1, y+2, y+3, y+4};        
    }

   public void Move(string direction)
    {
        Energy -= Xs.Count * Ys.Count;

        switch (direction)
        {
            case "LEFT":
                if (Xs.All(x => x - 1 >= 0))
                {
                    for (int i = 0; i < Xs.Count; i++)
                    {
                        Xs[i] -= 1;
                    }
                }
                break;

            case "RIGHT":
                if (Xs.All(x => x + 1 < Grid.GridXSize))
                {
                    for (int i = 0; i < Xs.Count; i++)
                    {
                        Xs[i] += 1;
                    }
                }
                break;

            case "UP":
                if (Ys.All(y => y + 1 <= Grid.GridYSize))
                {
                    for (int i = 0; i < Ys.Count; i++)
                    {
                        Ys[i] += -1;
                    }
                }
                break;

            case "DOWN":
                if (Ys.All(y => y - 1 >= 0))
                {
                    for (int i = 0; i < Ys.Count; i++)
                    {
                        Ys[i] += 1;
                    }
                }
                break;
 
            case "STAY": break;
        }

        // if (Energy <= 0) Kill();
    }


    public void Level2() {}

    public void Level3() {}
    public void Kill(){
        // You have sadly perished
        // Write to console
    }

}