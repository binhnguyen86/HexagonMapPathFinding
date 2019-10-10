using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Spine;
using Spine.Unity;
using Unity.Mathematics;
using Unity.Transforms;

public class ActorController : MonoBehaviour
{
    public const float StopDistance = 0.025f;

    public SkeletonAnimation SkeletonAnim;
    [Header("Animations")]
    [SpineAnimation]
    public string IdleAnim;
    [SpineAnimation]
    public string MoveAnim;
    public float Speed = 1;
    public int MaxTryFindPath = 3;
    public int AttackTime = 1;
    public int StartHp = 1;
    public Slider HeathSlider;
    public Color HpColor;
    public Color HpDefaultColor;

    public bool IsReachDestination
    {
        get
        {

            if ( CurrentPath == null || CurrentPath.Count <= 1 )
            {
                //if(CurrentPath != null )
                //{
                //    Debug.Log("CurrentPath Count: " + CurrentPath.Count);
                //}

                return true;
            }
            float distance = ((Vector2)transform.position - Destination).magnitude;
            return distance < StopDistance;
        }
    }

    public bool IsReachMaxTry
    {
        get
        {
            return MaxTryFindPath < _findCount;
        }
    }

    public bool Moveable
    {
        get
        {
            return CurrentPath != null && CurrentPath.Count > 1 && !IsBlocked();
        }
    }

    public Vector2 NextDestination
    {
        get
        {
            if ( CurrentPath != null && _pathIndex < CurrentPath.Count )
            {
                return CurrentPath[_pathIndex];
            }
            return transform.position;
        }
    }

    public bool CanAttack
    {
        get
        {
            return _attackCount <= AttackTime && _defenderTarget != null;
        }
    }

    public Vector2 Destination { get; set; }

    public int Hp { get; private set; }
    
    private bool _isDefender;

    public bool IsDead
    {
        get
        {
            return Hp <= 0;
        }
    }

    public List<Vector2> CurrentPath = null;   

    public enum ActionState { Idle, Move }

    public ActionState State
    {
        get
        {
            return _state;
        }
        set
        {
            if ( _state != value )
            {
                _state = value;
            }
        }
    }

    public bool WasMoved = false;

    private ActionState _state;
    private bool _isDamaged = false;    
    private MaterialPropertyBlock _proterty;
    private MeshRenderer _mesh;
    private float _currentHpSlideValue;
    private int _attackCount = 0;
    private int _pathIndex = 1;
    private int _findCount = 0;
    private ActorController _defenderTarget;
    private ActorController _attackerTarget;
    private List<ShortestPathToZero> _openList = new List<ShortestPathToZero>();
    private List<Vector2> _closeList = new List<Vector2>();
    private WaitForSeconds _wait = new WaitForSeconds(0.25f);

    private void Awake()
    {
        Hp = StartHp;
        HeathSlider.maxValue = StartHp;
        HeathSlider.value = StartHp;
        _currentHpSlideValue = StartHp;
        _proterty = new MaterialPropertyBlock();
        _mesh = GetComponentInChildren<MeshRenderer>();

    }

    private void Start()
    {
        if( SkeletonAnim  != null )
        {
            SkeletonAnim.state.Complete -= HandleComplete;
            SkeletonAnim.state.Complete += HandleComplete;
        }
        
    }

    public void HandlePhysics()
    {
        if ( HexagonMapGenerator.Instance.DoneSimulation
            || (!HexagonMapGenerator.Instance.DoneSimulation && (Vector2)transform.position == Destination)
            || IsDead )
        {
            return;
        }
        if ( !IsReachDestination )
        {
            State = ActionState.Move;
            Vector2 direction = (Destination - (Vector2)transform.position).normalized;
            transform.Translate(direction * Time.deltaTime * Speed, Space.World);
            return;
        }
        State = ActionState.Idle;
        transform.position = Destination;
    }

    public void UpdateNewDestination()
    {
        if ( CurrentPath != null && CurrentPath.Count > 1 )
        {
            Destination = CurrentPath[_pathIndex];
        }
    }

    public void OnDoneSimulation()
    {
        _findCount = 0;
        _attackCount = 0;
        WasMoved = false;
        if ( CurrentPath == null
            || CurrentPath.Count <= 1
            || Destination != CurrentPath[_pathIndex]
            || IsNearDefender() )
        {
            return;
        }
        State = ActionState.Idle;
        Destination = CurrentPath[_pathIndex];
        _pathIndex = Mathf.Min(CurrentPath.Count - 1, _pathIndex + 1);
        // If Path cannot reach zero we clear it
        if ( !CurrentPath.Contains(Vector2.zero) )
        {
            Destination = transform.position;
            CurrentPath.Clear();
        }
    }
    
    public Vector2 SnapToHexNode()
    {
        float x1 = Mathf.Round(transform.position.x / 0.42f);
        float x = (float)Math.Round(x1 * 0.42f, 2);

        float startY = x1 % 2 == 0 ? 0 : 0.28f;
        float y1 = Mathf.Round((transform.position.y - startY) / 0.56f);
        float y = (float)Math.Round(y1 * 0.56f, 2) + startY;
        return new Vector2(x, y);
    }

