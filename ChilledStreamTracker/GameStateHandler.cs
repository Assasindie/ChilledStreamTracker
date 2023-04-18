using System.Diagnostics;
using CSGSI;
using CSGSI.Nodes;

namespace ChilledStreamTracker;

public class GameStateHandler
{
    private string _kdInfo = "";
    private int _killsInRound;
    private bool _enteredFreezeTime;
    private bool _saidChilledDiedFirst;
    private int _clutchEnemies;
    private BombState _previousBombState = BombState.Undefined;
    private readonly Stopwatch _timeAlive;
    private readonly Stopwatch _timeDead;
    private readonly SpeechGenerator _speech;

    public GameStateHandler()
    {
        _timeAlive = Stopwatch.StartNew();
        _timeDead = new Stopwatch();
        var gameStateListener = new GameStateListener(3000);
        gameStateListener.NewGameState += HandleGameEvent;
        gameStateListener.EnableRaisingIntricateEvents = true;
        gameStateListener.Start();
        if (!gameStateListener.Running)
        {
            throw new Exception("check not running 2 versions or something cause it didnt start up");
        } 
        _speech = new SpeechGenerator();
    }

    private async void HandleGameEvent(GameState gs)
    {
        await HandleBombPlanted(gs);

        if (!IsPlayerChilled(gs))
        {
            return;
        }

        var alive = gs.Player.State.Health != 0;
        await HandlerTimersOnRoundEnd(gs);
        await HandleTimersOnDeath(alive);

        var chilledTeam = gs.Player.Team;
        var currentlyAliveEnemyTeam = gs.AllPlayers.Count(x => x.Team != chilledTeam && x.State.Health != 0);
        var currentlyAliveChilledTeam = gs.AllPlayers.Count(x => x.Team == chilledTeam && x.State.Health != 0);

        await CheckForClutching(alive, currentlyAliveEnemyTeam, currentlyAliveChilledTeam);
        await CheckForFirstDeath(alive, currentlyAliveEnemyTeam, currentlyAliveChilledTeam);

        if (_killsInRound < gs.Player.State.RoundKills)
        {
            _killsInRound = gs.Player.State.RoundKills;
            Console.WriteLine($"{DateTime.Now} chilled got a kill rare as hell total this round {_killsInRound}");
            await _speech.GenerateAndSpeak("Chilled has somehow gotten a kill", detailed: false);
        }

        _kdInfo =
            $"Kills {gs.Player.MatchStats.Kills}, Assists {gs.Player.MatchStats.Assists}, Deaths {gs.Player.MatchStats.Deaths}";
        await FileUtils.WriteFile(_kdInfo, _timeAlive.Elapsed, _timeDead.Elapsed);
    }

    private async Task HandleTimersOnDeath(bool alive)
    {
        if (!alive && !_timeDead.IsRunning)
        {
            Console.WriteLine($"{DateTime.Now} chilled dead as hell");
            _timeDead.Start();
            _timeAlive.Stop();
            await _speech.GenerateAndSpeak("chilled has died");
        }
        else if (alive && !_timeAlive.IsRunning)
        {
            Console.WriteLine($"{DateTime.Now} Chilled alive? how?");
            _timeAlive.Start();
            _timeDead.Stop();
        }
    }

    private async Task HandlerTimersOnRoundEnd(GameState gs)
    {
        if (gs.Round.Phase is RoundPhase.FreezeTime or RoundPhase.Undefined && !_enteredFreezeTime)
        {
            //reset all the random stuff to stop it spamming out
            _enteredFreezeTime = true;
            _saidChilledDiedFirst = false;
            _clutchEnemies = 0;
            
            Console.WriteLine($"{DateTime.Now} stopping timer round end, total kills {_killsInRound}");
            _timeDead.Stop();
            _timeAlive.Stop();
            
            await _speech.GenerateAndSpeak($"Chilled had a total of {_killsInRound} kills that round");
            _killsInRound = 0;

        }
        else if (gs.Round.Phase == RoundPhase.Live && _enteredFreezeTime)
        {
            _enteredFreezeTime = false;
            Console.WriteLine($"{DateTime.Now} resuming timer round restart");
            await _speech.GenerateAndSpeak($"A new round has started");
            
            _timeAlive.Start();
        }
    }

    private async Task CheckForClutching(bool currentlyAlive, int currentlyAliveEnemyTeam, int currentlyAliveChilledTeam)
    {
        if (currentlyAlive && currentlyAliveEnemyTeam > 0 && currentlyAliveChilledTeam == 1 &&
            currentlyAliveEnemyTeam != _clutchEnemies)
        {
            _clutchEnemies = currentlyAliveEnemyTeam;
            Console.WriteLine($"{DateTime.Now} Clutch attempt");
            await _speech.GenerateAndSpeak($"Chilled currently in a 1 on {currentlyAliveEnemyTeam}");
        }
    }

    private async Task CheckForFirstDeath(bool currentlyAlive, int currentlyAliveEnemyTeam, int currentlyAliveChilledTeam)
    {
        if (!currentlyAlive && currentlyAliveEnemyTeam == 5 && currentlyAliveChilledTeam == 4 && !_saidChilledDiedFirst)
        {
            _saidChilledDiedFirst = true;
            Console.WriteLine($"{DateTime.Now} First death");
            await _speech.GenerateAndSpeak("Chilled has died first probably because he rushed in by himself");
        }
    }

    async Task HandleBombPlanted(GameState gs)
    {
        //this dont work :/ ?
        if (gs.Bomb.State != _previousBombState)
        {
            switch (gs.Bomb.State)
            {
                case BombState.Planting:
                    await _speech.GenerateAndSpeak("Bomb is being planted");
                    _previousBombState = gs.Bomb.State;
                    break;
                case BombState.Planted:
                    await _speech.GenerateAndSpeak("Bomb has been planted");
                    _previousBombState = gs.Bomb.State;
                    break;
                case BombState.Defusing:
                    await _speech.GenerateAndSpeak("Bomb is being defused");
                    _previousBombState = gs.Bomb.State;
                    break;
                case BombState.Defused:
                    await _speech.GenerateAndSpeak("Bomb has been defused");
                    _previousBombState = gs.Bomb.State;
                    break;
                case BombState.Exploded:
                    await _speech.GenerateAndSpeak("Bomb has exploded");
                    _previousBombState = gs.Bomb.State;
                    break;
            }
        }
    }
    
    private static bool IsPlayerChilled(GameState gs) => gs.Player.SteamID == "76561198108536647";

    public async Task WriteCurrentGameStats()
    {
        await FileUtils.WriteFile(_kdInfo, _timeAlive.Elapsed, _timeDead.Elapsed);
    }

    public void ResetCurrentGameStats()
    {
        _kdInfo = "";
        _timeAlive.Restart();
        _timeDead.Reset();
    }
}