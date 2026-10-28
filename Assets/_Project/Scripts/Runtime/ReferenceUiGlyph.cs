using UnityEngine;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    /// <summary>Small code-native UI symbols; avoids platform-dependent missing font glyphs.</summary>
    public sealed class ReferenceUiGlyph : Graphic
    {
        public enum Symbol { Home, Lock, Pin, RightArrow, UpArrow }
        public Symbol Shape;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();Rect r=rectTransform.rect;
            if(Shape==Symbol.Home)
            {
                Quad(mesh,r,.15f,.05f,.85f,.58f);
                Triangle(mesh,r,new Vector2(.02f,.52f),new Vector2(.5f,.98f),new Vector2(.98f,.52f));
            }
            else if(Shape==Symbol.Lock)
            {
                Quad(mesh,r,.18f,.04f,.82f,.57f);
                Quad(mesh,r,.29f,.55f,.39f,.80f);Quad(mesh,r,.61f,.55f,.71f,.80f);Quad(mesh,r,.36f,.77f,.64f,.90f);
            }
            else if(Shape==Symbol.Pin)
            {
                Triangle(mesh,r,new Vector2(.18f,.6f),new Vector2(.82f,.6f),new Vector2(.5f,.02f));
                Vector2 centre=new Vector2(.5f,.65f);
                for(int i=0;i<16;i++)
                {float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Triangle(mesh,r,centre,centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.35f,centre+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.35f);}
            }
            else if(Shape==Symbol.RightArrow)
            {Quad(mesh,r,.02f,.36f,.65f,.64f);Triangle(mesh,r,new Vector2(.55f,.06f),new Vector2(.98f,.5f),new Vector2(.55f,.94f));}
            else
            {Quad(mesh,r,.36f,.02f,.64f,.65f);Triangle(mesh,r,new Vector2(.06f,.55f),new Vector2(.5f,.98f),new Vector2(.94f,.55f));}
        }
        void Quad(VertexHelper mesh,Rect r,float x0,float y0,float x1,float y1)
        {Triangle(mesh,r,new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1));Triangle(mesh,r,new Vector2(x0,y0),new Vector2(x1,y1),new Vector2(x0,y1));}
        void Triangle(VertexHelper mesh,Rect r,Vector2 a,Vector2 b,Vector2 c)
        {
            int first=mesh.currentVertCount;
            foreach(Vector2 p in new[]{a,b,c})mesh.AddVert(new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height,0),color,Vector2.zero);
            mesh.AddTriangle(first,first+1,first+2);
        }
    }
}
