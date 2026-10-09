using System;
using System.Drawing;
using System.IO;
internal static class MakeIcon {
    static void Main(string[] args) {
        int[] sizes={16,24,32,48,64,128,256};byte[][] data=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++) using(Bitmap b=Brand.Logo(sizes[i])) using(MemoryStream stream=new MemoryStream()) {b.Save(stream,System.Drawing.Imaging.ImageFormat.Png);data[i]=stream.ToArray();}
        using(BinaryWriter w=new BinaryWriter(File.Create(args[0]))) {
            w.Write((short)0);w.Write((short)1);w.Write((short)sizes.Length);int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++) {w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((short)1);w.Write((short)32);w.Write(data[i].Length);w.Write(offset);offset+=data[i].Length;}
            foreach(byte[] bytes in data) w.Write(bytes);
        }
    }
}
