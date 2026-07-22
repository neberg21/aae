import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, getAgent, getAgents, searchAgents, sendLeoMessage } from '../modules/agents/api'

describe('agents api', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('searchAgents sends only non-empty query params', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        items: [],
        totalCount: 0,
        pageSize: 0,
        pageNumber: 1,
        totalPages: 0,
      }),
    })
    vi.stubGlobal('fetch', fetchMock)

    await searchAgents({ name: 'Leo', department: '  ', jobTitle: undefined })

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const url = String(fetchMock.mock.calls[0][0])
    expect(url).toBe('/api/agents/search?name=Leo')
  })

  it('getAgents calls the list endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        items: [],
        totalCount: 0,
        pageSize: 0,
        pageNumber: 1,
        totalPages: 0,
      }),
    })
    vi.stubGlobal('fetch', fetchMock)

    await getAgents()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith('/api/agents')
  })

  it('getAgent returns detail on success', async () => {
    const body = {
      agentId: 'leo',
      name: 'Leo',
      department: 'Ops',
      jobTitle: 'Orchestrator',
      systemPrompt: 'You are Leo.',
    }
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => body,
      }),
    )

    const agent = await getAgent('leo')
    expect(agent).toEqual(body)
    expect(fetch).toHaveBeenCalledWith('/api/agents/leo')
  })

  it('getAgent throws ApiError with status on failure', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 404,
        statusText: 'Not Found',
      }),
    )

    await expect(getAgent('missing')).rejects.toMatchObject({
      name: 'ApiError',
      status: 404,
    })
    await expect(getAgent('missing')).rejects.toBeInstanceOf(ApiError)
  })

  it('sendLeoMessage posts to the leo webhook with thread id and history', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      headers: {
        get: () => 'application/json',
      },
      json: async () => ({
        reply: 'Hello from Leo',
      }),
    })
    vi.stubGlobal('fetch', fetchMock)

    const reply = await sendLeoMessage(
      'Hi Leo',
      [{ role: 'assistant', content: 'How can I help?' }],
      'thread-123',
    )

    expect(reply).toBe('Hello from Leo')
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith('/webhook/leo-think', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json, text/plain;q=0.9, */*;q=0.8',
      },
      body: JSON.stringify({
        agentId: 'leo',
        threadId: 'thread-123',
        message: 'Hi Leo',
        input: 'Hi Leo',
        text: 'Hi Leo',
        prompt: 'Hi Leo',
        chatInput: 'Hi Leo',
        history: [{ role: 'assistant', content: 'How can I help?' }],
        messages: [
          { role: 'assistant', content: 'How can I help?' },
          { role: 'user', content: 'Hi Leo' },
        ],
      }),
    })
  })

  it('sendLeoMessage falls back to plain text responses', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        headers: {
          get: () => 'text/plain',
        },
        text: async () => 'Plain text from Leo',
      }),
    )

    await expect(sendLeoMessage('Hi again')).resolves.toBe('Plain text from Leo')
  })
})
