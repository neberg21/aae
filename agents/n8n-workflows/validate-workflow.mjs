import fs from 'node:fs';

const path = process.argv[2];
const expected = process.argv[3];
const wf = JSON.parse(fs.readFileSync(path, 'utf8'));
const hasWebhook = wf.nodes.some(
  (n) => n.type === 'n8n-nodes-base.webhook' && n.parameters?.path === expected,
);
if (!hasWebhook) {
  console.error('missing webhook', expected);
  process.exit(1);
}
if (wf.nodes.some((n) => /wait/i.test(n.type) && !/webhook/i.test(n.type))) {
  console.error('wait node found');
  process.exit(2);
}
console.log(expected, 'ok', wf.nodes.length, 'nodes');
