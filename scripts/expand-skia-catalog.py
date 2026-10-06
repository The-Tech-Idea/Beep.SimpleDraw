"""Repository-local generation of fixed factories from the checked-in diagram projects."""
from pathlib import Path
import re, json
app = Path(__file__).resolve().parent.parent
skia = app.parent / 'Beep.Skia'
families = ['FlowChart','UML','ERD','MindMap','Network','Business','Cloud','DFD','ECAD','ETL','ML','PM','Security','StateMachine','Quantitative','WellLogs']
projects = ['Beep.Ski.Quantitative' if f == 'Quantitative' else 'Beep.Skia.'+f for f in families]
classes = []
for family, project in zip(families, projects):
    csproj = skia/project/(project+'.csproj')
    text = csproj.read_text(encoding='utf-8-sig')
    excluded = re.findall(r'<Compile Remove="([^"]+)"',text)
    # Keep ordinary targets; select net10 only for the explicitly requested portable graph.
    text = re.sub(r'<TargetFrameworks(?: Condition="[^"]*")?>[^<]+</TargetFrameworks>',lambda m: m.group(0) if 'Condition=' in m.group(0) else m.group(0).replace('<TargetFrameworks>', '<TargetFrameworks Condition="\'$(BeepSkiaPortableTargetFramework)\' != \'net10.0\'">'),text)
    if '== \'net10.0\'' not in text:
        text = text.replace('<PropertyGroup>','<PropertyGroup>\n    <TargetFrameworks Condition="\'$(BeepSkiaPortableTargetFramework)\' == \'net10.0\'">net10.0</TargetFrameworks>',1)
    text = text.replace('<Target Name="PostBuild"','<Target Condition="\'$(BeepSkiaLocalBuildCopies)\' != \'false\'" Name="PostBuild"').replace('<Target Name="CopyPackage"','<Target Condition="\'$(BeepSkiaLocalBuildCopies)\' != \'false\'" Name="CopyPackage"')
    csproj.write_text(text,encoding='utf-8')
    for file in sorted((skia/project).rglob('*.cs')):
        if any(p in ('bin','obj') for p in file.parts) or file.name in excluded: continue
        source = file.read_text(encoding='utf-8-sig')
        ns = re.search(r'namespace\s+([\w.]+)',source)
        if not ns: continue
        for match in re.finditer(r'public\s+(?:(abstract|sealed|partial)\s+)?class\s+(\w+)\s*:\s*([\w.]+)',source):
            modifier,name,base = match.groups()
            classes.append(dict(family=family,project=project,name=name,base=base.split('.')[-1],abstract=modifier=='abstract',type=ns[1]+'.'+name,file=str(file.relative_to(skia))))
bases = {'MaterialControl'}
for _ in range(8):
    bases.update(c['name'] for c in classes if c['base'] in bases)
entries = [c for c in classes if c['base'] in bases and not c['abstract'] and c['name'] != 'NetworkLink']
catalog = 'using Beep.Skia;\nnamespace Beep.SimpleDraw.Core.Library;\npublic static partial class SkiaCatalog\n{\n    public static IReadOnlyList<CatalogEntry> Entries { get; } = new CatalogEntry[]\n    {\n'
for c in entries:
    label = re.sub(r'(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])',' ',c['name'])
    catalog += f'        Entry<{c["type"]}>("{c["family"]}", "{label}", () => new {c["type"]}()),\n'
catalog += '    };\n}\n'
(app/'src/Beep.SimpleDraw.Core/Library/SkiaCatalog.Generated.cs').write_text(catalog,encoding='utf-8')
core = app/'src/Beep.SimpleDraw.Core/Beep.SimpleDraw.Core.csproj'
text = core.read_text(encoding='utf-8-sig')
for project in projects[1:]:
    include = '../../../Beep.Skia/'+project+'/'+project+'.csproj'
    if include not in text: text = text.replace('  </ItemGroup>',f'    <ProjectReference Include="{include}" AdditionalProperties="GeneratePackageOnBuild=false;SkipMinVer=true;BeepSkiaLocalBuildCopies=false" />\n  </ItemGroup>',1)
core.write_text(text,encoding='utf-8')
# Use the shared typeface cache in all family rendering; desktop default remains Segoe UI.
for project in projects:
    for file in (skia/project).rglob('*.cs'):
        if any(p in ('obj','bin') for p in file.parts): continue
        source = file.read_text(encoding='utf-8-sig')
        changed = source.replace('SKTypeface.Default','Beep.Skia.TypefaceCache.Default')
        if changed != source: file.write_text(changed,encoding='utf-8')
(app/'docs/evidence/default-skia-catalog-inventory.json').write_text(json.dumps(entries,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'families':len(families),'components':len(entries),'counts':{f:sum(c['family']==f for c in entries) for f in families}}))
