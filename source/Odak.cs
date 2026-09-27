using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

public class Todo { public string Text = ""; public bool Done; }
public class State {
 public string Day = DateTime.Today.ToString("yyyy-MM-dd");
 public List<Todo> Tasks = new List<Todo>();
 public bool Running; public double Elapsed, TodaySeconds; public int TimerVersion;
 public bool Welcome = false; public int WidgetVersion; public bool Positioned, Locked; public double WidgetLeft, WidgetTop;
 public bool Rollover(DateTime now) { if(Day == now.ToString("yyyy-MM-dd")) return false; Day=now.ToString("yyyy-MM-dd"); Tasks.Clear(); TodaySeconds=0; return true; }
 public void Reset(){Running=false;Elapsed=0;}
 public static string Duration(double seconds){var t=TimeSpan.FromSeconds(Math.Floor(Math.Max(0,seconds)));return ((long)t.TotalHours).ToString("00")+":"+t.Minutes.ToString("00")+":"+t.Seconds.ToString("00");}
 public bool Add(string s) { if(Tasks.Count >=3 || String.IsNullOrWhiteSpace(s)) return false; Tasks.Add(new Todo { Text=s.Trim() }); return true; }
 public void Move(int i,int d) { int j=i+d; if(j<0||j>=Tasks.Count) return; Todo t=Tasks[i];Tasks[i]=Tasks[j];Tasks[j]=t; }
 public void Advance(double seconds,DateTime now) { bool changed=Rollover(now); if(!Running)return;seconds=Math.Max(0,seconds);Elapsed+=seconds;TodaySeconds+=changed?Math.Min(seconds,now.TimeOfDay.TotalSeconds):seconds; }

}
public static class Program {
 public static bool Test; public static bool Measure; public static string Root;
 [STAThread] public static void Main(string[] args) {
  Measure=args.Contains("--measure"); Test=args.Contains("--self-test"); Root=Test?Path.Combine(Path.GetTempPath(),"Odak-Test-"+Guid.NewGuid()):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Odak");
  Directory.CreateDirectory(Root);
  if(Test) { Tests(); return; }
  bool first; using(var mutex=new Mutex(true,"Local\\Odak.FocusDashboard",out first)) { if(!first)return;
   RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
   var app=new Application(); app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show("İşlem tamamlanamadı. "+e.Exception.Message,"Odak");e.Handled=true;}; app.Run(new Dashboard(args.Contains("--preview")||Measure)); }
 }
 static void Check(bool b,string s) { if(!b)throw new Exception(s); }
 static void Tests() {
  var s=new State(); Check(s.Add("a")&&s.Add("b")&&s.Add("c")&&!s.Add("d"),"three limit"); s.Move(0,1);Check(s.Tasks[1].Text=="a","reorder");
  var now=new DateTime(2026,9,25,12,0,0);s.Day=now.ToString("yyyy-MM-dd");s.Running=true;s.Advance(65,now);Check(s.Elapsed==65&&s.TodaySeconds==65,"count up");s.Running=false;s.Advance(20,now);Check(s.Elapsed==65,"pause");s.Running=true;s.Advance(10,now);Check(s.Elapsed==75&&s.TodaySeconds==75,"resume");s.Reset();Check(!s.Running&&s.Elapsed==0&&s.TodaySeconds==75,"reset preserves daily total");
  s.Running=true;s.Advance(3661,now);Check(State.Duration(s.Elapsed)=="01:01:01","hour format");Check(State.Duration(90061)=="25:01:01","hours do not wrap");
  s.Advance(10,now.Date.AddDays(1).AddSeconds(3));Check(s.Tasks.Count==0&&s.TodaySeconds==3&&s.Elapsed==3671&&s.Running,"midnight split");
  var json=new JavaScriptSerializer();var copy=json.Deserialize<State>(json.Serialize(s));Check(copy.Elapsed==3671&&copy.TodaySeconds==3,"persist");var legacy=json.Deserialize<State>("{\"Running\":true,\"Remaining\":1500,\"Completed\":3,\"Tasks\":[{\"Text\":\"koru\",\"Done\":true}]}");Check(legacy.Elapsed==0&&legacy.Tasks.Count==1,"legacy migration");
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),"PASS: 3-task limit, reorder, stopwatch count up, pause/resume, reset preserves daily total, hour formatting, midnight split, persistence, legacy migration.\r\n");
 }
}
public class Dashboard:Window {
 State state; JavaScriptSerializer json=new JavaScriptSerializer(); string path; bool savingError, exiting, preview;
 Brush ink=B("#E9EDE7"), muted=B("#8D9991"), accent=B("#CFEB9C");
 StackPanel todoPanel; TextBlock clock,phase,count,taskCount,date; ProgressBar taskBar; Button start; Border welcome; Grid shell;
 Window focusWindow; bool positionReady; Stopwatch timeSource=Stopwatch.StartNew(); double lastSample, lastCheckpoint;
 DispatcherTimer timer=new DispatcherTimer(); Forms.NotifyIcon tray;
 public static Brush B(string color) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
 TextBlock Txt(string text,double size,Brush brush=null) { return new TextBlock{Text=text,FontSize=size,Foreground=brush??ink,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)}; }
 Button Btn(string text,Action action,bool primary=false) { var b=new Button{Content=text,Foreground=primary?B("#182014"):ink,Background=primary?accent:B("#26302B"),BorderThickness=new Thickness(0),Padding=new Thickness(16,10,16,10),Margin=new Thickness(0,0,8,0),Cursor=System.Windows.Input.Cursors.Hand}; b.Click+=(s,e)=>action();return b; }
 Border Card(StackPanel p) { return new Border{Background=B("#171E1A"),BorderBrush=B("#2A352E"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(18),Padding=new Thickness(26),Margin=new Thickness(8),Child=p}; }
 public Dashboard(bool p) {
  preview=p; path=Path.Combine(Program.Root,"state.json");try{state=File.Exists(path)?json.Deserialize<State>(File.ReadAllText(path)):new State();}catch{state=new State();savingError=true;}state.Running=false;state.TimerVersion=1;state.Rollover(DateTime.Now);
  if(state.WidgetVersion<1){state.Welcome=false;state.WidgetVersion=1;}
  Title="Odak Widget"; Width=358;Height=480;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Foreground=ink;FontFamily=new FontFamily("Segoe UI");WindowStartupLocation=WindowStartupLocation.Manual;Topmost=false;
  var area=SystemParameters.WorkArea;Height=Math.Min(480,area.Height-24);Left=state.Positioned?state.WidgetLeft:area.Right-Width-20;Top=state.Positioned?state.WidgetTop:area.Top+12;KeepOnScreen();
  SourceInitialized+=(s,e)=>{var handle=new WindowInteropHelper(this).Handle;SetWindowLong(handle,-20,GetWindowLong(handle,-20)|0x80);};
  Deactivated+=(s,e)=>{if(focusWindow==null)Lower();};
  LocationChanged+=(s,e)=>{if(positionReady&&IsVisible){state.WidgetLeft=Left;state.WidgetTop=Top;state.Positioned=true;}};
  Build(); SetupTray();
  timer.Interval=TimeSpan.FromSeconds(1);timer.Tick+=(s,e)=>Tick();timer.Start();
  Closing+=(s,e)=>{if(!exiting){e.Cancel=true;Hide();}else{Sample();state.Running=false;timer.Stop();tray.Dispose();if(focusWindow!=null)focusWindow.Close();Save();}};
  StateChanged+=(s,e)=>{if(WindowState==WindowState.Minimized)Hide();};
  KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape && welcome.Visibility==Visibility.Visible)Dismiss(false);};
  Loaded+=(s,e)=>{positionReady=true;if(preview){if(Program.Measure)Measure();else Capture();}else{Lower();if(state.Welcome)ShowFocus();}if(savingError)MessageBox.Show("Kaydedilen veriler okunamadı; yeni oturum açıldı. Eski dosya ilk kayıtta .bak olarak korunur.","Odak");};
 }
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
 [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd,int index);
 [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd,int index,int value);
 void Lower(){var h=new WindowInteropHelper(this).Handle;if(h!=IntPtr.Zero)SetWindowPos(h,new IntPtr(1),0,0,0,0,0x0013);}
 void KeepOnScreen(){var a=SystemParameters.WorkArea;Left=Math.Max(a.Left,Math.Min(Left,a.Right-Width));Top=Math.Max(a.Top,Math.Min(Top,a.Bottom-Height));}
 void DragWidget(object sender,System.Windows.Input.MouseButtonEventArgs e){if(state.Locked||e.ChangedButton!=System.Windows.Input.MouseButton.Left)return;try{DragMove();KeepOnScreen();Save();}catch(InvalidOperationException){} }
 Border Widget(StackPanel p){var b=Card(p);b.Padding=new Thickness(20,16,20,14);b.Margin=new Thickness(0,0,0,10);return b;}
 TextBlock Grip(string label){var t=Txt(label,11,accent);t.Cursor=System.Windows.Input.Cursors.SizeAll;t.ToolTip="Widget’ı taşımak için sürükle · sağ tık: seçenekler";t.MouseLeftButtonDown+=DragWidget;return t;}
 void OpenWidgets(){timer.Interval=TimeSpan.FromSeconds(1);Show();WindowState=WindowState.Normal;KeepOnScreen();Lower();}
 void GameMode(){timer.Interval=TimeSpan.FromSeconds(15);Hide();}
 void Build() {
  shell=new Grid();Content=shell;var fit=new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top};shell.Children.Add(fit);var stack=new StackPanel{Width=358};fit.Child=stack;
  var menu=new ContextMenu();Action<string,Action> add=(label,action)=>{var item=new MenuItem{Header=label};item.Click+=(s,e)=>action();menu.Items.Add(item);};
  add("Odak ekranı",ShowFocus);add("Ayarlar",Settings);add("Konumu kilitle / kilidi aç",()=>{state.Locked=!state.Locked;Save();});add("Oyun modu / gizle",GameMode);add("Çıkış",()=>{exiting=true;Close();});stack.ContextMenu=menu;
  var pom=new StackPanel();var head=new DockPanel();var options=Btn("···",()=>{menu.PlacementTarget=stack;menu.IsOpen=true;});options.Padding=new Thickness(7,0,7,0);options.Margin=new Thickness(0);DockPanel.SetDock(options,Dock.Right);head.Children.Add(options);head.Children.Add(Grip("◉  O D A K                 /  01"));pom.Children.Add(head);
  date=Txt("",11,muted);pom.Children.Add(date);phase=Txt("Kronometre",16);phase.Margin=new Thickness(0,3,0,0);pom.Children.Add(phase);clock=Txt("00:00:00",52);clock.FontWeight=FontWeights.SemiBold;clock.Margin=new Thickness(0,0,0,8);pom.Children.Add(clock);
  var controls=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,14)};start=Btn("Başlat",Toggle,true);controls.Children.Add(start);controls.Children.Add(Btn("Sıfırla",()=>{Sample();state.Reset();Save();Refresh();}));pom.Children.Add(controls);count=Txt("",12,muted);count.Margin=new Thickness(0);pom.Children.Add(count);stack.Children.Add(Widget(pom));
  var todos=new StackPanel();todos.Children.Add(Grip("02 / BUGÜNÜN ÜÇ ADIMI"));taskCount=Txt("0/3 tamamlandı",18);todos.Children.Add(taskCount);taskBar=Bar();todos.Children.Add(taskBar);todoPanel=new StackPanel{MinHeight=100};todos.Children.Add(todoPanel);stack.Children.Add(Widget(todos));
  welcome=new Border{Visibility=Visibility.Collapsed};Refresh();RenderTasks();
 }
 void ShowFocus(){if(focusWindow!=null){focusWindow.Activate();return;}var w=new Window{Title="Odak",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=B("#0E1511"),Foreground=ink,WindowState=WindowState.Maximized,ShowInTaskbar=false,Topmost=false};focusWindow=w;var hero=new StackPanel{Width=720,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};hero.Children.Add(Txt("O D A K  /  "+DateTime.Now.ToString("dd MMMM yyyy, dddd",CultureInfo.GetCultureInfo("tr-TR")),15,accent));var h=Txt("Bilgisayar açıldı.\nBaşka yere bakma.\nŞimdi ders zamanı.",52);h.FontWeight=FontWeights.SemiBold;hero.Children.Add(h);hero.Children.Add(Txt("Küçük adımlar, büyük sonuçlar.",19,muted));hero.Children.Add(Btn("Kronometreyi başlat →",()=>{if(!state.Running)Toggle();w.Close();},true));hero.Children.Add(Txt("ESC ile widget’a dön",12,muted));w.Content=new Viewbox{Child=hero,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Margin=new Thickness(40)};w.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)w.Close();};w.Closed+=(s,e)=>{focusWindow=null;};w.Show();}
 ProgressBar Bar(){return new ProgressBar{Maximum=100,Height=5,Foreground=accent,Background=B("#2B352F"),BorderThickness=new Thickness(0),Margin=new Thickness(0,0,0,12)};}
 void Dismiss(bool run){welcome.Visibility=Visibility.Collapsed;WindowState=WindowState.Normal;if(run&&!state.Running)Toggle();}
 void Sample(){double now=timeSource.Elapsed.TotalSeconds;string oldDay=state.Day;state.Advance(now-lastSample,DateTime.Now);lastSample=now;if(oldDay!=state.Day&&todoPanel!=null)RenderTasks();}
 void Toggle(){Sample();state.Running=!state.Running;Save();Refresh();}
 void Refresh(){date.Text=DateTime.Now.ToString("dd MMMM yyyy, dddd",CultureInfo.GetCultureInfo("tr-TR"));clock.Text=State.Duration(state.Elapsed);phase.Text=state.Running?"Kronometre • çalışıyor":state.Elapsed>0?"Kronometre • duraklatıldı":"Kronometre";start.Content=state.Running?"Duraklat":state.Elapsed>0?"Devam et":"Başlat";count.Text="Bugünkü çalışma: "+State.Duration(state.TodaySeconds);taskCount.Text=state.Tasks.Count(x=>x.Done)+"/3 tamamlandı";taskBar.Value=state.Tasks.Count(x=>x.Done)*100.0/3;}
 void RenderTasks(){todoPanel.Children.Clear();for(int n=0;n<state.Tasks.Count;n++){int i=n;var row=new DockPanel{Margin=new Thickness(0,0,0,6)};var buttons=new StackPanel{Orientation=Orientation.Horizontal};var up=Btn("↑",()=>{state.Move(i,-1);Save();RenderTasks();});up.Padding=new Thickness(6,2,6,2);up.IsEnabled=i>0;buttons.Children.Add(up);var down=Btn("↓",()=>{state.Move(i,1);Save();RenderTasks();});down.Padding=new Thickness(6,2,6,2);down.IsEnabled=i<state.Tasks.Count-1;buttons.Children.Add(down);var del=Btn("×",()=>{state.Tasks.RemoveAt(i);Save();RenderTasks();Refresh();});del.Padding=new Thickness(6,2,6,2);buttons.Children.Add(del);DockPanel.SetDock(buttons,Dock.Right);row.Children.Add(buttons);var check=new CheckBox{Content=new TextBlock{Text=state.Tasks[i].Text,TextWrapping=TextWrapping.NoWrap,TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=state.Tasks[i].Text,MaxWidth=170},Foreground=ink,IsChecked=state.Tasks[i].Done,VerticalAlignment=VerticalAlignment.Center};check.Click+=(s,e)=>{state.Tasks[i].Done=check.IsChecked==true;Save();Refresh();};row.Children.Add(check);todoPanel.Children.Add(row);}if(state.Tasks.Count<3){var entry=new TextBox{Background=B("#202B24"),Foreground=ink,BorderThickness=new Thickness(0),Padding=new Thickness(10),MaxLength=100,ToolTip="Görevi yaz, Enter’a bas",Text=""};var add=Btn("+",()=>{if(state.Add(entry.Text)){Save();RenderTasks();Refresh();}},true);var r=new DockPanel();DockPanel.SetDock(add,Dock.Right);r.Children.Add(add);r.Children.Add(entry);entry.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter&&state.Add(entry.Text)){Save();RenderTasks();Refresh();}};todoPanel.Children.Add(r);}}
 void Save(){if(preview)return;try{string temp=path+".tmp";File.WriteAllText(temp,json.Serialize(state),Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}catch{if(!savingError){savingError=true;MessageBox.Show("Yerel kayıt başarısız; disk izinlerini kontrol et.","Odak");}}}
 void Tick(){if(!preview&&File.Exists(Path.Combine(Program.Root,"exit-request"))){File.Delete(Path.Combine(Program.Root,"exit-request"));exiting=true;Close();return;}string day=state.Day;Sample();if(day!=state.Day||(state.Running&&lastSample-lastCheckpoint>=30)){Save();lastCheckpoint=lastSample;}if(IsVisible)Refresh();}
 void SetupTray(){tray=new Forms.NotifyIcon{Icon=System.Drawing.SystemIcons.Application,Text="Odak • Widget",Visible=true};var menu=new Forms.ContextMenuStrip();menu.Items.Add("Widget’ı göster",null,(s,e)=>Dispatcher.Invoke(new Action(OpenWidgets)));menu.Items.Add("Odak ekranı",null,(s,e)=>Dispatcher.Invoke(new Action(ShowFocus)));menu.Items.Add("Ayarlar",null,(s,e)=>Dispatcher.Invoke(new Action(Settings)));menu.Items.Add("Oyun modu / gizle",null,(s,e)=>Dispatcher.Invoke(new Action(GameMode)));menu.Items.Add("Konumu sıfırla",null,(s,e)=>Dispatcher.Invoke(new Action(()=>{var a=SystemParameters.WorkArea;Left=a.Right-Width-20;Top=a.Top+12;KeepOnScreen();Save();OpenWidgets();})));menu.Items.Add("Çıkış",null,(s,e)=>Dispatcher.Invoke(new Action(()=>{exiting=true;Close();})));tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(new Action(OpenWidgets));}
 void Settings(){var w=new Window{Title="Odak • Ayarlar",Width=440,Height=330,Background=B("#111A14"),Foreground=ink,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterScreen};var p=new StackPanel{Margin=new Thickness(26)};w.Content=p;p.Children.Add(Txt("Widget ayarları",24));
  var welcomeCheck=new CheckBox{Content="Açılışta tam ekran Odak kartı",Foreground=ink,IsChecked=state.Welcome,Margin=new Thickness(0,15,0,12)};p.Children.Add(welcomeCheck);
  var auto=new CheckBox{Content="Windows oturum açılışında başlat",Foreground=ink,IsChecked=File.Exists(StartupPath()),Margin=new Thickness(0,0,0,16)};p.Children.Add(auto);
  p.Children.Add(Txt("Başlıklardan sürükle; sağ tık veya ··· ile menüyü aç. Gün değişince görevler ve günlük toplam sıfırlanır.",12,muted));
  p.Children.Add(Btn("Kaydet",()=>{state.Welcome=welcomeCheck.IsChecked==true;try{SetStartup(auto.IsChecked==true);}catch(Exception ex){MessageBox.Show(w,"Otomatik başlatma ayarlanamadı: "+ex.Message);}Save();w.Close();},true));w.ShowDialog();}
 string StartupPath(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"Odak.vbs");}
 void SetStartup(bool yes){string f=StartupPath();if(yes)File.WriteAllText(f,"CreateObject(\"WScript.Shell\").Run Chr(34) & \""+System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("\"","\"\"")+"\" & Chr(34), 1, False",Encoding.Unicode);else if(File.Exists(f))File.Delete(f);}
 void Measure(){var proc=Process.GetCurrentProcess();var initial=proc.TotalProcessorTime;var sw=Stopwatch.StartNew();var t=new DispatcherTimer{Interval=TimeSpan.FromSeconds(10)};t.Tick+=(s,e)=>{t.Stop();proc.Refresh();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"resource-check.txt"),"Idle working set: "+(proc.WorkingSet64/1048576.0).ToString("F1",CultureInfo.InvariantCulture)+" MB\r\nPrivate memory: "+(proc.PrivateMemorySize64/1048576.0).ToString("F1",CultureInfo.InvariantCulture)+" MB\r\nCPU (one core): "+((proc.TotalProcessorTime-initial).TotalMilliseconds/sw.Elapsed.TotalMilliseconds*100).ToString("F2",CultureInfo.InvariantCulture)+"% over 10 seconds. Not a game benchmark.\r\n");exiting=true;Close();};t.Start();}
 void Capture(){Dispatcher.BeginInvoke(new Action(()=>{UpdateLayout();var bmp=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(this);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png")))png.Save(f);exiting=true;Close();}),DispatcherPriority.ApplicationIdle);}
}





