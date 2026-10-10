using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

internal static class UITests {
    static int Count;
    static void Assert(bool pass,string name) {if(!pass) throw new Exception(name);Count++;Console.WriteLine("PASS UI: "+name);}
    static void Event(GlassButton button,string name,object argument) {typeof(GlassButton).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(button,new object[]{argument});}
    [STAThread] static int Main() {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        using(Form host=new Form {BackColor=Color.FromArgb(37,67,91),StartPosition=FormStartPosition.Manual,Location=new Point(-32000,-32000),ShowInTaskbar=false}) {
            GlassButton button=new GlassButton {Bounds=new Rectangle(20,20,160,42),Text="Test",BackColor=Color.FromArgb(38,102,214),ForeColor=Color.White};
            host.Controls.Add(button);host.Show();
            int clicks=0;button.Click+=delegate {clicks++;};
            Event(button,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,30,20,0));
            Event(button,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,30,20,0));
            Assert(clicks==1,"mouse click fires exactly once");
            Event(button,"OnKeyDown",new KeyEventArgs(Keys.Space));Event(button,"OnKeyUp",new KeyEventArgs(Keys.Space));
            Assert(clicks==2,"Space activates button");
            Event(button,"ProcessDialogKey",Keys.Enter);Assert(clicks==3,"Enter activates button");
            button.Enabled=false;button.PerformClick();Assert(clicks==3,"disabled button cannot activate");button.Enabled=true;
            button.AccessibilityObject.DoDefaultAction();Assert(clicks==4,"accessible press action works");
            using(Bitmap image=new Bitmap(button.Width,button.Height)) {
                button.DrawToBitmap(image,button.ClientRectangle);
                Assert(image.GetPixel(0,0).ToArgb()==host.BackColor.ToArgb() && image.GetPixel(button.Width-1,button.Height-1).ToArgb()==host.BackColor.ToArgb(),"rounded corners preserve parent background");
            }
            Event(button,"OnMouseEnter",EventArgs.Empty);
            Event(button,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,30,20,0));
            using(Bitmap image=new Bitmap(button.Width,button.Height)) {button.DrawToBitmap(image,button.ClientRectangle);Assert(image.GetPixel(0,0).ToArgb()==host.BackColor.ToArgb(),"pressed/hover corners remain clean");}
            host.Hide();
        }
        int[] intervals={0,2,5,30,15};
        using(AppIntervalsDialog dialog=new AppIntervalsDialog(intervals,30,false)) {
            dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-32000,-32000);dialog.ShowInTaskbar=false;dialog.Show();
            NumericUpDown[] inputs=(NumericUpDown[])typeof(AppIntervalsDialog).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(dialog);
            Assert(inputs[0].Minimum==0 && inputs[0].Maximum==3600 && inputs[4].AccessibleName.Contains("Illustrator"),"app interval bounds and accessible names");
            inputs[0].Value=1;dialog.AcceptButton.PerformClick();
            Assert(dialog.DialogResult==DialogResult.OK && dialog.Values[0]==1 && dialog.Values[4]==15,"Use intervals accepts independent app values");
            Assert(intervals[0]==0,"editing intervals does not mutate applied settings");
        }
        using(AppIntervalsDialog dialog=new AppIntervalsDialog(intervals,30,true)) {
            dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-32000,-32000);dialog.ShowInTaskbar=false;dialog.Show();dialog.CancelButton.PerformClick();
            Assert(dialog.DialogResult==DialogResult.Cancel,"Cancel dismisses interval changes");
        }
        // Form.Close disposes the window; Program's using block disposes it again.
        using(HealthSettingsDialog dialog=new HealthSettingsDialog(true,120)) {
            dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-32000,-32000);dialog.ShowInTaskbar=false;dialog.Show();
            NumericUpDown delay=(NumericUpDown)typeof(HealthSettingsDialog).GetField("Delay",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(dialog);
            Assert(delay.Minimum==15 && delay.Maximum==3600 && delay.Value==120,"warning dialog bounds and default delay");
            delay.Value=60;dialog.AcceptButton.PerformClick();Assert(dialog.DialogResult==DialogResult.OK && dialog.WarningSeconds==60 && dialog.WarningsEnabled,"warning dialog accepts settings");
        }
        using(MainWindow warningWindow=new MainWindow(new Preferences {AfterChanges=true,HealthWarningSeconds=15},true,false)) {
            ProgressState pending=new ProgressState();pending.PendingSince[1]=DateTime.UtcNow.AddSeconds(-20);
            typeof(MainWindow).GetField("Latest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(warningWindow,pending);
            MethodInfo refresh=typeof(MainWindow).GetMethod("RefreshStatus",BindingFlags.Instance|BindingFlags.NonPublic);refresh.Invoke(warningWindow,null);
            Label status=(Label)typeof(MainWindow).GetField("Status",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(warningWindow);
            Label[] apps=(Label[])typeof(MainWindow).GetField("AppStatus",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(warningWindow);
            Assert(status.Text=="Saving needs attention" && apps[1].Text.Contains("Unsaved changes need attention"),"warning highlights overview and affected app");
            pending.PendingSince[1]=null;refresh.Invoke(warningWindow,null);Assert(status.Text=="Checking for changes" && !apps[1].Text.Contains("need attention"),"recovery restores normal overview status");
        }
        Store.DirectoryPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"LocalSave-exit-tests-"+Guid.NewGuid().ToString("N"));
        Preferences disabled=new Preferences {Word=false,Excel=false,PowerPoint=false};
        MainWindow exitWindow=new MainWindow(disabled,false,false,false);
        exitWindow.Dispose();
        exitWindow.Dispose();
        Assert(exitWindow.IsDisposed,"repeated shutdown cleanup is safe");
        using(MainWindow trayWindow=new MainWindow(disabled,false,false,false)) {
            trayWindow.StartPosition=FormStartPosition.Manual;trayWindow.Location=new Point(-32000,-32000);trayWindow.ShowInTaskbar=false;
            trayWindow.Show();trayWindow.Hide();
            NotifyIcon tray=(NotifyIcon)typeof(MainWindow).GetField("Tray",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(trayWindow);
            Icon normal=tray.Icon;
            trayWindow.UpdateTrayStatus(true);
            Icon warning=tray.Icon;
            Assert(!Object.ReferenceEquals(normal,warning) && tray.Text.Contains("needs attention"),"overdue save changes the tray icon and tooltip");
            trayWindow.UpdateTrayStatus(false);
            Assert(Object.ReferenceEquals(tray.Icon,normal),"save recovery restores the normal tray icon");
            typeof(MainWindow).GetField("Paused",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(trayWindow,true);
            trayWindow.UpdateTrayStatus(true);
            Assert(Object.ReferenceEquals(tray.Icon,normal) && tray.Text.Contains("paused"),"pause suppresses the warning tray icon");
            typeof(MainWindow).GetField("Paused",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(trayWindow,false);
            trayWindow.UpdateTrayStatus(true);
            ToolStripMenuItem exitItem=(ToolStripMenuItem)tray.ContextMenuStrip.Items[tray.ContextMenuStrip.Items.Count-1];
            exitItem.PerformClick();
            Assert(trayWindow.IsDisposed,"tray Exit closes a hidden window");
            trayWindow.Dispose();
            Assert(trayWindow.IsDisposed,"tray Exit followed by using-block cleanup succeeds");
            Assert(typeof(MainWindow).GetField("WarningTrayIcon",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(trayWindow)==null,"warning icon is released safely during repeated exit cleanup");
        }
        Console.WriteLine("All "+Count+" UI checks passed.");return 0;
    }
}
