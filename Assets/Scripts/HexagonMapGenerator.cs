using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class HexagonMapGenerator : Singleton<HexagonMapGenerator>
{
    public int MapSize = 5;

    public float CellSize = 0.56f;

    public float MoveCost { get; private set; }

    [SerializeField]
    private GameObjectEntity _tileEntityPrefab;

    [SerializeField]
    private ActorController _attackerPref;

    [SerializeField]
    private ActorController _defenderPref;

    public bool DoneSimulation = true;
    public bool StartSimulating = false;
    public bool IsFinishTurn
    {
        get
        {
            return StartSimulating == false && DoneSimulation == true;
        }
    }


    private List<ActorController> _attackers;
    private List<ActorController> _defenders;
    // -x , y
    private List<HexagonNode> _topLeftNodes = new List<HexagonNode>();
    // x , y
    private List<HexagonNode> _topRightNodes = new List<HexagonNode>();
    // -x , -y
    private List<HexagonNode> _botLeftNodes = new List<HexagonNode>();
    // x , -y
    private List<HexagonNode> _botRightNodes = new List<HexagonNode>();

    

    private WaitForSeconds _wait = new WaitForSeconds(1.5f);
    private int _limitPathCount;
    
    public void SetupGamePlay(int mapSize)
    {
        MapSize = mapSize;
        _limitPathCount = MapSize;
        MoveCost = 0.75f * CellSize;

        GenerateNodes();
        int attackerRoundNum = Mathf.FloorToInt((mapSize - 1)/2);
        int defenderRoundNum = mapSize - 1 - attackerRoundNum;
        StartCoroutine(StartLevel(attackerRoundNum, defenderRoundNum));
    }

    private IEnumerator StartLevel(int attackerRoundNum, int defenderRoundNum)
    {
        //yield return _wait;
        //SpawnDefenders(defenderRoundNum);
        yield return _wait;
        //SpawnAttackers(attackerRoundNum);
        SpawnAttackers(MapSize);
        //SpawnAttackers(MapSize-1);
        //UIManager.Instance.SetupPower(_attackers.Count, _defenders.Count);
        yield return _wait;
        InvokeRepeating("TurnBeat", 2f, 2f);
    }

    private void TurnBeat()
    {
        if ( IsFinishTurn )
        {
            //if ( _defenders == null || _defenders.Count == 0 )
            //{
            //    CancelInvoke();
            //    UIManager.Instance.OpenRestartPanel();
            //    return;
            //}
            StartSimulating = true;
        }
    }

    private void GenerateNodes()
    {
        GenerateNodeRecursive(HexagonNode.zero);
        _topLeftNodes.Clear();
        _topLeftNodes = null;
        _topRightNodes.Clear();
        _topRightNodes = null;
        _botLeftNodes.Clear();
        _botLeftNodes = null;
        _botRightNodes.Clear();
        _botRightNodes = null;
    }

    private void GenerateNodeRecursive(HexagonNode node)
    {
        GameObjectEntity nodeEntity = Instantiate(_tileEntityPrefab);
        nodeEntity.transform.position = new Vector2(node.X, node.Y);
        AddNodes(node);

        HexagonNode curNode;
        Vector2[] adjacentNodes = node.GetAdjacentNodes(CellSize, MapSize);
        for ( int i = 0; i < adjacentNodes.Length; i++ )
        {
            curNode = new HexagonNode(adjacentNodes[i].x, adjacentNodes[i].y);
            if ( SearchNodeByPosition(curNode) )
            {
                continue;
            }
            GenerateNodeRecursive(curNode);
        }
    }

    public static bool CheckInsideHexagon(float x, float y, float cellSize, float size)
    {
        float maxX = size * (0.75f * cellSize);
        if ( x < 0 )
        {
            float maxY = size * cellSize + x * ((float)2 / 3);
            if ( y < 0 )
            {
                return (Mathf.Approximately(y, -maxY) || y > -maxY) && x >= -maxX;
            }
            else
            {
                return (Mathf.Approximately(y, maxY) || y < maxY) && x >= -maxX;
            }
        }
        else
        {
            float maxY = size * cellSize - x * ((float)2 / 3);
            if ( y < 0 )
            {
                return (Mathf.Approximately(y, -maxY) || y > -maxY) && x <= maxX;
            }
            else
            {
                return (Mathf.Approximately(y, maxY) || y < maxY) && x <= maxX;
            }
        }
    }

    private bool SearchNodeByPosition(HexagonNode cell)
    {
        return GetNodesByPos(cell).Contains(cell);
    }

    private void AddNodes(HexagonNode node)
    {
        if ( node.X < 0 )
        {
            if ( node.Y < 0 )
            {
                _botLeftNodes.Add(node);
            }
            else
            {
                _topLeftNodes.Add(node);
            }
        }
        else
        {
            if ( node.Y < 0 )
            {
                _botRightNodes.Add(node);
            }
            else
            {
                _topRightNodes.Add(node);
            }
        }
    }

    private List<HexagonNode> GetNodesByPos(HexagonNode node)
    {
        if ( node.X < 0 )
        {
            if ( node.Y < 0 )
            {
                return _botLeftNodes;
            }
            else
            {
                return _topLeftNodes;
            }
        }
        else
        {
            if ( node.Y < 0 )
            {
                return _botRightNodes;
            }
            else
            {
                return _topRightNodes;
            }
        }
    }

    public List<Vector2> FindPath(
        ShortestPathToZero currentPos,
        List<ShortestPathToZero> openList,
        List<Vector2> closeList)
    {
        List<Vector2> result = new List<Vector2>();
        FindPathRecursive(result, currentPos, openList, closeList);
        return result;
    }

    private void FindPathRecursive(
        List<Vector2> resultPath,
        ShortestPathToZero currentPos,
        List<ShortestPathToZero> openList,
        List<Vector2> closeList)
    {
        resultPath.Add(currentPos.GetPosition());
        if ( currentPos.IsDestination()
            || resultPath.Count >= _limitPathCount )
        {
            return;
        }
        bool isReachDestination = false;
        Vector2[] adjacentNodes = currentPos.Node.GetAdjacentNodes(CellSize, MapSize);
        for ( int i = 0; i < adjacentNodes.Length; i++ )
        {
            HexagonNode node = new HexagonNode(adjacentNodes[i].x, adjacentNodes[i].y);
            var estimatePath =
                new ShortestPathToZero(
                    node,
                    currentPos.MoveCost + MoveCost,
                    (node - HexagonNode.zero).Magnitude,
                    currentPos);

            if ( closeList.Contains(adjacentNodes[i]) )
            {
                continue;
            }

            if ( estimatePath.IsDestination() )
            {
                //resultPath.Add(estimatePath.GetPosition());
                isReachDestination = true;
                break;
            }

            int findedIndex = openList.IndexOf(estimatePath);

            if ( findedIndex > -1
                && openList[findedIndex].TotalCost != estimatePath.TotalCost )
            {
                openList[findedIndex] = estimatePath;
            }
            else
            {
                openList.Add(estimatePath);
            }
        }
        if ( isReachDestination
            || openList.Count == 0 )
        {
            return;
        }
        openList.Sort();
        ShortestPathToZero currentShortPath = openList[0];
        openList.RemoveAt(0);
        closeList.Add(currentShortPath.GetPosition());
        FindPathRecursive(resultPath, currentShortPath, openList, closeList);
    }

    public List<HexagonNode> GetNodesByRound(int roundIndex)
    {
        float stepX = 0.75f * CellSize;
        Vector2 maxByRound = new Vector2((float)Math.Round(roundIndex * stepX, 2), (float)Math.Round(roundIndex * CellSize, 2));
        HexagonNode maxXPoint = new HexagonNode(maxByRound.x, 0);
        List<HexagonNode> result = new List<HexagonNode>();

        Vector2 top = new Vector2(0, CellSize);

        float boundLength = roundIndex * CellSize;
        float offset = (float)2 / 3;
        for ( float x = stepX; x <= maxByRound.x; x += stepX )
        {
            float y = boundLength - x * offset;
            result.Add(new HexagonNode(x, y));
            result.Add(new HexagonNode(-x, y));
            result.Add(new HexagonNode(x, -y));
            result.Add(new HexagonNode(-x, -y));
        }

        float topLeftY = (float)Math.Round(boundLength - maxByRound.x * offset, 2);
        for ( float y = topLeftY; y >= -topLeftY; y = (float)Math.Round(y - CellSize, 2) )
        {
            result.Add(new HexagonNode(maxByRound.x, y));
            result.Add(new HexagonNode(-maxByRound.x, y));
        }
        // Add Top bot of hexagon
        result.Add(new HexagonNode(0, maxByRound.y));
        result.Add(new HexagonNode(0, -maxByRound.y));
        return result;
    }

    private void SpawnAttackers(int roundNum)
    {
        List<HexagonNode> spawnPositions = new List<HexagonNode>();
        _attackers = new List<ActorController>();
        //for ( int i = MapSize - roundNum; i <= MapSize; i++ )
        //{
        //    spawnPositions = GetNodesByRound(i);
        //    foreach ( HexagonNode node in spawnPositions )
        //    {
        //        ActorController actor = Instantiate(_attackerPref);
        //        actor.transform.position = new Vector2(node.X, node.Y);
        //        actor.SetupAttacker();
        //        _attackers.Add(actor);
        //    }
        //}

        spawnPositions = GetNodesByRound(roundNum);
        foreach ( HexagonNode node in spawnPositions )
        {
            ActorController actor = Instantiate(_attackerPref);
            actor.transform.position = new Vector2(node.X, node.Y);
            actor.SetupAttacker();
            _attackers.Add(actor);
        }
    }
    
    public bool CheckIsBlocked(ActorController currentAttacker)
    {
        if ( currentAttacker == null || _attackers == null )
        {
            return false;
        }

        for ( int i = 0; i < _attackers.Count; i++ )
        {
            // We only check the attacker move before the current attacker.
            if ( _attackers[i] == currentAttacker )
            {
                break;
            }
            if ( _attackers[i] != null
                && _attackers[i] != currentAttacker
                // When iterating we update Destination of currentEnemy but not update the others yet.
                // So the other.destination still = other.currentposition.
                && _attackers[i].Destination == currentAttacker.Destination )
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnDefenders(int roundNum)
    {
        List<HexagonNode> spawnPositions = new List<HexagonNode>();
        _defenders = new List<ActorController>();
        for ( int i = 1; i < roundNum; i++ )
        {
            spawnPositions = GetNodesByRound(i);
            foreach ( HexagonNode node in spawnPositions )
            {
                ActorController actor = Instantiate(_defenderPref);
                actor.Destination = new Vector2(node.X, node.Y);
                actor.transform.position = actor.Destination;
                actor.SetupDefender();
                _defenders.Add(actor);
            }
        }

        // Spawn Defender at centre
        ActorController centreActor = Instantiate(_defenderPref);
        centreActor.Destination = new Vector2(0, 0);
        centreActor.transform.position = centreActor.Destination;
        centreActor.SetupDefender();
        _defenders.Add(centreActor);

    }

    public ActorController GetNearDefender(ActorController currentAttacker)
    {
        if ( currentAttacker == null || _defenders == null )
        {
            return null;
        }

        Vector2[] adjacentNodes = HexagonNode.GetAdjacentNodesByPos(currentAttacker.SnapToHexNode(), CellSize, MapSize);
        _defenders.RemoveAll(_ => _ == null);
        for ( int i = 0; i < adjacentNodes.Length; i++ )
        {
            foreach ( ActorController def in _defenders )
            {
                if( def == null || def.Hp <= 0 )
                {
                    continue;
                }
                if ( adjacentNodes[i] == def.Destination)
                {
                    return def;
                }
            }
        }
        return null;
    }

    public List<Vector2> GetObstaclesByAdjacentNode(Vector2 curPos)
    {
        List<Vector2> result = new List<Vector2>();
        float radius = CellSize*2;
        if ( _attackers == null )
        {
            return result;
        }
        _attackers.RemoveAll(_ => _ == null);

        for ( int i = 0; i < _attackers.Count; i++ )
        {
            if ( _attackers[i].Hp <= 0 )
            {
                continue;
            }
            if( (_attackers[i].Destination - curPos).magnitude <= radius )
            {
                result.Add(_attackers[i].Destination);
            }
            
        }
        return result;
    }


    #region Editor command Testing
    #if UNITY_EDITOR
    private Transform _selectionHex;

    private void Update()
    {
        if ( Selection.activeTransform != null
            && Selection.activeTransform.GetComponent<ActorController>() != null
            && (_selectionHex == null || _selectionHex != Selection.activeTransform) )
        {
            _selectionHex = Selection.activeTransform;
            FindPathForSelectionHexOnEditor();
        }

    }

    public List<Vector2> CurrentPath;

    private void FindPathForSelectionHexOnEditor()
    {
        //ShortestPathToZero startPath;
        //List<ShortestPathToZero> openList = new List<ShortestPathToZero>();
        //List<ShortestPathToZero> closeList = new List<ShortestPathToZero>();
        ////_closeList.Clear();
        ////_openList.Clear();
        //CurrentPath.Clear();
        //HexagonNode node = new HexagonNode(_selectionHex.position, CellSize, MapSize);
        //startPath = new ShortestPathToZero(node, MoveCost, node.Magnitude, null);
        //_closeList.Add(startPath);


        ActorController selectedActor = _selectionHex.GetComponent<ActorController>();
        selectedActor.FindPath();
        CurrentPath = selectedActor.CurrentPath;
    }

    [ContextMenu("SpawnActor")]
    public void SpawnActor()
    {
        int attackerRoundNum = Mathf.FloorToInt((MapSize - 1) / 2);
        int defenderRoundNum = MapSize - 1 - attackerRoundNum;
        SpawnAttackers(attackerRoundNum);
        SpawnDefenders(defenderRoundNum);
    }

    [ContextMenu("NextTurn")]
    public void NextTurn()
    {
        StartSimulating = true;
    }
    #endif
    #endregion

}

public class ShortestPathToZero : IComparable<ShortestPathToZero>
{
    public HexagonNode Node;
    public float MoveCost;
    public float EstimateScore;
    public ShortestPathToZero Parent;

    public float TotalCost;

    public ShortestPathToZero(HexagonNode pos, float moveCost, float estScore, ShortestPathToZero parent)
    {
        Node = pos;
        MoveCost = moveCost;
        EstimateScore = estScore;
        TotalCost = MoveCost + EstimateScore;
        Parent = parent;
    }

    public bool IsDestination()
    {
        return Node == HexagonNode.zero
            || (Node - HexagonNode.zero).Magnitude <= Mathf.Epsilon;
    }

    public int CompareTo(ShortestPathToZero other)
    {
        if ( TotalCost != other.TotalCost )
        {
            return TotalCost.CompareTo(other.TotalCost);
        }
        else
        {
            return -MoveCost.CompareTo(other.MoveCost);
        }

    }

    public static bool operator ==(ShortestPathToZero a, ShortestPathToZero b)
    {
        return Equals(a, b);
    }

    public static bool operator !=(ShortestPathToZero a, ShortestPathToZero b)
    {
        return !Equals(a, b);
    }

    public static bool Equals(ShortestPathToZero a, ShortestPathToZero b)
    {
        return a.Node == b.Node
            || (a.Node - b.Node).Magnitude <= Mathf.Epsilon;
    }

    public override bool Equals(object obj)
    {
        if ( obj == null || obj.GetType() != typeof(ShortestPathToZero) )
        {
            return false;
        }
        return Equals(this, (ShortestPathToZero)obj);
    }

    public override int GetHashCode()
    {
        return Node.X.GetHashCode() ^ Node.Y.GetHashCode() >> 1;
    }

    public Vector2 GetPosition()
    {
        return new Vector2(Node.X, Node.Y);
    }

    public static List<Vector2> CreateListShortestPathByActors(ActorController[] actors)
    {
        List<Vector2> result = new List<Vector2>();
        if ( actors == null )
        {
            return result;
        }
        for ( int i = 0; i < actors.Length; i++ )
        {
            if ( actors[i] != null )
            {
                result.Add(actors[i].Destination);
            }
        }
        return result;
    }

    public static ShortestPathToZero Zero
    {
        get
        {
            return new ShortestPathToZero(HexagonNode.zero, 100, 100, null);
        }
    }

}

public class HexagonNode : IComparable<HexagonNode>
{
    public float X;
    public float Y;

    public HexagonNode(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static HexagonNode operator -(HexagonNode a, HexagonNode b)
    {
        return new HexagonNode(a.X - b.X, a.Y - b.Y);
    }

    public float Magnitude
    {
        get
        {
            return Mathf.Sqrt(SqrMagnitude);
        }
    }

    public float SqrMagnitude
    {
        get
        {
            return X * X + Y * Y;
        }
    }

    public static readonly HexagonNode zero = new HexagonNode(0, 0);

    public int CompareTo(HexagonNode other)
    {
        int d;
        d = X.CompareTo(other.X);
        if ( d != 0 )
        {
            return d;
        }
        d = Y.CompareTo(other.Y);
        if ( d != 0 )
        {
            return d;
        }
        return 0;

    }

    public static bool operator ==(HexagonNode a, HexagonNode b)
    {
        return Equals(a, b);
    }

    public static bool operator !=(HexagonNode a, HexagonNode b)
    {
        return !Equals(a, b);
    }

    public static bool Equals(HexagonNode a, HexagonNode b)
    {
        return Mathf.Abs(a.X - b.X) < Mathf.Epsilon
                && Mathf.Abs(a.Y - b.Y) < Mathf.Epsilon;
    }

    public override bool Equals(object obj)
    {
        if ( obj == null || obj.GetType() != typeof(HexagonNode) )
        {
            return false;
        }
        return Equals(this, (HexagonNode)obj);
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() >> 1;
    }

    public Vector2[] GetAdjacentNodes(float cellSize, float mapSize)
    {
        return GetAdjacentNodesByPos(X, Y, cellSize, mapSize);
    }

    public static Vector2[] GetAdjacentNodesByPos(Vector2 pos, float cellSize, float mapSize)
    {
        return GetAdjacentNodesByPos(pos.x, pos.y, cellSize, mapSize);
    }

    public static Vector2[] GetAdjacentNodesByPos(float x, float y, float cellSize, float mapSize)
    {
        float nextTopY = (float)Math.Round(y + cellSize, 2);
        float nextBotY = (float)Math.Round(y - cellSize, 2);
        float nextLeftX = (float)Math.Round(x - 0.75f * cellSize, 2);
        float nextSideBotY = (float)Math.Round(y - (cellSize * 0.5f), 2);
        float nextRightX = (float)Math.Round(x + 0.75f * cellSize, 2);
        float nextSideTopY = (float)Math.Round(y + (cellSize * 0.5f), 2);

        List<Vector2> adjacentNodes = new List<Vector2>();

        if ( HexagonMapGenerator.CheckInsideHexagon(x, nextTopY, cellSize, mapSize) )
        {
            adjacentNodes.Add(new Vector2(x, nextTopY));
        }

        if ( HexagonMapGenerator.CheckInsideHexagon(x, nextBotY, cellSize, mapSize) )
        {
            adjacentNodes.Add(new Vector2(x, nextBotY));
        }

        if ( HexagonMapGenerator.CheckInsideHexagon(nextLeftX, nextSideTopY, cellSize, mapSize) )
        {
            adjacentNodes.Add(new Vector2(nextLeftX, nextSideTopY));
        }

        if ( HexagonMapGenerator.CheckInsideHexagon(nextLeftX, nextSideBotY, cellSize, mapSize) )
        {
            adjacentNodes.Add(new Vector2(nextLeftX, nextSideBotY));
        }

        if ( HexagonMapGenerator.CheckInsideHexagon(nextRightX, nextSideTopY, cellSize, mapSize) )
        {
            adjacentNodes.Add(new Vector2(nextRightX, nextSideTopY));
        }

        if ( HexagonMapGenerator.CheckInsideHexagon(nextRightX, nextSideBotY, cellSize, mapSize) )
        {
            adjacentNodes.Add(new Vector2(nextRightX, nextSideBotY));
        }

        return adjacentNodes.ToArray();
    }

}
