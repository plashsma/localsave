using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("LocalSave")]
[assembly: AssemblyProduct("LocalSave")]
[assembly: AssemblyDescription("Local automatic saving for desktop Word, Excel and PowerPoint")]
[assembly: AssemblyCompany("PLASHSMA")]
[assembly: AssemblyCopyright("Created by PLASHSMA")]
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.0.0")]

internal static class Program {
    [STAThread] static int Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length==2 && args[0]=="--preview") {
            using(MainWindow window=new MainWindow(new Preferences(),true,false)) {
                window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-32000,-32000);window.ShowInTaskbar=false;
                window.CreateControl();
                for(int i=0;i<3;i++) {
                    window.SelectPage(i); window.Show();window.Hide();
                    using(Bitmap image=new Bitmap(window.Width,window.Height)) {window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(args[1],"localsave-"+i+".png"));}
                }
            }
            return 0;
        }
        bool owner;
        using(Mutex mutex=new Mutex(true,@"Local\LocalOfficeAutoSave-Native",out owner)) {
            if(!owner) {MessageBox.Show("LocalSave or an earlier version is already running. Open its tray icon, or exit it before starting this version.","LocalSave",MessageBoxButtons.OK,MessageBoxIcon.Information);return 0;}
            try {
                Preferences p=Store.Load();
                using(MainWindow window=new MainWindow(p,false,Array.IndexOf(args,"--background")>=0)) Application.Run(window);
                return 0;
            } catch(Exception ex) {MessageBox.Show("LocalSave could not start.\r\n\r\n"+ex.Message,"LocalSave",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
            finally {mutex.ReleaseMutex();}
        }
    }
}

internal sealed class Card : Panel {
    internal Card(int x,int y,int width,int height) {SetBounds(x,y,width,height);BackColor=Brand.Paper;DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e) {Brand.Background(e.Graphics,ClientRectangle,new Point(Left,Top));}
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using(var shadow=Brand.Round(new RectangleF(2,4,Width-5,Height-5),20)) using(Brush b=new SolidBrush(Color.FromArgb(19,65,75,118))) e.Graphics.FillPath(b,shadow);
        using(var path=Brand.Round(new RectangleF(1,1,Width-4,Height-5),20)) {
            using(var fill=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,Color.FromArgb(154,255,255,255),Color.FromArgb(65,255,255,255),110f)) e.Graphics.FillPath(fill,path);
            using(Pen pen=new Pen(Color.FromArgb(248,255,255,255),1.6f)) e.Graphics.DrawPath(pen,path);
            using(var inset=Brand.Round(new RectangleF(3,3,Width-8,Height-9),18)) using(Pen rim=new Pen(Color.FromArgb(72,255,255,255),1)) e.Graphics.DrawPath(rim,inset);
            var clip=e.Graphics.Save();e.Graphics.SetClip(path);
            using(var sheen=new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,0,Width,Height),Color.FromArgb(36,255,255,255),Color.FromArgb(0,255,255,255),35f)) e.Graphics.FillEllipse(sheen,-Width/3,-Height*2,Width*2,Height*3);
            e.Graphics.Restore(clip);
        }
        base.OnPaint(e);
    }
}

