using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Checkers;
using System.Linq;
using System;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    List<CellComponent> _blackCell = new();
    [SerializeField]
    List<ChipComponent> _whiteShips=new();
    [SerializeField]
    List<ChipComponent> _blackShips = new();
    [SerializeField]
    Material _materialEnter,_materialClick;

    [SerializeField]
    bool _isLock=false;
    [SerializeField]
    private ColorType _ColorTeam;
    [SerializeField]
    private ChipComponent _selectChip;
    [SerializeField]
    private float _speedChip;
    [SerializeField]
    private float _speedCamera;


    #region observer 
    private Observer _observer;
    public event Action StepOver;


    #endregion



    private void Awake()
    {
        if(TryGetComponent(out _observer))
        {
            _observer.NextStepReady += _observer_NextStepReady;
        }

        _ColorTeam=ColorType.White;
        foreach (CellComponent cell in GameObject.FindObjectsOfType<CellComponent>().Where(t=>t.GetColor==ColorType.Black))
        {
            _blackCell.Add(cell);
            cell.OnFocusEventHandler += OnFocusEventHandler;
            cell.OnClickEventHandler += Cell_OnClickEventHandler;
            FindMove(cell);
        }
       
        foreach (ChipComponent chip in _whiteShips)
        {
            ChipCreate(chip);
        }
        foreach (ChipComponent chip in _blackShips)
        {
            ChipCreate(chip);
            
        }
    }

    private void _observer_NextStepReady(int x,int y)
    {
        if(x==-1||y==-1)
        {
            StepOver?.Invoke();
            return;
        }
        var targetCell=_blackCell.FirstOrDefault(t=>t.transform.position.x==x&&t.transform.position.z==y);
        Cell_OnClickEventHandler(targetCell);
    }

    private void Cell_OnClickEventHandler(BaseClickComponent component)
    {
        if (component.Pair == null)
        {
            for (byte i = 0; i < 4; i++)
            {
                CellComponent cellTemp = ((CellComponent)_selectChip?.Pair)?.GetNeighbors((NeighborType)i);
                if (cellTemp != null && cellTemp == component)
                {
                    Debug.Log("ход");
                    ChipComponent enemy = null;
                    if (Vector3.Distance(_selectChip.transform.localPosition,component.transform.localPosition) >=2)
                    {
                        Vector3 wayEnemy = VectorWay((NeighborType)i);                         
                        enemy =(ChipComponent) _blackCell.First(t => t.Pair != null&& t.transform.localPosition == _selectChip.transform.localPosition+ wayEnemy).Pair;
                    }
                    StartCoroutine(Move(cellTemp, enemy));
                    break;
                }
            }
        }
        OnClickEventHandler((ChipComponent)component.Pair);       
    }

    private void CheckWin()
    {
        if (_whiteShips.Count == 0 || _blackShips.Any(t => t.transform.position.z == 0))
        {
            _isLock = true;
            Debug.Log("Выграли черные");
            return;
        }
        if (_blackShips.Count == 0 || _whiteShips.Any(t => t.transform.position.z == 7))
        {
            _isLock = true;
            Debug.Log("Выграли белые");
            return;
        }
    }

    private Vector3 VectorWay(NeighborType i)
    {
        switch ((NeighborType)i)
        {
            case NeighborType.TopLeft: return new Vector3(-1, 0, 1);
            case NeighborType.TopRight: return new Vector3(1, 0, 1);
            case NeighborType.BottomLeft: return new Vector3(-1, 0, -1);
            case NeighborType.BottomRight: return new Vector3(1, 0, -1);
            default: throw new NotImplementedException();
        }
        
    }

    private IEnumerator Move(BaseClickComponent component, ChipComponent enemy=null)
    {
        _observer?.Serialize(_selectChip.transform.ToCoordiante().ToSerializable(_ColorTeam, RecordType.Move, component.transform.ToCoordiante()));
        

        _isLock = true;
        if (_selectChip.GetColor == ColorType.White)
            _ColorTeam = ColorType.Black;
        else
            _ColorTeam = ColorType.White;
        for (byte i = 0; i < 4; i++)
        {
            ((CellComponent)_selectChip?.Pair)?.GetNeighbors((NeighborType)i)?.RemoveAdditionalMaterial();
        }
        float angle = this.gameObject.transform.rotation.y;
        if (angle == 0f)
            angle = 180f;
        else
            angle = 0f;
        while (_selectChip.transform.localPosition!=component.transform.localPosition || (this.gameObject.transform.rotation!= Quaternion.Euler(0,0,0)&& this.gameObject.transform.rotation != Quaternion.Euler(0, 180, 0)))
        {
            _selectChip.transform.localPosition=Vector3.MoveTowards(_selectChip.transform.localPosition, component.transform.localPosition, _speedChip);
                this.gameObject.transform.rotation = Quaternion.RotateTowards(this.gameObject.transform.rotation, Quaternion.Euler(0f,angle, 0f),_speedCamera); 
             yield return null;
        }

        DestroyChip(enemy);
        _selectChip.Pair.RemoveAdditionalMaterial();
        _selectChip.Pair.Pair = null;
        component.Pair = _selectChip;
        _selectChip.Pair = component;
        _selectChip = null;
        _isLock = false;
        CheckWin();

        StepOver?.Invoke();
    }


    private void DestroyChip(ChipComponent enemy)
    {
        if (enemy == null) return;
        if (enemy.GetColor == ColorType.White)
        {
            enemy = _whiteShips.First(t => t == enemy);
            _whiteShips.Remove(enemy);
            _isLock = true;
        }
        else
        {
            enemy = _blackShips.First(t => t == enemy);
            _blackShips.Remove(enemy);
            _isLock = true;
        }

        _observer?.Serialize(enemy.transform.ToCoordiante().ToSerializable(_ColorTeam, RecordType.Remove));

        enemy.Pair.Pair = null;
        enemy.Pair = null;
        Destroy(enemy.gameObject);
        StepOver?.Invoke();
    }

    private void ChipCreate(ChipComponent Chip)
    {
        Chip.Pair= _blackCell.FirstOrDefault(t=>t.transform.localPosition == Chip.transform.localPosition);
        Chip.Pair.Pair = Chip;
        Chip.OnClickEventHandler += Chip_OnClickEventHandler;
        Chip.OnFocusEventHandler += OnFocusEventHandler;
    }

    private void Chip_OnClickEventHandler(BaseClickComponent component)
    {
        OnClickEventHandler((ChipComponent)component);
    }


    private void OnFocusEventHandler(CellComponent component, bool isSelect)
    {
        if (component == null || component.Pair == null) return;
        if (component.Pair == _selectChip || component == _selectChip) return;
        if (isSelect)
        {
            component.AddAdditionalMaterial(_materialEnter);
        }
        else 
        {
            component.RemoveAdditionalMaterial();
        }
    }

    private void OnClickEventHandler(ChipComponent component)
    {
        if (_isLock) return;
        if (component == null) return;
        if (component.GetColor != _ColorTeam)
        {
            Debug.Log("Ход противоположной стороны");
            return;
        }

        _observer?.Serialize(component.transform.ToCoordiante().ToSerializable(_ColorTeam,RecordType.Click));
        
        if (_selectChip != null)
        {
            _selectChip.Pair.RemoveAdditionalMaterial();
            for (byte i = 0; i < 4; i++)
            {
                ((CellComponent)_selectChip?.Pair)?.GetNeighbors((NeighborType)i)?.RemoveAdditionalMaterial();
            }
            if (_selectChip == component)
            {               
                _selectChip = null;
                return;
            }            
        }
        _selectChip = component;
        _selectChip.Pair.AddAdditionalMaterial(_materialClick);
       ((CellComponent)_selectChip.Pair).Configuration(FindMove((CellComponent)_selectChip.Pair));
       
        for (byte i=0;i<4;i++)
        {
           ((CellComponent)_selectChip.Pair).GetNeighbors((NeighborType)i)?.AddAdditionalMaterial(_materialEnter);
        }
        StepOver?.Invoke();
    }

    private Dictionary<NeighborType, CellComponent> FindMove(CellComponent selectCell)
    {
        Dictionary<NeighborType, CellComponent> configuration = new Dictionary<NeighborType, CellComponent>()
        {
            { NeighborType.TopLeft,null},
            {NeighborType.TopRight,null},
            {NeighborType.BottomLeft,null},
            {NeighborType.BottomRight,null}
        };
        if (selectCell.Pair == null) return configuration;
        var celllist = _blackCell.Where(t =>
          (t.Pair == null || t.Pair?.GetColor != selectCell.Pair.GetColor) &&
          (int)(selectCell.transform.localPosition - t.transform.localPosition).x <= 2 &&
          (int)(selectCell.transform.localPosition - t.transform.localPosition).x >= -2 &&
          (int)(selectCell.transform.localPosition - t.transform.localPosition).z <= 2 &&
          (int)(selectCell.transform.localPosition - t.transform.localPosition).z >= -2
            );
        foreach (var cell in celllist)
        {
            bool isEnemy = cell.Pair != null && cell.Pair.GetColor != _ColorTeam;
            if (selectCell.Pair.GetColor == ColorType.White)
            {
                //TopLeft
                if (cell.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.TopLeft))
                {
                    if (isEnemy)
                        configuration[NeighborType.TopLeft] = celllist.FirstOrDefault(t =>
                        t.Pair == null &&
                        t.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.TopLeft) * 2);
                    else
                        configuration[NeighborType.TopLeft] = cell;
                }
                else
                //TopRight
                if (cell.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.TopRight))
                {
                    if (isEnemy)
                        configuration[NeighborType.TopRight] = celllist.FirstOrDefault(t =>
                        t.Pair == null &&
                        t.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.TopRight) * 2);
                    else
                        configuration[NeighborType.TopRight] = cell;
                }
            }
            else
            //BottomLeft
            if (cell.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.BottomLeft))
            {
                if (isEnemy)
                    configuration[NeighborType.BottomLeft] = celllist.FirstOrDefault(t =>
                    t.Pair == null &&
                    t.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.BottomLeft) * 2);
                else
                    configuration[NeighborType.BottomLeft] = cell;
            }
            else
            //BottomRight
            if (cell.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.BottomRight))
            {
                if (isEnemy)
                    configuration[NeighborType.BottomRight] = celllist.FirstOrDefault(t =>
                    t.Pair == null &&
                    t.transform.localPosition == selectCell.transform.localPosition + VectorWay(NeighborType.BottomRight) * 2);
                else
                    configuration[NeighborType.BottomRight] = cell;
            }
        }
        return configuration;
    }

    
}
