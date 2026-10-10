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
[assembly: AssemblyDescription("Local automatic saving for Office, Photoshop and Illustrator")]
[assembly: AssemblyCompany("PLASHSMA")]
[assembly: AssemblyCopyright("Created by PLASHSMA")]
[assembly: AssemblyVersion("2.7.0.0")]
[assembly: AssemblyFileVersion("2.7.0.0")]

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
            using(AppIntervalsDialog dialog=new AppIntervalsDialog(new int[]{1,2,5,30,15},30,false)) {
                dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-32000,-32000);dialog.ShowInTaskbar=false;dialog.Show();dialog.Hide();
                using(Bitmap image=new Bitmap(dialog.Width,dialog.Height)) {dialog.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(args[1],"localsave-intervals.png"));}
            }
            using(HealthSettingsDialog dialog=new HealthSettingsDialog(true,120)) {
                dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-32000,-32000);dialog.ShowInTaskbar=false;dialog.Show();dialog.Hide();
                using(Bitmap image=new Bitmap(dialog.Width,dialog.Height)) {dialog.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(args[1],"localsave-warnings.png"));}
            }
            return 0;
        }
        bool owner;
        using(Mutex mutex=new Mutex(true,@"Local\LocalOfficeAutoSave-Native",out owner)) {
            if(!owner) {MessageBox.Show("LocalSave or an earlier version is already running. Open its tray icon, or exit it before starting this version.","LocalSave",MessageBoxButtons.OK,MessageBoxIcon.Information);return 0;}
            try {
                Preferences p=Store.Load();
                bool startupFailed=false;
                try {if(Store.RefreshStartup(Application.ExecutablePath)) Store.Log("Enabled Windows startup updated to the current app version.");}
                catch {startupFailed=true;Store.Log("Preferences loaded; Windows startup update failed. Apply settings to retry.");}
                using(MainWindow window=new MainWindow(p,false,Array.IndexOf(args,"--background")>=0)) {
                    if(startupFailed) window.Shown+=delegate {MessageBox.Show(window,"Your saved preferences are loaded, but Windows startup could not be updated. In Save settings, click Apply settings to retry.","Startup needs attention",MessageBoxButtons.OK,MessageBoxIcon.Warning);};
                    Application.Run(window);
                }
                return 0;
            } catch(Exception ex) {MessageBox.Show("LocalSave encountered an error.\r\n\r\n"+ex.Message,"LocalSave",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
            finally {mutex.ReleaseMutex();}
        }
    }
}

internal sealed class HealthSettingsDialog : Form {
    readonly CheckBox EnabledBox;
    readonly NumericUpDown Delay;
    internal bool WarningsEnabled {get {return EnabledBox.Checked;}}
    internal int WarningSeconds {get {return (int)Delay.Value;}}
    internal HealthSettingsDialog(bool enabled,int seconds) {
        Text="LocalSave - Save warnings";Icon=Brand.Icon();ClientSize=new Size(540,320);
        FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10f);ForeColor=Brand.Ink;BackColor=Brand.Paper;AutoScaleMode=AutoScaleMode.Dpi;
        GlassPage page=new GlassPage {Dock=DockStyle.Fill};Controls.Add(page);
        page.Controls.Add(new Label {Text="Know when saving needs help.",Left=24,Top=24,Width=492,Height=40,Font=new Font("Segoe UI",18,FontStyle.Bold),BackColor=Color.Transparent});
        EnabledBox=new CheckBox {Text="Show save-health status and tray warnings",Left=26,Top=82,Width=490,Height=28,Checked=enabled,BackColor=Color.Transparent};page.Controls.Add(EnabledBox);
        page.Controls.Add(new Label {Text="Warn after unsaved changes remain for",Left=26,Top=129,Width=300,Height=28,BackColor=Color.Transparent});
        Delay=new NumericUpDown {Left=332,Top=125,Width=82,Minimum=15,Maximum=3600,Value=seconds,AccessibleName="Save warning delay in seconds",Enabled=enabled};page.Controls.Add(Delay);
        page.Controls.Add(new Label {Text="seconds",Left=426,Top=129,Width=90,Height=28,BackColor=Color.Transparent});
        EnabledBox.CheckedChanged+=delegate {Delay.Enabled=EnabledBox.Checked;};
        page.Controls.Add(new Label {Text="Warnings require detected unsaved changes. Longer app intervals get an extra 30 seconds. Pausing suppresses warnings.\r\nOne notice per unresolved episode; notices are at least a minute apart.",Left=26,Top=174,Width=488,Height=82,ForeColor=Brand.Muted,BackColor=Color.Transparent,Font=new Font("Segoe UI",9)});
        GlassButton cancel=new GlassButton {Text="Cancel",Left=266,Top=266,Width=116,Height=36,DialogResult=DialogResult.Cancel,BackColor=Color.FromArgb(244,249,255),ForeColor=Brand.Ink};page.Controls.Add(cancel);
        GlassButton done=new GlassButton {Text="Use warnings",Left=394,Top=266,Width=122,Height=36,DialogResult=DialogResult.OK,BackColor=Brand.Teal,ForeColor=Color.White};page.Controls.Add(done);
        AcceptButton=done;CancelButton=cancel;
    }
}

