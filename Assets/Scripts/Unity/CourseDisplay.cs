using UnityEngine;

namespace ShiftRunner.Presentation
{
    internal sealed class CourseDisplay
    {
        public const float Unit = 27f, PacketX = 224f, FloorY = 454f;
        readonly TerminalCanvas pen;
        float origin, clock, shakeX, shakeY;
        public bool HighlightsFloor { get; private set; }
        public CourseDisplay(TerminalCanvas canvas) { pen = canvas; }
        float X(float worldX) { return PacketX + (worldX-origin)*Unit + shakeX; }
        float Y(float worldY) { return FloorY-worldY*Unit + shakeY; }
        public Vector2 WorldPoint(Vector2 point)
        {
            return new Vector2(origin+(point.x-PacketX-shakeX)/Unit, (FloorY+shakeY-point.y)/Unit);
        }

        public void Draw(RunnerGame game, float time, float shake, bool running, Vector2 pointer)
        {
            origin = game.X;
            clock = time;
            shakeX = Mathf.Sin(clock*97f)*shake;
            shakeY = Mathf.Cos(clock*89f)*shake*.6f;
            Backplane();
            foreach (Piece piece in game.World.Pieces)
            {
                if (piece.Kind == PieceKind.Ground && Visible(piece)) Ground(piece);
            }
            foreach (Gap gap in game.World.Gaps) Gap(gap.Left, gap.Right);
            foreach (Piece piece in game.World.Pieces)
            {
                if (piece.Kind == PieceKind.Ground || !Visible(piece)) continue;
                if (piece.Kind == PieceKind.Bridge) Bridge(piece);
                else Obstacle(piece);
            }
            if (game.Boost01 > .01f) SpeedLines(game.Boost01);
            HighlightsFloor = false;
            if (running) Selection(game.Selection, pointer);
            Packet(game);
            Ruler();
        }

        bool Visible(Piece p) { return X(p.Right) >= -15 && X(p.Left) <= 1295; }

        void Backplane()
        {
            pen.Gradient(0,0,1280,620,new Color32(6,17,24,255),new Color32(15,38,45,255));
            Color32 grid = new Color32(39,74,78,65);
            float gridOffset = Mathf.Repeat(-origin*Unit*.23f,48);
            for (float x=gridOffset; x<1280; x+=48) pen.Rect(x,85,1,500,grid);
            for (int y=116; y<594; y+=48) pen.Rect(0,y,1280,1,grid);
            for (float x=gridOffset; x<1280; x+=96)
                for (int y=116; y<=550; y+=96)
                {
                    pen.Rect(x-2,y,5,1,Ink.Alpha(Ink.Cyan,26));
                    pen.Rect(x,y-2,1,5,Ink.Alpha(Ink.Cyan,26));
                }

            // Routed circuit traces drift at a different rate from the lane.
            float offset = Mathf.Repeat(origin*Unit*.20f,1200f);
            for (int tile=-1; tile<=2; tile++)
            {
                float start=tile*1200f-offset;
                CircuitChip(start+908,251);
                for (int bus=0; bus<5; bus++)
                {
                    int d=bus*9;
                    Color32 trace = new Color32(46,99,101,(byte)(84-bus*8));
                    pen.Line(start+82,321+d,start+181+d,321+d,1.5f,trace);
                    pen.Line(start+181+d,321+d,start+249+d,253+d,1.5f,trace);
                    pen.Line(start+249+d,253+d,start+424+d,253+d,1.5f,trace);
                    pen.Line(start+424+d,253+d,start+474+d,303+d,1.5f,trace);
                    pen.Line(start+474+d,303+d,start+633+d,303+d,1.5f,trace);
                    pen.Line(start+633+d,303+d,start+733+d,203+d,1.5f,trace);
                    pen.Line(start+733+d,203+d,start+856,203+d,1.5f,trace);
                    pen.Frame(start+77,318+d,6,6,1,trace);
                }
            }
            // Low-contrast memory banks place the track inside a larger system.
            float banks=Mathf.Repeat(origin*Unit*.38f,94f);
            int first=Mathf.FloorToInt(origin*Unit*.38f/94f);
            for(int i=-1;i<16;i++)
            {
                float x=i*94f-banks, h=26+Mathf.Repeat((first+i)*29,72);
                var side=new Color32(25,53,58,155);
                pen.Rect(x,FloorY-h,74,h,new Color32(11,29,36,220));
                pen.Rect(x,FloorY-h,74,1,side); pen.Rect(x+73,FloorY-h,1,h,side);
                for(int row=0;row<3;row++) pen.Rect(x+8,FloorY-h+8+row*5,26,1,side);
            }
        }

