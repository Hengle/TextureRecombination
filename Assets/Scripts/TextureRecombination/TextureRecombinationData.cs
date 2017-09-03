using UnityEngine;
using System;
using System.Collections.Generic;


namespace Assets.UI.TextureRecombination
{
    public enum enDownSampleLevel
    {
        Level_1 = 1,
        Level_2 = 2,
        Level_4 = 4,
    }



    [Serializable]
    public class CConvertFormatData
    {
        public int validWidth;   //格子有效像素宽度
        public int validHeight;  //格子有效像素高度
        public int padding;      //四周Padding

        public int srcTexWidth;
        public int srcTexHeight;

        public int dstTexWidth;
        public int dstTexHeight; 

        //以左下角为原点，加上padding ，需要的宽度
        public int splitWidth
        {
            get
            {
                return validWidth + padding * 2; 
            }
        }

        public int splitHeight
        {
            get
            {
                return validHeight + padding * 2 ;
            }
        }


        public CConvertFormatData( 
            int srcWidth ,
            int srcHeight ,
            int dstWidth,
            int dstHeight,
            int width, 
            int height ,
            int borderPadding )
        {
            srcTexWidth = srcWidth;
            srcTexHeight = srcHeight;
            dstTexWidth = dstWidth;
            dstTexHeight = dstHeight;
            validWidth = width;
            validHeight = height ;
            padding = borderPadding;
        }
     
    }


    //采样的数据结构
    public class SampleBlockDetail
    {
        //实际宽度
        public int rw
        {
            get
            {
                return rvw + padding * 2; 
            }
        }

        public int rh
        {
            get
            {
               return rvh + padding * 2 ; 
            }
        }


        public int rvw
        {
            get
            {
                return vw / (int)downSampleLv; 
            }
        }
        

        public int rvh
        {
            get
            {
                return vh / (int)downSampleLv; 
            }
        }

        public int vw;  //有效像素长宽
        public int vh; 

        //整块图素
        public int oblx;  //像素坐标,在目标纹理上的bottom-left，包含padding
        public int obly;

        public int otlx
        {
            get
            {
                return oblx; 
            }
        }

        public int otly
        {
            get
            {
                return otry; 
            }
        }


        public int otrx
        {
            get
            {
                return oblx + rw -1 ; 
            }
        }

        public int otry
        {
            get
            {
                return obly + rh - 1 ; 
            }
        }

        public int obrx
        {
            get
            {
                return otrx;
            }
        }

        public int obry
        {
            get
            {
                return obly; 
            }
        }



        //有效图素
        public int vblx
        {
            get
            {
                return oblx + padding; 
            }
        }

        public int vbly
        {
            get
            {
                return obly + padding; 
            }

        }

        public int vtlx
        {   
            get
            {
                return vblx; 
            }    
        }

        public int vtly
        {
            get
            {
                return vtry;
            }

        }


        public int vtrx
        {
            get
            {
                return vblx + rvw - 1;
            }
        }


        public int vtry
        {
            get
            {
                return vbly + rvh - 1 ;
            }
        }


        public int vbrx
        {
            get
            {
                return vtrx;
            }
        }

        public int vbry
        {
            get
            {
                return vbly; 
            }
        }

    
        public int px;  //顶点位置
        public int py; 


        public bool IsFilped;  //是否翻转过了
        public int padding;

        #region 调试用
        public float avgAlpha;
        #endregion 

        public enDownSampleLevel downSampleLv = enDownSampleLevel.Level_1;
        public Color[] colors;


        public void Set(
           int posX,
           int posY,
           int validWidth,
           int validHeight,
           int borderPadding,
           Color[] inColors
           )
        {
            vw = validWidth;
            vh = validHeight;

            px = posX;
            py = posY;

            padding = borderPadding;

            colors = inColors;
            if (colors != null)
            {
                int nColCnt = colors.Length;
                float averageAlpha = 0.0f;
                int alphaBelow0_3 = 0;
                float alphaBelow0_3Pencent = 0.0f;
                int alphaBelow0_1 = 0;
                float alphaBelow0_1Pencent = 0.0f;

                for (int i = 0; i < colors.Length; ++i)
                {
                    float a = colors[i].a; 
                    averageAlpha += a ;

                    if( a <= 0.1f )
                    {
                        alphaBelow0_1++;
                    }
                    else if( a < 0.3f )
                    {
                        alphaBelow0_3++;
                    }
                }

                averageAlpha /= nColCnt; 
                avgAlpha = averageAlpha;
                alphaBelow0_3Pencent = (float)alphaBelow0_3 / (float)nColCnt;
                alphaBelow0_1Pencent = (float)alphaBelow0_1 / (float)nColCnt; 

                if (averageAlpha <= 0.1)  //缩小16分
                {
                    downSampleLv = enDownSampleLevel.Level_4;
                }
                else if (averageAlpha <= 0.3) //宽高减半
                {
                    if (alphaBelow0_3Pencent >= 0.5f)
                    {
                        downSampleLv = enDownSampleLevel.Level_2;
                    }
                    else
                    {
                        downSampleLv = enDownSampleLevel.Level_1;
                    } 
                }
                else  //不变
                {
                    downSampleLv = enDownSampleLevel.Level_1;
                }
            }
        }
    }




    [Serializable]
    public class TexBlockDetail
    {
        //在新图的UV信息
        public Vector2 uvBL; //bottom-left ;
        public Vector2 uvTL; //top-left 
        public Vector2 uvTR; //top-right ;
        public Vector2 uvBR; //bottom-BR ;

        //顶点位置 
        public Vector3 posBL;
        public Vector3 posTL;
        public Vector3 posTR;
        public Vector3 posBR;

        //public float avgAlpha; 

        public TexBlockDetail()
        {
            uvTL = new Vector2();
            uvTR = new Vector2();
            uvBL = new Vector2();
            uvBR = new Vector2();

            posBL = new Vector2();
            posTL = new Vector2();
            posTR = new Vector2();
            posBR = new Vector2();
        }
    }

    public class TextureRecombinationData : ScriptableObject
    {
        public Sprite refSprite;
        public Texture2D refTexture;
        public string texAssetPath;

        //转换信息
        public CConvertFormatData convertFormat = new CConvertFormatData(0,0,0,0,0,0,0);
        //拆分信息
        public List<TexBlockDetail> texBlockDetails = new List<TexBlockDetail>();

        public void CopyForm(TextureRecombinationData other)
        {
            if (other != null)
            {
                convertFormat = other.convertFormat;
                refTexture = other.refTexture;
                refSprite = other.refSprite;
                texAssetPath = other.texAssetPath; 
                texBlockDetails = new List<TexBlockDetail>(other.texBlockDetails);
            }
        }
    }

}



