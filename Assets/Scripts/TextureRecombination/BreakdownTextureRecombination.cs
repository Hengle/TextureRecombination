using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Assets.UI.TextureRecombination;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class BreakdownTextureRecombination : BaseVertexEffect
{
    private Image bindImage
    {
        get
        {
            return gameObject.GetComponent<Image>();
        }
    }

    private Sprite bindSprite
    {
        get
        {
            Sprite _bindSprite = null; 
            if( bindImage != null  )
            {
                _bindSprite = bindImage.sprite;
            }

            return _bindSprite;
        }
    }


    private RectTransform bindRectTran
    {
        get
        {
            return gameObject.GetComponent<RectTransform>(); 
        }
    }

    private float textureWidth
    {
        get
        {
            float ret = transformWidth;
            if( texRecombinationData != null )
            {
                ret = texRecombinationData.convertFormat.srcTexWidth ;
            }
            return ret;
        }
    }

    private float textureHeight
    {
        get
        {
            float ret = transformHeight;
            if (texRecombinationData != null)
            {
                ret = texRecombinationData.convertFormat.srcTexHeight;
            }
            return ret;
        }

    }

    private float transformWidth
    {
        get
        {
            return bindRectTran.rect.size.x;
        }
    }

    private float transformHeight
    {
        get
        {
             return  bindRectTran.rect.size.y;;
        }
    }




    [SerializeField]
    private TextureRecombinationData _texRecombinationData;
    public TextureRecombinationData texRecombinationData
    {
        set
        {
            if( _texRecombinationData != null 
                && _texRecombinationData.Equals(value) )
            {
                return;
            }

            _texRecombinationData = value;
            SetDirty();      
        }
        get
        {
            return _texRecombinationData; 
        }
    }
    

    [SerializeField]
    private float _widthAspectRatio = 1; 
    public float widthAspectRatio
    {
        set
        {
            if( _widthAspectRatio != value )
            {
                _widthAspectRatio = value;
                SetDirty(); 
            }          
        }
        get
        {
            return _widthAspectRatio; 
        }
    }


    [SerializeField]
    private float _heightAspectRatio = 1;
    public float heightAspectRatio
    {
        set
        {
            if (_heightAspectRatio != value)
            {
                _heightAspectRatio = value;
                SetDirty();
            }
        }
        get
        {
            return _heightAspectRatio;
        }

    }


    [SerializeField]
    private List<bool> _debugBitSet ; 
    public List<bool> debugBitSet
    {
        get
        {
            return _debugBitSet; 
        }
    } 

   


    [ContextMenu("Set Dirty")]
    void SetDirty()
    {
        if (bindImage)
        {
            bindImage.SetVerticesDirty();
        }
    }


    [ContextMenu("Reset Aspect Ratio")]
    public void ResetAspectRatio()
    {
        widthAspectRatio = transformWidth / textureWidth;
        heightAspectRatio = transformHeight / textureHeight; 
    }


    Vector3 CorrectVert( Vector3 verts , float widthRatio , float heightRatio )
    {
        Vector3 newVerts = new Vector3();
        if ( verts != null )
        {
            newVerts.x = verts.x * widthRatio;
            newVerts.y = verts.y * heightRatio;
            newVerts.z = verts.z;
        }
        return newVerts;
    }


    public override void ModifyVertices(List<UIVertex> vbo)
    {
        if( bindImage == null 
            || bindSprite == null
            || texRecombinationData == null )
        {
            return; 
        }

        if (texRecombinationData.texBlockDetails == null
            || (texRecombinationData.texBlockDetails != null && texRecombinationData.texBlockDetails.Count <= 0))
        {
            Debug.LogWarning("No vertexs ");
            return;
        }

#if UNITY_EDITOR_WIN
        if ( debugBitSet == null 
            || (debugBitSet.Count != texRecombinationData.texBlockDetails.Count ) )
        {
            _debugBitSet = new List<bool>(); 
            for( int i = 0;  i < texRecombinationData.texBlockDetails.Count; ++i)
            {
                _debugBitSet.Add(true);
            }
        }
#endif

        vbo.Clear();
        for (int i = 0; i < texRecombinationData.texBlockDetails.Count  ; ++i)
        {
            TexBlockDetail details = texRecombinationData.texBlockDetails[i];

#if UNITY_EDITOR_WIN
            if ( details != null && (i >= debugBitSet.Count || debugBitSet[i]))
#else
            if ( details != null )  
#endif  
            {
                //bottom-left
                UIVertex blVertex = new UIVertex();
                blVertex.position = CorrectVert(details.posBL,widthAspectRatio,heightAspectRatio) ;
                blVertex.uv0 = details.uvBL;
                blVertex.uv1 = details.uvBL;  //uv留给拆图用
                blVertex.color =  bindImage.color ;//colors[i];
                //blVertex.tangent = new Vector4(1, 0, 0, -1);
                //blVertex.normal = new Vector3(0, 0, -1);
                vbo.Add(blVertex);

                //top-left
                UIVertex tlVertex = new UIVertex();
                tlVertex.position = CorrectVert(details.posTL, widthAspectRatio, heightAspectRatio)  ;
                tlVertex.uv0 = details.uvTL;
                tlVertex.uv1 = details.uvTL;
                tlVertex.color = bindImage.color;
                //tlVertex.tangent = new Vector4(1, 0, 0, -1);
                //tlVertex.normal = new Vector3(0, 0, -1);
                vbo.Add(tlVertex);

                //top-right
                UIVertex trVertex = new UIVertex();
                trVertex.position = CorrectVert(details.posTR, widthAspectRatio, heightAspectRatio);
                trVertex.uv0 = details.uvTR;
                trVertex.uv1 = details.uvTR;
                trVertex.color = bindImage.color ;
                //trVertex.tangent = new Vector4(1, 0, 0, -1);
                //trVertex.normal = new Vector3(0, 0, -1);
                vbo.Add(trVertex);

                //bottom-right
                UIVertex brVertex = new UIVertex();
                brVertex.position = CorrectVert(details.posBR, widthAspectRatio, heightAspectRatio);
                brVertex.uv0 = details.uvBR;
                brVertex.uv1 = details.uvBR;
                brVertex.color = bindImage.color ;
                //brVertex.tangent = new Vector4(1, 0, 0, -1);
                //brVertex.normal = new Vector3(0, 0, -1);
                vbo.Add(brVertex);
            }
        }
    }  //moderfy


#if UNITY_EDITOR

    private void OnValidate()
    {
        SetDirty();
    }
#endif 


}