internal sealed class AppIntervalsDialog : Form {
    readonly NumericUpDown[] Inputs=new NumericUpDown[5];
    internal int[] Values {get {int[] values=new int[5];for(int i=0;i<5;i++) values[i]=(int)Inputs[i].Value;return values;}}
    internal AppIntervalsDialog(int[] values,int defaultSeconds,bool afterChanges) {
        Text="LocalSave - App intervals";Icon=Brand.Icon();ClientSize=new Size(540,440);
        FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10f);ForeColor=Brand.Ink;BackColor=Brand.Paper;AutoScaleMode=AutoScaleMode.Dpi;
        GlassPage page=new GlassPage {Dock=DockStyle.Fill};Controls.Add(page);
        page.Controls.Add(new Label {Text="A rhythm for each app.",Left=24,Top=24,Width=490,Height=40,Font=new Font("Segoe UI",20,FontStyle.Bold),BackColor=Color.Transparent});
        page.Controls.Add(new Label {Text="0 uses your default of "+defaultSeconds+" seconds. Range: 1-3600.",Left=26,Top=72,Width=490,Height=28,ForeColor=Brand.Muted,BackColor=Color.Transparent});
        Card card=new Card(24,110,492,224);page.Controls.Add(card);
        string[] names={"Word","Excel","PowerPoint","Photoshop","Illustrator"};
        for(int i=0;i<5;i++) {
            int y=16+i*40;
            card.Controls.Add(new Label {Text=names[i],Left=20,Top=y+3,Width=210,Height=28,BackColor=Color.Transparent});
            Inputs[i]=new NumericUpDown {Left=274,Top=y,Width=90,Minimum=0,Maximum=3600,Value=values[i],AccessibleName=names[i]+" interval in seconds"};card.Controls.Add(Inputs[i]);
            card.Controls.Add(new Label {Text="seconds",Left=378,Top=y+3,Width=90,Height=28,ForeColor=Brand.Muted,BackColor=Color.Transparent});
        }
        page.Controls.Add(new Label {Text=afterChanges?"After changes is active. These intervals apply when you switch to At an interval.":"Each app runs independently. Large saves may take longer than the interval.",Left=26,Top=346,Width=488,Height=40,ForeColor=Brand.Muted,BackColor=Color.Transparent,Font=new Font("Segoe UI",9)});
        GlassButton cancel=new GlassButton {Text="Cancel",Left=266,Top=390,Width=116,Height=36,DialogResult=DialogResult.Cancel,BackColor=Color.FromArgb(244,249,255),ForeColor=Brand.Ink};page.Controls.Add(cancel);
        GlassButton done=new GlassButton {Text="Use intervals",Left=394,Top=390,Width=122,Height=36,DialogResult=DialogResult.OK,BackColor=Brand.Teal,ForeColor=Color.White};page.Controls.Add(done);
        AcceptButton=done;CancelButton=cancel;
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
    SaveCoordinator Worker;
    NotifyIcon Tray;
    Icon WarningTrayIcon=Brand.WarningIcon();
    Panel[] Pages=new Panel[3];GlassButton[] Navigation=new GlassButton[3];
    Label Status,StatusDetail,Count,Last,Scope,Mode,Note;
    Label[] AppStatus=new Label[5];
    GlassButton PauseButton;
    ToolStripMenuItem TrayPause;
    RadioButton IntervalRadio,ChangesRadio;
    NumericUpDown Seconds;
    int[] AppSeconds;
    bool WarningEnabled;
    int WarningSeconds;
    readonly SaveHealth Health=new SaveHealth();
    System.Windows.Forms.Timer HealthTimer;
    CheckBox Word,Excel,PowerPoint,Photoshop,Illustrator,Limit,Startup;
    TextBox Folder;
    bool Paused,Quitting;
    ProgressState Latest=new ProgressState();
    System.Windows.Forms.Timer UninstallTimer;

