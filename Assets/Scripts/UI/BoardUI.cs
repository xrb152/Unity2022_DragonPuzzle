 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RescuePrincess.Managers;
 
//电子邮件puhalskijsemen@gmail.com
//源码网站 开vpn全局模式打开 http://web3incubators.com/
//电报https://t.me/gamecode999
//网页客服 http://web3incubators.com/kefu.html

public class BoardUI : MonoBehaviour
{
    public Text quantityText;
    public SpriteRenderer boardSprite;
    public Color[] colors; // Giữ lại để backward compatibility
    public int colorIndex;

    // Start is called before the first frame update
    void Start()
    {
        colorIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetBoard(int quantity, int colorIndex)
    {
        quantityText.text = quantity.ToString();
        
        // Sử dụng ColorConfig từ GameManager nếu có
        if (GameManager.Instance?.BoardManager?.colorConfig != null)
        {
            ColorData colorData = GameManager.Instance.BoardManager.colorConfig.GetColorByIndex(colorIndex);
            if (colorData != null)
            {
                boardSprite.color = colorData.colorTint;
                return;
            }
        }
        
        // Fallback: sử dụng mảng colors cũ
        if (colors.Length == 0)
            return;
        if (colorIndex <= colors.Length - 1)
        {
            boardSprite.color = colors[colorIndex];
        }
        else
        {
            boardSprite.color = colors[colors.Length - 1];
        }
    }
    public void SetBoardDefaultColor(int quantity)
    {
        quantityText.text = quantity.ToString();
        boardSprite.color = colors[0];
    }
    public void SetBoardNextColor(int quantity)
    {
        quantityText.text = quantity.ToString();
        colorIndex++;
        
        // Sử dụng ColorConfig từ GameManager nếu có
        if (GameManager.Instance?.BoardManager?.colorConfig != null)
        {
            ColorData colorData = GameManager.Instance.BoardManager.colorConfig.GetColorByIndex(colorIndex);
            if (colorData != null)
            {
                boardSprite.color = colorData.colorTint;
                return;
            }
        }
        
        // Fallback: sử dụng mảng colors cũ
        if (colors.Length == 0)
            return;
        if (colorIndex <= colors.Length - 1)
        {
            boardSprite.color = colors[colorIndex];
        }
        else
        {
            boardSprite.color = colors[colors.Length - 1];
        }
    }
}
