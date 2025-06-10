using System;
using System.Linq;
using Mars.Interfaces.Environments;

namespace LaserTagBox.Model.Mind;

public class YourPlayerMindPleaseRename : AbstractPlayerMind
{
    private PlayerMindLayer _mindLayer;

    private Position _goal;
    //private static bool _flagCarriedByFriend = false;
    //private static Guid _friendID;
    //private static Position _friendPosition;

    public override void Init(PlayerMindLayer mindLayer)
    {
        _mindLayer = mindLayer;
    }

    public override void Tick()
    {
        //Regel 1: Angriff, wenn Gegner Sichtbar
        var enemies = Body.ExploreEnemies1();
        if (enemies != null)
        {
            if (enemies.Any())
            {
                //_goal = enemies.First().Position.Copy();
                if (Body.RemainingShots == 0) Body.Reload3();
                Body.Tag5(enemies.First().Position);
            }
            //Fall 1: Kein Gegner im Sicht Body.ActionPoints = 9
            //Fall 2: Gegner getaggt, aber nicht reloaded Body.ActionPoints = 4
            //Fall 3: Gegner getaggt und reloaded Body.ActionPoints == 1
        }


        //Regel 2: wenn genug Actionpoints und sichtbar Gegnerflagge holen
        if (Body.RemainingShots > 1)
        {
            var flags = Body.ExploreFlags2();
            if (flags != null)
            {
                var enemyFlag = flags.FirstOrDefault(f => f.Team != Body.Color);
                if (!enemyFlag.Equals(default))
                {
                    _goal = enemyFlag.Position.Copy();
                }
            }

            // Fall 1 = 7 ActionPoints, Fall 2 = 2 ActionPoints, Fall 3 = 1 Actionpoints

            //Regel 3: Wenn eigene Falgge gestohlen, dann Rückeroberung (außer Player trägt selber eine Flagge)
            if (flags != null)
            {
                var ownfFlag = flags.FirstOrDefault(f => f.Team == Body.Color);
                if (!ownfFlag.Equals(default) && ownfFlag.PickedUp && !Body.CarryingFlag)
                {
                    _goal = ownfFlag.Position.Copy();
                }
            }
        }

        //Regel 4: Flagge zurückbringen, wenn man sie trägt
        if (Body.CarryingFlag)
        {
            var flagStand = Body.ExploreOwnFlagStand();
            if (flagStand != null)
            {
                _goal = flagStand;
            }
        }
        

        //Regel 5: Wenn noch kein Ziel gesetzt ist, gehe Richtung Gegnerflagge
        if (_goal == null || Body.GetDistance(_goal) == 1)
        {
            var enemyFlagStand = Body.ExploreEnemyFlagStands1();
            if (enemyFlagStand != null) _goal = enemyFlagStand.First();
        }


  
        _goal ??= Body.Position;
        var moved = Body.GoTo(_goal);
        if (!moved) _goal = null;
    }
}