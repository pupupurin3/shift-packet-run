using System.Globalization;
using UnityEngine;

namespace ShiftRunner.Presentation
{
    internal sealed class RunnerHud
    {
        readonly TerminalCanvas pen;
        public RunnerHud(TerminalCanvas canvas) { pen=canvas; }
        public static bool OverAudio(Vector2 p) { return p.x>=1180 && p.x<=1243 && p.y>=23 && p.y<=69; }

        public void Draw(RunnerGame game,bool running,bool muted,Vector2 pointer,string notice,Color32 noticeColor,
            float noticeTime,bool died,bool includesFloor)
        {
            Header(game,muted,OverAudio(pointer));
            Commands(game.Selection,includesFloor);
            if(!running) Ready();
            else if(noticeTime>0)
            {
                float w=TerminalCanvas.TextWidth(notice,1.9f)+40;
                pen.Rect(34,118,w,33,Ink.Panel); pen.Frame(34,118,w,33,1,Ink.Edge);
                pen.Rect(34,118,3,33,noticeColor); pen.Text(notice,51,128,1.9f,noticeColor);
                if(died && noticeTime>1.25f)
                    pen.Frame(1,94,1278,491,3,Ink.Alpha(Ink.Red,(byte)(180*Mathf.Clamp01((noticeTime-1.25f)*4))));
            }
        }

        void Header(RunnerGame game,bool muted,bool hoverAudio)
        {
            pen.Rect(0,0,1280,94,Ink.Night); pen.Rect(0,93,1280,1,Ink.Edge);
            for(int i=0;i<2;i++)
            {
                float x=34+i*17;
                pen.Quad(new Vector2(x+15,26),new Vector2(x+25,26),new Vector2(x+10,58),new Vector2(x,58),Ink.Mint);
            }
            pen.Text("SHIFT",92,21,5.6f,Ink.Bright,5);
            pen.Text("PACKET RUN",95,66,1.48f,Ink.Muted);

            Metric(573,"DISTANCE",Mathf.FloorToInt(game.Distance).ToString("00000"),"M",Ink.Bright);
            Metric(781,"BEST",Mathf.FloorToInt(game.Best).ToString("00000"),"M",Ink.Mint);
            Metric(986,"SPEED",game.Speed.ToString("0.0",CultureInfo.InvariantCulture),"M/S",game.Boost01>.05f ? Ink.Mint : Ink.Cyan);
            pen.Rect(545,24,1,46,Ink.Edge); pen.Rect(752,24,1,46,Ink.Edge); pen.Rect(957,24,1,46,Ink.Edge);
            pen.Rect(1180,23,63,46,hoverAudio ? Ink.Edge : Ink.Panel); pen.Frame(1180,23,63,46,1,Ink.Edge);
            Color32 speaker=muted ? Ink.Muted : Ink.Cyan;
            pen.Rect(1194,42,7,9,speaker);
            pen.Quad(new Vector2(1200,42),new Vector2(1208,36),new Vector2(1208,57),new Vector2(1200,51),speaker);
            if(muted)
            {
                pen.Line(1214,41,1224,52,2,speaker); pen.Line(1224,41,1214,52,2,speaker);
            }
            else
            {
                pen.Line(1213,41,1217,46,2,speaker); pen.Line(1217,46,1213,52,2,speaker);
                pen.Line(1219,37,1226,46,2,speaker); pen.Line(1226,46,1219,56,2,speaker);
            }
        }

        void Metric(int x,string label,string value,string unit,Color32 color)
        {
            pen.Text(label,x,22,1.55f,Ink.Muted); pen.Text(value,x,45,3.45f,color);
            pen.Text(unit,x+TerminalCanvas.TextWidth(value,3.45f)+9,57,1.55f,Ink.Muted);
        }

        void Ready()
        {
            const int x=45,y=157,w=679,h=198;
            pen.Rect(x+6,y+7,w,h,new Color32(2,9,13,115)); pen.Rect(x,y,w,h,new Color32(9,24,32,251));
            pen.Frame(x,y,w,h,1,new Color32(65,101,105,255));
            pen.Rect(x,y,80,3,Ink.Mint); pen.Rect(x,y,3,31,Ink.Mint);
            pen.Text("READY TO RUN",x+24,y+24,4,Ink.Bright);
            pen.Text("Click one edge of a gap. Shift-click the other.",x+25,y+79,1.95f,Ink.Muted);
            pen.Text("Press Enter to bridge it. Tight fits earn speed.",x+25,y+104,1.95f,Ink.Muted);
            pen.Rect(x+24,y+143,190,37,Ink.Mint);
            pen.Quad(new Vector2(x+39,y+153),new Vector2(x+48,y+161),new Vector2(x+39,y+169),
                new Vector2(x+39,y+169),Ink.Night);
            pen.Text("RUN PROGRAM",x+60,y+155,2,Ink.Night);
            pen.Text("CLICK OR ENTER",x+231,y+157,1.55f,Ink.Muted);
        }

        void Commands(RangeSelection selected,bool includesFloor)
        {
            pen.Rect(0,590,1280,130,Ink.Night); pen.Rect(0,590,1280,1,Ink.Edge);
            pen.Rect(0,590,89,2,Ink.Cyan);
            pen.Text("COMMANDS",36,607,1.55f,Ink.Muted);
            string status="RANGE / EMPTY";
            string prefix=includesFloor ? "RANGE + FLOOR / " : "RANGE / ";
            if(selected.HasEnd)
                status=prefix+(selected.Right-selected.Left).ToString("0.##",CultureInfo.InvariantCulture)+"M";
            else if(selected.HasStart) status=prefix+"ANCHOR SET";
            float statusW=TerminalCanvas.TextWidth(status,1.5f);
            pen.Text(status,1244-statusW,607,1.5f,selected.HasStart ? Ink.Amber : Ink.Muted);
            pen.Rect(35,626,1209,1,Ink.Edge);
            Command(36,"LMB","ATTACK + SET START");
            Command(320,"SHIFT + LMB","SET RANGE END");
            Command(600,"ENTER","BUILD BRIDGE");
            Command(850,"DEL / BKSP","ERASE RANGE");
            Command(1126,"R","RESTART");
            foreach(int x in separators) pen.Rect(x,642,1,56,Ink.Edge);
        }
        static readonly int[] separators={291,570,820,1096};

        void Command(int x,string key,string action)
        {
            float w=TerminalCanvas.TextWidth(key,1.65f)+22;
            pen.Rect(x,639,w,25,new Color32(22,45,53,255)); pen.Frame(x,639,w,25,1,Ink.Edge);
            pen.Text(key,x+11,646,1.65f,Ink.Bright); pen.Text(action,x,678,1.9f,Ink.Muted);
        }
    }
}
