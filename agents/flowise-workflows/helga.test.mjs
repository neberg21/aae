import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const flow = JSON.parse(readFileSync(join(__dirname, 'helga.json'), 'utf8'));

const SYSTEM_PLACEHOLDER = 'hol das aktuelle prompt aus dem repo';

describe('helga Flowise Agentflow V2', () => {
  it('has nodes and edges arrays', () => {
    assert.ok(Array.isArray(flow.nodes));
    assert.ok(Array.isArray(flow.edges));
    assert.equal(flow.nodes.length, 2);
    assert.equal(flow.edges.length, 1);
  });

  it('has Start then Agent nodes', () => {
    const start = flow.nodes.find((n) => n.data?.name === 'startAgentflow');
    const agent = flow.nodes.find((n) => n.data?.name === 'agentAgentflow');
    assert.ok(start, 'missing startAgentflow');
    assert.ok(agent, 'missing agentAgentflow');
    assert.equal(agent.data.label, 'Helga');
  });

  it('wires Start → Agent', () => {
    const edge = flow.edges[0];
    assert.equal(edge.source, 'startAgentflow_0');
    assert.equal(edge.target, 'agentAgentflow_0');
  });

  it('uses chatOpenAI with locked system placeholder and no tools', () => {
    const agent = flow.nodes.find((n) => n.data?.name === 'agentAgentflow');
    const inputs = agent.data.inputs;
    assert.equal(inputs.agentModel, 'chatOpenAI');
    assert.equal(inputs.agentEnableMemory, false);
    assert.ok(Array.isArray(inputs.agentMessages));
    const system = inputs.agentMessages.find((m) => m.role === 'system');
    assert.ok(system);
    assert.equal(system.content, SYSTEM_PLACEHOLDER);
    const tools = inputs.agentTools;
    assert.ok(tools === '' || tools === undefined || (Array.isArray(tools) && tools.length === 0));
  });

  it('does not embed API keys or credentials', () => {
    const raw = JSON.stringify(flow);
    assert.equal(/sk-[a-zA-Z0-9]/.test(raw), false);
    assert.equal(/api[_-]?key/i.test(raw) && /"[^"]*sk-/.test(raw), false);
    const agent = flow.nodes.find((n) => n.data?.name === 'agentAgentflow');
    assert.equal(agent.data.inputs.agentModelConfig?.credential, undefined);
  });
});
