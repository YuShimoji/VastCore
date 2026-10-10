import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { inspect } from './check.mjs';

function fixture(t, nested = false) {
  const repo = fs.mkdtempSync(path.join(os.tmpdir(), 'codex-unity-check-'));
  t.after(() => fs.rmSync(repo, { recursive: true, force: true }));
  assert.equal(spawnSync('git', ['init', '--quiet', repo]).status, 0);
  const root = nested ? path.join(repo, '99PercentSlops') : repo;
  const write = (name, value) => {
    const destination = path.join(root, name);
    fs.mkdirSync(path.dirname(destination), { recursive: true });
    fs.writeFileSync(destination, value);
  };
  write('ProjectSettings/ProjectVersion.txt', 'm_EditorVersion: 6000.3.6f1\n');
  write('Packages/manifest.json', '{"dependencies":{"example":"1.0.0"}}');
  write('Packages/packages-lock.json', '{"dependencies":{"example":{"version":"1.0.0"}}}');
  write('Assets/example.cs', '// fixture\n');
  write('Assets/example.cs.meta', 'guid: 11111111111111111111111111111111\n');
  return { repo, write };
}

for (const nested of [false, true]) test(`supports ${nested ? 'nested' : 'root'} project and untracked edits`, t => {
  const { repo } = fixture(t, nested);
  assert.deepEqual(inspect(repo).errors, []);
  assert.equal(inspect(repo).sourceFiles, 1);
});
test('rejects absent project', t => {
  const { repo } = fixture(t);
  fs.unlinkSync(path.join(repo, 'ProjectSettings/ProjectVersion.txt'));
  assert.match(inspect(repo).errors.join('\n'), /exactly one/);
});
test('rejects invalid package JSON and missing lock entries', t => {
  const { repo, write } = fixture(t);
  write('Packages/packages-lock.json', '{"dependencies":{}}');
  assert.match(inspect(repo).errors.join('\n'), /missing example/);
  write('Packages/manifest.json', '{');
  assert.match(inspect(repo).errors.join('\n'), /manifest.json/);
});
test('rejects missing, zero and duplicate GUIDs', t => {
  const { repo, write } = fixture(t);
  write('Assets/example.cs.meta', 'guid: 00000000000000000000000000000000\n');
  assert.match(inspect(repo).errors.join('\n'), /Invalid GUID/);
  write('Assets/example.cs.meta', 'guid: 11111111111111111111111111111111\n');
  write('Assets/second.cs', '// fixture\n');
  write('Assets/second.cs.meta', 'guid: 11111111111111111111111111111111\n');
  assert.match(inspect(repo).errors.join('\n'), /Duplicate GUID/);
});
test('rejects orphan metadata and missing companion metadata', t => {
  const { repo, write } = fixture(t);
  write('Assets/orphan.cs.meta', 'guid: 22222222222222222222222222222222\n');
  write('Assets/second.cs', '// fixture\n');
  assert.match(inspect(repo).errors.join('\n'), /Orphan file meta/);
  assert.match(inspect(repo).errors.join('\n'), /Missing companion meta/);
});
test('requires materialized LFS assets only for Editor readiness', t => {
  const { repo, write } = fixture(t);
  write('Assets/image.png', 'version https://git-lfs.github.com/spec/v1\noid sha256:fixture\nsize 10\n');
  assert.deepEqual(inspect(repo).errors, []);
  assert.equal(inspect(repo).lfsPointers, 1);
  assert.match(inspect(repo, { requireAssets: true }).errors.join('\n'), /LFS pointers/);
});
