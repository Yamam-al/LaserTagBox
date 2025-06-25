using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LaserTagBox.Model.Shared;
using Mars.Interfaces.Environments;

namespace LaserTagBox.Model.Mind
{
    public class SupermanMind : AbstractPlayerMind
    {
        private QTableManagerSuperman _qManager;
        private SupermanState _state;
        internal bool HasMoved;

        // State properties
        internal List<EnemySnapshot> Enemies;
        internal List<Position> Hills;
        internal List<Position> Ditches;
        internal List<FriendSnapshot> Friends;
        private List<FlagSnapshot> _flags;
        private int _remainingShotsBeforeReload;
        private List<Position> _barrels;

        public bool HasCapturedFlagThisTick { get; set; }
        public bool HasCapturedFlagPreviosly { get; set; }

        public override void Init(PlayerMindLayer mindLayer)
        {
            _qManager = new QTableManagerSuperman();
            _qManager.LoadQTable();
            HasCapturedFlagThisTick = false;
        }

        public override void Tick()
        {
            HasMoved = false;
            UpdateSenses();
            _state = new SupermanState(this);

            while (Body.ActionPoints > 0)
            {
                int actionIdx = _qManager.GetBestAction(_state);
                DoAction(actionIdx);

                // Q-Learning Update nach jeder Action
                double reward = GetReward(actionIdx);
                _qManager.UpdateQ(_state, actionIdx, reward);

                UpdateSenses();
                _state.Update();
            }

            _qManager.SaveQTable();
        }

        private void UpdateSenses()
        {
            Enemies = Body.ExploreEnemies1()?.ToList() ?? new List<EnemySnapshot>();
            Hills = Body.ExploreHills1() ?? new List<Position>();
            Ditches = Body.ExploreDitches1() ?? new List<Position>();
            Friends = Body.ExploreTeam().ToList();
            _flags = Body.ExploreFlags2();
        }


        // Actions mapped by index
        private void DoAction(int idx)
        {
            switch (idx)
            {
                case 0: Body.ChangeStance2(Stance.Standing); break;
                case 1: Body.ChangeStance2(Stance.Kneeling); break;
                case 2: Body.ChangeStance2(Stance.Lying); break;
                case 3:
                    var ownStand = Body.ExploreOwnFlagStand();
                    if (ownStand != null)
                        GoTo(ownStand);
                    break;
                case 4: 
                    var enemyStands = Body.ExploreEnemyFlagStands1();
                    if (enemyStands.Any())
                        GoTo(enemyStands.First());
                    break;

                case 5: GoTo(GetOwnFlagPosition()); break;
                case 6: GoTo(GetEnemyFlagPosition()); break;
                case 7: GoToClosestFriend(); break;
                case 8: GoToClosestEnemy(); break;
                case 9: GoToClosestHill(); break;
                case 10: GoToClosestDitch(); break;
                case 11: ExploreBarrels(); break;
                case 12: TagEnemy(); break;
                case 13: TagBarrel(); break;
                case 14:
                {
                    _remainingShotsBeforeReload = Body.RemainingShots;
                    Body.Reload3(); break;
                }
                default: break;
            }

            if (Body.CarryingFlag)
            {
                if (!HasCapturedFlagPreviosly)
                {
                    HasCapturedFlagThisTick = true;
                    HasCapturedFlagPreviosly = true;
                }
                else
                {
                    HasCapturedFlagThisTick = false;
                }
            }
            else
            {
                HasCapturedFlagThisTick = false;
                HasCapturedFlagPreviosly = false;
            }
        }

        private Position GetEnemyFlagPosition()
        {
            if (_flags != null && _flags.Any())
            {
                var enemyFlag = _flags.FirstOrDefault(f => f.Team != Body.Color);
                if (enemyFlag.Position != null)
                    return enemyFlag.Position;
            }
            // Fallback: eigene Position oder (0,0)
            return Body.Position ?? new Position(0, 0);
          
        }

