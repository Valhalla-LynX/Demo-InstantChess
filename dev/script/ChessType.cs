using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public enum ChessTypeId
{
    [Description("Empty")]
    Empty = 0,
    [Description("JIANG")]
    JIANG = 1,
    [Description("SHI")]
    SHI = 2,
    [Description("XIANG")]
    XIANG = 3,
    [Description("MA")]
    MA = 4,
    [Description("CHE")]
    CHE = 5,
    [Description("PAO")]
    PAO = 6,
    [Description("BING")]
    BING = 7
}

static class EnumExtensions
{
    public static string GetDescription(this Enum val)
    {
        var field = val.GetType().GetField(val.ToString());
        var customAttribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return customAttribute == null ? val.ToString() : ((DescriptionAttribute)customAttribute).Description;
    }
}

public abstract class ChessType
{
    public static readonly string nameReadyTime = "readyTime";
    public static readonly string nameMoveTime = "moveTime";
    public static readonly string nameLoadTime = "loadTime";

    public int id;
    public float ready_time;
    public float move_time;
    public float load_time;

    protected ChessType() {
        id = 0;       
        ready_time = 100f;
        move_time = 100f;
        load_time = 100f;
    }
    protected ChessType(ChessTypeId chessTypeId, float default_rt, float default_mt, float default_wt)
    {
        id = (int)chessTypeId;
        INIParser iniParser = new INIParser();
        iniParser.Open(Application.streamingAssetsPath + "/ChessAttr.ini");        
        ready_time = Convert.ToSingle(iniParser.ReadValue(chessTypeId.GetDescription(), nameReadyTime, default_rt));
        move_time = Convert.ToSingle(iniParser.ReadValue(chessTypeId.GetDescription(), nameMoveTime, default_mt));
        load_time = Convert.ToSingle(iniParser.ReadValue(chessTypeId.GetDescription(), nameLoadTime, default_wt));
        iniParser.Close();

    }
    public virtual ChessType GetChessType() { return null; }
    public abstract bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board);

    public bool InLegalArea(Coord coord)
    {
        if (coord.row >= BoardAttr.Zero && coord.row < BoardAttr.maxCoordLength.row &&
            coord.col >= BoardAttr.Zero && coord.col < BoardAttr.maxCoordLength.col)
        {
            return true;
        }
        return false;
    }

    // 不含终点
    public int InLineChessHave(Coord coord, Coord tarCoord, string[][] board)
    {
        if (coord.row == tarCoord.row)
        {
            int min; int max;int sum = 0;
            if (coord.col < tarCoord.col)
            {
                min = coord.col + 1; max = tarCoord.col;
            }
            else
            {
                min = tarCoord.col + 1; max = coord.col;
            }
            for (int i = min; i < max; i++)
            {
                if (board[coord.row][i] != null)
                {
                    sum++;
                }
            }
            return sum;
        } else if (coord.col == tarCoord.col)
        {
            int min; int max; int sum = 0;
            if (coord.row < tarCoord.row)
            {
                min = coord.row + 1; max = tarCoord.row;
            }
            else
            {
                min = tarCoord.row + 1; max = coord.row;
            }
            for (int i = min; i < max; i++)
            {
                if (board[i][coord.col] != null)
                {
                    sum++;
                }
            }
            return sum;
        } else
        {
            // 不为直线
            return int.MaxValue;
        }
    }
}