        void CircuitChip(float x,float y)
        {
            if(x< -250 || x>1500) return;
            var outline = new Color32(36,78,81,220);
            var accent = new Color32(46,105,102,185);
            const int half=53;
            for(int i=-4;i<=4;i++)
            {
                float n=i*9;
                pen.Rect(x-half-12,y+n,13,3,outline); pen.Rect(x+half,y+n,13,3,outline);
                pen.Rect(x+n,y-half-12,3,13,outline); pen.Rect(x+n,y+half,3,13,outline);
                pen.Line(x+65,y+n+1,x+115-i*3,y+n+1,1,outline);
                pen.Line(x+115-i*3,y+n+1,x+154-i*3,y+n+40,1,outline);
                pen.Line(x+154-i*3,y+n+40,x+235-i*3,y+n+40,1,outline);
            }
            pen.Rect(x-half,y-half,half*2,half*2,new Color32(11,28,35,255));
            pen.Frame(x-half,y-half,half*2,half*2,2,accent);
            pen.Frame(x-45,y-45,90,90,1,outline);
            pen.Frame(x-39,y-39,78,78,1,outline);
            for(int i=0;i<3;i++)
            {
                float left=x-24+i*15;
                pen.Quad(new Vector2(left+11,y-15),new Vector2(left+17,y-15),
                    new Vector2(left+4,y+15),new Vector2(left-2,y+15),accent);
            }
            pen.Rect(x-45,y+35,3,3,accent);
        }

        void Ground(Piece p)
        {
            float a=X(p.Left), b=X(p.Right), top=Y(p.Top), floor=Y(p.Bottom);
            float left=Mathf.Max(-2,a), right=Mathf.Min(1282,b);
            pen.Gradient(left,top,right-left,floor-top,new Color32(22,64,68,255),new Color32(12,29,37,255));
            pen.Rect(left,top-4,right-left,9,Ink.Alpha(Ink.Cyan,33));
            pen.Rect(left,top,right-left,3,new Color32(111,235,224,255));
            pen.Rect(left,top+5,right-left,1,new Color32(51,123,119,255));
            pen.Rect(left,floor-2,right-left,2,Ink.Edge);
            var seam=new Color32(41,88,88,255);
            float pitch=Unit*3;
            float first=left+Mathf.Repeat(a-left,pitch);
            for(float x=first; x<right; x+=pitch)
            {
                pen.Rect(x,top+9,1,floor-top-11,seam);
                float end=Mathf.Min(x+16,right);
                pen.Line(x,top+28,end,top+28-(end-x),1,seam);
                if(x+28<right)
                {
                    pen.Rect(x+8,floor-13,18,2,Ink.Alpha(Ink.Cyan,55));
                    pen.Rect(x+8,floor-9,8,1,seam);
                }
            }
            if(a>=0) pen.Rect(a,top,2,floor-top,Ink.Alpha(Ink.Cyan,170));
            if(b<=1280) pen.Rect(b-2,top,2,floor-top,Ink.Alpha(Ink.Cyan,170));
        }

        void Gap(float a, float b)
        {
            float left=X(a), right=X(b), mid=(left+right)*.5f, top=Y(0), line=top+61;
            if(right<0 || left>1280) return;
            Color32 tint=Ink.Alpha(Ink.Amber,100);
            pen.Gradient(left,top+1,right-left,108,new Color32(4,13,20,255),new Color32(9,23,30,255));
            pen.Line(left+7,line,right-7,line,1,tint);
            pen.Rect(left+7,line-3,1,7,tint); pen.Rect(right-8,line-3,1,7,tint);
            string label = "GAP " + Mathf.RoundToInt(b-a) + "M";
            float width = TerminalCanvas.TextWidth(label,1.5f);
            if(width<right-left-25)
            {
                pen.Rect(mid-width*.5f-6,line-7,width+12,15,new Color32(7,18,24,255));
                pen.Text(label,mid-width*.5f,line-5,1.5f,Ink.Amber);
            }
            pen.Rect(left-6,top-4,6,4,Ink.Amber); pen.Rect(right,top-4,6,4,Ink.Amber);
        }

