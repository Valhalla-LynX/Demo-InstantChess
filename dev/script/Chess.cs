using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Serialization;
using Mirror;
using UnityEditor;

public class Coord
{
    public static readonly Coord Zero = new Coord();
    public static readonly Coord Moving = new Coord(-1, -1);

    public int row { get; set; }
    public int col { get; set; }

    public Coord()
    { }

    public Coord(int row, int col)
    {
        this.row = row; this.col = col;
    }

    public void Set(int row, int col)
    {
        this.row = row; this.col = col;
    }

    public void Set(Coord coord)
    {
        row = coord.row; col = coord.col;
    }

    public void SetMoving()
    {
        row = -1; col = -1;
    }

    public override string ToString()
    {
        return "row:" + row + " col:" + col;
    }
}

public struct BoardAttr
{
    public static readonly int Zero = 0;
    public static readonly float chess_r = 0.3f;
    public static readonly float offset_unit_y = 0.34f;
    public static readonly float offset_click = offset_unit_y - chess_r;
    public static readonly float basic_unit = 0.66f;
    public static readonly float basic_half_unit = basic_unit / 2;
    public static readonly float min_l = -4.5f * basic_unit;
    public static readonly float max_r = 4.5f * basic_unit;
    public static readonly float max_bt = 5 * basic_unit + (offset_click);
    public static readonly float min_bb = 5 * basic_unit + (offset_click);
    public static readonly float max_rt = -5 * basic_unit - (offset_click);
    public static readonly float min_rb = -5 * basic_unit - (offset_click);
    public static readonly Coord maxCoordLength = new Coord(10, 9);
    public static readonly int coordRowHalfFromZero = maxCoordLength.row / 2 - 1;
    public static readonly int coordRowHalf = maxCoordLength.row / 2;
    public static readonly int coordColOffset = maxCoordLength.col / 2;
    public static readonly Coord[] bPalace = new Coord[2] { new Coord(0, 3), new Coord(3, 5) };
    public static readonly Coord[] rPalace = new Coord[2] { new Coord(7, 3), new Coord(9, 5) };
    public static readonly int eatenLength = 4;
    public static readonly float eatenChessLength = 0.7f;
    public static readonly float eatenChessWidthOffset = -6f;
    public static readonly float eatenChessHeightOffset = 3f;
}

// 棋子控制按坐标控制
public class Chess : MonoBehaviour
{
    public static Dictionary<string, GameObject> chessMap = new Dictionary<string, GameObject>();
    private static SystemControl control;
    private NetPlayer netPlayer;

    public static readonly int maxMovingCount = 3;
    public static int inMovingCount = 0;

    /*
    * 棋子xOy坐标系
    * 数组坐标arr[x][y]
    *  O y→
    *  x
    * ↓
    */
    // 吃子棋盘状态 只记录静止棋子 计算吃子状态 需要同步
    public static string[][] chessBoard;
    // 双方落点棋盘 棋子移动前更新落点 防止重合
    public static string[][] myBoard;

    public ChessTypeId chessTypeId;
    public bool isRed;

    [Header("InitCoord")]
    [FormerlySerializedAs("_coordRow")]
    [Tooltip("初始化坐标位置x，-1表示正在移动")]
    [Range(-1, 9)]
    public int initCoordRow;
    [FormerlySerializedAs("_coordCol")]
    [Tooltip("初始化当前坐标位置y，-1表示正在移动")]
    [Range(-1, 8)]
    public int initCoordCol;

    [HideInInspector]
    public ChessType chessType;
    public Coord coord = new Coord();
    private Motion motion = new Motion();
    private class Motion
    {
        public bool isPreparing = false;
        public float preparingWait;
        public bool isWaiting = false;
        public float waitTime;
        public bool isMoving = false;
        public Coord tarCoord = new Coord();
        public Vector3 tarPos;
        public float speed;
    };

