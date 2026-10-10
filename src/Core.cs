using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;

[Serializable] public class Preferences {
    public int Interval = 30;
    public int WordInterval, ExcelInterval, PowerPointInterval, PhotoshopInterval, IllustratorInterval;
    public int AppInterval(int index) {
        int[] values={WordInterval,ExcelInterval,PowerPointInterval,PhotoshopInterval,IllustratorInterval};
        return values[index]==0?Interval:values[index];
    }
    public bool AfterChanges;
    public bool SaveHealthWarnings = true;
    public int HealthWarningSeconds = 120;
    public bool Word = true;
    public bool Excel = true;
    public bool PowerPoint = true;
    public bool Photoshop;
    public bool Illustrator;
    public bool LimitFolder;
    public string Folder = "";
    public Preferences Copy() { return (Preferences)MemberwiseClone(); }
    public void Validate() {
        if (Interval < 1 || Interval > 3600) throw new InvalidDataException("Choose an interval between 1 and 3600 seconds.");
        if(HealthWarningSeconds<15 || HealthWarningSeconds>3600) throw new InvalidDataException("Choose a warning delay between 15 and 3600 seconds.");
        foreach(int value in new int[]{WordInterval,ExcelInterval,PowerPointInterval,PhotoshopInterval,IllustratorInterval})
            if(value<0 || value>3600) throw new InvalidDataException("App intervals must be 0 (use default) or between 1 and 3600 seconds.");
        Folder = Folder ?? "";
        if (LimitFolder && (!Policy.LocalPath(Folder) || !Directory.Exists(Folder) || Policy.HasLink(Folder))) throw new InvalidDataException("Choose an existing local folder without a junction or symbolic link.");
    }
}

internal static class Store {
    internal static string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalOfficeAutoSave");
    internal static string SettingsPath { get { return Path.Combine(DirectoryPath,"settings.xml"); } }
    internal static string LogPath { get { return Path.Combine(DirectoryPath,"activity-v2.log"); } }
    static object Gate = new object();
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "LocalOfficeAutoSave";
    internal static void CheckStorage() {
        if (Policy.HasLink(DirectoryPath)) throw new IOException("The settings folder contains a junction or symbolic link. Use a normal local profile folder.");
        Directory.CreateDirectory(DirectoryPath);
    }
    internal static void CheckFile(string path) {
        CheckStorage();
        if (Policy.HasLink(path)) throw new IOException("A settings or log path is redirected. Operation stopped.");
    }
    internal static Preferences Load() {
        CheckFile(SettingsPath);
        Preferences p;
        if (File.Exists(SettingsPath)) {
            if (new FileInfo(SettingsPath).Length > 65536) throw new InvalidDataException("The settings file is too large.");
            using (XmlReader r = XmlReader.Create(SettingsPath,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=65536})) p = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(r);
        } else {
            p = new Preferences();
            string legacy = Path.Combine(DirectoryPath,"interval.txt"); CheckFile(legacy);
            int interval;
            if (File.Exists(legacy) && new FileInfo(legacy).Length < 32 && int.TryParse(File.ReadAllText(legacy),out interval) && interval >= 1 && interval <= 3600) p.Interval = interval;
            legacy = Path.Combine(DirectoryPath,"mode.txt"); CheckFile(legacy);
            if (File.Exists(legacy) && new FileInfo(legacy).Length < 32) p.AfterChanges = File.ReadAllText(legacy).Trim() == "changes";
        }
        p.Validate(); return p;
    }
    internal static void Save(Preferences p) {
        p.Validate();
        lock(Gate) {
            CheckFile(SettingsPath);
            string temporary = SettingsPath + ".tmp"; CheckFile(temporary);
            using (FileStream stream = new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)) {new XmlSerializer(typeof(Preferences)).Serialize(stream,p); stream.Flush(true);}
            if (File.Exists(SettingsPath)) File.Replace(temporary,SettingsPath,null); else File.Move(temporary,SettingsPath);
        }
    }
    internal static bool StartupEnabled() { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue(RunName) != null; }
    internal static bool StartupHere(string exe) { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && String.Equals(k.GetValue(RunName) as string,"\""+exe+"\" --background",StringComparison.OrdinalIgnoreCase); }
    internal static bool RefreshStartup(string exe,Func<bool> enabled=null,Func<string,bool> here=null,Action<bool,string> write=null) {
        if(enabled==null) enabled=StartupEnabled;if(here==null) here=StartupHere;if(write==null) write=Startup;
        // Keep the existing user's choice; only relocate an already enabled entry.
        if(!enabled() || here(exe)) return false;
        write(true,exe);return true;
    }
    internal static void Startup(bool enabled,string exe) {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey)) {
            if (enabled) k.SetValue(RunName,"\""+exe+"\" --background"); else k.DeleteValue(RunName,false);
        }
    }
    internal static bool Log(string text) {
        lock(Gate) try {
            CheckFile(LogPath); string old = LogPath+".previous"; CheckFile(old);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 1048576) {if (File.Exists(old)) File.Delete(old); File.Move(LogPath,old);}
            File.AppendAllText(LogPath,DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz")+"  "+text+Environment.NewLine);
            return true;
        } catch { return false; }
    }
    internal static void Remove() {
        Startup(false,"");
        lock(Gate) {
            CheckStorage();
            foreach (string name in new string[] {"settings.xml","settings.xml.tmp","interval.txt","mode.txt","activity.log","activity.log.previous","activity-v2.log","activity-v2.log.previous"}) {
                string path = Path.Combine(DirectoryPath,name); CheckFile(path); if (File.Exists(path)) File.Delete(path);
            }
            // Delete only known application files; never recurse through a user's directory.
            if (Directory.GetFileSystemEntries(DirectoryPath).Length == 0) Directory.Delete(DirectoryPath);
        }
    }
}

