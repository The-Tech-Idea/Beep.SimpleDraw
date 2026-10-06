import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const root = new URL('../docs/evidence/', import.meta.url);
const read = name => fs.readFileSync(new URL(name, root), 'utf8');
const sha256 = name => crypto.createHash('sha256').update(fs.readFileSync(new URL(name, root))).digest('hex');
function counters(name, expected) {
  const trx = read(name);
  const tag = trx.match(/<Counters\s+[^>]+\/>/)?.[0];
  assert.ok(tag, `Missing counters: ${name}`);
  const counts = Object.fromEntries([...tag.matchAll(/(\w+)="(\d+)"/g)].map(m => [m[1], Number(m[2])]));
  for (const [key, value] of Object.entries(expected)) assert.equal(counts[key], value, `${name}: ${key}`);
  assert.equal(counts.executed, counts.total);
  assert.equal(counts.passed + counts.failed, counts.total);
  return counts;
}
const mask = counters('p6-text-mask-rejected.trx', {total: 6, passed: 0, failed: 6});
const clip = counters('p6-retained-clip-rejected.trx', {total: 12, passed: 2, failed: 10});
const restored = counters('p6-raster-trials-restored.trx', {total: 191, passed: 191, failed: 0});
const maskSamples = [...read('p6-text-mask-rejected.log').matchAll(/Changed channels=(\d+); max difference=(\d+); zoom=([\d.]+); stroke=(#[\da-f]+)/g)]
  .map(m => ({changedChannels: +m[1], maxDifference: +m[2], zoom: +m[3], stroke: m[4]}));
assert.equal(maskSamples.length, 6);
assert.deepEqual(maskSamples.map(s => s.changedChannels).sort((a,b) => a-b), [383,619,1001,1412,2052,2970]);
assert.ok(maskSamples.every(s => s.maxDifference === 2));
const clipFailures = [...read('p6-retained-clip-rejected.log').matchAll(/zoom=([\d.]+); grid=(True|False); isolated overlap=(True|False); step=(\d+); changed channels=(\d+); max=(\d+); device bounds=([^\r\n]+)/g)]
  .map(m => ({zoom: +m[1], grid: m[2] === 'True', isolatedOverlap: m[3] === 'True', step: +m[4], changedChannels: +m[5], maxDifference: +m[6], deviceBounds: m[7]}));
assert.equal(clipFailures.length, 10);
assert.ok(clipFailures.every(s => s.step === 0 && s.changedChannels > 0));
const summary = {date: '2026-10-06', scope: 'Native prototype pixels and rebuilt canonical managed suite; no browser timing or adoption',
  mask: {counters: mask, sourceSha256: sha256('p6-text-mask-rejected.cs'), samples: maskSamples},
  retainedClip: {counters: clip, sourceSha256: sha256('p6-retained-clip-rejected.cs'), firstFailureSamples: clipFailures},
  restoredCanonical: {counters: restored, trxSha256: sha256('p6-raster-trials-restored.trx')},
  decision: 'Neither prototype adopted; production source and published candidate unchanged; performance gate open'};
fs.writeFileSync(new URL('p6-raster-trials-summary.json', root), JSON.stringify(summary, null, 2) + '\n');
console.log(JSON.stringify(summary));