internal sealed class GlassPage : Panel {
    internal GlassPage() {DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e) {Brand.Background(e.Graphics,ClientRectangle,Point.Empty);}
}
internal sealed class GlassRail : Panel {
    internal GlassRail() {DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e) {
        Brand.Background(e.Graphics,ClientRectangle,new Point(-208,0));
        using(var b=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,Color.FromArgb(158,255,255,255),Color.FromArgb(70,255,255,255),100f)) e.Graphics.FillRectangle(b,ClientRectangle);
        using(Pen p=new Pen(Color.FromArgb(220,255,255,255))) e.Graphics.DrawLine(p,Width-1,0,Width-1,Height);
    }
}
// A fully managed control avoids the native themed button's rectangular paint layer.
internal sealed class GlassButton : Control, IButtonControl {
    bool Hover,Pressed,DefaultButton;
    internal bool Navigation,Selected;
    internal int Glyph;
    internal ContentAlignment TextAlign=ContentAlignment.MiddleCenter;
    public DialogResult DialogResult {get;set;}
    internal GlassButton() {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable|ControlStyles.SupportsTransparentBackColor,true);
        SetStyle(ControlStyles.StandardClick|ControlStyles.StandardDoubleClick,false);
        TabStop=true;AccessibleRole=AccessibleRole.PushButton;
    }
    public void NotifyDefault(bool value) {DefaultButton=value;Invalidate();}
    public void PerformClick() {if(Enabled && Visible) {OnClick(EventArgs.Empty);Form form=FindForm();if(form!=null && DialogResult!=DialogResult.None) form.DialogResult=DialogResult;}}
    protected override AccessibleObject CreateAccessibilityInstance() {return new ButtonAccessibility(this);}
    sealed class ButtonAccessibility : ControlAccessibleObject {
        readonly GlassButton Button;
        internal ButtonAccessibility(GlassButton b):base(b) {Button=b;}
        public override string DefaultAction {get {return "Press";}}
        public override void DoDefaultAction() {Button.PerformClick();}
    }
    protected override void OnMouseEnter(EventArgs e) {Hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e) {Hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e) {if(e.Button==MouseButtons.Left) {Focus();Pressed=true;Capture=true;Invalidate();}base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e) {bool click=Pressed && e.Button==MouseButtons.Left && ClientRectangle.Contains(e.Location);Pressed=false;Capture=false;Invalidate();base.OnMouseUp(e);if(click) PerformClick();}
    protected override void OnMouseCaptureChanged(EventArgs e) {if(!Capture) {Pressed=false;Invalidate();}base.OnMouseCaptureChanged(e);}
    protected override void OnGotFocus(EventArgs e) {Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e) {Pressed=false;Invalidate();base.OnLostFocus(e);}
    protected override void OnEnabledChanged(EventArgs e) {Invalidate();base.OnEnabledChanged(e);}
    protected override void OnKeyDown(KeyEventArgs e) {if(e.KeyCode==Keys.Space) {Pressed=true;Invalidate();e.Handled=true;e.SuppressKeyPress=true;}base.OnKeyDown(e);}
    protected override void OnKeyUp(KeyEventArgs e) {if(e.KeyCode==Keys.Space && Pressed) {Pressed=false;Invalidate();PerformClick();e.Handled=true;}base.OnKeyUp(e);}
    protected override bool ProcessDialogKey(Keys keyData) {if(keyData==Keys.Enter) {PerformClick();return true;}return base.ProcessDialogKey(keyData);}
    protected override void OnPaintBackground(PaintEventArgs e) {
        if(Parent==null) {base.OnPaintBackground(e);return;}
        var state=e.Graphics.Save();
        try {
            e.Graphics.TranslateTransform(-Left,-Top);
            using(PaintEventArgs parentPaint=new PaintEventArgs(e.Graphics,new Rectangle(Left,Top,Width,Height))) {InvokePaintBackground(Parent,parentPaint);InvokePaint(Parent,parentPaint);}
        } finally {e.Graphics.Restore(state);}
    }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color color=BackColor;
        if(Hover) color=ControlPaint.Light(color,.12f);
        if(Pressed) color=ControlPaint.Dark(color,.08f);
        if(!Enabled) color=Color.FromArgb(230,235,242);
        float scale=e.Graphics.DpiX/96f;
        using(var p=Brand.Round(new RectangleF(1,1,Width-3,Height-3),12*scale)) {
            if(!Navigation || Selected || Hover || Pressed) {
                Color top=ControlPaint.Light(color,.05f),bottom=color;
                if(Navigation) {top=Color.FromArgb(Selected?205:110,255,255,255);bottom=Color.FromArgb(Selected?110:60,255,255,255);}
                else if(ForeColor!=Color.White && Enabled) {top=Color.FromArgb(Hover?230:200,255,255,255);bottom=Color.FromArgb(Hover?185:105,255,255,255);}
                using(var b=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,top,bottom,90f)) e.Graphics.FillPath(b,p);
                using(Pen pen=new Pen(Color.FromArgb(Navigation?210:230,255,255,255))) e.Graphics.DrawPath(pen,p);
            }
            if((Focused && ShowFocusCues) || DefaultButton) {
                using(var focus=Brand.Round(new RectangleF(4,4,Width-9,Height-9),9*scale)) using(Pen pen=new Pen(Navigation?Color.FromArgb(155,206,255):Color.FromArgb(60,130,235),1.5f)) e.Graphics.DrawPath(pen,focus);
            }
        }
        if(Navigation) PaintGlyph(e.Graphics,scale);
        TextFormatFlags flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix;
        flags|=TextAlign==ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter;
        int inset=(int)((Navigation?43:12)*scale);
        Rectangle text=new Rectangle(inset,0,Width-inset-(int)(12*scale),Height);
        TextRenderer.DrawText(e.Graphics,Text,Font,text,Enabled?ForeColor:SystemColors.GrayText,flags);
    }
    void PaintGlyph(Graphics g,float scale) {
        var state=g.Save();g.TranslateTransform(15*scale,(Height-16*scale)/2);g.ScaleTransform(scale,scale);
        using(Pen p=new Pen(ForeColor,1.4f)) {
            p.StartCap=p.EndCap=System.Drawing.Drawing2D.LineCap.Round;
            if(Glyph==0) {g.DrawRectangle(p,1,1,5,5);g.DrawRectangle(p,10,1,5,5);g.DrawRectangle(p,1,10,5,5);g.DrawRectangle(p,10,10,5,5);}
            else if(Glyph==1) {g.DrawLine(p,1,4,15,4);g.DrawLine(p,1,12,15,12);using(Brush b=new SolidBrush(ForeColor)) {g.FillEllipse(b,4,1,6,6);g.FillEllipse(b,9,9,6,6);}}
            else {g.DrawEllipse(p,1,1,14,14);g.DrawLine(p,8,7,8,12);g.DrawEllipse(p,7.5f,4,1,1);}
        }
        g.Restore(state);
    }
}