    readonly bool? StartupState;
    internal MainWindow(Preferences settings,bool preview,bool background,bool? startupState=null) {
        StartupState=startupState;
        Preview=preview;Background=background;Settings=settings.Copy();
        AppSeconds=new int[]{Settings.WordInterval,Settings.ExcelInterval,Settings.PowerPointInterval,Settings.PhotoshopInterval,Settings.IllustratorInterval};
        WarningEnabled=Settings.SaveHealthWarnings;WarningSeconds=Settings.HealthWarningSeconds;
        Text="LocalSave";Icon=Brand.Icon();ClientSize=new Size(960,720);MinimumSize=MaximumSize=Size;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        Font=new Font("Segoe UI",10f);ForeColor=Brand.Ink;BackColor=Brand.Paper;
        Build(); SelectPage(0);ApplyLabels();
        if(!preview) {
            Worker=new SaveCoordinator(Settings);Worker.Changed+=OnProgress;
            HealthTimer=new System.Windows.Forms.Timer {Interval=1000};HealthTimer.Tick+=delegate {if(!Quitting) RefreshStatus();};HealthTimer.Start();
            Tray=new NotifyIcon {Icon=Icon,Text="LocalSave - checking for changes",Visible=true};
            ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("Open LocalSave",null,delegate {Reveal();});
            TrayPause=new ToolStripMenuItem("Pause saving",null,delegate {TogglePause();});menu.Items.Add(TrayPause);
            menu.Items.Add("Save now",null,delegate {Worker.SaveNow();});
            menu.Items.Add("Activity log",null,delegate {OpenLog();});menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit",null,delegate {ExitApp();});Tray.ContextMenuStrip=menu;Tray.DoubleClick+=delegate {Reveal();};
            Shown+=delegate {Store.Log("LocalSave 2.7.0 started. No document names or contents are logged.");Worker.Start();if(Background) Hide();};
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
        Heading(page,"A little less to remember.","Automatic saving for local documents and creative projects.");
        Card hero=new Card(32,128,688,112);page.Controls.Add(hero);
        LabelAt(hero,"AUTOMATIC SAVING",22,17,360,22,8,Brand.Teal,true);
        Status=LabelAt(hero,"Checking for changes",22,36,600,36,21,Brand.Ink,true);
        StatusDetail=LabelAt(hero,"Only changed, writable local files are saved.",22,77,635,24,10,Brand.Muted,false);
        Card apps=new Card(32,250,688,202);page.Controls.Add(apps);
        string[] names={"Word","Excel","PowerPoint","Photoshop","Illustrator"};string[] letters={"W","X","P","Ps","Ai"};Color[] colors={Color.FromArgb(39,99,184),Color.FromArgb(24,118,76),Color.FromArgb(187,80,45),Color.FromArgb(26,65,105),Color.FromArgb(136,72,17)};
        for(int i=0;i<names.Length;i++) {
            Label badge=LabelAt(apps,letters[i],20,12+i*37,32,28,11,Color.White,true);badge.BackColor=colors[i];badge.TextAlign=ContentAlignment.MiddleCenter;
            LabelAt(apps,names[i],68,14+i*37,160,28,11,Brand.Ink,true);
            AppStatus[i]=LabelAt(apps,Latest.Apps[i],264,10+i*37,400,36,8.5f,Brand.Muted,false);AppStatus[i].AutoEllipsis=true;
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
        Card mode=new Card(32,128,688,144);page.Controls.Add(mode);LabelAt(mode,"WHEN TO SAVE",20,16,350,22,8,Brand.Teal,true);
        GlassButton warnings=ButtonAt(mode,"Save warnings...",478,8,190,false,delegate {
            using(HealthSettingsDialog dialog=new HealthSettingsDialog(WarningEnabled,WarningSeconds))
                if(dialog.ShowDialog(this)==DialogResult.OK) {WarningEnabled=dialog.WarningsEnabled;WarningSeconds=dialog.WarningSeconds;Note.Text="Warnings selected. Click Apply settings to save.";}
        });warnings.Height=30;
        IntervalRadio=new RadioButton {Text="At an interval",Left=20,Top=47,Width=170,Height=28,Checked=!Settings.AfterChanges,BackColor=Color.Transparent};mode.Controls.Add(IntervalRadio);
        Seconds=new NumericUpDown {Left=208,Top=47,Width=85,Minimum=1,Maximum=3600,Value=Settings.Interval};mode.Controls.Add(Seconds);LabelAt(mode,"seconds",306,51,140,26,10,Brand.Muted,false);
        ChangesRadio=new RadioButton {Text="After changes",Left=20,Top=87,Width=170,Height=28,Checked=Settings.AfterChanges,BackColor=Color.Transparent};mode.Controls.Add(ChangesRadio);LabelAt(mode,"Checks about every 250 ms; saves when the app is ready.",208,91,455,28,9,Brand.Muted,false);
        ChangesRadio.CheckedChanged+=delegate {Seconds.Enabled=!ChangesRadio.Checked;};Seconds.Enabled=!Settings.AfterChanges;
        Card apps=new Card(32,282,688,120);page.Controls.Add(apps);LabelAt(apps,"APPLICATIONS",20,10,350,22,8,Brand.Teal,true);
        GlassButton timing=ButtonAt(apps,"App intervals...",478,8,190,false,delegate {
            using(AppIntervalsDialog dialog=new AppIntervalsDialog(AppSeconds,(int)Seconds.Value,ChangesRadio.Checked))
                if(dialog.ShowDialog(this)==DialogResult.OK) {AppSeconds=dialog.Values;Note.Text="App intervals selected. Click Apply settings to save.";}
        });timing.Height=30;
        Word=Check(apps,"Word",20,42,170,Settings.Word);Excel=Check(apps,"Excel",226,42,170,Settings.Excel);PowerPoint=Check(apps,"PowerPoint",442,42,200,Settings.PowerPoint);
        Photoshop=Check(apps,"Photoshop",20,78,170,Settings.Photoshop);Illustrator=Check(apps,"Illustrator",226,78,170,Settings.Illustrator);LabelAt(apps,"PSD / PSB and AI files",442,81,220,28,9,Brand.Muted,false);
        Card scope=new Card(32,412,688,127);page.Controls.Add(scope);Limit=Check(scope,"Only save files inside this folder and its subfolders",20,10,648,Settings.LimitFolder);
        Folder=new TextBox {Left=20,Top=52,Width=523,Text=Settings.Folder,ReadOnly=true,BackColor=Color.White};scope.Controls.Add(Folder);
        ButtonAt(scope,"Browse",554,48,112,false,delegate {
            using(FolderBrowserDialog dialog=new FolderBrowserDialog {Description="Choose the local folder LocalSave can save in.",ShowNewFolderButton=false}) {if(dialog.ShowDialog(this)==DialogResult.OK) {Folder.Text=dialog.SelectedPath;Limit.Checked=true;}}
        });
        LabelAt(scope,"Save Adobe projects locally once first. Export formats are skipped.",20,96,648,24,9,Brand.Muted,false);
        Card launch=new Card(32,549,688,76);page.Controls.Add(launch);Startup=Check(launch,"Start LocalSave when I sign into Windows",20,10,640,!Preview && (StartupState ?? Store.StartupEnabled()));
        LabelAt(launch,"Saved in AppData. Enabled startup follows this EXE when a new version runs.",20,44,648,24,9,Brand.Muted,false);
        ButtonAt(page,"Apply settings",32,648,174,true,delegate {ApplySettings();});Note=LabelAt(page,"",224,655,496,40,9,Brand.Muted,false);
    }
    void BuildHelp(Panel page) {
        Heading(page,"Private, with clear boundaries.","Independent saving for Office and Adobe desktop apps on Windows.");
        Card privacy=new Card(32,128,688,175);page.Controls.Add(privacy);
        LabelAt(privacy,"ON YOUR COMPUTER",20,16,640,22,8,Brand.Teal,true);
        LabelAt(privacy,"No account, telemetry or keyboard recording.",20,45,640,28,12,Brand.Ink,true);
        LabelAt(privacy,"The helper makes no network requests. Logs contain app names, save counts and error codes, never document names or contents. Office may still sync files through its own services.",20,85,645,72,10,Brand.Muted,false);
        Card help=new Card(32,319,688,197);page.Controls.Add(help);
        LabelAt(help,"HOW TO USE IT",20,15,640,22,8,Brand.Teal,true);
        LabelAt(help,"1. Save each new file once to choose its name and location.\r\n2. Leave LocalSave running; close the window to keep it in the tray.\r\n3. Check Overview for successful saves and any app delays.",20,45,645,79,10,Brand.Ink,false);
        LabelAt(help,"Adobe support is opt-in: existing local PSD/PSB and AI files only. One registered instance per app (Excel supports multiple). Finish edits and dialogs first. Keep recovery and backups enabled.",20,130,645,61,9,Brand.Muted,false);
        LabelAt(page,"FREE FOR PERSONAL USE ONLY  /  .NET FRAMEWORK 4.8+",34,534,680,22,8,Brand.Teal,true);
        LabelAt(page,"Install using LocalSave Setup. Remove the app in Windows Settings > Apps > Installed apps. Preferences stay in AppData for reinstalling; Reset preferences clears them. This preview is unsigned; Windows or your organization may block it.",34,565,680,73,9,Brand.Muted,false);
        ButtonAt(page,"Reset preferences",32,648,178,false,delegate {Uninstall();});
        ButtonAt(page,"Exit LocalSave",224,648,162,false,delegate {ExitApp();});
        LinkLabel license=new LinkLabel {Text="Personal-use license",Left=408,Top=649,Width=300,Height=22,BackColor=Color.Transparent};page.Controls.Add(license);
        license.LinkClicked+=delegate {
            using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("LocalSave.LICENSE")) {
                if(stream!=null) using(StreamReader reader=new StreamReader(stream)) {
                    using(Form terms=new Form {Text="LocalSave Personal Use License",Size=new Size(800,580),StartPosition=FormStartPosition.CenterParent}) {
                        TextBox text=new TextBox {Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Font=new Font("Segoe UI",10),Text=reader.ReadToEnd()};terms.Controls.Add(text);terms.ShowDialog(this);
                    }
                }
            }
        };
        LabelAt(page,"v2.7.0  |  Created by PLASHSMA",408,675,310,24,9,Brand.Muted,false);
    }
    internal void SelectPage(int index) {
        for(int i=0;i<3;i++) {Pages[i].Visible=i==index;Navigation[i].Selected=i==index;Navigation[i].BackColor=Brand.Paper;Navigation[i].ForeColor=i==index?Brand.Ink:Brand.Muted;Navigation[i].Invalidate();}
    }
    void ApplyLabels() {
        bool custom=Settings.WordInterval!=0 || Settings.ExcelInterval!=0 || Settings.PowerPointInterval!=0 || Settings.PhotoshopInterval!=0 || Settings.IllustratorInterval!=0;
        Mode.Text=Settings.AfterChanges?"Save after changes  /  each app checks about every 250 ms":custom?"Separate app intervals  /  default "+Settings.Interval+" seconds":"Save every "+Settings.Interval+" second"+(Settings.Interval==1?"":"s");
        Scope.Text=Settings.LimitFolder?"Folder restriction enabled: "+Settings.Folder:"Scope: eligible local files in your selected apps";Scope.AutoEllipsis=true;
    }
    void ApplySettings() {
        if(Preview) return;
        try {
            Preferences next=new Preferences {Interval=(int)Seconds.Value,AfterChanges=ChangesRadio.Checked,Word=Word.Checked,Excel=Excel.Checked,PowerPoint=PowerPoint.Checked,Photoshop=Photoshop.Checked,Illustrator=Illustrator.Checked,LimitFolder=Limit.Checked,Folder=Folder.Text};
            next.WordInterval=AppSeconds[0];next.ExcelInterval=AppSeconds[1];next.PowerPointInterval=AppSeconds[2];next.PhotoshopInterval=AppSeconds[3];next.IllustratorInterval=AppSeconds[4];
            next.SaveHealthWarnings=WarningEnabled;next.HealthWarningSeconds=WarningSeconds;
            next.Validate();Store.Save(next);Settings=next;Worker.Update(next);ApplyLabels();RefreshStatus();
            try {Store.Startup(Startup.Checked,Application.ExecutablePath);} catch(Exception ex) {Note.Text="Save settings applied; startup update failed.";MessageBox.Show(this,ex.Message,"Startup could not be updated",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
            Note.Text="Settings saved.";
        } catch(Exception ex) {Note.Text="Settings were not applied.";MessageBox.Show(this,ex.Message,"Check settings",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    void OnProgress(ProgressState state) {
        if(Quitting || Disposing || IsDisposed || !IsHandleCreated) return;
        try {BeginInvoke((Action)delegate {if(Quitting || Disposing || IsDisposed) return;Latest=state;RefreshStatus();});} catch(InvalidOperationException) {}
    }
    void RefreshStatus() {
        HealthReport health=Health.Evaluate(Latest,Settings,Paused,DateTime.UtcNow);
        bool needsAttention=Array.IndexOf(health.Warnings,true)>=0;
        Status.Text=Paused?"Automatic saving paused":(!Settings.Word && !Settings.Excel && !Settings.PowerPoint && !Settings.Photoshop && !Settings.Illustrator)?"No applications selected":"Checking for changes";
        StatusDetail.Text=Latest.LogUnavailable?"Activity log is unavailable. Check permissions on your profile folder.":Paused?"Use Resume saving to continue. Save now still works.":"Only changed, writable local files are saved.";
        Count.Text=Latest.Saves.ToString("N0");Last.Text=Latest.LastSave.HasValue?Latest.LastSave.Value.ToString("h:mm:ss tt"):"No saves yet";
        Status.ForeColor=needsAttention?Color.FromArgb(153,78,15):Brand.Ink;
        if(needsAttention) {Status.Text="Saving needs attention";StatusDetail.Text="Check the highlighted apps. Finish edits/dialogs or save manually.";}
        for(int i=0;i<AppStatus.Length;i++) {
            AppStatus[i].ForeColor=health.Warnings[i]?Color.FromArgb(153,78,15):Brand.Muted;
            AppStatus[i].Text=health.Warnings[i]?"Unsaved changes need attention\n"+Latest.Apps[i].Split('\n')[0]:Latest.Apps[i];
        }
        if(Tray!=null) {
            UpdateTrayStatus(needsAttention);
            if(health.Notify.Count>0) {
                string apps=String.Join(", ",health.Notify);
                Store.Log(apps+": save-health warning; detected changes remain unconfirmed.");
                Tray.ShowBalloonTip(10000,"LocalSave - saving needs attention",apps+": detected changes remain unsaved. Finish edits/dialogs or save manually. Open LocalSave for status.",ToolTipIcon.Warning);
            }
        }
    }
    internal void UpdateTrayStatus(bool needsAttention) {
        if(Tray==null || Quitting) return;
        Icon desired=needsAttention && !Paused?WarningTrayIcon:Icon;
        if(!Object.ReferenceEquals(Tray.Icon,desired)) Tray.Icon=desired;
        Tray.Text=Paused?"LocalSave - paused":needsAttention?"LocalSave - saving needs attention":"LocalSave - checking for changes";
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
        if(MessageBox.Show(this,"Reset LocalSave's startup entry, preferences and activity logs?\r\nYour documents and the installed app will remain. Use Windows Settings to uninstall the app.","Reset preferences",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK) return;
        Worker.Stop();Enabled=false;
        UninstallTimer=new System.Windows.Forms.Timer {Interval=250};int ticks=0;
        UninstallTimer.Tick+=delegate {
            ticks++;
            if(!Worker.IsStopped) {if(ticks==20) {Enabled=true;MessageBox.Show(this,"LocalSave is waiting for an Office call to finish. Close any Office save dialogs. Uninstall will continue when the worker stops.","Finishing the current operation");}return;}
            UninstallTimer.Stop();Enabled=true;
            try {Store.Remove();MessageBox.Show(this,"Startup, preferences and logs removed. LocalSave will close.\r\nUse Windows Settings > Apps to remove the installed app.","Preferences reset");Quitting=true;Close();}
            catch(Exception ex) {Paused=true;MessageBox.Show(this,"Cleanup could not finish: "+ex.Message+"\r\nSaving has stopped. Exit and restart to resume.","Uninstall needs attention");}
        };
        UninstallTimer.Start();
    }
    void ExitApp() {if(Preview || Quitting) return;Quitting=true;if(Worker!=null) Worker.Stop();Close();}
    protected override void Dispose(bool disposing) {
        if(disposing) {
            Quitting=true;
            System.Windows.Forms.Timer healthTimer=HealthTimer;HealthTimer=null;if(healthTimer!=null) {healthTimer.Stop();healthTimer.Dispose();}
            SaveCoordinator worker=Worker;Worker=null;
            if(worker!=null) {worker.Changed-=OnProgress;worker.Stop();}
            NotifyIcon tray=Tray;Tray=null;
            if(tray!=null) {
                ContextMenuStrip menu=tray.ContextMenuStrip;
                tray.ContextMenuStrip=null;tray.Visible=false;tray.Dispose();
                if(menu!=null) menu.Dispose();
            }
            Icon warningIcon=WarningTrayIcon;WarningTrayIcon=null;if(warningIcon!=null) warningIcon.Dispose();
            System.Windows.Forms.Timer timer=UninstallTimer;UninstallTimer=null;
            if(timer!=null) {timer.Stop();timer.Dispose();}
        }
        base.Dispose(disposing);
    }
}
