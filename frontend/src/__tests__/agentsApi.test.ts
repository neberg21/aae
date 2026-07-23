import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  ApiError,
  getAgent,
  getAgents,
  getThread,
  getThreads,
  searchAgents,
  sendLeoMessage,
} from '../modules/agents/api'

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
    expect(url).toBe('/ai-api/agents/search?name=Leo')
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
    expect(fetchMock).toHaveBeenCalledWith('/ai-api/agents')
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
    expect(fetch).toHaveBeenCalledWith('/ai-api/agents/leo')
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

  it('getThreads calls the threads list endpoint', async () => {
    const body = {
      items: [
        {
          threadId: 'thread-1',
          createdAt: '2026-07-22T14:01:17.8984086Z',
          updatedAt: '2026-07-22T14:01:17.8984086Z',
          messageCount: 1,
        },
      ],
      totalCount: 1,
      pageSize: 1,
      pageNumber: 1,
      totalPages: 1,
    }
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => body,
      }),
    )

    await expect(getThreads()).resolves.toEqual(body)
    expect(fetch).toHaveBeenCalledWith('/ai-api/threads')
  })

  it('getThread calls the thread detail endpoint with encoding', async () => {
    const body = {
      threadId: 'thread/1',
      messages: [
        {
          sender: 'leo',
          receiver: 'helga',
          content: 'Hello',
          createdAt: '2026-07-22T14:01:17.8984086Z',
        },
      ],
    }
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => body,
      }),
    )

    await expect(getThread('thread/1')).resolves.toEqual(body)
    expect(fetch).toHaveBeenCalledWith('/ai-api/threads/thread%2F1')
  })

  it('sendLeoMessage routes via default backend and resolves Leo reply from thread messages', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          threadId: 'thread-123',
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          threadId: 'thread-123',
          messages: [
            {
              sender: 'helga',
              receiver: 'leo',
              content: 'Hi Leo',
              createdAt: '2026-07-23T08:00:00Z',
            },
            {
              sender: 'leo',
              receiver: 'helga',
              content: 'Hello from Leo',
              createdAt: '2026-07-23T08:00:01Z',
            },
          ],
        }),
      })
    vi.stubGlobal('fetch', fetchMock)

    const reply = await sendLeoMessage(
      'Hi Leo',
      [{ role: 'assistant', content: 'How can I help?' }],
      'thread-123',
    )

    expect(reply).toBe('Hello from Leo')
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/ai-api/agents/actions/route-chat-message', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        threadId: 'thread-123',
        senderAgentId: 'helga',
        targetAgentId: 'leo',
        content: 'Hi Leo',
      }),
    })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/ai-api/threads/thread-123')
  })
})