internal sealed class MainWindow : Form {
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        if(SystemInformation.HighContrast) return;
        // These attributes are optional; older Windows versions retain their own frame.
        try {
            int caption=ColorTranslator.ToWin32(Color.FromArgb(234,241,250));
            int text=ColorTranslator.ToWin32(Brand.Ink);
            int border=ColorTranslator.ToWin32(Color.FromArgb(201,216,231));
            DwmSetWindowAttribute(Handle,35,ref caption,4);
            DwmSetWindowAttribute(Handle,36,ref text,4);
            DwmSetWindowAttribute(Handle,34,ref border,4);
        } catch(DllNotFoundException) {} catch(EntryPointNotFoundException) {}
    }
    readonly bool Preview,Background;
    Preferences Settings;
    SaveWorker Worker;
    NotifyIcon Tray;
    Panel[] Pages=new Panel[3];GlassButton[] Navigation=new GlassButton[3];
    Label Status,StatusDetail,Count,Last,Scope,Mode,Note;
    Label[] AppStatus=new Label[3];
    GlassButton PauseButton;
    ToolStripMenuItem TrayPause;
    RadioButton IntervalRadio,ChangesRadio;
    NumericUpDown Seconds;
    CheckBox Word,Excel,PowerPoint,Limit,Startup;
    TextBox Folder;
    bool Paused,Quitting;
    ProgressState Latest=new ProgressState();
    System.Windows.Forms.Timer UninstallTimer;

    internal MainWindow(Preferences settings,bool preview,bool background) {
        Preview=preview;Background=background;Settings=settings.Copy();
        Text="LocalSave";Icon=Brand.Icon();ClientSize=new Size(960,720);MinimumSize=MaximumSize=Size;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        Font=new Font("Segoe UI",10f);ForeColor=Brand.Ink;BackColor=Brand.Paper;
        Build(); SelectPage(0);ApplyLabels();
        if(!preview) {
            Worker=new SaveWorker(Settings);Worker.Changed+=OnProgress;
            Tray=new NotifyIcon {Icon=Icon,Text="LocalSave - checking for changes",Visible=true};
            ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("Open LocalSave",null,delegate {Reveal();});
            TrayPause=new ToolStripMenuItem("Pause saving",null,delegate {TogglePause();});menu.Items.Add(TrayPause);
            menu.Items.Add("Save now",null,delegate {Worker.SaveNow();});
            menu.Items.Add("Activity log",null,delegate {OpenLog();});menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit",null,delegate {ExitApp();});Tray.ContextMenuStrip=menu;Tray.DoubleClick+=delegate {Reveal();};
            Shown+=delegate {Store.Log("LocalSave 2.2 started. No document names or contents are logged.");Worker.Start();if(Background) Hide();};
            FormClosing+=delegate(object sender,FormClosingEventArgs e) {if(!Quitting && e.CloseReason==CloseReason.UserClosing) {e.Cancel=true;Hide();}};
        }
    }
    Label LabelAt(Control parent,string text,int x,int y,int width,int height,float size,Color color,bool bold) {
        Label label=new Label {Text=text,Left=x,Top=y,Width=width,Height=height,ForeColor=color,BackColor=Color.Transparent,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)};parent.Controls.Add(label);return label;
    }
    GlassButton ButtonAt(Control parent,string text,int x,int y,int width,bool primary,EventHandler click) {
        GlassButton b=new GlassButton {Text=text,Left=x,Top=y,Width=width,Height=42,BackColor=primary?Brand.Teal:Color.FromArgb(244,249,255),ForeColor=primary?Color.White:Brand.Ink,Cursor=Cursors.Hand};
        b.Click+=click;parent.Controls.Add(b);return b;
    }
    void Build() {
        Panel rail=new GlassRail {Left=0,Top=0,Width=208,Height=720,BackColor=Brand.Navy};Controls.Add(rail);
        PictureBox logo=new PictureBox {Left=24,Top=28,Width=48,Height=48,BackColor=Color.Transparent,Image=Brand.Logo(96),SizeMode=PictureBoxSizeMode.Zoom};rail.Controls.Add(logo);
        LabelAt(rail,"LocalSave",24,90,174,36,22,Brand.Ink,true);
        LabelAt(rail,"YOUR FILES. YOUR PC.",26,137,176,24,8,Brand.Muted,true);
        string[] nav={"Overview","Save settings","Privacy & help"};
        for(int i=0;i<3;i++) {
            int index=i;
            GlassButton b=ButtonAt(rail,nav[i],16,216+i*52,176,false,delegate {SelectPage(index);});
            b.Navigation=true;b.Glyph=i;b.TextAlign=ContentAlignment.MiddleLeft;Navigation[i]=b;
            Pages[i]=new GlassPage {Left=208,Top=0,Width=752,Height=720,BackColor=Brand.Paper};Controls.Add(Pages[i]);
        }
        LabelAt(rail,"LOCAL BY DESIGN",24,544,176,24,8,Brand.Teal,true);
        LabelAt(rail,"No account required.\r\nNo uploads by LocalSave.",24,573,170,52,9,Brand.Muted,false);
        LabelAt(rail,"CREATED BY",24,640,170,20,8,Brand.Muted,true);
        LabelAt(rail,"PLASHSMA",24,661,170,28,12,Brand.Ink,true);
        BuildOverview(Pages[0]);BuildSettings(Pages[1]);BuildHelp(Pages[2]);
    }
    void Heading(Panel page,string title,string subtitle) {
        LabelAt(page,title,32,34,682,44,24,Brand.Ink,true);LabelAt(page,subtitle,34,83,682,32,10,Brand.Muted,false);
    }
    void BuildOverview(Panel page) {
        Heading(page,"A little less to remember.","Automatic saving for your local Office documents.");
        Card hero=new Card(32,128,688,132);page.Controls.Add(hero);
        LabelAt(hero,"AUTOMATIC SAVING",22,17,360,22,8,Brand.Teal,true);
        Status=LabelAt(hero,"Checking for changes",22,42,600,36,21,Brand.Ink,true);
        StatusDetail=LabelAt(hero,"Only changed, writable local files are saved.",22,87,635,24,10,Brand.Muted,false);
        Card apps=new Card(32,276,688,170);page.Controls.Add(apps);
        string[] names={"Word","Excel","PowerPoint"};string[] letters={"W","X","P"};Color[] colors={Color.FromArgb(39,99,184),Color.FromArgb(24,118,76),Color.FromArgb(187,80,45)};
        for(int i=0;i<3;i++) {
            Label badge=LabelAt(apps,letters[i],20,18+i*49,32,30,13,Color.White,true);badge.BackColor=colors[i];badge.TextAlign=ContentAlignment.MiddleCenter;
            LabelAt(apps,names[i],68,21+i*49,160,28,11,Brand.Ink,true);
            AppStatus[i]=LabelAt(apps,"Waiting for application",264,17+i*49,400,42,9,Brand.Muted,false);AppStatus[i].AutoEllipsis=true;
        }
        Card saves=new Card(32,462,334,94);page.Controls.Add(saves);LabelAt(saves,"SUCCESSFUL SAVES THIS SESSION",20,15,298,22,8,Brand.Muted,true);Count=LabelAt(saves,"0",20,42,290,40,23,Brand.Ink,true);
        Card last=new Card(382,462,338,94);page.Controls.Add(last);LabelAt(last,"LAST SUCCESSFUL SAVE",20,15,298,22,8,Brand.Muted,true);Last=LabelAt(last,"No saves yet",20,45,295,30,14,Brand.Ink,true);
        Mode=LabelAt(page,"",34,572,684,24,10,Brand.Ink,true);Scope=LabelAt(page,"",34,598,684,24,9,Brand.Muted,false);
        PauseButton=ButtonAt(page,"Pause saving",32,640,154,true,delegate {TogglePause();});
        ButtonAt(page,"Save now",210,640,158,false,delegate {if(Worker!=null) Worker.SaveNow();});
        ButtonAt(page,"Activity log",384,640,160,false,delegate {OpenLog();});
        ButtonAt(page,"Hide to tray",560,640,160,false,delegate {if(!Preview) Hide();});
    }
    CheckBox Check(Control parent,string text,int x,int y,int width,bool value) {
        CheckBox box=new CheckBox {Text=text,Left=x,Top=y,Width=width,Height=28,Checked=value,BackColor=Color.Transparent};parent.Controls.Add(box);return box;
    }
    void BuildSettings(Panel page) {
        Heading(page,"Set your saving rhythm.","Choose when to save and which documents LocalSave can touch.");
        Card mode=new Card(32,128,688,144);page.Controls.Add(mode);LabelAt(mode,"WHEN TO SAVE",20,16,620,22,8,Brand.Teal,true);
        IntervalRadio=new RadioButton {Text="At an interval",Left=20,Top=47,Width=170,Height=28,Checked=!Settings.AfterChanges,BackColor=Color.Transparent};mode.Controls.Add(IntervalRadio);
        Seconds=new NumericUpDown {Left=208,Top=47,Width=85,Minimum=1,Maximum=3600,Value=Settings.Interval};mode.Controls.Add(Seconds);LabelAt(mode,"seconds",306,51,140,26,10,Brand.Muted,false);
        ChangesRadio=new RadioButton {Text="After changes",Left=20,Top=87,Width=170,Height=28,Checked=Settings.AfterChanges,BackColor=Color.Transparent};mode.Controls.Add(ChangesRadio);LabelAt(mode,"Checks about every 250 ms; saves when Office is ready.",208,91,455,28,9,Brand.Muted,false);
        ChangesRadio.CheckedChanged+=delegate {Seconds.Enabled=!ChangesRadio.Checked;};Seconds.Enabled=!Settings.AfterChanges;
        Card apps=new Card(32,288,688,87);page.Controls.Add(apps);LabelAt(apps,"APPLICATIONS",20,12,620,22,8,Brand.Teal,true);
        Word=Check(apps,"Word",20,42,170,Settings.Word);Excel=Check(apps,"Excel",226,42,170,Settings.Excel);PowerPoint=Check(apps,"PowerPoint",442,42,200,Settings.PowerPoint);
        Card scope=new Card(32,391,688,137);page.Controls.Add(scope);Limit=Check(scope,"Only save files inside this folder and its subfolders",20,13,648,Settings.LimitFolder);
        Folder=new TextBox {Left=20,Top=52,Width=523,Text=Settings.Folder,ReadOnly=true,BackColor=Color.White};scope.Controls.Add(Folder);
        ButtonAt(scope,"Browse",554,48,112,false,delegate {
            using(FolderBrowserDialog dialog=new FolderBrowserDialog {Description="Choose the local folder LocalSave can save in.",ShowNewFolderButton=false}) {if(dialog.ShowDialog(this)==DialogResult.OK) {Folder.Text=dialog.SelectedPath;Limit.Checked=true;}}
        });
        LabelAt(scope,"Untitled, read-only, network and redirected paths are skipped.",20,101,648,24,9,Brand.Muted,false);
        Card launch=new Card(32,544,688,76);page.Controls.Add(launch);Startup=Check(launch,"Start LocalSave when I sign into Windows",20,10,640,!Preview && Store.StartupEnabled());
        LabelAt(launch,"Keep the EXE in one location. Apply settings to update its startup path.",20,44,648,24,9,Brand.Muted,false);
        ButtonAt(page,"Apply settings",32,648,174,true,delegate {ApplySettings();});Note=LabelAt(page,"",224,655,496,40,9,Brand.Muted,false);
    }
    void BuildHelp(Panel page) {
        Heading(page,"Private, with clear boundaries.","LocalSave is an independent utility for Microsoft Office on Windows.");
        Card privacy=new Card(32,128,688,175);page.Controls.Add(privacy);
        LabelAt(privacy,"ON YOUR COMPUTER",20,16,640,22,8,Brand.Teal,true);
        LabelAt(privacy,"No account, telemetry or keyboard recording.",20,45,640,28,12,Brand.Ink,true);
        LabelAt(privacy,"The helper makes no network requests. Logs contain app names, save counts and error codes, never document names or contents. Office may still sync files through its own services.",20,85,645,72,10,Brand.Muted,false);
        Card help=new Card(32,319,688,197);page.Controls.Add(help);
        LabelAt(help,"HOW TO USE IT",20,15,640,22,8,Brand.Teal,true);
        LabelAt(help,"1. Save each new file once to choose its name and location.\r\n2. Leave LocalSave running; close the window to keep it in the tray.\r\n3. Check Overview for successful saves and any app delays.",20,45,645,79,10,Brand.Ink,false);
        LabelAt(help,"Excel detects multiple desktop instances; Word and PowerPoint use one registered instance. Commit Excel cell edits first. Busy dialogs can delay saves. Keep AutoRecover and backups enabled.",20,130,645,61,9,Brand.Muted,false);
        LabelAt(page,"PORTABLE WINDOWS APP  /  .NET FRAMEWORK 4.8+",34,534,680,22,8,Brand.Teal,true);
        LabelAt(page,"Copy this EXE to a Windows PC with desktop Office. No administrator rights or security-setting changes are needed. This build is unsigned; Windows or your organization may block it. A publisher signature and reputation are required for smoother distribution.",34,565,680,73,9,Brand.Muted,false);
        ButtonAt(page,"Uninstall / reset",32,648,178,false,delegate {Uninstall();});
        ButtonAt(page,"Exit LocalSave",224,648,162,false,delegate {ExitApp();});
        LabelAt(page,"v2.2  |  Created by PLASHSMA",408,659,310,24,9,Brand.Muted,false);
    }
    internal void SelectPage(int index) {
        for(int i=0;i<3;i++) {Pages[i].Visible=i==index;Navigation[i].Selected=i==index;Navigation[i].BackColor=Brand.Paper;Navigation[i].ForeColor=i==index?Brand.Ink:Brand.Muted;Navigation[i].Invalidate();}
    }
    void ApplyLabels() {
        Mode.Text=Settings.AfterChanges?"Save after changes  /  checks about every 250 ms":"Save every "+Settings.Interval+" second"+(Settings.Interval==1?"":"s");
        Scope.Text=Settings.LimitFolder?"Folder restriction enabled: "+Settings.Folder:"Scope: eligible local files in your selected Office apps";Scope.AutoEllipsis=true;
    }
    void ApplySettings() {
        if(Preview) return;
        try {
            Preferences next=new Preferences {Interval=(int)Seconds.Value,AfterChanges=ChangesRadio.Checked,Word=Word.Checked,Excel=Excel.Checked,PowerPoint=PowerPoint.Checked,LimitFolder=Limit.Checked,Folder=Folder.Text};
            next.Validate();Store.Save(next);Settings=next;Worker.Update(next);ApplyLabels();
            try {Store.Startup(Startup.Checked,Application.ExecutablePath);} catch(Exception ex) {Note.Text="Save settings applied; startup update failed.";MessageBox.Show(this,ex.Message,"Startup could not be updated",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
            Note.Text="Settings saved.";
        } catch(Exception ex) {Note.Text="Settings were not applied.";MessageBox.Show(this,ex.Message,"Check settings",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    void OnProgress(ProgressState state) {
        if(IsDisposed || !IsHandleCreated) return;
        try {BeginInvoke((Action)delegate {if(IsDisposed) return;Latest=state;RefreshStatus();});} catch(InvalidOperationException) {}
    }
    void RefreshStatus() {
        Status.Text=Paused?"Automatic saving paused":(!Settings.Word && !Settings.Excel && !Settings.PowerPoint)?"No applications selected":"Checking for changes";
        StatusDetail.Text=Latest.LogUnavailable?"Activity log is unavailable. Check permissions on your profile folder.":Paused?"Use Resume saving to continue. Save now still works.":"Only changed, writable local files are saved.";
        Count.Text=Latest.Saves.ToString("N0");Last.Text=Latest.LastSave.HasValue?Latest.LastSave.Value.ToString("h:mm:ss tt"):"No saves yet";
        for(int i=0;i<3;i++) AppStatus[i].Text=Latest.Apps[i];
    }
    void TogglePause() {
        if(Preview) return;
        Paused=!Paused;Worker.Pause(Paused);PauseButton.Text=Paused?"Resume saving":"Pause saving";TrayPause.Text=PauseButton.Text;Tray.Text=Paused?"LocalSave - paused":"LocalSave - checking for changes";RefreshStatus();Store.Log(Paused?"Automatic saving paused.":"Automatic saving resumed.");
    }
    void Reveal() {Show();WindowState=FormWindowState.Normal;Activate();}
    void OpenLog() {
        if(Preview) return;
        try {
            Store.CheckFile(Store.LogPath);
            Form log=new Form {Text="LocalSave activity",Size=new Size(820,510),StartPosition=FormStartPosition.CenterParent,Icon=Icon};
            TextBox text=new TextBox {Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,WordWrap=false,Font=new Font("Consolas",10),BackColor=Color.White};
            text.Text=File.Exists(Store.LogPath)?File.ReadAllText(Store.LogPath):"No activity recorded yet.";log.Controls.Add(text);using(log) log.ShowDialog(this);
        } catch(Exception ex) {MessageBox.Show(this,ex.Message,"Could not open the log");}
    }
    void Uninstall() {
        if(Preview) return;
        if(MessageBox.Show(this,"Remove LocalSave's startup entry, settings and activity logs?\r\nYour Office files will not be removed. You can delete this portable EXE afterward.","Uninstall LocalSave",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK) return;
        Worker.Stop();Enabled=false;
        UninstallTimer=new System.Windows.Forms.Timer {Interval=250};int ticks=0;
        UninstallTimer.Tick+=delegate {
            ticks++;
            if(!Worker.IsStopped) {if(ticks==20) {Enabled=true;MessageBox.Show(this,"LocalSave is waiting for an Office call to finish. Close any Office save dialogs. Uninstall will continue when the worker stops.","Finishing the current operation");}return;}
            UninstallTimer.Stop();Enabled=true;
            try {Store.Remove();MessageBox.Show(this,"Startup, settings and logs removed. LocalSave will close.\r\nYou can now delete this EXE.","Uninstalled");Quitting=true;Close();}
            catch(Exception ex) {Paused=true;MessageBox.Show(this,"Cleanup could not finish: "+ex.Message+"\r\nSaving has stopped. Exit and restart to resume.","Uninstall needs attention");}
        };
        UninstallTimer.Start();
    }
    void ExitApp() {if(Preview) return;Quitting=true;Worker.Stop();Close();}
    protected override void Dispose(bool disposing) {
        if(disposing) {
            if(Worker!=null) {Worker.Changed-=OnProgress;Worker.Stop();}
            if(Tray!=null) {Tray.Visible=false;Tray.ContextMenuStrip.Dispose();Tray.Dispose();}
            if(UninstallTimer!=null) UninstallTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
