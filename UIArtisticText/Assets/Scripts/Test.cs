using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using System;
public class Test : MonoBehaviour {
    UIArtisticText comps;
    // public float ptConvertPxRatio = 1.3333f;
    private Sprite[] spriteList;
    public Text text;
    // public Image image;
    // Start is called before the first frame update
    private string[] numKeys= {
        "0","1","2","3","4","5","6","7","8","9"
    };

    enum Fontlib {
        NumFont = 230,
    }
    void Start() {
        SetUIArtisticText(Fontlib.NumFont,"123442556",230);
        // Sprite[] sprites = Resources.LoadAll<Sprite>("Atlas/Nums");
        print(text.preferredHeight);
    }

    void SetUIArtisticText(Fontlib font,string content,int fontSize) {
        // SpriteAtlas atlas = Resources.Load<SpriteAtlas>("Atlas/NumFont");
        Sprite[] atlas = Resources.LoadAll<Sprite>("Atlas/Nums");
        spriteList = new Sprite[content.Length];
        UIArtisticText comps = gameObject.GetComponent<UIArtisticText>();
        if (comps == null) {
            comps = gameObject.AddComponent<UIArtisticText>();
        }
        for (int i = 0; i < content.ToString().Length; i++) {
            // print(content[i].ToString());
            Sprite sp = atlas[Convert.ToInt32(content[i].ToString())];
            // print(sp.name);
            // image.sprite = sp;
            spriteList[i] = sp;
        }
        
        comps.scale = fontSize / 230f;
        // print(comps.scale);
        comps.SetSpriteArray(spriteList);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
