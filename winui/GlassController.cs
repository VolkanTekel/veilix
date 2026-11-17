using System;
namespace Veilix;
public sealed partial class MainWindow {
 UnifiedGlass unifiedGlass;bool glassCreationFailed;
 bool SharedGlass=>prefs.Material=="Cam"&&unifiedGlass!=null;
 void UpdateSharedGlass(){
  if(prefs.Material!="Cam"){unifiedGlass?.Dispose();unifiedGlass=null;glassCreationFailed=false;return;}
  bool wasVisible=unifiedGlass?.Visible??false;
  if(unifiedGlass==null&&!glassCreationFailed){try{unifiedGlass=new UnifiedGlass(host.Hwnd,leftShoulderMask,rightShoulderMask);unifiedGlass.SetBounds(width,surface.Height,VisibleHeight(),VisibleHeight(),host.Dpi,false,0);}catch(Exception ex){glassCreationFailed=true;Program.Trace("Unified glass failed: "+ex);}}
  if(unifiedGlass!=null&&!wasVisible){
   unifiedGlass.SetBounds(width,surface.Height,VisibleHeight(),VisibleHeight(),host.Dpi,false,0);
   if(hidden&&!resizing)unifiedGlass.SetNarrow(true,false);
   unifiedGlass.SetHandleRegion(HiddenHandleRegion(TargetScale));
   unifiedGlass.SetCurveOpacity((float)shoulderLayer.Opacity);
   unifiedGlass.SetBodyOpacity((float)tint.Opacity);
  }
  unifiedGlass?.SetVisible(ready);
 }
 void SyncGlassBounds(double extent,double from,double to,bool animate){if(SharedGlass)unifiedGlass.SetBounds(width,extent,from,to,host.Dpi,animate,resizeDuration);}
}
