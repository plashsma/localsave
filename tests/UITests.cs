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
        Console.WriteLine("All "+Count+" UI checks passed.");return 0;
    }
}