internal static class Policy {
    internal static bool LocalPath(string path) {
        if (String.IsNullOrWhiteSpace(path) || path.Length < 3 || !Char.IsLetter(path[0]) || path[1] != ':' || path[2] != '\\') return false;
        try {if (path.IndexOf(':',2) >= 0) return false; DriveInfo d = new DriveInfo(path.Substring(0,3)); return d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable);} catch {return false;}
    }
    internal static bool Within(string path,string folder) {
        try {string root=Path.GetFullPath(folder).TrimEnd('\\')+"\\"; return Path.GetFullPath(path).StartsWith(root,StringComparison.OrdinalIgnoreCase);} catch {return false;}
    }
    internal static bool HasLink(string path) {
        try {
            string full=Path.GetFullPath(path);
            while (!String.IsNullOrEmpty(full)) {
                if ((File.Exists(full) || Directory.Exists(full)) && (File.GetAttributes(full)&FileAttributes.ReparsePoint) != 0) return true;
                string parent=Path.GetDirectoryName(full); if (parent==full) break; full=parent;
            }
            return false;
        } catch {return true;}
    }
    internal static bool Allows(string path,Preferences p) {
        return LocalPath(path) && (!p.LimitFolder || Within(path,p.Folder)) && !HasLink(path);
    }
}

internal sealed class ProgressState {
    internal long Saves;
    internal DateTime? LastSave;
    internal string[] Apps = {"Waiting for Word","Waiting for Excel","Waiting for PowerPoint","Photoshop disabled","Illustrator disabled"};
    internal bool Busy;
    internal bool LogUnavailable;
    internal DateTime?[] PendingSince=new DateTime?[5];
    internal ProgressState Copy() {ProgressState p=(ProgressState)MemberwiseClone();p.Apps=(string[])Apps.Clone();p.PendingSince=(DateTime?[])PendingSince.Clone();return p;}
}

internal sealed class OfficeOperationException : Exception {
    internal readonly string Operation;
    internal OfficeOperationException(string operation,Exception inner):base(operation,inner) {Operation=operation;HResult=inner.GetBaseException().HResult;}
}

internal sealed class HealthReport {
    internal readonly bool[] Warnings=new bool[5];
    internal readonly List<string> Notify=new List<string>();
}
internal sealed class SaveHealth {
    readonly bool[] Notified=new bool[5];
    DateTime? LastNotification;
    internal static int Threshold(Preferences p,int index) {return Math.Max(p.HealthWarningSeconds,p.AfterChanges?0:p.AppInterval(index)+30);}
    internal HealthReport Evaluate(ProgressState state,Preferences p,bool paused,DateTime utcNow) {
        HealthReport report=new HealthReport();
        bool[] enabled={p.Word,p.Excel,p.PowerPoint,p.Photoshop,p.Illustrator};
        string[] names={"Word","Excel","PowerPoint","Photoshop","Illustrator"};
        bool canNotify=!LastNotification.HasValue || (utcNow-LastNotification.Value).TotalSeconds>=60;
        for(int i=0;i<5;i++) {
            if(!state.PendingSince[i].HasValue) {Notified[i]=false;continue;}
            report.Warnings[i]=p.SaveHealthWarnings && enabled[i] && !paused && (utcNow-state.PendingSince[i].Value).TotalSeconds>=Threshold(p,i);
            if(report.Warnings[i] && !Notified[i] && canNotify) {report.Notify.Add(names[i]);Notified[i]=true;}
        }
        if(report.Notify.Count>0) LastNotification=utcNow;
        return report;
    }
}

