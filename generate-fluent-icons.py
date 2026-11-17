"""Keep original Microsoft SVG viewboxes and convert paths to frozen WPF geometries."""
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parent
icons={}
for svg in sorted((root/'app/Icons').glob('*.svg')):
    doc=ET.parse(svg).getroot()
    assert doc.attrib.get('viewBox')=='0 0 24 24',svg
    paths=[]
    for path in doc.iter('{http://www.w3.org/2000/svg}path'):
        paths.append(('F0 ' if path.attrib.get('fill-rule')=='evenodd' else 'F1 ')+path.attrib['d'])
    assert paths,svg
    icons[svg.stem]=paths
source='''// Microsoft Fluent UI System Icons, 24 Regular.
// Copyright (c) 2020 Microsoft Corporation. MIT license: Icons/LICENSE.
// Generated from original SVG paths; shapes and viewboxes are unmodified.
using System.Collections.Generic;
using System.Windows.Media;
namespace Ada {
 public static class FluentIcons {
  static readonly Dictionary<string,Geometry[]> Cache=new Dictionary<string,Geometry[]>();
  static readonly Dictionary<string,string[]> Paths=new Dictionary<string,string[]> {
'''
for name,paths in icons.items():
    source+='   {"'+name+'",new[]{'+','.join('"'+p+'"' for p in paths)+'}},\n'
source+='''  };
  public static Geometry[] Get(string name){Geometry[] geometries;if(Cache.TryGetValue(name,out geometries))return geometries;var paths=Paths.ContainsKey(name)?Paths[name]:Paths["settings"];geometries=new Geometry[paths.Length];for(int i=0;i<paths.Length;i++){geometries[i]=Geometry.Parse(paths[i]);geometries[i].Freeze();}Cache[name]=geometries;return geometries;}
 }
}
'''
(root/'app/FluentIcons.cs').write_text(source,encoding='utf-8')
p=root/'app/Design.cs';s=p.read_text(encoding='utf-8');a=s.index('  FrameworkElement Glyph(');b=s.index('  object ButtonContent(',a)
s=s[:a]+'''  FrameworkElement Glyph(string name,double size=20,string color="Text"){
   var canvas=new Canvas{Width=24,Height=24,IsHitTestVisible=false};foreach(var geometry in FluentIcons.Get(name)){var path=new System.Windows.Shapes.Path{Data=geometry,IsHitTestVisible=false};path.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,color);canvas.Children.Add(path);}return new Viewbox{Width=size,Height=size,Child=canvas,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
  }
'''+s[b:];p.write_text(s,encoding='utf-8')
if 'chevron' in icons:
    p=root/'app/Styles.xaml';s=p.read_text(encoding='utf-8');old="<Path Data='M1 1 L5 5 L9 1' Stroke='{DynamicResource Muted}' StrokeThickness='1.5' Width='10' Height='6' HorizontalAlignment='Right' VerticalAlignment='Center'/>"
    new="<Viewbox Width='14' Height='14' HorizontalAlignment='Right' VerticalAlignment='Center'><Canvas Width='24' Height='24'>"+''.join("<Path Data='"+data+"' Fill='{DynamicResource Muted}'/>" for data in icons['chevron'])+"</Canvas></Viewbox>"
    s=s.replace(old,new);p.write_text(s,encoding='utf-8')
print(f'{len(icons)} official Fluent SVG icons converted; original 24x24 viewboxes preserved.')