        private Position GetOwnFlagPosition()
        {
           
            if (_flags != null && _flags.Any())
            {
                var ownFlag = _flags.FirstOrDefault(f => f.Team == Body.Color);
                if (ownFlag.Position != null)
                    return ownFlag.Position;
            }
            return Body.Position ?? new Position(0, 0);
        }

        private void GoTo(Position pos)
        {
            if (pos != null)
            {
                Body.GoTo(pos);
                HasMoved = true;
            }
        }

        private void GoToClosestFriend()
        {
            if (Friends != null && Friends.Any(f => f.Position != null))
            {
                var best = Friends
                    .Where(f => f.Position != null)
                    .OrderBy(f => Body.GetDistance(f.Position))
                    .FirstOrDefault();
                if (best.Position != null)
                    GoTo(best.Position);
            }
        }

        private void GoToClosestEnemy()
        {
            if (Enemies != null && Enemies.Any(e =>  e.Position != null))
            {
                var best = Enemies
                    .Where(e => e.Position != null)
                    .OrderBy(e => Body.GetDistance(e.Position))
                    .FirstOrDefault();
                if (best.Position != null)
                    GoTo(best.Position);
            }
        }

        private void GoToClosestHill()
        {
            if (Hills != null && Hills.Any(p => p != null))
            {
                var best = Hills.Where(p => p != null)
                    .OrderBy(p => Body.GetDistance(p))
                    .FirstOrDefault();
                if (best != null)
                    GoTo(best);
            }
        }

        private void GoToClosestDitch()
        {
            if (Ditches != null && Ditches.Any(p => p != null))
            {
                var best = Ditches.Where(p => p != null)
                    .OrderBy(p => Body.GetDistance(p))
                    .FirstOrDefault();
                if (best != null)
                    GoTo(best);
            }
        }

        private void ExploreBarrels()
        {
            _barrels = Body.ExploreBarrels1();
        }

        private void TagEnemy()
        {
            if (Enemies != null && Enemies.Any(e => e.Position != null))
            {
                var best = Enemies
                    .Where(e => e.Position != null)
                    .OrderBy(e => Body.GetDistance(e.Position))
                    .FirstOrDefault();
                if (best.Position != null)
                {
                    try { Body.Tag5(best.Position); }
                    catch (Exception) { }
                }
            }
        }

        private void TagBarrel()
        {
            if (_barrels != null && _barrels.Any(b => b != null))
            {
                var best = _barrels.Where(b => b != null)
                    .OrderBy(b => Body.GetDistance(b))
                    .FirstOrDefault();
                if (best != null)
                {
                    try { Body.Tag5(best); }
                    catch (Exception) { }
                }
            }
        }


        // Reward function (anpassen je nach gewünschtem Verhalten)
        private double GetReward(int action)
        {
            if (HasCapturedFlagThisTick) return 50; // big reward
            if (action == 12 && Enemies.Any(e => e.Position.Equals(Body.Position))) return 10; // tagged enemy
            if (action == 13) return 5; // tagged barrel
            if (Body.WasTaggedLastTick) return -15; // got tagged
            if (action == 14 && _remainingShotsBeforeReload != 0) return -2; // unnecessary reload
            if (HasMoved) return 4; // exploring reward
            return 0;
        }
    }

    // --- State Representation for SupermanMind ---
    public class SupermanState
    {
        private SupermanMind _agent;
        public int ActionPoints, Energy, Shots, CarryingFlag;
        public int FriendDist, EnemyDist, OwnFlagstandDist;
        public int HasMoved, HillDist, DitchDist;
        
        private const int AP_BINS = 3;
        private const int ENERGY_BINS = 2;
        private const int SHOTS_BINS = 3;
        private const int FLAG_BINS = 2;
        private const int DIST_BINS = 3;
        private const int FLAGSTAND_BINS = 3;
        private const int MOVED_BINS = 2;


        public SupermanState(SupermanMind agent)
        {
            _agent = agent;
            Update();
        }