    void Awake()
    {
        if (control == null)
            control = GameObject.Find("board").GetComponent<SystemControl>();

        netPlayer = transform.parent.gameObject.GetComponent<NetPlayer>();

        if (!chessMap.ContainsKey(gameObject.name))
            chessMap.Add(gameObject.name, gameObject);
        else
            chessMap[gameObject.name] = gameObject;

        coord.Set(initCoordRow, initCoordCol);

        switch (chessTypeId)
        {
            case ChessTypeId.Empty:
                chessType = Empty.GetChessType();
                break;
            case ChessTypeId.JIANG:
                chessType = JIANG.GetChessType();
                break;
            case ChessTypeId.SHI:
                chessType = SHI.GetChessType();
                break;
            case ChessTypeId.XIANG:
                chessType = XIANG.GetChessType();
                break;
            case ChessTypeId.MA:
                chessType = MA.GetChessType();
                break;
            case ChessTypeId.CHE:
                chessType = CHE.GetChessType();
                break;
            case ChessTypeId.PAO:
                chessType = PAO.GetChessType();
                break;
            case ChessTypeId.BING:
                chessType = BING.GetChessType();
                break;
            default:
                chessType = Empty.GetChessType();
                break;
        }
    }

    void Start()
    {
        // 涉及net部分在Awake中不生效（netid尚未执行）
        // 因此棋盘初始化放到Start中实现
        if (chessBoard == null)
            InitBoard();
        if (IsMyChess())
            myBoard[coord.row][coord.col] = gameObject.name;
        chessBoard[coord.row][coord.col] = gameObject.name;
        SetObjPosByCoord(gameObject, coord);
    }

    void Update()
    {
        Preparing();
        MovingToCoord();
        Loading();
    }

    // 鼠标点击
    public void OnMouseDown()
    {
        if (IsMyChess())
        {
            control.SetNowSelect(this);
        }
    }

    public bool IsMyChess()
    {
        return netPlayer.isLocalPlayer;
    }

    // 行为调用 与 坐标切换
    #region
    // 本地许可cmd发送
    public void SendCMDMove(Coord tarCoord)
    {
        Debug.Log(LegalMyTargetCoord(tarCoord));
        Debug.Log(chessType.LegalCoord(coord, tarCoord, isRed, chessBoard));
        if (IsMyChess() &&
            !motion.isPreparing && !motion.isMoving && !motion.isWaiting &&
            inMovingCount < maxMovingCount &&
            LegalMyTargetCoord(tarCoord) &&
            chessType.LegalCoord(coord, tarCoord, isRed, chessBoard))

        {
            netPlayer.CMDMove(name, tarCoord.row, tarCoord.col);
        }
    }
    // cmd网络rpc调用
    public void ReciveCMDMove(Coord tarCoord)
    {
        LeavingTo(coord, tarCoord);
    }
    // 将对象移动至坐标
    public static void SetObjPosByCoord(GameObject go, Coord tarCoord)
    {
        Vector2 pos = new Vector2();
        if (tarCoord.row > BoardAttr.coordRowHalf)
        {
            pos.y = (BoardAttr.coordRowHalf - tarCoord.row) * BoardAttr.basic_unit - BoardAttr.offset_unit_y;
        }
        else
        {
            pos.y = (BoardAttr.coordRowHalfFromZero - tarCoord.row) * BoardAttr.basic_unit + BoardAttr.offset_unit_y;

        }
        pos.x = (tarCoord.col - BoardAttr.coordColOffset) * BoardAttr.basic_unit;
        go.transform.localPosition = pos;
    }
    // 获取坐标对应的系统位置
    public static Vector2 GetPosByCoord(Coord coord)
    {
        Vector2 pos = new Vector2();
        if (coord.row > BoardAttr.coordRowHalf)
        {
            pos.y = (BoardAttr.coordRowHalf - coord.row) * BoardAttr.basic_unit - BoardAttr.offset_unit_y;
        }
        else
        {
            pos.y = (BoardAttr.coordRowHalfFromZero - coord.row) * BoardAttr.basic_unit + BoardAttr.offset_unit_y;

        }
        pos.x = (coord.col - BoardAttr.coordColOffset) * BoardAttr.basic_unit;
        return pos;
    }
    #endregion

