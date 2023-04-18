using ChilledStreamTracker;

var gameHandler = new GameStateHandler();
var timer = new System.Timers.Timer(500);
timer.Elapsed += async ( _ , _ ) => await gameHandler.WriteCurrentGameStats();
timer.Start();

while (true)
{
    var text = Console.ReadLine();
    if (text == "restart")
    {
        gameHandler.ResetCurrentGameStats();
    }
}