        void Bridge(Piece p)
        {
            float a=Mathf.Max(-4,X(p.Left)), b=Mathf.Min(1284,X(p.Right)), y=Y(p.Top);
            pen.Rect(a,y-7,b-a,22,Ink.Alpha(Ink.Mint,25));
            pen.Rect(a,y,b-a,11,new Color32(41,113,76,255));
            pen.Rect(a,y-1,b-a,3,Ink.Mint); pen.Rect(a,y+9,b-a,2,Ink.Alpha(Ink.Mint,220));
            for(float i=a+6;i<b-9;i+=16) pen.Line(i,y+8,i+6,y+3,2,Ink.Alpha(Ink.Mint,160));
        }

        void Obstacle(Piece p)
        {
            float a=X(p.Left), b=X(p.Right), top=Y(p.Top), bottom=Y(p.Bottom), mid=(a+b)*.5f;
            float w=b-a, h=bottom-top;
            Color32 body = new Color32(80,40,44,255);
            if(p.Kind==PieceKind.Wall)
            {
                pen.Rect(a-3,top-3,w+6,h+6,Ink.Alpha(Ink.Red,24));
                pen.Rect(a,top,w,h,body); pen.Frame(a,top,w,h,2,Ink.Red);
                for(float y=top+8;y<bottom-5;y+=9)
                    pen.Line(a+2,y,b-2,Mathf.Min(y+5,bottom-2),2,Ink.Alpha(Ink.Red,135));
                if(w>9) pen.Rect(a+w*.5f-3,top+5,6,h-10,body);
            }
            else if(p.Kind==PieceKind.Enemy)
            {
                pen.Rect(a-4,top+5,w+8,h-10,Ink.Alpha(Ink.Red,110));
                pen.Rect(a,top+2,w,h-3,body); pen.Frame(a,top+2,w,h-3,2,Ink.Red);
                pen.Rect(a+3,top+8,w-6,6,Ink.Red); pen.Rect(mid-2,top+8,4,4,body);
                pen.Line(a+2,top+2,a-3,top-4,2,Ink.Red); pen.Line(b-2,top+2,b+3,top-4,2,Ink.Red);
            }
            else
            {
                for(int i=0;i<3;i++)
                {
                    float x=a+i*w/3, spike=x+w/6;
                    pen.Quad(new Vector2(x,bottom),new Vector2(spike,top),new Vector2(spike,top),
                        new Vector2(x+w/3,bottom),body);
                    pen.Line(x,bottom,spike,top,2,Ink.Red); pen.Line(spike,top,x+w/3,bottom,2,Ink.Red);
                }
                pen.Rect(a,bottom-2,w,2,Ink.Red);
            }
            string name = p.Kind==PieceKind.Wall ? "FIREWALL" : p.Kind==PieceKind.Enemy ? "SENTRY" : "HAZARD";
            pen.Text(name,mid-TerminalCanvas.TextWidth(name,1.3f)*.5f,top-22,1.3f,Ink.Red);
        }

        void Selection(RangeSelection selection,Vector2 pointer)
        {
            if(!selection.HasStart) return;
            float start=X(selection.StartX), end=X(selection.EndX), lane=Y(0);
            bool preview=!selection.HasEnd && pointer.y>96 && pointer.y<585;
            if(preview) end=pointer.x;
            HighlightsFloor = selection.IncludesFloor || (preview && WorldPoint(pointer).y <= -RangeSelection.FloorDepth);
            float rangeBottom = HighlightsFloor ? Y(-3f) : lane+2;
            if(selection.HasEnd || preview)
            {
                float a=Mathf.Max(-2,Mathf.Min(start,end)), b=Mathf.Min(1282,Mathf.Max(start,end));
                var tinted=Ink.Alpha(Ink.Amber,selection.HasEnd ? (byte)30 : (byte)13);
                pen.Rect(a,lane-90,b-a,rangeBottom-(lane-90),tinted);
                pen.Rect(a,lane-90,b-a,2,Ink.Alpha(Ink.Amber,160));
                pen.Rect(a,rangeBottom-2,b-a,2,Ink.Alpha(Ink.Amber,125));
            }
            Marker(start,Y(selection.StartY),rangeBottom,"A");
            if(selection.HasEnd) Marker(end,Y(selection.EndY),rangeBottom,"B");
        }

