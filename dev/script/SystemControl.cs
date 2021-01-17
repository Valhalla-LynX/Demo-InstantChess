using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 人为操作控制按系统坐标控制
// 需要转换坐标控制棋子
public class SystemControl : MonoBehaviour
{
    // UI-选择的棋子
    public Text mt, lt, mm;
    private Texture2D temp_texture2d;
    private SpriteRenderer schess_sprite;
    private Sprite temp_sprite;

    // 我选择的棋子
    private Chess now_select;
    private GameObject selectedFrame;

    private Ray now_mouse_ray;
    private RaycastHit mouseRightHit;
    private Coord mouseCoord = new Coord();

    // 触控
    private bool newTouch = false;
    private float touchTime;

    void Start()
    {
        mt = GameObject.Find("mtvalue").GetComponent<Text>();
        lt = GameObject.Find("ltvalue").GetComponent<Text>();
        mm = GameObject.Find("mmvalue").GetComponent<Text>();

        selectedFrame = GameObject.Find("selectedFrame");
        schess_sprite = GameObject.Find("schess").GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        GetMouseCoordByPos();
        UpdateUI();
        MoveOperation();
    }

    // 选中与移动操作
    #region
    public void SetNowSelect(Chess now)
    {
        now_select = now;
        UpdateSelectUI(now);
    }
    private void GetMouseCoordByPos()
    {
        now_mouse_ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(now_mouse_ray, out mouseRightHit, 1000, LayerMask.GetMask("MapCube"));
        mouseCoord.col = (int)Math.Ceiling((mouseRightHit.point.x - BoardAttr.basic_half_unit) / BoardAttr.basic_unit + BoardAttr.coordColOffset);
        if (mouseRightHit.point.y < 0 && mouseRightHit.point.y > BoardAttr.min_rb)
        {
            mouseCoord.row = BoardAttr.coordRowHalf - (int)Math.Round((mouseRightHit.point.y + BoardAttr.offset_unit_y) / BoardAttr.basic_unit);
        }
        else if (mouseRightHit.point.y < BoardAttr.max_bt)
        {
            mouseCoord.row = BoardAttr.coordRowHalfFromZero - (int)Math.Round((mouseRightHit.point.y - BoardAttr.offset_unit_y) / BoardAttr.basic_unit);
        }
    }
    private void MoveOperation()
    {
        if (now_select != null && Input.GetMouseButtonDown(1))
        {
            now_select.SendCMDMove(mouseCoord);
        }
        // 触摸长按行动
        /*if (now_select != null && Input.GetMouseButton(0))
        {
            //记录触摸
            Touch touch = Input.GetTouch(0);
            //触摸刚开始
            if (touch.phase == TouchPhase.Began)
            {
                //设置bool触摸为真，且记录时间
                newTouch = true;
                touchTime = Time.time;

            }   //触摸静止
            else if (touch.phase == TouchPhase.Stationary)
            {
                if (newTouch == true && (Time.time - touchTime) >= 0.3f)
                {
                    now_select.MoveToCoordinate(chess_coordinate, MouseInDown());
                    newTouch = false;
                }
                //其他情况
            }
            else
            {
                newTouch = false;
            }
        }*/
    }
    #endregion

    // UI更新
    #region
    private void UpdateSelectUI(Chess now)
    {
        mt.text = now.chessType.move_time + "秒";
        lt.text = now.chessType.load_time + "秒";
        temp_texture2d = (Texture2D)Resources.Load("chess/c_" + (now.isRed?-1:1)*(int)now.chessTypeId);
        temp_sprite = Sprite.Create(temp_texture2d, new Rect(0, 0, 84, 84), Vector2.zero);
        schess_sprite.sprite = temp_sprite;
    }
    private void UpdateUI()
    {
        mm.text = Chess.inMovingCount + "/" + Chess.maxMovingCount;
        Chess.SetObjPosByCoord(selectedFrame, mouseCoord);
    }
    #endregion
}