internal sealed class SaveWorker {
    readonly object Gate=new object();
    Preferences Options;
    ProgressState State=new ProgressState();
    readonly AutoResetEvent Wake=new AutoResetEvent(false);
    Thread Worker;
    volatile bool Paused,StopRequested;
    bool Requested,ResetSchedule;
    internal event Action<ProgressState> Changed;
    readonly Dictionary<int,int> Failures=new Dictionary<int,int>();
    readonly Dictionary<int,DateTime> Retry=new Dictionary<int,DateTime>();
    readonly string[] LastSuccessful={"","","","",""};
    readonly string[] LoggedStatus={"","","","",""};
    readonly Func<int,List<object>> ApplicationProvider;
    readonly Dictionary<string,DateTime>[] Pending={new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase),new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase),new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase),new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase),new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase)};
    void Observe(int index,string path,bool dirty) {
        bool changed;
        lock(Gate) {
            DateTime? previous=State.PendingSince[index];
            if(dirty) {if(!Pending[index].ContainsKey(path)) Pending[index][path]=DateTime.UtcNow;}
            else Pending[index].Remove(path);
            UpdatePending(index);
            changed=previous!=State.PendingSince[index];
        }
        if(changed) Publish(); // Notify before a potentially blocking Save call, without flooding idle UI updates.
    }
    void UpdatePending(int index) {
        DateTime? oldest=null;
        foreach(DateTime time in Pending[index].Values) if(!oldest.HasValue || time<oldest.Value) oldest=time;
        State.PendingSince[index]=oldest;
    }
    void ClearPending(int index) {lock(Gate) {Pending[index].Clear();UpdatePending(index);}}
    readonly int AppIndex;
    internal SaveWorker(Preferences p,Func<int,List<object>> applicationProvider=null,int appIndex=-1) {Options=p.Copy();ApplicationProvider=applicationProvider;AppIndex=appIndex;}
    internal void Start() {Worker=new Thread(Loop);Worker.IsBackground=true;Worker.Name="LocalSave app worker "+AppIndex;Worker.SetApartmentState(ApartmentState.STA);Worker.Start();}
    internal void Update(Preferences p) {lock(Gate) {Options=p.Copy();ResetSchedule=true;} Wake.Set();}
    internal void Pause(bool pause) {Paused=pause;Wake.Set();}
    internal void SaveNow() {lock(Gate) Requested=true;Wake.Set();}
    internal void Stop() {StopRequested=true;Wake.Set();}
    internal bool IsBusy {get {lock(Gate) return State.Busy;}}
    internal bool IsStopped {get {return Worker==null || !Worker.IsAlive;}}
    internal static int Delay(Preferences p) {return p.AfterChanges?250:p.Interval*1000;}
    internal static int Delay(Preferences p,int index) {return p.AfterChanges?250:p.AppInterval(index)*1000;}
    void Publish() {
        ProgressState p;lock(Gate) p=State.Copy();
        string[] names={"Word","Excel","PowerPoint","Photoshop","Illustrator"};
        for(int i=0;i<names.Length;i++) {
            if(AppIndex>=0 && i!=AppIndex) continue;
            string status=p.Apps[i];int savedAt=status.IndexOf("\nLast saved",StringComparison.Ordinal);if(savedAt>=0) status=status.Substring(0,savedAt);
            if(status!=LoggedStatus[i]) {LoggedStatus[i]=status;Log(names[i]+": "+status.Replace("\n","; "));}
        }
        Action<ProgressState> handler=Changed;if(handler!=null) handler(p);
    }
    void Log(string message) {if (!Store.Log(message)) {lock(Gate) State.LogUnavailable=true;}}
    void Loop() {
        DateTime due=DateTime.UtcNow;
        try {
            while (!StopRequested) {
                Preferences p; bool requested;
                lock(Gate) {p=Options.Copy();requested=Requested;Requested=false;if (ResetSchedule) {due=DateTime.UtcNow;ResetSchedule=false;}}
                if (requested || (!Paused && DateTime.UtcNow>=due)) {
                    Scan(p,requested); due=DateTime.UtcNow.AddMilliseconds(AppIndex<0?Delay(p):Delay(p,AppIndex));
                }
                int wait=Paused?250:Math.Max(1,Math.Min(250,(int)(due-DateTime.UtcNow).TotalMilliseconds));
                Wake.WaitOne(wait);
            }
        } catch(Exception ex) {Log("Worker "+AppIndex+" stopped: "+Code(ex)+". Restart LocalSave.");lock(Gate) {State.Busy=false;for(int i=0;i<State.Apps.Length;i++) if(AppIndex<0 || i==AppIndex) State.Apps[i]="Worker stopped; restart app";}Publish();}
    }
    internal void Scan(Preferences p,bool manual) {
        lock(Gate) State.Busy=true;
        Publish();
        try {
            string[] names={"Word","Excel","PowerPoint","Photoshop","Illustrator"};string[] collections={"Documents","Workbooks","Presentations","Documents","Documents"};bool[] enabled={p.Word,p.Excel,p.PowerPoint,p.Photoshop,p.Illustrator};
            for(int index=0;index<names.Length && !StopRequested && (manual || !Paused);index++) {
                if(AppIndex>=0 && index!=AppIndex) continue;
                if(!enabled[index]) {ClearPending(index);lock(Gate) State.Apps[index]="Disabled";continue;}
                if(Retry.ContainsKey(index) && DateTime.UtcNow<Retry[index] && !manual) continue;
                object app=null;ExcelConnection excel=null;
                try {
                    List<object> applications=new List<object>();
                    if(ApplicationProvider!=null) {
                        applications.AddRange(ApplicationProvider(index));
                        if(applications.Count==0) {ClearPending(index);lock(Gate) State.Apps[index]="Not running or not available";continue;}
                    } else if(index==1) {
                        excel=ExcelConnection.Open();applications.AddRange(excel.Applications);
                        if(applications.Count==0) {
                            if(excel.WindowsFound==0 && excel.ErrorCode==0) ClearPending(index);
                            lock(Gate) State.Apps[index]=excel.WindowsFound>0?"Excel found; waiting for access\nFinish the cell edit or close any dialog":"Not running or not available";
                            if(excel.ErrorCode!=0) throw new COMException("Excel connection unavailable",excel.ErrorCode);
                            continue;
                        }
                    } else {
                        try {app=Marshal.GetActiveObject(names[index]+".Application");}
                        catch(COMException ex) {if(ex.ErrorCode==unchecked((int)0x800401E3)) {ClearPending(index);lock(Gate) State.Apps[index]=index>=3?"Open desktop app; automation unavailable":"Not running or not available";continue;}throw;}
                        applications.Add(app);
                    }
                    int eligible=0,skipped=0,saved=0,total=0;Exception failure=null;
                    HashSet<string> reasons=new HashSet<string>();
                    HashSet<string> observed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach(object application in applications) {
                        if(StopRequested || (!manual && Paused)) break;
                        object files=null;
                        try {
                            files=Required(application,collections[index]);int count=Convert.ToInt32(Required(files,"Count"));total+=count;
                            for(int i=1;i<=count && !StopRequested && (manual || !Paused);i++) {
                                object file=null;
                                try {
                                    file=Item(files,i);string reason;int result=SaveDocument(file,names[index],p,out reason,delegate(string path,bool dirty) {observed.Add(path);Observe(index,path,dirty);});
                                    if(result<0) {skipped++;reasons.Add(reason);} else {eligible++;if(result==1) {saved++;LastSuccessful[index]=DateTime.Now.ToString("h:mm:ss tt");lock(Gate) {State.Saves++;State.LastSave=DateTime.Now;}}}
                                } catch(Exception ex) {if(failure==null) failure=ex;}
                                finally {Release(file);}
                            }
                        } catch(Exception ex) {if(failure==null) failure=ex;}
                        finally {Release(files);}
                    }
                    lock(Gate) State.Apps[index]=total==0?"No open documents":eligible+" eligible"+(skipped>0?" / "+skipped+" skipped":"")+(reasons.Count>0?"\n"+String.Join(", ",reasons):LastSuccessful[index]!=""?"\nLast saved "+LastSuccessful[index]:"\nWaiting for unsaved changes");
                    if(saved>0) Log(names[index]+": saved "+saved+" file(s).");
                    if(failure!=null) throw failure;
                    if(!StopRequested && (manual || !Paused)) lock(Gate) {
                        foreach(string path in new List<string>(Pending[index].Keys)) if(!observed.Contains(path)) Pending[index].Remove(path);
                        UpdatePending(index);
                    }
                    Failures[index]=0;Retry.Remove(index);
                } catch(Exception ex) {
                    int failures=Failures.ContainsKey(index)?Failures[index]+1:1;Failures[index]=Math.Min(failures,6);
                    bool editing=index==1 && IsExcelBusy(ex);
                    int seconds=editing?1:Math.Min(30,1<<Math.Min(failures-1,5));Retry[index]=DateTime.UtcNow.AddSeconds(seconds);
                    lock(Gate) State.Apps[index]=editing?"Excel is editing or busy\nPress Enter in the cell; retrying automatically":"Retry in "+seconds+"s ("+Code(ex)+")\nCheck app dialogs and activity log";
                    if(failures==1 || failures==6) Log(names[index]+": "+Operation(ex)+" deferred/failed "+Code(ex)+"; retrying. Save manually if needed.");
                } finally {if(excel!=null) excel.Dispose();Release(app);}
            }
        } finally {lock(Gate) State.Busy=false;Publish();}
    }
    internal static object Get(object target,string name) {return target.GetType().InvokeMember(name,BindingFlags.GetProperty,null,target,null);}
    static object Required(object target,string name) {try {return Get(target,name);} catch(Exception ex) {throw new OfficeOperationException("read "+name,ex);}}
    internal static object Item(object target,int index) {return target.GetType().InvokeMember("Item",BindingFlags.GetProperty|BindingFlags.InvokeMethod,null,target,new object[]{index});}
    internal static int SaveDocument(object file,string app,Preferences p) {
        string reason;return SaveDocument(file,app,p,out reason);
    }
    internal static int SaveDocument(object file,string app,Preferences p,out string reason,Action<string,bool> observe=null) {
        reason="";
        if(app=="Photoshop" || app=="Illustrator") return SaveAdobeDocument(file,app,p,out reason,observe);
        string directory=Convert.ToString(Required(file,"Path"));
        if(String.IsNullOrWhiteSpace(directory)) {reason="Save new file once first";return -1;}
        if(!Policy.LocalPath(directory)) {reason="Not a local file";return -1;}
        if(Convert.ToBoolean(Required(file,"ReadOnly"))) {reason="Read-only file";return -1;}
        if(app=="Excel" && Convert.ToBoolean(Required(file,"IsAddin"))) {reason="Excel add-in";return -1;}
        string path=Convert.ToString(Required(file,"FullName"));
        if(p.LimitFolder && !Policy.Within(path,p.Folder)) {reason="Outside selected folder";return -1;}
        if(!Policy.Allows(path,p)) {reason="Nonlocal or redirected path";return -1;}
        // Cloud AutoSave already owns these files; do not compete with it.
        try {if(Convert.ToBoolean(Get(file,"AutoSaveOn"))) {reason="Office AutoSave already on";return -1;}} catch(MissingMethodException) {} catch(COMException) {} catch(TargetInvocationException) {}
        if(Convert.ToBoolean(Required(file,"Saved"))) {if(observe!=null) observe(path,false);return 0;}
        if(observe!=null) observe(path,true);
        try {file.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,file,null);} catch(Exception ex) {throw new OfficeOperationException("save file",ex);}
        if(!Convert.ToBoolean(Required(file,"Saved"))) throw new InvalidOperationException("Office did not confirm the save.");
        if(observe!=null) observe(path,false);
        return 1;
    }
    internal static int SaveAdobeDocument(object file,string app,Preferences p,out string reason,Action<string,bool> observe=null) {
        reason="";
        // Adobe's document model has FullName and Saved, not Office's Path/ReadOnly.
        string path;
        try {path=Convert.ToString(Required(file,"FullName"));}
        catch(Exception ex) {
            int code=ex.GetBaseException().HResult;
            if(code==unchecked((int)0x80010001) || code==unchecked((int)0x8001010A)) throw;
            reason="Save locally once first; path unavailable";return -1;
        }
        if(!Policy.LocalPath(path)) {reason="Save locally once first";return -1;}
        if(p.LimitFolder && !Policy.Within(path,p.Folder)) {reason="Outside selected folder";return -1;}
        if(!Policy.Allows(path,p)) {reason="Nonlocal or redirected path";return -1;}
        string extension=System.IO.Path.GetExtension(path);
        bool supported=app=="Photoshop"?(extension.Equals(".psd",StringComparison.OrdinalIgnoreCase) || extension.Equals(".psb",StringComparison.OrdinalIgnoreCase)):extension.Equals(".ai",StringComparison.OrdinalIgnoreCase);
        if(!supported) {reason=app=="Photoshop"?"Use a local PSD or PSB file":"Use a local AI file";return -1;}
        if(!File.Exists(path)) {reason="Save local file once first";return -1;}
        if((File.GetAttributes(path)&FileAttributes.ReadOnly)!=0) {reason="Read-only file";return -1;}
        if(Convert.ToBoolean(Required(file,"Saved"))) {if(observe!=null) observe(path,false);return 0;}
        if(observe!=null) observe(path,true);
        // Preserve document format/options. Never SaveAs, export, close, or suppress dialogs.
        try {file.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,file,null);} catch(Exception ex) {throw new OfficeOperationException("save file",ex);}
        if(!Convert.ToBoolean(Required(file,"Saved"))) throw new InvalidOperationException("Adobe app did not confirm the save.");
        if(observe!=null) observe(path,false);
        return 1;
    }
    internal static bool IsExcelBusy(Exception ex) {int code=ex.GetBaseException().HResult;return code==unchecked((int)0x80010001) || code==unchecked((int)0x8001010A) || code==unchecked((int)0x800AC472);}
    static void Release(object o) {if(o!=null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o);}
    static string Code(Exception ex) {return "0x"+ex.GetBaseException().HResult.ToString("X8");}
    internal static string Operation(Exception ex) {for(Exception current=ex;current!=null;current=current.InnerException) {OfficeOperationException operation=current as OfficeOperationException;if(operation!=null) return operation.Operation;}return "Office operation";}
}

