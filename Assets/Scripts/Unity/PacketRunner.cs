using UnityEngine;

namespace ShiftRunner.Presentation
{
    // The course and HUD share one logical screen space. Mouse picks use the exact
    // projection from the already displayed frame, including the boost camera shake.
    public sealed class PacketRunner : MonoBehaviour
    {
        const string BestKey="shift.packet.best", MuteKey="shift.packet.mute";
        RunnerGame game;
        Camera view;
        TerminalCanvas canvas;
        CourseDisplay course;
        RunnerHud hud;
        PacketAudio tones;
        bool running, died;
        float clock, shake, noticeTime, savedBest;
        string notice="";
        Color32 noticeColor;
        Rect viewport;

        void Awake()
        {
            Application.targetFrameRate=60;
            game=new RunnerGame(System.Environment.TickCount);
            savedBest=PlayerPrefs.GetFloat(BestKey,0);
            game.Best=savedBest;

            view=Camera.main;
            if(!view) view=new GameObject("Course Camera").AddComponent<Camera>();
            view.transform.SetPositionAndRotation(new Vector3(640,-360,-10),Quaternion.identity);
            view.orthographic=true; view.orthographicSize=360;
            view.nearClipPlane=.1f; view.farClipPlane=20; view.clearFlags=CameraClearFlags.SolidColor;
            view.backgroundColor=Ink.Night; view.allowHDR=false; view.allowMSAA=false;
            if(!FindFirstObjectByType<AudioListener>()) view.gameObject.AddComponent<AudioListener>();
            canvas=new TerminalCanvas(gameObject);
            course=new CourseDisplay(canvas); hud=new RunnerHud(canvas);
            tones=new PacketAudio(gameObject) { Muted=PlayerPrefs.GetInt(MuteKey,0)!=0 };
            game.Cue+=OnCue;
        }

        void Update()
        {
            // Keep the full course and controls visible on itch embeds and fullscreen.
            float actual=Screen.width/(float)Mathf.Max(1,Screen.height);
            const float design=16f/9f;
            float w=Mathf.Min(1,design/actual), h=Mathf.Min(1,actual/design);
            view.rect=new Rect((1-w)*.5f,(1-h)*.5f,w,h);
            view.aspect=design;
            viewport=view.pixelRect;
            Vector2 mouse=Input.mousePosition;
            Vector2 pointer=new Vector2((mouse.x-viewport.x)/viewport.width*1280,
                (1-(mouse.y-viewport.y)/viewport.height)*720);
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            clock+=dt; noticeTime-=dt; shake=Mathf.MoveTowards(shake,0,dt*12f);
            ReadInput(pointer);
            if(running) game.Tick(dt);
            canvas.Begin();
            course.Draw(game,clock,shake,running,pointer);
            hud.Draw(game,running,tones.Muted,pointer,notice,noticeColor,noticeTime,died,course.HighlightsFloor);
            canvas.Flush();
        }

        void ReadInput(Vector2 pointer)
        {
            bool enter=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
            bool clicked=Input.GetMouseButtonDown(0) && viewport.Contains(Input.mousePosition);
            if(clicked && RunnerHud.OverAudio(pointer))
            {
                tones.Muted=!tones.Muted;
                PlayerPrefs.SetInt(MuteKey,tones.Muted?1:0); PlayerPrefs.Save();
                if(!tones.Muted) tones.Play(GameCue.Build);
                return;
            }
            if(!running)
            {
                if(clicked || enter) { running=true; Note("RUN STARTED",Ink.Cyan,1.2f); }
                return;
            }
            if(Input.GetKeyDown(KeyCode.R))
            {
                SaveBest(); game.Restart(); Note("RUN RESTARTED",Ink.Cyan,1.2f); return;
            }
            if(clicked && pointer.y>94 && pointer.y<590)
            {
                Vector2 world=course.WorldPoint(pointer);
                bool extend=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
                game.Click(world.x,world.y,extend);
                if(extend && !game.Selection.HasStart) Note("CLICK TO SET A START POINT",Ink.Amber,1f);
            }
            if(enter && !game.Build()) Note("SELECT TWO EDGES TO BUILD",Ink.Amber,1.2f);
            if((Input.GetKeyDown(KeyCode.Delete)||Input.GetKeyDown(KeyCode.Backspace)) && !game.Erase())
                Note(game.Selection.HasEnd?"NO EDITABLE BLOCKS IN RANGE":"SELECT TWO EDGES TO ERASE",Ink.Amber,1.2f);
        }

        void OnCue(GameCue cue)
        {
            tones.Play(cue);
            switch(cue)
            {
                case GameCue.Death:
                    SaveBest(); Note("PACKET LOST / REBOOTED",Ink.Red,1.55f); died=true; shake=2.1f; break;
                case GameCue.Build: Note("BRIDGE COMPILED",Ink.Mint,1.1f); break;
                case GameCue.Erase: Note("RANGE ERASED",Ink.Amber,1.1f); break;
                case GameCue.Boost:
                    Note("BOOST / +"+Mathf.RoundToInt(game.Boost01*72)+"% SPEED",Ink.Mint,1.5f);
                    shake=2.7f*game.Boost01; break;
            }
        }

        void Note(string text,Color32 color,float duration)
        {
            notice=text; noticeColor=color; noticeTime=duration; died=false;
        }

        void SaveBest()
        {
            if(game!=null && game.Best>savedBest)
            {
                savedBest=game.Best; PlayerPrefs.SetFloat(BestKey,savedBest); PlayerPrefs.Save();
            }
        }
        void OnApplicationFocus(bool focus) { if(!focus) SaveBest(); }
        void OnApplicationPause(bool pause) { if(pause) SaveBest(); }
        void OnApplicationQuit() { SaveBest(); }
        void OnDestroy()
        {
            if(game!=null) game.Cue-=OnCue;
            if(canvas!=null) canvas.Dispose();
            if(tones!=null) tones.Dispose();
        }
    }
}
