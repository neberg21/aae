import type {
  AgentDetail,
  AgentSearchFilters,
  AgentsPage,
  ChatMessage,
  ThreadDetail,
  ThreadsPage,
} from './types'

const n8nUrl = 'https://convenient-nonie-neberg-ad5744ad.koyeb.app/webhook'
const leoWebhookUrl = `${n8nUrl}/leo-think`

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, response.statusText || `HTTP ${response.status}`)
  }
  return (await response.json()) as T
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function extractLeoResponseText(payload: unknown): string | null {
  if (typeof payload === 'string') {
    const value = payload.trim()
    return value.length > 0 ? value : null
  }

  if (typeof payload === 'number' || typeof payload === 'boolean') {
    return String(payload)
  }

  if (Array.isArray(payload)) {
    const parts = payload
      .map((item) => extractLeoResponseText(item))
      .filter((item): item is string => Boolean(item))

    return parts.length > 0 ? parts.join('\n\n') : null
  }

  if (!isRecord(payload)) {
    return null
  }

  const preferredKeys = ['reply', 'response', 'message', 'output', 'text', 'content', 'answer']
  for (const key of preferredKeys) {
    const candidate = extractLeoResponseText(payload[key])
    if (candidate) {
      return candidate
    }
  }

  const nestedKeys = ['data', 'result', 'body']
  for (const key of nestedKeys) {
    const candidate = extractLeoResponseText(payload[key])
    if (candidate) {
      return candidate
    }
  }

  for (const value of Object.values(payload)) {
    const candidate = extractLeoResponseText(value)
    if (candidate) {
      return candidate
    }
  }

  return null
}

export async function getAgents(): Promise<AgentsPage> {
  const url = '/api/agents';
  const response = await fetch(url)
  return readJson<AgentsPage>(response);
}

export async function searchAgents(filters: AgentSearchFilters): Promise<AgentsPage> {
  const params = new URLSearchParams()
  if (filters.name?.trim()) params.set('name', filters.name.trim())
  if (filters.department?.trim()) params.set('department', filters.department.trim())
  if (filters.jobTitle?.trim()) params.set('jobTitle', filters.jobTitle.trim())

  const query = params.toString()
  const url = query ? `/api/agents/search?${query}` : '/api/agents/search'
  const response = await fetch(url)
  return readJson<AgentsPage>(response)
}

export async function getAgent(id: string): Promise<AgentDetail> {
  const response = await fetch(`/api/agents/${encodeURIComponent(id)}`)
  return readJson<AgentDetail>(response)
}

export async function getThreads(): Promise<ThreadsPage> {
  const response = await fetch('/api/agents/threads')
  return readJson<ThreadsPage>(response)
}

export async function getThread(threadId: string): Promise<ThreadDetail> {
  const response = await fetch(`/api/agents/threads/${encodeURIComponent(threadId)}`)
  return readJson<ThreadDetail>(response)
}

export async function sendLeoMessage(
  message: string,
  history: ChatMessage[] = [],
  threadId?: string,
): Promise<string> {
  const trimmedMessage = message.trim()
  if (!trimmedMessage) {
    throw new Error('Message is required')
  }

  const transcript = [...history, { role: 'user' as const, content: trimmedMessage }]
  const response = await fetch(leoWebhookUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json, text/plain;q=0.9, */*;q=0.8',
    },
    body: JSON.stringify({
      agentId: 'leo',
      threadId,
      message: trimmedMessage,
      input: trimmedMessage,
      text: trimmedMessage,
      prompt: trimmedMessage,
      chatInput: trimmedMessage,
      history,
      messages: transcript,
    }),
  })

  if (!response.ok) {
    throw new ApiError(response.status, response.statusText || `HTTP ${response.status}`)
  }

  const contentType = response.headers.get('content-type') ?? ''
  const payload = contentType.includes('application/json')
    ? ((await response.json()) as unknown)
    : await response.text()

  const reply = extractLeoResponseText(payload)
  if (!reply) {
    throw new Error('Leo returned an empty response')
  }

  return reply
}

