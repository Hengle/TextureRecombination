using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.UI; 
using UnityEditor;
using MiniJSON;

namespace Assets.UI.TextureRecombination
{
    public class TextureRecombination
    {
        public static string ConfigPath = "Assets/Resources/UI/TextureRecombination/TextureRecombinationCfg.txt";
        private static Dictionary<string, CConvertFormatData> mDataDict = new Dictionary<string, CConvertFormatData>();
        private static string WRITE__ASSET_FILE_FOLDER = "Assets/Resources/UI/TextureRecombination";
        private static Color mDefaultColor = new Color(0, 0, 0, 0);


        #region common function

        public static TextureImporter GetTextureImporter(Texture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            return AssetImporter.GetAtPath(assetPath) as TextureImporter;
        }


        public static TextureImporterSettings GetTextureImporterSettings(Texture2D tex)
        {
            TextureImporterSettings settings = null;
            if (tex != null)
            {
                TextureImporter importer = GetTextureImporter(tex);
                if (importer != null)
                {
                    settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                }
            }
            return settings;
        }

        public static void SetTextureImporterSettings(Texture2D tex, TextureImporterSettings settings)
        {
            if (tex != null
                && settings != null)
            {
                TextureImporter importer = GetTextureImporter(tex);
                if (importer != null)
                {
                    importer.SetTextureSettings(settings);

                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
            }
        }

        public static void SetTextureReadble(Texture2D tex, bool bReadble)
        {
            if (tex != null)
            {
                TextureImporterSettings settings = GetTextureImporterSettings(tex);
                if (settings != null)
                {
                    settings.readable = bReadble;
                    SetTextureImporterSettings(tex, settings);
                }
            }
        }
       

        #region rotate texture 
        public static Texture2D FlipTexture(Texture2D original)
        {
            Texture2D flipped = new Texture2D(original.width, original.height);

            int xN = original.width;
            int yN = original.height;


            for (int i = 0; i < xN; i++)
            {
                for (int j = 0; j < yN; j++)
                {
                    flipped.SetPixel(xN - i - 1, j, original.GetPixel(i, j));
                }
            }
            flipped.Apply();

            return flipped;
        }


        public static void RotateTexture(Sprite target)
        {
            if (target != null
               && target.texture)
            {
                Texture2D srcTex = target.texture;

                SetTextureReadble(srcTex, true);
                Texture2D newTex = RotateTextureImpl(target.texture);
                File.WriteAllBytes(AssetDatabase.GetAssetPath(target.texture), newTex.EncodeToPNG());
                SetTextureReadble(srcTex, false);
            }
        }

        public static Texture2D RotateTextureImpl(Texture2D image)
        {
            Texture2D target = new Texture2D(image.height, image.width, image.format, false);    //flip image width<>height, as we rotated the image, it might be a rect. not a square image

            Color[] pixels = image.GetPixels();
            pixels = rotateTextureGrid(pixels, image.width, image.height);
            target.SetPixels(pixels);
            target.Apply();

            return target;
        }


        public static Color[] rotateTextureGrid(Color[] tex, int wid, int hi)
        {
            Color[] ret = new Color[wid * hi];      //reminder we are flipping these in the target

            for (int y = 0; y < hi; y++)
            {
                for (int x = 0; x < wid; x++)
                {
                    ret[(hi - 1) - y + x * hi] = tex[x + y * wid];         //juggle the pixels around

                }
            }

            return ret;
        }


        #endregion


        public static string GetKey(int w, int h)
        {
            return string.Format("{0}x{1}", w, h);
        }


        public static void LoadConfig( string path  )
        {
            StreamReader r = new StreamReader(path); 
            if( r != null )
            {
                string jsonStr = r.ReadToEnd();
                if( string.IsNullOrEmpty(jsonStr) )
                {
                    Debug.LogError( string.Format("{0} has some format error",path));
                    return;  
                }

                List<CConvertFormatData> dataList = new List<CConvertFormatData>();
                object raw = Json.Deserialize(jsonStr);
                List<object> dicts = Json.Deserialize(jsonStr) as List<object> ;
                if( dicts != null )
                {
                    mDataDict.Clear();
                    for ( int i = 0; i < dicts.Count; ++i)
                    {
                        Dictionary<string, object> dict = dicts[i] as Dictionary<string,object> ;
                        if( dict != null )
                        {
                            int srcTexWidth = Convert.ToInt32(dict["srcTexWidth"]) ;
                            int srcTexHeight = Convert.ToInt32(dict["srcTexHeight"]);
                            int dstTexWidth = Convert.ToInt32(dict["dstTexWidth"]);
                            int dstTexHeight = Convert.ToInt32(dict["dstTexHeight"]);
                            int validWidth = Convert.ToInt32(dict["validWidth"]);
                            int validHeight = Convert.ToInt32(dict["validHeight"]);
                            int padding = Convert.ToInt32(dict["padding"]);

                            CConvertFormatData tData = new CConvertFormatData(
                                srcTexWidth,
                                srcTexHeight,
                                dstTexWidth,
                                dstTexHeight,
                                validWidth,
                                validHeight,
                                padding 
                                );

                            string key = string.Format("{0}x{1}",srcTexWidth,srcTexHeight);
                            mDataDict.Add(key,tData); 
                        }
                    }
                }
            }
        }


        private static bool GetConfigData( string key , out CConvertFormatData tConvertData )
        {
            tConvertData = null; 
            bool bRet = false;
           
            LoadConfig(ConfigPath);
            if ( mDataDict != null )
            {
                bRet = mDataDict.TryGetValue(key, out tConvertData);
            }
            return bRet;
        }



        private static void SetDefaultColor(ref Texture2D tex, Color color)
        {
            if (tex != null)
            {
                SetTextureReadble(tex, true);
                for (int i = 0; i < tex.width; ++i)
                {
                    for (int j = 0; j < tex.height; ++j)
                    {
                        tex.SetPixel(i, j, color);
                    }
                }

                tex.Apply();
                SetTextureReadble(tex, false);
            }
        }

        private static void SetPixelsBlock(
            Texture2D tex,
            SampleBlockDetail blockDetail
            )
        {
            if (tex != null
                && blockDetail != null
                && blockDetail.colors != null 
                 )
            {
                Color[] colors = blockDetail.colors; 
                int w = blockDetail.IsFilped ? blockDetail.rvh : blockDetail.rvw;
                int h = blockDetail.IsFilped ? blockDetail.rvw : blockDetail.rvh;
                int padding = blockDetail.padding; 

                //居中
                int vBottomLeftX = blockDetail.vblx ;
                int vBottomLeftY = blockDetail.vbly ;

                int vTopRightX = blockDetail.vtrx;
                int vTopRightY = blockDetail.vtry; 

                int vTopLeftX = vBottomLeftX ;
                int vTopLeftY = vTopRightY;

                int vBottomRightX = vTopRightX  ;
                int vBottomRightY =  vBottomLeftY ;

                int oBottomLeftX = blockDetail.oblx ;
                int oBottomLeftY = blockDetail.obly ;

                int oTopRightX = blockDetail.otrx;
                int oTopRightY = blockDetail.otry; 

                int oTopLeftX = oBottomLeftX ;
                int oTopLeftY = oTopRightY ;

                int oBottomRightX = oTopRightX;
                int oBottomRightY = oBottomLeftY ;

                //四个角的点
                Color blCol = colors[0];
                Color tlCol = colors[(h - 1) * w];
                Color trCol = colors[w - 1 + (h - 1) * w];
                Color brCol = colors[w - 1]; 


                //将四条边外拓
                //左移一格
                if( oBottomLeftX + w <= tex.width 
                    && vBottomLeftY + h <= tex.height )
                {
                    tex.SetPixels(oBottomLeftX, vBottomLeftY, w, h, colors);
                }
                else
                {
                    Debug.LogWarning(string.Format("x:{0} y:{1} w:{2} h:{3} len:{4}",
                        oBottomLeftX,
                        vBottomLeftY,
                        w,
                        h,
                        colors.Length 
                        ) ); 
                }
              

                if (vBottomLeftX + padding + w <= tex.width
                  && vBottomLeftY + h <= tex.height)
                {
                    //右移一格
                    tex.SetPixels(vBottomLeftX + padding, vBottomLeftY, w, h, colors);
                }
                else
                {
                    Debug.LogWarning(string.Format("x:{0} y:{1} w:{2} h:{3} len:{4}",
                        vBottomLeftX + padding,
                        vBottomLeftY,
                        w,
                        h,
                        colors.Length
                        ));
                }


                if (vBottomLeftX + w <= tex.width
                  && vBottomLeftY + padding + h <= tex.height)
                {
                    ////上移一格
                    tex.SetPixels(vBottomLeftX, vBottomLeftY + padding, w, h, colors);
                }
                else
                {
                    Debug.LogWarning(string.Format("x:{0} y:{1} w:{2} h:{3} len:{4}",
                        vBottomLeftX,
                        vBottomLeftY + padding,
                        w,
                        h,
                        colors.Length
                        ));
                }


                if (vBottomLeftX + w <= tex.width
                  && vBottomLeftY - padding + h <= tex.height)
                {
                    //下移一格
                    tex.SetPixels(vBottomLeftX, vBottomLeftY - padding, w, h, colors);
                }
                else
                {
                    Debug.LogWarning(string.Format("x:{0} y:{1} w:{2} h:{3} len:{4}",
                        vBottomLeftX,
                        vBottomLeftY - padding,
                        w,
                        h,
                        colors.Length
                        ));
                }


                if (vBottomLeftX + w <= tex.width
                  && vBottomLeftY + h <= tex.height)
                {
                    //最后才画有效像素
                    tex.SetPixels(vBottomLeftX, vBottomLeftY, w, h, colors);
                }
                else
                {
                    Debug.LogWarning(string.Format("x:{0} y:{1} w:{2} h:{3} len:{4}",
                        vBottomLeftX,
                        vBottomLeftY,
                        w,
                        h,
                        colors.Length
                        ));
                }

                //bottom left 外拓
                for (int j = oBottomLeftY ;  j < vBottomLeftY ; ++j)
                {
                    for( int i = oBottomLeftX ; i < vBottomLeftX ; ++i)
                    {
                        tex.SetPixel(i, j, blCol);
                    }
                }

                //top left 外拓
                for( int j = oTopLeftY ; j > vTopLeftY; --j )
                {
                    for( int i = oTopLeftX ;  i < vTopLeftX ; ++i) 
                    {
                        tex.SetPixel(i,j,tlCol); 
                    }
                }

                //top right 外拓
                for (int j = oTopRightY; j > vTopRightY; --j)
                {
                    for (int i = oTopRightX ; i > vTopRightX ; --i)
                    {
                        tex.SetPixel(i, j, trCol);
                    }
                }


                //bottom right 外拓
                for (int j = oBottomRightY; j < vBottomRightY; ++j)
                {
                    for (int i = oBottomRightX; i > vBottomRightX; --i)
                    {
                        tex.SetPixel(i, j, brCol);
                    }
                }
            }
        }


        private static void WriteResult(
                string folder,
                string fileName,
                ref TextureRecombinationData newRecombinationData
                )
        {
            if (newRecombinationData == null
                || string.IsNullOrEmpty(newRecombinationData.texAssetPath)
                || string.IsNullOrEmpty(fileName)
                || string.IsNullOrEmpty(folder)
                )
            {
                return;
            }


            //写纹理
            File.WriteAllBytes(newRecombinationData.texAssetPath, newRecombinationData.refTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(newRecombinationData.texAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            //关联
            Texture2D tSrcTexture = AssetDatabase.LoadAssetAtPath(newRecombinationData.texAssetPath, typeof(Texture2D)) as Texture2D;
            if (tSrcTexture != null)
            {
                //设置为Sprite
                TextureImporter importer = GetTextureImporter(tSrcTexture);
                TextureImporterSettings settings = GetTextureImporterSettings(tSrcTexture);
                if (importer != null
                    && settings != null)
                {
                    settings.ApplyTextureType(TextureImporterType.Advanced, false);
                    SetTextureImporterSettings(tSrcTexture, settings);

                    //要来回设置一下,设置才会生效
                    settings.ApplyTextureType(TextureImporterType.Sprite, false);
                    SetTextureImporterSettings(tSrcTexture, settings);
                }

                newRecombinationData.refTexture = tSrcTexture;
                newRecombinationData.refSprite = AssetDatabase.LoadAssetAtPath(newRecombinationData.texAssetPath, typeof(Sprite)) as Sprite;
            }


            //写Asset文件
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            //存在就更新，不能创建，会丢失引用
            string assetFilePath = string.Format("{0}/{1}.asset", folder, fileName);
            TextureRecombinationData tRecombinationData = AssetDatabase.LoadAssetAtPath(assetFilePath, typeof(TextureRecombinationData)) as TextureRecombinationData;
            if (tRecombinationData != null)
            {
                tRecombinationData.CopyForm(newRecombinationData);
                //修改引用为已经存在的Asset文件
                newRecombinationData = tRecombinationData;
            }
            else
            {
                AssetDatabase.CreateAsset(newRecombinationData, assetFilePath);
            }

            //这个必须得加上，不然的话Asset文件不会保存到磁盘
            EditorUtility.SetDirty(newRecombinationData);
        }

        #endregion

       
        #region texture break with outer padding
        private static void GetValidSampleTexDetail(Texture2D srcTexture, CConvertFormatData tConvertData, out List<SampleBlockDetail> sampleBlockDetails )
        {
            sampleBlockDetails = new List<SampleBlockDetail>();
            if (srcTexture != null)
            {
                //水平
                for (int w = 0; w < tConvertData.srcTexWidth; w += tConvertData.validWidth)  //不从边界开始
                {
                    int vw = tConvertData.validWidth;
                    //纵向
                    for (int h = 0; h < tConvertData.srcTexHeight; h += tConvertData.validHeight)
                    {
                        int vh = tConvertData.validHeight;
                        
                        if (w + vw <= tConvertData.srcTexWidth
                           && h + vh <= tConvertData.srcTexHeight)
                        {
                            Color[] colors = srcTexture.GetPixels(w, h, vw, vh);
                            SampleBlockDetail blockDetail = new SampleBlockDetail();
                            blockDetail.Set(w, h, vw, vh,tConvertData.padding ,colors);
                            sampleBlockDetails.Add(blockDetail);
                        }
                    }
                }
            }
        }

        private static void SortBlockDetails(ref List<SampleBlockDetail> sampleBlockList)
        {
            if (sampleBlockList != null
                && sampleBlockList.Count > 1)
            {
                sampleBlockList.Sort(delegate (SampleBlockDetail ta, SampleBlockDetail tb)
                {
                    if (ta.rw * ta.rh > tb.rw * tb.rh)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                );
            }
        }


        //防止代码编译不过
        public static void RecombinationTexture(
              Sprite target,
              out TextureRecombinationData texRecombinationData)
        {
            texRecombinationData = null;
            if (target != null
                && target.texture)
            {
                Texture2D srcTexture = target.texture;
                string key = GetKey(srcTexture.width, srcTexture.height);
                CConvertFormatData tConvertData;
                if (GetConfigData(key, out tConvertData))
                {
                    //可读
                    SetTextureReadble(srcTexture, true);

                    TextureImporter importer = GetTextureImporter(srcTexture);
                    if (importer == null)
                    {
                        return;
                    }

                    texRecombinationData = ScriptableObject.CreateInstance<TextureRecombinationData>();
                    Texture2D newTex = new Texture2D(tConvertData.dstTexWidth, tConvertData.dstTexHeight, importer.DoesSourceTextureHaveAlpha() ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                    SetDefaultColor(ref newTex, mDefaultColor);
                    texRecombinationData.convertFormat = tConvertData;
                    texRecombinationData.texAssetPath = AssetDatabase.GetAssetPath(srcTexture);
                    texRecombinationData.refTexture = newTex;

                    //获取所有块的信息
                    List<SampleBlockDetail> sampleBlockDetails;
                    GetValidSampleTexDetail(srcTexture, tConvertData, out sampleBlockDetails);
                    for (int i = 0; i < sampleBlockDetails.Count; ++i)
                    {
                        SampleBlockDetail tBlockDetail = sampleBlockDetails[i];
                        DownSampleTexture(ref tBlockDetail);
                    }
                    SortBlockDetails(ref sampleBlockDetails); 

                    //计算UV信息并输出到新纹理
                    CNode rootNode = new CNode();
                    rootNode.SetRect(0, 0, tConvertData.dstTexWidth, tConvertData.dstTexHeight);

                    for (int i = 0; i < sampleBlockDetails.Count; ++i)
                    {
                        SampleBlockDetail tBlockDetail = sampleBlockDetails[i];
                        TexBlockDetail texBlock = new TexBlockDetail();
                        rootNode.Insert(newTex, tConvertData, tBlockDetail, out texBlock);
                        if (rootNode == null || texBlock == null)
                        {
                            Debug.LogWarning("out of texture range!");
                        }
                        texRecombinationData.texBlockDetails.Add(texBlock);
                    }

                    string assetFileName = string.Format(
                      "{0}x{1}To{2}x{3}_{4}_{5}",
                      texRecombinationData.convertFormat.srcTexWidth,
                      texRecombinationData.convertFormat.srcTexHeight,
                      texRecombinationData.convertFormat.dstTexWidth,
                      texRecombinationData.convertFormat.dstTexHeight,
                      tConvertData.padding,
                      Path.GetFileNameWithoutExtension(texRecombinationData.texAssetPath)
                      );


                    WriteResult(
                        WRITE__ASSET_FILE_FOLDER,
                        assetFileName,
                        ref texRecombinationData
                        );


                    SetTextureReadble(srcTexture, false);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogWarning(string.Format("No this key {0}", key));
                }
            }
        }


        private static void CalcBlockVertexAttribute(
              CConvertFormatData convertData,
              SampleBlockDetail sampleBlockDetails,
              out TexBlockDetail texBlockDetails
              )
        {
            texBlockDetails = new TexBlockDetail();

            //算出格子在原来纹理的顶点位置
            Vector2 srcTexBottomLeft = new Vector2((float)-convertData.srcTexWidth / (float)2, (float)-convertData.srcTexHeight / (float)2);
            Vector2 srcTexTopLeft = new Vector2((float)-convertData.srcTexWidth / (float)2, (float)convertData.srcTexHeight / (float)2);
            Vector2 srcTexTopRight = new Vector2((float)convertData.srcTexWidth / (float)2, (float)convertData.srcTexHeight / (float)2);
            Vector2 srcTexBottomRight = new Vector2((float)convertData.srcTexWidth / (float)2, (float)-convertData.srcTexHeight / (float)2);

            //原点从左下角平移到纹理的中心
            Vector3 bottomLeftPos = new Vector3(
                srcTexBottomLeft.x + (float)sampleBlockDetails.px,
                srcTexBottomLeft.y + (float)sampleBlockDetails.py,
                0
                );

            //起点加长度 - 1 为最后一个坐标的位置
            texBlockDetails.posBL = bottomLeftPos;
            texBlockDetails.posTL = new Vector3(bottomLeftPos.x, bottomLeftPos.y + convertData.validHeight);       //top-left ;
            texBlockDetails.posTR = new Vector3(bottomLeftPos.x + convertData.validWidth, bottomLeftPos.y + convertData.validHeight);
            texBlockDetails.posBR = new Vector3(bottomLeftPos.x + convertData.validWidth, bottomLeftPos.y);

            Vector2 uvBottomLeft;
            Vector2 uvTopRight;
            Vector2 uvTopLeft;
            Vector2 uvBottomRight;

            //算出有效像素的采样范围，注意，边界范围的采样，有效像素其实是减了1像素当作Padding的。   
            int dx = sampleBlockDetails.vtrx - sampleBlockDetails.vblx;
            int dy = sampleBlockDetails.vtry - sampleBlockDetails.vbly;

            if (sampleBlockDetails.IsFilped)
            {
                uvTopLeft = new Vector2((float)sampleBlockDetails.vblx / (float)convertData.dstTexWidth, (float)sampleBlockDetails.vbly / (float)convertData.dstTexHeight);
                uvBottomRight = new Vector2((float)(sampleBlockDetails.vblx + dy) / (float)convertData.dstTexWidth, (float)(sampleBlockDetails.vbly + dx) / (float)convertData.dstTexHeight);
                uvBottomLeft = new Vector2(uvBottomRight.x, uvTopLeft.y);
                uvTopRight = new Vector2(uvTopLeft.x, uvBottomRight.y);
            }
            else
            {
                uvBottomLeft = new Vector2((float)sampleBlockDetails.vblx / (float)convertData.dstTexWidth, sampleBlockDetails.vbly / (float)convertData.dstTexHeight);
                uvTopRight = new Vector2((float)(sampleBlockDetails.vtrx + 1) / (float)convertData.dstTexWidth, (sampleBlockDetails.vtry + 1) / (float)convertData.dstTexHeight);
                uvTopLeft = new Vector2(uvBottomLeft.x, (float)uvTopRight.y);
                uvBottomRight = new Vector2(uvTopRight.x, (float)uvBottomLeft.y);
            }


            texBlockDetails.uvBL = uvBottomLeft;
            texBlockDetails.uvTL = uvTopLeft;
            texBlockDetails.uvTR = uvTopRight;
            texBlockDetails.uvBR = uvBottomRight;
        }


        private static void DownSampleTexture(ref SampleBlockDetail blockDetails)
        {
            if (blockDetails != null
                && blockDetails.colors != null
                && blockDetails.colors.Length > 0
                && blockDetails.downSampleLv > enDownSampleLevel.Level_1 )
            {
                Texture2D srcTex = new Texture2D(blockDetails.vw, blockDetails.vh);
                srcTex.SetPixels(blockDetails.colors);

                int downSampleLv = (int)blockDetails.downSampleLv;
                srcTex = ScaleTexture(srcTex, blockDetails.rvw, blockDetails.rvh);
                blockDetails.colors = srcTex.GetPixels();
            }
        }


        private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

            //特么居然是以行优先的
            Color[] cols = new Color[result.width * result.height];
            int nCnt = 0;
            for (int i = 0; i < result.height; ++i)
            {
                for (int j = 0; j < result.width; ++j)
                {
                    Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                    cols[nCnt++] = newColor;
                }
            }
            result.SetPixels(cols);
            result.Apply();
            return result;
        }


        #endregion


        #region Simple Packer

        public class CNode
        {
            public CNode leftChild;
            public CNode rightChild;

            public Rect rect;
            public bool IsFilled = false;

            public void SetRect(int x, int y,int w,int h )
            {
                rect.x = x;
                rect.y = y;
                rect.width = w;
                rect.height = h; 
            }


            //是否为叶子结点
            public bool IsLeaf
            {
                get
                {
                    return leftChild == null && rightChild == null;
                }
            }

            //是否能放得下
            public bool CanFillIn(int weight, int height, out bool outNeedFilp)
            {
                outNeedFilp = false;
                bool ret = false;
                if ( rect.width >= weight && rect.height >= height)
                {
                    ret = true;
                }
                else if (rect.height >= weight && rect.width >= height)
                {
                    outNeedFilp = true;
                    ret = true;
                }
                return ret;
            }

            //刚好塞得下
            public bool CanPerfectFillIn(int weight, int height)
            {
                return (rect.width == weight && rect.height == height);
            }


            private void Fill(
                ref CNode retNode,
                Texture2D packTex,
                CConvertFormatData convertCfg,
                SampleBlockDetail sampleBlockDetail,
                out TexBlockDetail texBlockDetail,
                bool bNeedFliped
                )
            {
                texBlockDetail = null; 
                if (retNode != null
                    && sampleBlockDetail != null)
                {
                    retNode.IsFilled = true;
                    sampleBlockDetail.IsFilped = bNeedFliped;   //翻转的
                    sampleBlockDetail.oblx = (int)rect.x;   //这是第几个像素的意思。。。
                    sampleBlockDetail.obly = (int)rect.y;
                    CalcBlockVertexAttribute(convertCfg, sampleBlockDetail, out texBlockDetail);
                    Fill2PackTextureEx(packTex, sampleBlockDetail);
                }
            }


            private static void Fill2PackTextureEx(
                Texture2D packTex,
                SampleBlockDetail sampleBlockDetail
                 )
            {
                if (packTex != null
                    && sampleBlockDetail != null)
                {
                    Color[] cols = sampleBlockDetail.colors;
                    if (sampleBlockDetail.IsFilped)
                    {
                        Color[] pixels = rotateTextureGrid(cols, sampleBlockDetail.rvw, sampleBlockDetail.rvh);
                        sampleBlockDetail.colors = pixels; 
                    }
                    SetPixelsBlock(packTex, sampleBlockDetail);
                }
            }

            private  Color32[] RotateMatrix(Color32[] matrix, int w, int h)
            {
                Color32[] ret = new Color32[w * h];

                for (int i = 0; i < w; ++i)
                {
                    for (int j = 0; j < h; ++j)
                    {
                        ret[i * w + j] = matrix[(h - j - 1) * w + i ];
                    }
                }

                return ret;
            }



            private void SplitNode(int fillWidth, int fillHeight, out CNode outLeftChild, out CNode outRightChild)
            {
                outLeftChild = new CNode();
                outRightChild = new CNode();

                int dw = (int)rect.width - fillWidth;
                int dh = (int)rect.height - fillHeight;

                if (dw >= dh)  //左右分割
                {
                    outLeftChild.SetRect((int)rect.x, (int)rect.y, fillWidth,(int)rect.height);
                    outRightChild.SetRect((int)rect.x + fillWidth , (int)rect.y , dw, (int)rect.height); 
                }
                else  //上下分割
                {
                    outLeftChild.SetRect((int)rect.x, (int)rect.y ,(int)rect.width , fillHeight );
                    outRightChild.SetRect((int)rect.x , (int)rect.y + fillHeight, (int)rect.width, dh);
                }
            }


            public CNode Insert(
                Texture2D packTex ,
                CConvertFormatData convertConfig,
                SampleBlockDetail sampleBlockDetail  ,
                out TexBlockDetail texBlockDetail
                )
            {
                CNode retNode = null;
                texBlockDetail = null; 
                if (sampleBlockDetail != null)
                {
                    int texWidth = sampleBlockDetail.rw;
                    int texHeight = sampleBlockDetail.rh;

                    if (!IsLeaf)  //不是叶子结点了
                    {
                        if (leftChild != null)
                        {
                            retNode = leftChild.Insert( packTex , convertConfig , sampleBlockDetail , out texBlockDetail);
                        }
                        if (retNode == null
                            && rightChild != null)
                        {
                            retNode = rightChild.Insert(packTex, convertConfig, sampleBlockDetail, out texBlockDetail);
                        }
                    }
                    else
                    {
                        bool bNeedFliped = false;
                        if (IsFilled)
                        {
                            retNode = null;
                        }
                        else if (CanFillIn(texWidth, texHeight, out bNeedFliped))
                        {
                            bool bPerfectFillIn = bNeedFliped ?
                                  CanPerfectFillIn(texHeight, texWidth) :
                                  CanPerfectFillIn(texWidth, texHeight);

                            //如果完美匹配
                            if (bPerfectFillIn)
                            {
                                retNode = this;
                                Fill(ref retNode, packTex,  convertConfig, sampleBlockDetail, out texBlockDetail, bNeedFliped); 
                            }
                            else
                            {
                                int fillWidth = (bNeedFliped ? texHeight : texWidth);
                                int fillHeight = (bNeedFliped ? texWidth : texHeight);

                                SplitNode(fillWidth, fillHeight, out leftChild, out rightChild);

                                if (leftChild != null)
                                {
                                    retNode = leftChild.Insert(packTex, convertConfig, sampleBlockDetail, out texBlockDetail);
                                }
                            }
                        }
                    }
                }

                return retNode;
            }
        }

        #endregion


        #region Editor Method

        //Note: the string "GameObject" is necessary !!!
        [MenuItem("GameObject/Tools/UI/TextureRecombination/Texture Recombination", false,0)]
        public static void RecombinationImageInEditor( MenuCommand menuCommand )
        {
            GameObject go = menuCommand.context as GameObject; 
            if( go )
            {
                Image image = go.GetComponent<Image>();
                if( image != null )
                {
                    TextureRecombinationData data ;
                    RecombinationTexture(image.overrideSprite, out data); 
                    if( data != null )
                    {
                        BreakdownTextureRecombination script = go.GetComponent<BreakdownTextureRecombination>(); 
                        if( script == null )
                        {
                            script = go.AddComponent<BreakdownTextureRecombination>();
                        }
                        if( script != null )
                        {
                            script.texRecombinationData = data;
                            script.ResetAspectRatio(); 
                        }
                    }

                } 
                else
                {
                    Debug.LogError("No Image Comp!"); 
                }
            }
        }

        #endregion

    }

}






