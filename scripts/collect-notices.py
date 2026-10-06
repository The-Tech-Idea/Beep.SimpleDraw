"""Capture reviewed, exact notice inputs. Offline builds only verify these copies.

Use an absent output directory; changing inputs requires explicit review and a
new capture. This command never downloads or edits package/SDK/sibling inputs.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parent.parent
sha = lambda b: hashlib.sha256(b).hexdigest()


def collect(packages, sdk, capture, output, audit_path):
    if output.exists():
        raise ValueError('Notice capture output must be absent.')
    audit = json.loads(audit_path.read_text())
    pins = json.loads((ROOT / 'dependencies/nuget-archives.lock.json').read_text())
    pinned = {p['id'].lower() + '/' + p['version']: p for p in pins['packages']}
    upstream = json.loads((capture / 'sources.json').read_text(encoding='utf-8-sig'))
    blobs, groups = {}, []

    def add(data, origin):
        digest = sha(data)
        record = blobs.setdefault(digest, {'path': 'texts/' + digest + '.txt', 'sha256': digest,
                                          'bytes': len(data), 'origins': [], '_bytes': data})
        if origin not in record['origins']:
            record['origins'].append(origin)
        return record['path']

    def local(file, origin):
        return add(file.read_bytes(), origin)

    runtime = sdk / 'packs/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.12'
    runtime_files = [local(runtime / name, {'kind': 'sdk-pack', 'pack': 'Microsoft.NETCore.App.Runtime.Mono.browser-wasm',
                     'version': '10.0.12', 'path': name}) for name in ['LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT']]
    groups.append({'id': 'sdk:mono/10.0.12', 'title': '.NET browser runtime, Mono and ICU data',
                   'license': 'MIT and retained vendor third-party terms', 'files': runtime_files})
    emscripten = sdk / 'packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64/10.0.12/tools/emscripten'
    ems_names = ['LICENSE', 'AUTHORS', 'system/lib/compiler-rt/LICENSE.TXT', 'system/lib/libunwind/LICENSE.TXT',
                 'system/lib/libcxx/LICENSE.TXT', 'system/lib/libcxxabi/LICENSE.TXT', 'system/lib/libc/musl/COPYRIGHT']
    ems_files = [local(emscripten / name, {'kind': 'sdk-pack', 'pack': 'Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64',
                'version': '10.0.12', 'path': 'tools/emscripten/' + name}) for name in ems_names]
    groups.append({'id': 'sdk:emscripten/3.1.56', 'title': 'Emscripten and linked standard-library notices',
                   'license': 'Original MIT/NCSA, LLVM exceptions and musl notices', 'files': ems_files})
    owner = (capture / 'Beep-owner-MIT.txt').read_bytes()
    svg = (capture / 'SkiaSharp.Svg-LICENSE.txt').read_bytes()
    if sha(owner) != upstream['OwnerLicenseSha256'] or sha(svg) != upstream['SvgLicenseSha256']:
        raise ValueError('Captured upstream license hashes changed.')
    owner_file = add(owner, {'kind': 'upstream-source', 'url': 'https://github.com/The-Tech-Idea/BeepDM/blob/f305eaf64037392fa4b907aa69f64f5d641c69d4/LICENSE'})
    svg_file = add(svg, {'kind': 'upstream-source', 'url': 'https://github.com/mono/SkiaSharp.Extended/blob/' + upstream['SvgCommit'] + '/LICENSE'})
    for p in audit['packages']:
        if not p['webResolvedTargets']:
            continue
        key = p['id'].lower() + '/' + p['version']
        archive = packages / p['id'].lower() / p['version'] / (p['id'].lower() + '.' + p['version'] + '.nupkg')
        if sha(archive.read_bytes()) != pinned[key]['archiveSha256'] or p['archiveSha256'] != pinned[key]['archiveSha256']:
            raise ValueError('Archive/audit mismatch: ' + key)
        notices = []
        with zipfile.ZipFile(archive) as z:
            for n in p['embeddedNoticeFiles']:
                data = z.read(n['archivePath'])
                if sha(data) != n['sha256'] or len(data) != n['bytes']:
                    raise ValueError('Embedded notice mismatch: ' + key)
                notices.append(add(data, {'kind': 'package-archive', 'package': key,
                               'archiveSha256': p['archiveSha256'], 'member': n['archivePath']}))
        license = p['nuspec'].get('license', {}).get('text')
        if p['id'].startswith('TheTechIdea.Beep.'):
            if license != 'MIT' or p['nuspec']['authors']['text'] != 'The Tech Idea' or p['nuspec']['copyright']['text'] != '2022':
                raise ValueError('Owner MIT attribution changed: ' + key)
            notices.append(owner_file)
        elif p['id'] == 'SkiaSharp.Svg':
            notices.append(svg_file)
        elif license == 'MIT':
            # Expression-only packages still need the full permission text; retain
            # their own nuspec authors/copyright in the public aggregate and index.
            notices.append(runtime_files[0])
        if not notices:
            raise ValueError('Web input has no full license/notice material: ' + key)
        groups.append({'id': 'package:' + key, 'title': p['id'] + ' ' + p['version'],
                       'license': license or 'See original license text', 'authors': p['nuspec'].get('authors', {}).get('text'),
                       'copyright': p['nuspec'].get('copyright', {}).get('text'),
                       'archiveSha256': p['archiveSha256'], 'files': sorted(set(notices))})
    beep = local(ROOT.parent / 'Beep.Skia/LICENSE.txt', {'kind': 'beep-source', 'path': 'LICENSE.txt',
                 'baseCommit': '19bb026ba403ff91bd1a2ce324000ddce07cd26d'})
    groups.append({'id': 'source:beep-skia', 'title': 'Beep.Skia source and SimpleDraw integration changes',
                   'license': 'MIT as supplied in the source repository', 'files': [beep]})
    font = local(ROOT / 'src/Beep.SimpleDraw.Core/Fonts/OFL.txt', {'kind': 'font-source', 'path': 'Fonts/OFL.txt'})
    groups.append({'id': 'font:kalam', 'title': 'Kalam Regular — Indian Type Foundry, 2014',
                   'license': 'SIL Open Font License 1.1', 'files': [font]})
    manifest = {'version': 1, 'packageArchiveLockSha256': sha((ROOT / 'dependencies/nuget-archives.lock.json').read_bytes()),
                'webLockSha256': sha((ROOT / 'src/Beep.SimpleDraw.Web/packages.net10.lock.json').read_bytes()),
                'toolchainLockSha256': sha((ROOT / 'dependencies/toolchain.lock.json').read_bytes()),
                'fontSha256': sha((ROOT / 'src/Beep.SimpleDraw.Core/Fonts/Kalam-Regular.ttf').read_bytes()),
                'groups': groups, 'files': [{k: v for k, v in b.items() if k != '_bytes'} for b in blobs.values()],
                'scope': 'Full original notice materials for Web restore candidates plus linked SDK/native inputs. Runtime presence is determined separately from the actual published files.',
                'reviewNotes': ['Beep.Skia source MIT header retains upstream [year]/[fullname] placeholders; owner header review is open.',
                                'Vis.Modules exact nuspec declares MIT, The Tech Idea, copyright 2022. Shared owner MIT permission text is retained; a separate upstream license file was not available at its recorded source URL.',
                                'Vendor third-party collections are retained conservatively in full; no claim that each listed subcomponent survives native linking.']}
    (output / 'texts').mkdir(parents=True)
    for b in blobs.values():
        (output / b['path']).write_bytes(b['_bytes'])
    (output / 'inputs.lock.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps({'groups': len(groups), 'uniqueOriginalTexts': len(blobs), 'bytes': sum(b['bytes'] for b in blobs.values())}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--packages', required=True, type=Path)
    parser.add_argument('--sdk', required=True, type=Path)
    parser.add_argument('--capture', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--audit', type=Path, default=ROOT / 'docs/evidence/p6-package-license-audit.json')
    args = parser.parse_args()
    collect(args.packages, args.sdk, args.capture, args.output, args.audit)