    // 行为控制
    #region
    private bool LegalMyTargetCoord(Coord tarCoord)
    {
        return IsMyChess() && myBoard[tarCoord.row][tarCoord.col] == null;
    }
    private void StartPreparing()
    {
        if (chessType.ready_time > 0 && !motion.isWaiting)
        {
            motion.preparingWait = 0;
            motion.isPreparing = true;
        }
    }
    private void LeavingTo(Coord coord, Coord tarCoord)
    {
        StartPreparing();
        motion.isMoving = true;
        // 移动条件1：目标位置，更新棋盘
        motion.tarCoord.Set(tarCoord);
        // 移动棋子优先显示
        transform.position = new Vector3(transform.position.x, transform.position.y, -chessType.id);
        if (IsMyChess())
        {
            myBoard[coord.row][coord.col] = null;
            myBoard[tarCoord.row][tarCoord.col] = gameObject.name;
        }
        chessBoard[coord.row][coord.col] = null;
        coord.SetMoving();
        ++inMovingCount;
        // 移动目标2：移动速度
        Vector2 tmp = GetPosByCoord(tarCoord);
        motion.tarPos = new Vector3(tmp.x, tmp.y, -chessType.id);
        motion.speed = Vector3.Distance(transform.position, motion.tarPos) / chessType.move_time;
    }
    private void EatChess()
    {
        string tarName = chessBoard[motion.tarCoord.row][motion.tarCoord.col];
        if (tarName != null)
        {
            Chess tarChess = GameObject.Find(tarName).GetComponent<Chess>();
            if (tarChess != null && IsMyChess() ^ tarChess.IsMyChess() && !tarChess.motion.isMoving)
            {
                tarChess.enabled = false;
                chessBoard[tarChess.coord.row][tarChess.coord.col] = null;
                if (tarChess.IsMyChess())
                {
                    myBoard[tarChess.coord.row][tarChess.coord.col] = null;
                }
                if (tarChess.isRed)
                {
                    tarChess.transform.localPosition =
                        new Vector3(BoardAttr.eatenChessWidthOffset + tarChess.netPlayer.eaten % BoardAttr.eatenLength * BoardAttr.eatenChessLength,
                        BoardAttr.eatenChessHeightOffset - tarChess.netPlayer.eaten / BoardAttr.eatenLength * BoardAttr.eatenChessLength, 0);
                }
                else
                {
                    tarChess.transform.localPosition =
                        new Vector3(BoardAttr.eatenChessWidthOffset + tarChess.netPlayer.eaten % BoardAttr.eatenLength * BoardAttr.eatenChessLength,
                        -BoardAttr.eatenChessLength + tarChess.netPlayer.eaten / BoardAttr.eatenLength * -BoardAttr.eatenChessLength, 0);
                }
                ++tarChess.netPlayer.eaten;
            }
        }
    }
    private void ArrivingAt()
    {
        --inMovingCount;
        coord.Set(motion.tarCoord);
        chessBoard[coord.row][coord.col] = gameObject.name;
        // 非移动棋子滞后显示
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        motion.isMoving = false;
    }
    private void StartLoading()
    {
        if (!motion.isWaiting)
        {
            motion.waitTime = 0;
            motion.isWaiting = true;
        }
    }
    #endregion

    // 即时行为
    #region
    private void Preparing()
    {
        if (motion.isPreparing && motion.preparingWait > chessType.ready_time)
        {
            motion.isPreparing = false;
        }
        else if (motion.isPreparing)
        {
            motion.preparingWait += Time.deltaTime;
        }
    }
    private void MovingToCoord()
    {
        if (motion.isMoving && !motion.isPreparing)
        {
            transform.position = Vector3.MoveTowards(transform.position, motion.tarPos, motion.speed * Time.deltaTime);
            if (transform.position.Equals(motion.tarPos))
            {
                EatChess();
                ArrivingAt();
                StartLoading();
            }
        }
    }
    private void Loading()
    {
        if (motion.isWaiting && motion.waitTime > chessType.load_time)
        {
            motion.isWaiting = false;
        }
        else if (motion.isWaiting)
        {
            motion.waitTime += Time.deltaTime;
        }
    }
    #endregion

    private void InitBoard()
    {
        chessMap.Clear();
        chessBoard = new string[BoardAttr.maxCoordLength.row][];
        myBoard = new string[BoardAttr.maxCoordLength.row][];
        for (int i = 0; i < BoardAttr.maxCoordLength.row; i++)
        {
            chessBoard[i] = new string[BoardAttr.maxCoordLength.col];
            myBoard[i] = new string[BoardAttr.maxCoordLength.col];
        }
    }

    public void InitChess()
    {
        if (netPlayer.eaten != 0)
            netPlayer.eaten = 0;
        enabled = true;
        Awake();
        Start();
    }
}