        public void Update()
        {
            ActionPoints = BinActionPoints(_agent.Body.ActionPoints);
            Energy = BinEnergy(_agent.Body.Energy);
            Shots = BinShots(_agent.Body.RemainingShots);
            CarryingFlag = _agent.Body.CarryingFlag ? 1 : 0;
            FriendDist = BinDist(CalcDist(_agent.Friends.Select(f => f.Position)));
            EnemyDist = BinDist(CalcDist(_agent.Enemies.Select(e => e.Position)));
            OwnFlagstandDist = BinFlagstandDist(_agent.Body.GetDistance(_agent.Body.ExploreOwnFlagStand()));
            HasMoved = _agent.HasMoved ? 1 : 0;
            HillDist = BinDist(CalcDist(_agent.Hills));
            DitchDist = BinDist(CalcDist(_agent.Ditches));
        }

        private int BinActionPoints(int ap)
        {
            if (ap <= 2) return 0;
            if (ap <= 6) return 1;
            return 2;
        }

        private int BinEnergy(int energy)
        {
            return energy < 40 ? 0 : 1;
        }

        private int BinShots(int shots)
        {
            if (shots == 0) return 0;
            if (shots < 4) return 1;
            return 2;
        }

        private int BinDist(int dist)
        {
            if (dist <= 5) return 0;      // nah
            if (dist <= 15) return 1;     // mittel
            return 2;                     // weit oder nicht vorhanden (999)
        }

        private int BinFlagstandDist(int dist)
        {
            if (dist <= 10) return 0;
            if (dist <= 20) return 1;
            return 2;
        }


        private int CalcDist(IEnumerable<Position> positions)
        {
            if (!positions.Any()) return 999;
            return positions.Min(p => _agent.Body.GetDistance(p));
        }

        // State as unique index for Q-Table (binning/discretization can be added for efficiency)
        public int GetIndex()
        {
            int idx = ActionPoints;
            idx = idx * ENERGY_BINS + Energy;
            idx = idx * SHOTS_BINS + Shots;
            idx = idx * FLAG_BINS + CarryingFlag;
            idx = idx * DIST_BINS + FriendDist;
            idx = idx * DIST_BINS + EnemyDist;
            idx = idx * FLAGSTAND_BINS + OwnFlagstandDist;
            idx = idx * MOVED_BINS + HasMoved;
            idx = idx * DIST_BINS + HillDist;
            idx = idx * DIST_BINS + DitchDist;
            return idx;
        }


    }

    // --- QTableManager for SupermanMind ---
    public class QTableManagerSuperman
    {
        private static readonly object FileLock = new object();
        private Dictionary<int, double[]> _qTable = new();
        private string FilePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, 
            "Model", "Mind", "SupermanMind_qtable.json");
        
        private const int ActionCount = 15;

        public void LoadQTable()
        {
            lock (FileLock)
            {
                if (File.Exists(FilePath))
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<int, double[]>>(File.ReadAllText(FilePath));
                    if (dict != null) _qTable = dict;
                }
            }
        }

        public void SaveQTable()
        {
            lock (FileLock)
            {
                var json = JsonSerializer.Serialize(_qTable);
                File.WriteAllText(FilePath, json);
            }
        }

        public int GetBestAction(SupermanState state)
        {
            var idx = state.GetIndex();
            if (!_qTable.ContainsKey(idx))
                _qTable[idx] = new double[ActionCount];
            // Epsilon-greedy for exploration
            double epsilon = 0.1;
            if (new Random().NextDouble() < epsilon)
                return new Random().Next(ActionCount);
            double[] qVals = _qTable[idx];
            return Array.IndexOf(qVals, qVals.Max());
        }

        public void UpdateQ(SupermanState state, int action, double reward)
        {
            var idx = state.GetIndex();
            if (!_qTable.ContainsKey(idx))
                _qTable[idx] = new double[ActionCount];
            double alpha = 0.3, gamma = 0.9;
            double maxNext = _qTable[idx].Max();
            _qTable[idx][action] += alpha * (reward + gamma * maxNext - _qTable[idx][action]);
        }
    }
}