public class Empty : ChessType
{
    private static ChessType _single;
    private Empty() : base() { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new Empty();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        return false;
    }
}
public class JIANG : ChessType
{
    private static ChessType _single;
    private JIANG() : base(ChessTypeId.JIANG, 0, 2f, 2f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new JIANG();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        if (InLegalArea(tarCoord))
        {
            if (Math.Abs(tarCoord.row - coord.row) +
                Math.Abs(tarCoord.col - coord.col) == 1)
            {
                if (isRed)
                {
                    if (tarCoord.row >= BoardAttr.rPalace[0].row && tarCoord.row <= BoardAttr.rPalace[1].row &&
                        tarCoord.col >= BoardAttr.rPalace[0].col && tarCoord.col <= BoardAttr.rPalace[1].col)
                    {
                        return true;
                    }
                }
                else
                {
                    if (tarCoord.row >= BoardAttr.bPalace[0].row && tarCoord.row <= BoardAttr.bPalace[1].row &&
                        tarCoord.col >= BoardAttr.bPalace[0].col && tarCoord.col <= BoardAttr.bPalace[1].col)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
        }
}
public class SHI : ChessType
{
    private static ChessType _single;
    public SHI() : base(ChessTypeId.SHI, 0, 1f, 1f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new SHI();
        }
        return _single;
    }

     public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
            if (InLegalArea(tarCoord))
            {
                if (Math.Abs(tarCoord.row - coord.row) == 1 &&
                    Math.Abs(tarCoord.col - coord.col) == 1)
                {
                if (isRed)
                {
                    if (tarCoord.row >= BoardAttr.rPalace[0].row && tarCoord.row <= BoardAttr.rPalace[1].row &&
                        tarCoord.col >= BoardAttr.rPalace[0].col && tarCoord.col <= BoardAttr.rPalace[1].col)
                    {
                        return true;
                    }
                }
                else
                {
                    if (tarCoord.row >= BoardAttr.bPalace[0].row && tarCoord.row <= BoardAttr.bPalace[1].row &&
                        tarCoord.col >= BoardAttr.bPalace[0].col && tarCoord.col <= BoardAttr.bPalace[1].col)
                    {
                        return true;
                    }
                }
            }
            }
            return false;
        }
    }
public class XIANG : ChessType
{
    private static ChessType _single;
    public XIANG() : base(ChessTypeId.XIANG, 0, 2f, 2f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new XIANG();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        if (InLegalArea(tarCoord))
        {
            if (Math.Abs(tarCoord.row - coord.row) == 2 &&
                Math.Abs(tarCoord.col - coord.col) == 2)
            {
                if (isRed && tarCoord.row >= BoardAttr.coordRowHalf)
                {
                    return true;
                }
                else if (!isRed && tarCoord.row < BoardAttr.coordRowHalf)
                {
                    return true;
                }

            }
        }
        return false;
    }
}
public class MA : ChessType
{
    private static ChessType _single;
    public MA() : base(ChessTypeId.MA, 0, 2f, 2f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new MA();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        if (InLegalArea(tarCoord))
        {
            if ((Math.Abs(tarCoord.row - coord.row) == 1 && Math.Abs(tarCoord.col - coord.col) == 2) ||
                (Math.Abs(tarCoord.row - coord.row) == 2 && Math.Abs(tarCoord.col - coord.col) == 1))
            {
                 return true;
            }
        }
        return false;
    }
}
public class CHE : ChessType
{
    private static ChessType _single;
    public CHE() : base(ChessTypeId.CHE, 0, 3f, 3f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new CHE();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        if (InLegalArea(tarCoord))
        {
            if (InLineChessHave(coord, tarCoord, board) == 0)
            {
                return true;
            }
        }
        return false;
    }
}
public class PAO : ChessType
{
    private static ChessType _single;
    public PAO() : base(ChessTypeId.PAO, 0, 1f, 1f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new PAO();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        if (InLegalArea(tarCoord))
        {
            int linehave = InLineChessHave(coord, tarCoord, board);
            // 直线移动无阻碍，终点无目标。终点已确认不能为同一方。
            // 跳跃吃子一个阻碍，终点又目标。终点已确认不能为同一方。
            if ((linehave == 0 && board[tarCoord.row][tarCoord.col] == null) ||
                (linehave == 1 && board[tarCoord.row][tarCoord.col] != null))
            {
                return true;
            }
        }
        return false;
    }
}
public class BING : ChessType
{
    private static ChessType _single;
    public BING() : base(ChessTypeId.BING, 0, 1f, 1f) { }
    public new static ChessType GetChessType()
    {
        if (_single == null)
        {
            _single = new BING();
        }
        return _single;
    }

    public override bool LegalCoord(Coord coord, Coord tarCoord, bool isRed, string[][] board)
    {
        if (InLegalArea(tarCoord))
        {
            if(isRed)
            {
                if (coord.row >= BoardAttr.coordRowHalf &&
                    tarCoord.row - coord.row == -1 && (Math.Abs(tarCoord.col - coord.col) == 0))
                {
                    return true;
                } else if (coord.row < BoardAttr.coordRowHalf &&
                           (tarCoord.row - coord.row == -1 && (Math.Abs(tarCoord.col - coord.col) == 0) ||
                            tarCoord.row - coord.row == 0 && (Math.Abs(tarCoord.col - coord.col) == 1)))
                {
                    return true;
                }
            }
            else
            {
                if (coord.row < BoardAttr.coordRowHalf &&
                    tarCoord.row - coord.row == 1 && (Math.Abs(tarCoord.col - coord.col) == 0))
                {
                    return true;
                }
                else if (coord.row >= BoardAttr.coordRowHalf &&
                         (tarCoord.row - coord.row == 1 && (Math.Abs(tarCoord.col - coord.col) == 0) ||
                          tarCoord.row - coord.row == 0 && (Math.Abs(tarCoord.col - coord.col) == 1)))
                {
                    return true;
                }
            }
        }
        return false;
    }
}