    public void FindPath()
    {
        WasMoved = true;
        _findCount++;
        if ( CurrentPath != null )
        {
            _pathIndex = 1;
            CurrentPath.Clear();
        }
        Vector2 cellPos = SnapToHexNode();
        transform.position = cellPos;
        Destination = cellPos;
        HexagonNode node = new HexagonNode(cellPos.x, cellPos.y);
        ShortestPathToZero startPath = new ShortestPathToZero(node, HexagonMapGenerator.Instance.MoveCost, node.Magnitude, null);
        _closeList.AddRange(HexagonMapGenerator.Instance.GetObstaclesByAdjacentNode(cellPos));
        _closeList.Add(cellPos);

        CurrentPath = HexagonMapGenerator.Instance.FindPath(startPath, _openList, _closeList);
        _closeList.Clear();
        _openList.Clear();

        if ( CurrentPath.Count > 1 )
        {
            Destination = CurrentPath[_pathIndex];            
        }       
    }

    public bool IsBlocked()
    {
        if ( CurrentPath == null || CurrentPath.Count <= 1 )
        {
            return true;
        }
        bool isBLocked = HexagonMapGenerator.Instance.CheckIsBlocked(this);
        return isBLocked;
    }

    public bool IsNearDefender()
    {
        if ( _defenderTarget != null && _defenderTarget.Hp > 1 )
        {
            return true;
        }
        _defenderTarget = HexagonMapGenerator.Instance.GetNearDefender(this);
        if( _defenderTarget  != null && _defenderTarget.Hp > 0 )
        {
            _defenderTarget.SetAttackerTarget(this);
            return true;
        }
        return false;
    }

    public void SetAttackerTarget(ActorController attacker)
    {
        if ( _attackerTarget == null || _attackerTarget.Hp <= 0 )
        {
            _attackerTarget = attacker;
        }
        
    }

    public void AttackTarget()
    {
        if ( _attackCount > AttackTime || _defenderTarget == null )
        {
            return;
        }
        State = ActionState.Move;
        _attackCount++;
        CurrentPath.Clear();
        //Debug.Log("attack " + _target + " | " + _target.Hp);
        int damage = CalculateDamage();
        _defenderTarget.SendMessage("Hit", damage, SendMessageOptions.DontRequireReceiver);
        UIManager.Instance.SendMessage("DefenderHitted", damage, SendMessageOptions.DontRequireReceiver);
        WasMoved = true;
    }

    private void Hit(int damage)
    {
        
        //SoundPalette.PlaySound(hitSound, 1, 1, transform.position);
        _isDamaged = true;
        if ( Hp <= 0 )
            return;

        Hp -= damage;
        HeathSlider.value = Hp;
        if ( Hp <= 0 )
        {
            StartCoroutine(StartDestroyActor());
            return;
        }
        if( _attackerTarget  != null && _attackerTarget.Hp > 0)
        {
            State = ActionState.Move;
            int dealDamageBack = CalculateDamage();
            _attackerTarget.SendMessage("Hit", dealDamageBack, SendMessageOptions.DontRequireReceiver);
            UIManager.Instance.SendMessage("AttackerHitted", dealDamageBack, SendMessageOptions.DontRequireReceiver);
        }
        
    }

    private IEnumerator StartDestroyActor()
    {        
        GetComponent<GameObjectEntity>().enabled = false;
        yield return _wait;
        SkeletonAnim.gameObject.SetActive(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// Attacking logic between two characters: each character will generate a random number in range 02
    /// If 3 + attacker_number - target_number) % 3 == 0, deal 4 damage.
    /// If 3 + attacker_number - target_number) % 3 == 1, deal 5 damage.
    /// If 3 + attacker_number - target_number) % 3 == 2, deal 3 damage.
    /// </summary>
    /// <returns></returns>
    private int CalculateDamage()
    {
        float attackRoll = (UnityEngine.Random.value + UnityEngine.Random.value) * 10;
        float defendRoll = (UnityEngine.Random.value + UnityEngine.Random.value) * 10;
        int result = Mathf.RoundToInt((3 + attackRoll + defendRoll) % 3);
        switch ( result )
        {
            case 1:
                return 5;
            case 2:
                return 3;
            default:
                return 4;
        }
    }

    public void UpdateAnim()
    {
        switch ( State )
        {
            case ActionState.Idle:
                SkeletonAnim.AnimationName = IdleAnim;
                break;
            case ActionState.Move:
                SkeletonAnim.AnimationName = MoveAnim;
                break;
        }
    }

    private void HandleComplete(TrackEntry entry)
    {
        if ( entry.Animation.Name == MoveAnim && _isDefender )
        {
            State = ActionState.Idle;
        }
    }

    public void DamagedAnim()
    {
        if ( _isDamaged )
        {
            HpColor = Color.red;
        }
        else
        {
            if ( HpColor == HpDefaultColor )
            {
                // return if there is no color change
                return;
            }
            HpColor = Color.Lerp(HpColor, HpDefaultColor, 3 * Time.deltaTime);
        }
        _proterty.SetColor("_Black", HpColor);
        _mesh.SetPropertyBlock(_proterty);
        _isDamaged = false;
        if( _currentHpSlideValue  == Hp )
        {
            return ;
        }
        _currentHpSlideValue = Mathf.Lerp(_currentHpSlideValue, Hp, 2 * Time.deltaTime);
        HeathSlider.value = _currentHpSlideValue;
    }

    public void SetupAttacker()
    {
        GameObjectEntity Ent = GetComponent<GameObjectEntity>();
        if ( Ent != null && Ent.EntityManager != null )
        {
            Ent.EntityManager.AddComponentData(Ent.Entity, new Attacker());
        }
        _isDefender = false;
    }

    public void SetupDefender()
    {
        GameObjectEntity Ent = GetComponent<GameObjectEntity>();
        if ( Ent != null && Ent.EntityManager != null )
        {
            Ent.EntityManager.AddComponentData(Ent.Entity, new Defender());
        }
        _isDefender = true;
    }

}

public struct Defender: IComponentData
{

}

public struct Attacker : IComponentData
{

}