        void Marker(float x,float y,float rangeBottom,string letter)
        {
            if(x< -10 || x>1290) return;
            float floor=Y(0), marker=Mathf.Clamp(y,125,548);
            float min=Mathf.Min(marker-10,floor-90), max=Mathf.Max(marker+10,rangeBottom);
            for(float line=min;line<max;line+=9) pen.Rect(x-1,line,2,5,Ink.Alpha(Ink.Amber,195));
            pen.Rect(x-7,marker-7,14,14,Ink.Amber); pen.Text(letter,x-4,marker-5,1.5f,Ink.Night);
        }

        void Packet(RunnerGame game)
        {
            float x=X(game.X), y=Y(game.Y), sz=RunnerGame.PacketSize*Unit;
            var tint=game.Boost01>.1f ? Ink.Mint : Ink.Cyan;
            for(int i=7;i>0;i--)
            {
                float length=i/7f, s=sz*(1f-length*.76f);
                float tx=x-i*(5.6f+4f*game.Boost01);
                pen.Rect(tx-s/2,y-s/2,s,s,Ink.Alpha(tint,(byte)(105-10*i)));
            }
            for(int i=0;i<4;i++)
            {
                float travel=Mathf.Repeat(clock*(38+i*4)+i*28,79);
                float ty=y+(i-1.5f)*5;
                pen.Rect(x-13-travel,ty,6+i,1.5f,Ink.Alpha(tint,(byte)(145-travel)));
            }
            pen.Rect(x-sz*.5f-4,y-sz*.5f-4,sz+8,sz+8,Ink.Alpha(tint,31));
            pen.Rect(x-sz*.5f,y-sz*.5f,sz,sz,tint);
            pen.Rect(x-sz*.5f+3,y-sz*.5f+3,sz-6,sz-6,new Color32(20,65,72,255));
            pen.Line(x-3,y-4,x+2,y,2.6f,Ink.Bright); pen.Line(x+2,y,x-3,y+4,2.6f,Ink.Bright);
            if(game.AttackTime>.012f)
            {
                float reach=RunnerGame.AttackReach*Unit, f=game.AttackTime/.17f;
                pen.Quad(new Vector2(x+9,y-5*f),new Vector2(x+reach,y-1),new Vector2(x+reach+7,y),
                    new Vector2(x+9,y+5*f),Ink.Alpha(Ink.Cyan,(byte)(90+155*f)));
                pen.Line(x+reach-12,y-10*f,x+reach+7,y,2,Ink.Bright);
                pen.Line(x+reach+7,y,x+reach-12,y+10*f,2,Ink.Bright);
            }
        }

        void SpeedLines(float strength)
        {
            for(int i=0;i<21;i++)
            {
                float y=140+(i*73)%374;
                float x=1420-Mathf.Repeat(i*198.2f+clock*(480+(i%6)*77),1480f);
                pen.Rect(x,y,(18+(i%5)*17)*strength,1+(i%2),Ink.Alpha(Ink.Mint,(byte)(95*strength)));
            }
        }

        void Ruler()
        {
            int first=Mathf.FloorToInt(WorldPoint(Vector2.zero).x);
            for(int m=first;m<first+51;m++)
            {
                float x=X(m);
                bool major=m%5==0;
                pen.Rect(x,570,1,major?7:3,Ink.Alpha(Ink.Muted,major?(byte)140:(byte)62));
                if(major && m>=0) pen.Text(m+"M",x+5,569,1.05f,Ink.Alpha(Ink.Muted,190));
            }
        }
    }
}