// Each app owns its COM objects and schedule on a separate STA thread.
internal sealed class SaveCoordinator {
    readonly object Gate=new object();
    readonly SaveWorker[] Workers=new SaveWorker[5];
    readonly ProgressState[] States=new ProgressState[5];
    internal event Action<ProgressState> Changed;
    internal SaveCoordinator(Preferences p,Func<int,List<object>> provider=null) {
        for(int i=0;i<Workers.Length;i++) {
            int index=i;States[i]=new ProgressState();
            Workers[i]=new SaveWorker(p,provider,index);
            Workers[i].Changed+=delegate(ProgressState state) {Receive(index,state);};
        }
    }
    void Receive(int index,ProgressState state) {
        lock(Gate) {
            States[index]=state;
            ProgressState combined=new ProgressState();
            for(int i=0;i<States.Length;i++) {
                ProgressState app=States[i];combined.Apps[i]=app.Apps[i];
                combined.PendingSince[i]=app.PendingSince[i];
                combined.Saves+=app.Saves;combined.Busy|=app.Busy;combined.LogUnavailable|=app.LogUnavailable;
                if(app.LastSave.HasValue && (!combined.LastSave.HasValue || app.LastSave.Value>combined.LastSave.Value)) combined.LastSave=app.LastSave;
            }
            // Serialize notifications so the UI cannot receive an older total after a newer one.
            Action<ProgressState> handler=Changed;if(handler!=null) handler(combined);
        }
    }
    internal void Start() {foreach(SaveWorker worker in Workers) worker.Start();}
    internal void Update(Preferences p) {foreach(SaveWorker worker in Workers) worker.Update(p);}
    internal void Pause(bool paused) {foreach(SaveWorker worker in Workers) worker.Pause(paused);}
    internal void SaveNow() {foreach(SaveWorker worker in Workers) worker.SaveNow();}
    internal void Stop() {foreach(SaveWorker worker in Workers) worker.Stop();}
    internal bool IsBusy {get {foreach(SaveWorker worker in Workers) if(worker.IsBusy) return true;return false;}}
    internal bool IsStopped {get {foreach(SaveWorker worker in Workers) if(!worker.IsStopped) return false;return true;}}
}
