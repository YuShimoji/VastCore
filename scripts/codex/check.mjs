import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const repository = fileURLToPath(new URL('../../', import.meta.url));

// No Unity import or C# compilation is implied by this structural check.
export function inspect(repo, { requireAssets = false } = {}) {
  const errors = [];
  const candidates = ['.', '99PercentSlops'].filter(candidate =>
    fs.existsSync(path.join(repo, candidate, 'ProjectSettings/ProjectVersion.txt')));
  if (candidates.length !== 1) {
    return { errors: ['Expected exactly one Unity project at . or 99PercentSlops.'] };
  }
  const project = candidates[0];
  const prefix = project === '.' ? '' : `${project}/`;
  const read = relative => fs.readFileSync(path.join(repo, prefix, relative), 'utf8').replace(/^\uFEFF/, '');
  const json = relative => {
    try { return JSON.parse(read(relative)); }
    catch (error) { errors.push(`${relative}: ${error.message}`); return null; }
  };
  const version = read('ProjectSettings/ProjectVersion.txt').match(/^m_EditorVersion:\s*(\S+)/m)?.[1];
  if (!version) errors.push('ProjectVersion.txt has no m_EditorVersion.');
  const manifest = json('Packages/manifest.json');
  const lock = json('Packages/packages-lock.json');
  if (!manifest?.dependencies || !lock?.dependencies) errors.push('Package manifest/lock must have dependencies.');
  for (const name of Object.keys(manifest?.dependencies ?? {})) {
    if (!lock?.dependencies?.[name]) errors.push(`Package lock is missing ${name}.`);
  }
  const listed = spawnSync('git', ['ls-files', '--cached', '--others', '--exclude-standard', '-z'], {
    cwd: repo, encoding: 'utf8', maxBuffer: 16 * 1024 * 1024,
  });
  if (listed.status !== 0) return { errors: [...errors, listed.stderr || 'git ls-files failed.'] };
  const assets = [...new Set(listed.stdout.split('\0').filter(name => name.startsWith(`${prefix}Assets/`)))];
  const paths = new Map();
  const guids = new Map();
  let jsonFiles = 0;
  let metaFiles = 0;
  let sourceFiles = 0;
  const pointers = [];
  for (const name of assets) {
    const lower = name.toLowerCase();
    if (paths.has(lower) && paths.get(lower) !== name) errors.push(`Case collision: ${paths.get(lower)} / ${name}`);
    paths.set(lower, name);
    const absolute = path.join(repo, name);
    if (!fs.existsSync(absolute)) { errors.push(`Missing tracked asset: ${name}`); continue; }
    if (fs.statSync(absolute).isDirectory()) continue;
    const descriptor = fs.openSync(absolute, 'r');
    const header = Buffer.alloc(128);
    let bytes;
    try { bytes = fs.readSync(descriptor, header, 0, header.length, 0); }
    finally { fs.closeSync(descriptor); }
    if (header.subarray(0, bytes).toString().startsWith('version https://git-lfs.github.com/spec/v1')) pointers.push(name);
    if (/\.(asmdef|asmref|inputactions)$/.test(name)) {
      json(name.slice(prefix.length));
      jsonFiles++;
    }
    if (name.endsWith('.meta')) {
      const text = fs.readFileSync(absolute, 'utf8');
      const guid = text.match(/^guid:\s*([a-fA-F0-9]{32})\s*$/m)?.[1]?.toLowerCase();
      if (!guid || /^0+$/.test(guid)) errors.push(`Invalid GUID: ${name}`);
      else if (guids.has(guid)) errors.push(`Duplicate GUID: ${guids.get(guid)} / ${name}`);
      else guids.set(guid, name);
      if (!/^folderAsset:\s*yes\s*$/m.test(text) && !fs.existsSync(absolute.slice(0, -5))) errors.push(`Orphan file meta: ${name}`);
      metaFiles++;
    } else if (/\.(cs|unity|prefab|shader|compute|asmdef|asmref|inputactions)$/.test(name)) {
      if (!fs.existsSync(`${absolute}.meta`)) errors.push(`Missing companion meta: ${name}`);
      if (name.endsWith('.cs')) sourceFiles++;
    }
  }
  if (requireAssets && pointers.length) errors.push(`${pointers.length} LFS pointers need authenticated git lfs pull before Unity import.`);
  return { project, version, jsonFiles, metaFiles, sourceFiles, lfsPointers: pointers.length, errors };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const args = process.argv.slice(2);
  if (args.some(arg => arg !== '--require-assets')) {
    console.error('Usage: node scripts/codex/check.mjs [--require-assets]');
    process.exit(2);
  }
  try {
    const result = inspect(repository, { requireAssets: args.includes('--require-assets') });
    console.log(JSON.stringify(result, null, 2));
    console.log('Scope: structure only; Unity import, C# compilation, EditMode, PlayMode and visual acceptance are NOT RUN.');
    if (result.lfsPointers) console.log('LFS assets are pointers. Source editing is available; materialize assets before Editor work.');
    process.exitCode = result.errors.length ? 1 : 0;
  } catch (error) { console.error(error.message); process.exitCode = 1; }
}
