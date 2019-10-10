using Unity.Entities;

public class GamePlaySimulationSystem : ComponentSystem
{
    EntityQuery query;
    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(ActorController), typeof(Attacker));
    }

    protected override void OnUpdate()
    {
        if ( !HexagonMapGenerator.Instance.StartSimulating )
        {
            return;
        }

        bool atLeastOneActorDontHavePath = false;
        if ( HexagonMapGenerator.Instance.DoneSimulation
            && HexagonMapGenerator.Instance.StartSimulating )
        {
            Entities.With(query).ForEach((ActorController actor, ref Attacker attacker) =>
            {
                if ( actor == null || actor.IsDead )
                {
                    return;
                }

                if ( !actor.IsNearDefender() )
                {
                    actor.UpdateNewDestination();
                    if ( !actor.IsReachMaxTry && !actor.Moveable )
                    {
                        atLeastOneActorDontHavePath = true;
                        actor.FindPath();
                    }

                    //actor.AttackTarget();
                }
            });
        }
        if ( atLeastOneActorDontHavePath )
        {
            return;
        }
        bool atLeastOneActorNotAttackYet = false;

        Entities.With(query).ForEach((ActorController actor) =>
        {
            if ( actor == null || actor.IsDead || actor.WasMoved )
            {
                return;
            }

            if ( actor.IsNearDefender() )
            {
                atLeastOneActorNotAttackYet = true;
                actor.AttackTarget();
            }
        });

        if ( atLeastOneActorNotAttackYet )
        {
            return;
        }

        HexagonMapGenerator.Instance.DoneSimulation = false;

        bool atLeastOneActorNotDoneYet = false;
        // Start simulating
        Entities.With(query).ForEach((ActorController actor) =>
        {
            if ( !actor.IsReachDestination )
            {
                atLeastOneActorNotDoneYet = true;
                return;
            }
        });

        if ( atLeastOneActorNotDoneYet )
        {
            return;
        }
        HexagonMapGenerator.Instance.DoneSimulation = true;
        HexagonMapGenerator.Instance.StartSimulating = false;
        Entities.With(query).ForEach((ActorController actor) =>
        {
            actor.OnDoneSimulation();
        });
    }

}